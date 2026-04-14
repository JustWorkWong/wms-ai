# Troubleshooting Guide

## Common Issues and Solutions

### Database Connection Problems

#### Issue: "Cannot connect to PostgreSQL"

**Symptoms:**
```
Npgsql.NpgsqlException: Connection refused
```

**Solutions:**

1. **Check PostgreSQL is running**
   ```bash
   docker ps | grep postgres
   ```

2. **Verify connection string**
   ```bash
   # Check Aspire dashboard for actual connection details
   # Default: Host=localhost;Database=UserDb;Username=postgres;Password=postgres
   ```

3. **Check port conflicts**
   ```bash
   lsof -i :5432
   # If port is occupied, stop conflicting service or change port
   ```

4. **Restart PostgreSQL container**
   ```bash
   docker restart <postgres-container-id>
   ```

5. **Check firewall settings**
   ```bash
   # Ensure localhost connections are allowed
   ```

#### Issue: "Database does not exist"

**Symptoms:**
```
Npgsql.PostgresException: database "UserDb" does not exist
```

**Solutions:**

1. **Let Aspire create databases automatically**
   - Databases are created on first service startup
   - Check Aspire dashboard for initialization logs

2. **Manually create database**
   ```bash
   docker exec -it <postgres-container-id> psql -U postgres
   CREATE DATABASE "UserDb";
   CREATE DATABASE "BusinessDb";
   CREATE DATABASE "AiDb";
   \q
   ```

3. **Run database initializers**
   ```bash
   # Databases are initialized in Program.cs of each service
   # Platform: PlatformDatabaseInitializer.InitializeAsync()
   # Inbound: BusinessDatabaseInitializer.InitializeAsync()
   # AiGateway: AiGatewayDatabaseInitializer.InitializeAsync()
   ```

#### Issue: "Migration pending" or "Schema mismatch"

**Symptoms:**
```
The model backing the context has changed since the database was created
```

**Solutions:**

1. **Apply migrations**
   ```bash
   cd src/Platform/WmsAi.Platform.Infrastructure
   dotnet ef database update --context UserDbContext
   
   cd src/Inbound/WmsAi.Inbound.Infrastructure
   dotnet ef database update --context BusinessDbContext
   
   cd src/AiGateway/WmsAi.AiGateway.Infrastructure
   dotnet ef database update --context AiDbContext
   ```

2. **Drop and recreate database (development only)**
   ```bash
   docker exec -it <postgres-container-id> psql -U postgres
   DROP DATABASE "UserDb";
   CREATE DATABASE "UserDb";
   \q
   
   # Restart service to trigger EnsureCreatedAsync()
   ```

3. **Check migration history**
   ```bash
   dotnet ef migrations list --context UserDbContext
   ```

### CAP Event Delivery Failures

#### Issue: "Events not being consumed"

**Symptoms:**
- Events published but not received by consumers
- CAP dashboard shows failed messages

**Solutions:**

1. **Check RabbitMQ is running**
   ```bash
   docker ps | grep rabbitmq
   ```

2. **Verify RabbitMQ connection**
   ```bash
   # Access RabbitMQ Management UI
   # http://localhost:15672 (guest/guest)
   # Check Connections and Channels tabs
   ```

3. **Check exchange and queues**
   ```bash
   # In RabbitMQ Management UI:
   # - Exchange: wmsai.events should exist
   # - Queues: cap.queue.<service-name> should exist
   # - Bindings: queues should be bound to exchange
   ```

4. **Verify CAP configuration**
   ```csharp
   // In ModuleExtensions.cs
   services.AddCap(options =>
   {
       options.UseEntityFramework<UserDbContext>();
       options.UseRabbitMQ(rabbitOptions =>
       {
           rabbitOptions.ConnectionFactoryOptions = factory =>
           {
               factory.Uri = new Uri("amqp://guest:guest@localhost:5672");
           };
           rabbitOptions.ExchangeName = "wmsai.events";
       });
   });
   ```

5. **Check consumer registration**
   ```csharp
   // Ensure consumer is registered in DI
   services.AddScoped<PlatformEventConsumer>();
   
   // Ensure [CapSubscribe] attribute is present
   [CapSubscribe("TenantCreatedV1")]
   public async Task Handle(TenantCreatedV1 @event) { ... }
   ```

6. **Restart services**
   ```bash
   # Restart all services via Aspire
   # Or restart individual services
   ```

#### Issue: "CAP outbox table growing"

**Symptoms:**
- `cap.published` table has many rows
- Disk space increasing

**Solutions:**

1. **Check CAP cleanup configuration**
   ```csharp
   services.AddCap(options =>
   {
       options.SucceedMessageExpiredAfter = 24 * 3600; // 24 hours
       options.FailedMessageExpiredAfter = 15 * 24 * 3600; // 15 days
   });
   ```

2. **Manually clean old messages**
   ```sql
   -- Delete succeeded messages older than 7 days
   DELETE FROM cap.published 
   WHERE statusname = 'Succeeded' 
   AND added < NOW() - INTERVAL '7 days';
   ```

