# Monitoring Guide

## Overview

WMS AI provides multiple monitoring dashboards and tools to track system health, performance, and operations.

## Monitoring Stack

### Available Dashboards

1. **Aspire Dashboard** - Service orchestration and distributed tracing
2. **CAP Dashboard** - Event delivery monitoring
3. **Hangfire Dashboard** - Background job monitoring
4. **RabbitMQ Management** - Message broker monitoring
5. **MinIO Console** - Object storage monitoring
6. **Nacos Console** - Configuration and service registry

## Aspire Dashboard

### Access

**URL**: http://localhost:15888

### Features

#### Resources View
- Service status (Running, Stopped, Failed)
- Container health
- Resource consumption (CPU, Memory)
- Port mappings

#### Logs View
- Real-time log streaming
- Filter by service
- Search log content
- Export logs

#### Traces View
- Distributed tracing across services
- Request flow visualization
- Performance bottlenecks
- Error tracking

#### Metrics View
- HTTP request rates
- Response times
- Error rates
- Custom metrics

### Key Metrics to Monitor

```
Service Health:
- All services showing "Running" status
- No restart loops
- Health check endpoints responding

Performance:
- HTTP request duration < 500ms (p95)
- Database query time < 100ms (p95)
- Event processing time < 1s (p95)

Errors:
- HTTP 5xx rate < 1%
- Exception rate < 0.1%
- Failed health checks = 0
```

## CAP Dashboard

### Access

Each service exposes its own CAP dashboard:

- **Platform**: http://localhost:5001/cap
- **Inbound**: http://localhost:5002/cap
- **AiGateway**: http://localhost:5003/cap

### Features

#### Published Messages
- View all published events
- Message status (Succeeded, Failed, Scheduled)
- Retry attempts
- Message content

#### Received Messages
- View consumed events
- Consumer status
- Processing time
- Error details

#### Subscribers
- List of registered consumers
- Subscription topics
- Consumer group

### Monitoring Event Delivery

#### Check Published Events

```sql
-- Query published events
SELECT 
    id,
    name,
    statusname,
    added,
    retries,
    expiresAt
FROM cap.published
WHERE statusname != 'Succeeded'
ORDER BY added DESC
LIMIT 100;
```

#### Check Received Events

```sql
-- Query received events
SELECT 
    id,
    name,
    group,
    statusname,
    added,
    retries
FROM cap.received
WHERE statusname != 'Succeeded'
ORDER BY added DESC
LIMIT 100;
```

#### Key Metrics

```
Event Publishing:
- Publish success rate > 99.9%
- Average publish time < 50ms
- Outbox table size < 10,000 rows

Event Consumption:
- Consumption success rate > 99%
- Average processing time < 500ms
- Retry queue size < 100

Delivery Latency:
- End-to-end event delivery < 2s (p95)
- Cross-service event propagation < 5s (p99)
```

### Alerts to Configure

```yaml
# Example alert rules (pseudo-code)
alerts:
  - name: HighEventFailureRate
    condition: failed_events / total_events > 0.01
    severity: critical
    
  - name: EventDeliveryDelay
    condition: event_age > 300s
    severity: warning
    
  - name: OutboxTableGrowth
    condition: outbox_size > 50000
    severity: warning
```

## Hangfire Dashboard

### Access

**URL**: http://localhost:5004/hangfire

Note: In production, secure with authentication:

```csharp
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
```

### Features

#### Jobs View
- Enqueued jobs
- Processing jobs
- Scheduled jobs
- Succeeded jobs
- Failed jobs

#### Recurring Jobs
- List of scheduled jobs
- Next execution time
- Last execution result
- Cron expression

#### Servers
- Active Hangfire servers
- Worker count
- Heartbeat status

#### Retries
- Failed jobs with retry attempts
- Retry schedule
- Error details

### Key Metrics

```
Job Processing:
- Job success rate > 99%
- Average job duration < 30s
- Queue length < 100

Scheduled Jobs:
- All recurring jobs executing on schedule
- No missed executions
- Retry queue size < 10

Server Health:
- All servers heartbeating
- Worker utilization < 80%
- No server disconnections
```

### Common Jobs to Monitor

```csharp
// Example recurring jobs
RecurringJob.AddOrUpdate(
    "cleanup-old-events",
    () => CleanupOldEventsJob.Execute(),
    Cron.Daily(2, 0)); // 2 AM daily

RecurringJob.AddOrUpdate(
    "sync-external-data",
    () => SyncExternalDataJob.Execute(),
    Cron.Hourly()); // Every hour
```

## RabbitMQ Management

### Access

**URL**: http://localhost:15672
**Credentials**: guest/guest

### Features

#### Overview
- Message rates (publish, deliver, ack)
- Queue totals
- Connection count
- Channel count

#### Connections
- Active connections
- Client properties
- Connection state

#### Channels
- Active channels
- Prefetch count
- Unacked messages

#### Exchanges
- Exchange list
- Bindings
- Message rates

#### Queues
- Queue list
- Message count
- Consumer count
- Message rates

### Key Metrics

```
Message Flow:
- Publish rate: steady, no spikes
- Delivery rate: matches publish rate
- Ack rate: matches delivery rate

Queue Health:
- Queue depth < 1000 messages
- No queues in "blocked" state
- Consumer count > 0 for all queues

Connections:
- All services connected
- No connection errors
- Channel count stable

Memory:
- Memory usage < 80%
- No memory alarms
- Disk space > 20% free
```

### Monitoring Queries

```bash
# List queues with message counts
rabbitmqctl list_queues name messages consumers

# List exchanges
rabbitmqctl list_exchanges name type

# List bindings
rabbitmqctl list_bindings

# Check cluster status
rabbitmqctl cluster_status
```

## AI Workflow Monitoring

### Session Monitoring

```sql
-- Active AI sessions
SELECT 
    id,
    tenant_id,
    warehouse_id,
    session_type,
    status,
    created_at,
    updated_at
FROM maf_sessions
WHERE status IN ('Active', 'Paused')
ORDER BY updated_at DESC;

-- Session duration
SELECT 
    id,
    EXTRACT(EPOCH FROM (updated_at - created_at)) as duration_seconds
FROM maf_sessions
WHERE status = 'Completed'
ORDER BY duration_seconds DESC
LIMIT 10;
```

### Workflow Monitoring

```sql
-- Workflow run status
SELECT 
    status,
    COUNT(*) as count
FROM maf_workflow_runs
WHERE created_at > NOW() - INTERVAL '24 hours'
GROUP BY status;

-- Failed workflows
SELECT 
    id,
    workflow_code,
    status,
    error_message,
    created_at
FROM maf_workflow_runs
WHERE status = 'Failed'
ORDER BY created_at DESC
LIMIT 20;
```

### Inspection Monitoring

```sql
-- Inspection run metrics
SELECT 
    status,
    COUNT(*) as count,
    AVG(EXTRACT(EPOCH FROM (updated_at - created_at))) as avg_duration_seconds
FROM ai_inspection_runs
WHERE created_at > NOW() - INTERVAL '24 hours'
GROUP BY status;

-- Manual review rate
SELECT 
    COUNT(CASE WHEN status = 'WaitingManualReview' THEN 1 END)::FLOAT / 
    COUNT(*)::FLOAT * 100 as manual_review_percentage
FROM ai_inspection_runs
WHERE created_at > NOW() - INTERVAL '24 hours';
```

### Model Call Monitoring

```sql
-- Model usage by provider
SELECT 
    provider_code,
    model_name,
    COUNT(*) as call_count,
    SUM(total_tokens) as total_tokens,
    AVG(latency_ms) as avg_latency_ms
FROM maf_model_call_logs
WHERE created_at > NOW() - INTERVAL '24 hours'
GROUP BY provider_code, model_name
ORDER BY call_count DESC;

-- Model errors
SELECT 
    provider_code,
    model_name,
    error_message,
    COUNT(*) as error_count
FROM maf_model_call_logs
WHERE error_message IS NOT NULL
  AND created_at > NOW() - INTERVAL '24 hours'
GROUP BY provider_code, model_name, error_message
ORDER BY error_count DESC;
```

### Key AI Metrics

```
Workflow Performance:
- Workflow completion rate > 95%
- Average workflow duration < 60s
- Manual review rate < 20%

Model Performance:
- Model call success rate > 99%
- Average latency < 2s
- Token usage within budget

Inspection Quality:
- Inspection accuracy > 90% (vs manual review)
- False positive rate < 5%
- False negative rate < 2%
```

## Performance Metrics

### Database Performance

```sql
-- Slow queries (PostgreSQL)
SELECT 
    query,
    calls,
    total_time,
    mean_time,
    max_time
FROM pg_stat_statements
WHERE mean_time > 100 -- queries averaging > 100ms
ORDER BY mean_time DESC
LIMIT 20;

-- Table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables
WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- Index usage
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch
FROM pg_stat_user_indexes
WHERE idx_scan = 0
ORDER BY pg_relation_size(indexrelid) DESC;
```