3. **Monitor CAP dashboard**
   ```bash
   # Access via service endpoint
   # http://localhost:5001/cap (Platform)
   # http://localhost:5002/cap (Inbound)
   # http://localhost:5003/cap (AiGateway)
   ```

### RabbitMQ Connectivity Issues

#### Issue: "Connection refused to RabbitMQ"

**Symptoms:**
```
RabbitMQ.Client.Exceptions.BrokerUnreachableException
```

**Solutions:**

1. **Check RabbitMQ container**
   ```bash
   docker ps | grep rabbitmq
   docker logs <rabbitmq-container-id>
   ```

2. **Verify ports**
   ```bash
   # AMQP: 5672
   # Management: 15672
   lsof -i :5672
   lsof -i :15672
   ```

3. **Check credentials**
   ```bash
   # Default: guest/guest
   # Verify in Aspire Program.cs
   ```

4. **Restart RabbitMQ**
   ```bash
   docker restart <rabbitmq-container-id>
   ```

#### Issue: "RabbitMQ out of memory"

**Symptoms:**
- RabbitMQ stops accepting connections
- Management UI shows memory alarm

**Solutions:**

1. **Check memory usage**
   ```bash
   # In RabbitMQ Management UI → Overview
   # Check memory and disk alarms
   ```

2. **Increase memory limit**
   ```bash
   # In Aspire Program.cs
   var rabbitmq = builder.AddRabbitMQ("rabbitmq")
       .WithEnvironment("RABBITMQ_VM_MEMORY_HIGH_WATERMARK", "1024MiB");
   ```

3. **Purge queues**
   ```bash
   # In RabbitMQ Management UI
   # Go to Queues → Select queue → Purge Messages
   ```

### Hangfire Job Failures

#### Issue: "Hangfire jobs not executing"

**Symptoms:**
- Jobs stuck in "Enqueued" state
- No job processing logs

**Solutions:**

1. **Check Hangfire dashboard**
   ```bash
   # http://localhost:5004/hangfire
   # Check Servers tab for active workers
   ```

2. **Verify Hangfire configuration**
   ```csharp
   // In OperationsModuleExtensions.cs
   services.AddHangfire(config =>
   {
       config.UsePostgreSqlStorage(connectionString);
   });
   
   services.AddHangfireServer();
   ```

3. **Check database connection**
   ```bash
   # Hangfire uses same database as Operations service
   # Verify connection string
   ```

4. **Restart Operations service**
   ```bash
   # Via Aspire dashboard or directly
   ```

#### Issue: "Hangfire job fails with exception"

**Symptoms:**
- Jobs move to "Failed" state
- Exception details in dashboard

**Solutions:**

1. **Check job logs**
   ```bash
   # In Hangfire dashboard → Failed Jobs
   # Click job to see exception details
   ```

2. **Retry failed jobs**
   ```bash
   # In Hangfire dashboard
   # Select failed jobs → Requeue
   ```

3. **Fix underlying issue**
   ```csharp
   // Add error handling in job methods
   public async Task ProcessJob()
   {
       try
       {
           // Job logic
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Job failed");
           throw; // Hangfire will retry
       }
   }
   ```

### AI Workflow Debugging

#### Issue: "AI workflow stuck in 'Running' state"

**Symptoms:**
- Workflow never completes
- No progress in logs

**Solutions:**

1. **Check workflow run status**
   ```sql
   SELECT * FROM maf_workflow_runs 
   WHERE status = 'Running' 
   ORDER BY created_at DESC;
   ```

2. **Check workflow step runs**
   ```sql
   SELECT * FROM maf_workflow_step_runs 
   WHERE workflow_run_id = '<workflow-run-id>' 
   ORDER BY sequence;
   ```

3. **Check for exceptions in logs**
   ```bash
   # In Aspire dashboard → AiGateway logs
   # Search for workflow_run_id
   ```

4. **Manually complete stuck workflow**
   ```sql
   UPDATE maf_workflow_runs 
   SET status = 'Failed', 
       error_message = 'Manually failed due to timeout'
   WHERE id = '<workflow-run-id>';
   ```

#### Issue: "Agent not responding"

**Symptoms:**
- Agent calls timeout
- No agent output in logs

**Solutions:**

1. **Check agent implementation**
   ```csharp
   // Ensure agent methods are async and properly awaited
   public async Task<AgentResponse> AnalyzeAsync(...)
   {
       // Implementation
   }
   ```

2. **Check model configuration**
   ```sql
   SELECT * FROM ai_model_providers WHERE status = 'Active';
   SELECT * FROM ai_model_profiles WHERE is_active = true;
   ```

3. **Add timeout handling**
   ```csharp
   using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
   var response = await agent.AnalyzeAsync(request, cts.Token);
   ```

4. **Check external API connectivity**
   ```bash
   # If using external AI APIs (OpenAI, etc.)
   # Verify API keys and network connectivity
   ```