### API Performance

Monitor via Aspire Dashboard or custom metrics:

```
HTTP Metrics:
- Request rate (req/s)
- Response time (p50, p95, p99)
- Error rate (%)
- Throughput (MB/s)

Endpoint Performance:
- POST /api/inbound/notices: < 200ms (p95)
- POST /api/inbound/receipts: < 300ms (p95)
- GET /api/inbound/qc/tasks: < 100ms (p95)
- POST /api/ai/sessions: < 500ms (p95)
```

## Alert Configuration

### Critical Alerts

```yaml
# Service down
- alert: ServiceDown
  expr: up{job="platform|inbound|ai-gateway"} == 0
  for: 1m
  severity: critical

# High error rate
- alert: HighErrorRate
  expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05
  for: 5m
  severity: critical

# Database connection pool exhausted
- alert: DbPoolExhausted
  expr: db_pool_active_connections / db_pool_max_connections > 0.9
  for: 2m
  severity: critical

# Event delivery failure
- alert: EventDeliveryFailure
  expr: rate(cap_published_failed_total[5m]) > 0.01
  for: 5m
  severity: critical
```

### Warning Alerts

```yaml
# High response time
- alert: HighResponseTime
  expr: histogram_quantile(0.95, http_request_duration_seconds) > 1
  for: 10m
  severity: warning

# Queue depth growing
- alert: QueueDepthGrowing
  expr: rabbitmq_queue_messages > 1000
  for: 10m
  severity: warning

# Disk space low
- alert: DiskSpaceLow
  expr: disk_free_percent < 20
  for: 5m
  severity: warning
```

## Log Analysis

### Structured Logging

All services use structured logging:

```csharp
_logger.LogInformation(
    "Inbound notice created: {NoticeId}, {TenantId}, {WarehouseId}",
    noticeId, tenantId, warehouseId);
```

### Log Aggregation

View logs via:
- Aspire Dashboard (real-time)
- Docker logs (container-level)
- File logs (if configured)

### Common Log Queries

```bash
# Search for errors
grep "ERROR" logs/*.log

# Search for specific tenant
grep "TenantId=ACME" logs/*.log

# Search for slow operations
grep "duration.*[5-9][0-9][0-9][0-9]ms" logs/*.log

# Count errors by type
grep "ERROR" logs/*.log | cut -d':' -f3 | sort | uniq -c | sort -rn
```

### Log Levels

```
TRACE: Detailed diagnostic information
DEBUG: Development debugging
INFORMATION: General informational messages
WARNING: Potentially harmful situations
ERROR: Error events that might still allow the application to continue
CRITICAL: Critical failures requiring immediate attention
```

## Health Checks

### Endpoint

Each service exposes health check endpoint:

```bash
curl http://localhost:5001/health  # Platform
curl http://localhost:5002/health  # Inbound
curl http://localhost:5003/health  # AiGateway
curl http://localhost:5004/health  # Operations
```

### Response Format

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0123456"
    },
    "rabbitmq": {
      "status": "Healthy",
      "duration": "00:00:00.0098765"
    }
  }
}
```

### Custom Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database")
    .AddRabbitMQ(rabbitMqConnection, name: "rabbitmq")
    .AddRedis(redisConnection, name: "redis")
    .AddCheck<CustomHealthCheck>("custom");
```

## Monitoring Best Practices

1. **Set up alerts for critical metrics**
   - Service availability
   - Error rates
   - Event delivery failures

2. **Monitor trends over time**
   - Response time trends
   - Queue depth trends
   - Resource usage trends

3. **Establish baselines**
   - Normal request rates
   - Typical response times
   - Expected queue depths

4. **Regular reviews**
   - Weekly performance review
   - Monthly capacity planning
   - Quarterly architecture review

5. **Incident response**
   - Document runbooks
   - Practice incident response
   - Post-mortem analysis

## Troubleshooting with Monitoring

When issues occur:

1. **Check Aspire Dashboard** - Service status and logs
2. **Check CAP Dashboard** - Event delivery status
3. **Check RabbitMQ** - Queue depths and message rates
4. **Check Database** - Slow queries and connection pools
5. **Check Hangfire** - Background job failures
6. **Analyze Logs** - Error messages and stack traces
7. **Review Metrics** - Performance degradation patterns

See [Troubleshooting Guide](troubleshooting.md) for detailed solutions.