### Log Analysis Guide

#### Viewing Logs

**Via Aspire Dashboard:**
1. Navigate to http://localhost:15888
2. Click "Logs" tab
3. Filter by service name
4. Search for keywords

**Via Console:**
```bash
# Run service directly to see console output
cd src/Platform/WmsAi.Platform.Host
dotnet run
```

**Via Docker:**
```bash
docker logs <container-id>
docker logs -f <container-id>  # Follow mode
```

#### Common Log Patterns

**Successful event publishing:**
```
[CAP] Published event: TenantCreatedV1
[CAP] Message sent to exchange: wmsai.events
```

**Event consumption:**
```
[CAP] Received event: TenantCreatedV1
[CAP] Processing message: <message-id>
[CAP] Message processed successfully
```

**Database operations:**
```
[EF] Executing DbCommand: INSERT INTO tenants ...
[EF] SaveChanges completed: 1 entities written
```

**Workflow execution:**
```
[Workflow] Starting workflow: InboundInspectionWorkflow
[Workflow] Executing step: CheckEvidenceCompleteness
[Workflow] Step completed: CheckEvidenceCompleteness
```

#### Log Levels

```csharp
// In appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information",
      "DotNetCore.CAP": "Information",
      "Hangfire": "Information"
    }
  }
}
```

### Performance Issues

#### Issue: "Slow API responses"

**Solutions:**

1. **Enable query logging**
   ```csharp
   options.UseNpgsql(connectionString)
       .LogTo(Console.WriteLine, LogLevel.Information)
       .EnableSensitiveDataLogging();
   ```

2. **Check database indexes**
   ```sql
   -- Find missing indexes
   SELECT schemaname, tablename, attname
   FROM pg_stats
   WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
   ORDER BY n_distinct DESC;
   ```

3. **Add caching**
   ```csharp
   services.AddDistributedMemoryCache();
   // Or use Redis
   services.AddStackExchangeRedisCache(options =>
   {
       options.Configuration = "localhost:6379";
   });
   ```

4. **Profile with dotnet-trace**
   ```bash
   dotnet tool install --global dotnet-trace
   dotnet trace collect --process-id <pid>
   ```

#### Issue: "High memory usage"

**Solutions:**

1. **Check for memory leaks**
   ```bash
   dotnet tool install --global dotnet-gcdump
   dotnet gcdump collect --process-id <pid>
   ```

2. **Optimize EF Core queries**
   ```csharp
   // Use AsNoTracking for read-only queries
   var tenants = await context.Tenants
       .AsNoTracking()
       .ToListAsync();
   ```

3. **Limit result sets**
   ```csharp
   var tasks = await context.QcTasks
       .Where(t => t.Status == QcTaskStatus.Pending)
       .Take(100)
       .ToListAsync();
   ```

### Frontend Issues

#### Issue: "CORS errors"

**Symptoms:**
```
Access to fetch at 'http://localhost:5000/api/...' from origin 'http://localhost:5173' has been blocked by CORS policy
```

**Solutions:**

1. **Check Gateway CORS configuration**
   ```csharp
   // In Gateway Program.cs
   services.AddCors(options =>
   {
       options.AddPolicy("AllowFrontend", policy =>
       {
           policy.WithOrigins("http://localhost:5173")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
       });
   });
   ```

2. **Verify CORS middleware order**
   ```csharp
   app.UseCors("AllowFrontend");
   app.UseMiddleware<FakeIdentityMiddleware>();
   app.MapReverseProxy();
   ```

#### Issue: "API calls failing from frontend"

**Solutions:**

1. **Check API base URL**
   ```typescript
   // In api/client.ts
   const apiClient = axios.create({
       baseURL: 'http://localhost:5000',
       timeout: 10000
   });
   ```

2. **Check network tab in browser DevTools**
   - Verify request URL
   - Check response status
   - Inspect request/response headers

3. **Add error handling**
   ```typescript
   try {
       const response = await apiClient.post('/api/platform/tenants', data);
       return response.data;
   } catch (error) {
       console.error('API call failed:', error);
       throw error;
   }
   ```

## Getting Help

1. **Check Aspire Dashboard**: http://localhost:15888
2. **Check service logs**: Look for exceptions and error messages
3. **Check database state**: Query tables directly
4. **Check infrastructure**: Verify all containers are running
5. **Restart services**: Often resolves transient issues
6. **Review documentation**: Check architecture and API docs

## Emergency Procedures

### Complete Reset (Development Only)

```bash
# Stop all services
docker stop $(docker ps -aq)

# Remove all containers
docker rm $(docker ps -aq)

# Remove volumes
docker volume prune -f

# Restart Aspire
cd src/AppHost/WmsAi.AppHost
dotnet run
```

### Database Reset

```bash
# Drop all databases
docker exec -it <postgres-container-id> psql -U postgres
DROP DATABASE "UserDb";
DROP DATABASE "BusinessDb";
DROP DATABASE "AiDb";
\q

# Restart services to recreate
```
