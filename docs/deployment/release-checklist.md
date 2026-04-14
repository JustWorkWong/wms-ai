# Release Checklist

## Pre-Deployment Verification

### Code Quality

- [ ] All unit tests passing
  ```bash
  dotnet test tests/WmsAi.Platform.Tests
  dotnet test tests/WmsAi.Inbound.Tests
  ```

- [ ] Integration tests passing
  ```bash
  dotnet test tests/WmsAi.Integration.Tests
  ```

- [ ] Architecture tests passing
  ```bash
  dotnet test tests/WmsAi.ArchitectureTests
  ```

- [ ] Code analysis warnings resolved
  ```bash
  dotnet build /p:TreatWarningsAsErrors=true
  ```

- [ ] Code formatted
  ```bash
  dotnet format --verify-no-changes
  ```

### Database Migrations

- [ ] All migrations created
  ```bash
  # Check for pending model changes
  cd src/Platform/WmsAi.Platform.Infrastructure
  dotnet ef migrations has-pending-model-changes --context UserDbContext
  
  cd src/Inbound/WmsAi.Inbound.Infrastructure
  dotnet ef migrations has-pending-model-changes --context BusinessDbContext
  
  cd src/AiGateway/WmsAi.AiGateway.Infrastructure
  dotnet ef migrations has-pending-model-changes --context AiDbContext
  ```

- [ ] Migration scripts generated
  ```bash
  dotnet ef migrations script --context UserDbContext --idempotent --output userdb-migration.sql
  dotnet ef migrations script --context BusinessDbContext --idempotent --output businessdb-migration.sql
  dotnet ef migrations script --context AiDbContext --idempotent --output aidb-migration.sql
  ```

- [ ] Migration scripts reviewed
  - Check for data loss operations (DROP COLUMN, DROP TABLE)
  - Verify indexes are created
  - Ensure foreign keys are correct
  - Review default values

- [ ] Migrations tested on staging database
  ```bash
  psql -h staging-host -U postgres -d UserDb -f userdb-migration.sql
  psql -h staging-host -U postgres -d BusinessDb -f businessdb-migration.sql
  psql -h staging-host -U postgres -d AiDb -f aidb-migration.sql
  ```

### Configuration Validation

- [ ] Environment variables configured
  - Database connection strings
  - RabbitMQ connection
  - Redis connection
  - MinIO credentials
  - Nacos configuration

- [ ] Secrets configured
  - Database passwords
  - API keys
  - JWT signing keys
  - External service credentials

- [ ] Feature flags reviewed
  - Disable experimental features in production
  - Enable production-ready features

- [ ] Logging configuration
  - Log level set appropriately (Information/Warning)
  - Structured logging enabled
  - Log retention configured

### Security Review

- [ ] Authentication enabled on all endpoints
  ```csharp
  // Verify FakeIdentityMiddleware is disabled in production
  // app.UseMiddleware<FakeIdentityMiddleware>(); // REMOVE IN PRODUCTION
  ```

- [ ] Authorization rules applied
  - Tenant isolation enforced
  - Warehouse-level permissions checked
  - User roles validated

- [ ] Sensitive data protected
  - Passwords hashed
  - API keys encrypted
  - PII data masked in logs

- [ ] CORS configured correctly
  ```csharp
  // Verify allowed origins
  policy.WithOrigins("https://production-domain.com")
  ```

- [ ] Rate limiting configured
  - API rate limits set
  - DDoS protection enabled

### Performance Testing

- [ ] Load testing completed
  - Target: 1000 requests/second
  - Response time < 500ms (p95)
  - Error rate < 1%

- [ ] Database performance validated
  - Query execution plans reviewed
  - Indexes optimized
  - Connection pooling configured

- [ ] Cache warming strategy
  - Frequently accessed data pre-cached
  - Cache hit rate > 80%

- [ ] Resource limits configured
  - CPU limits set
  - Memory limits set
  - Connection pool sizes tuned

### Backup and Recovery

- [ ] Database backup taken
  ```bash
  pg_dump -h production-host -U postgres -d UserDb -F c -f userdb-backup-$(date +%Y%m%d).dump
  pg_dump -h production-host -U postgres -d BusinessDb -F c -f businessdb-backup-$(date +%Y%m%d).dump
  pg_dump -h production-host -U postgres -d AiDb -F c -f aidb-backup-$(date +%Y%m%d).dump
  ```

- [ ] Backup verified
  ```bash
  # Test restore on staging
  pg_restore -h staging-host -U postgres -d UserDb_Test userdb-backup.dump
  ```

- [ ] Rollback plan documented
  - Previous version container images tagged
  - Database rollback scripts prepared
  - Configuration rollback procedure defined

### Documentation

- [ ] Release notes prepared
  - New features documented
  - Breaking changes highlighted
  - Migration guide provided

- [ ] API documentation updated
  - OpenAPI spec updated
  - Example requests/responses added

- [ ] Runbooks updated
  - New operational procedures documented
  - Troubleshooting guide updated

- [ ] Changelog updated
  ```markdown
  ## [1.2.0] - 2024-04-14
  ### Added
  - AI inspection workflow with dual-agent collaboration
  - MAF session management with checkpoints
  
  ### Changed
  - Improved event delivery reliability
  - Optimized database queries
  
  ### Fixed
  - Fixed race condition in QC task creation
  ```

## Deployment Steps

### 1. Pre-Deployment Communication

- [ ] Notify stakeholders of deployment window
- [ ] Schedule maintenance window if needed
- [ ] Prepare rollback communication template

### 2. Database Migration

- [ ] Enable maintenance mode (if applicable)
  ```bash
  # Redirect traffic to maintenance page
  ```

- [ ] Stop services (if required for migration)
  ```bash
  kubectl scale deployment platform --replicas=0
  kubectl scale deployment inbound --replicas=0
  kubectl scale deployment ai-gateway --replicas=0
  kubectl scale deployment operations --replicas=0
  ```

- [ ] Apply database migrations
  ```bash
  # Apply in order: Platform → Inbound → AiGateway
  psql -h production-host -U postgres -d UserDb -f userdb-migration.sql
  psql -h production-host -U postgres -d BusinessDb -f businessdb-migration.sql
  psql -h production-host -U postgres -d AiDb -f aidb-migration.sql
  ```

- [ ] Verify migrations applied
  ```sql
  -- Check migration history
  SELECT * FROM __EFMigrationsHistory ORDER BY migration_id DESC LIMIT 5;
  ```

- [ ] Verify data integrity
  ```sql
  -- Run validation queries
  SELECT COUNT(*) FROM tenants;
  SELECT COUNT(*) FROM inbound_notices;
  SELECT COUNT(*) FROM maf_sessions;
  ```

### 3. Service Deployment Order

Deploy services in dependency order:

- [ ] **Step 1: Deploy Platform service**
  ```bash
  kubectl set image deployment/platform platform=wmsai/platform:v1.2.0
  kubectl rollout status deployment/platform
  ```

- [ ] **Step 2: Deploy Inbound service**
  ```bash
  kubectl set image deployment/inbound inbound=wmsai/inbound:v1.2.0
  kubectl rollout status deployment/inbound
  ```

- [ ] **Step 3: Deploy AiGateway service**
  ```bash
  kubectl set image deployment/ai-gateway ai-gateway=wmsai/ai-gateway:v1.2.0
  kubectl rollout status deployment/ai-gateway
  ```

- [ ] **Step 4: Deploy Operations service**
  ```bash
  kubectl set image deployment/operations operations=wmsai/operations:v1.2.0
  kubectl rollout status deployment/operations
  ```

- [ ] **Step 5: Deploy Gateway service**
  ```bash
  kubectl set image deployment/gateway gateway=wmsai/gateway:v1.2.0
  kubectl rollout status deployment/gateway
  ```

- [ ] **Step 6: Deploy Frontend**
  ```bash
  # Build and deploy Vue app
  cd web/wms-ai-web
  npm run build
  # Deploy dist/ to CDN or web server
  ```

### 4. Configuration Updates

- [ ] Update Nacos configuration
  ```bash
  # Upload new configuration via Nacos console
  # http://nacos-host:8848/nacos
  ```

- [ ] Update environment variables
  ```bash
  kubectl set env deployment/platform NEW_CONFIG=value
  ```

- [ ] Restart services if config requires restart
  ```bash
  kubectl rollout restart deployment/platform
  ```

## Post-Deployment Validation

### Health Checks

- [ ] All services healthy
  ```bash
  kubectl get pods
  # All pods should be Running with READY 1/1
  ```

- [ ] Health endpoints responding
  ```bash
  curl http://platform/health
  curl http://inbound/health
  curl http://ai-gateway/health
  curl http://operations/health
  curl http://gateway/health
  ```

- [ ] Database connections established
  ```bash
  # Check service logs for successful DB connection
  kubectl logs deployment/platform | grep "Database"
  ```

### Functional Testing

- [ ] **Platform API**
  ```bash
  # Create tenant
  curl -X POST http://gateway/api/platform/tenants \
    -H "Content-Type: application/json" \
    -d '{"code":"TEST","name":"Test Tenant"}'
  ```

- [ ] **Inbound API**
  ```bash
  # Create inbound notice
  curl -X POST http://gateway/api/inbound/notices \
    -H "Content-Type: application/json" \
    -d '{
      "tenantId":"TEST",
      "warehouseId":"WH001",
      "noticeNo":"ASN-TEST-001",
      "lines":[{"skuCode":"SKU-001","expectedQuantity":"100"}]
    }'
  ```

- [ ] **QC API**
  ```bash
  # Get QC tasks
  curl "http://gateway/api/inbound/qc/tasks?tenantId=TEST&warehouseId=WH001"
  ```

- [ ] **AI Gateway API**
  ```bash
  # Create AI session
  curl -X POST http://gateway/api/ai/sessions \
    -H "Content-Type: application/json" \
    -d '{
      "tenantId":"TEST",
      "warehouseId":"WH001",
      "userId":"user001",
      "businessObjectType":"QcTask",
      "businessObjectId":"task-001"
    }'
  ```

### Event Flow Validation

- [ ] Events being published
  ```sql
  -- Check recent published events
  SELECT * FROM cap.published 
  WHERE added > NOW() - INTERVAL '5 minutes'
  ORDER BY added DESC;
  ```

- [ ] Events being consumed
  ```sql
  -- Check recent received events
  SELECT * FROM cap.received 
  WHERE added > NOW() - INTERVAL '5 minutes'
  ORDER BY added DESC;
  ```

- [ ] RabbitMQ message flow
  ```bash
  # Check RabbitMQ Management UI
  # http://rabbitmq-host:15672
  # Verify message rates are normal
  ```

### Performance Validation

- [ ] Response times acceptable
  ```bash
  # Use Aspire dashboard or APM tool
  # Check p95 response time < 500ms
  ```

- [ ] Error rates normal
  ```bash
  # Check error rate < 1%
  ```

- [ ] Database query performance
  ```sql
  -- Check slow queries
  SELECT query, mean_exec_time, calls
  FROM pg_stat_statements
  WHERE mean_exec_time > 100
  ORDER BY mean_exec_time DESC
  LIMIT 10;
  ```

- [ ] Cache hit rates
  ```bash
  # Check Redis stats
  redis-cli info stats | grep hit_rate
  ```

### Monitoring Validation

- [ ] Aspire dashboard accessible
  - http://aspire-dashboard-url

- [ ] CAP dashboards accessible
  - http://platform/cap
  - http://inbound/cap
  - http://ai-gateway/cap

- [ ] Hangfire dashboard accessible
  - http://operations/hangfire

- [ ] RabbitMQ management accessible
  - http://rabbitmq-host:15672

- [ ] Logs flowing to aggregation system
  ```bash
  # Check log aggregation tool (e.g., ELK, Splunk)
  ```

- [ ] Metrics being collected
  ```bash
  # Check metrics system (e.g., Prometheus, Grafana)
  ```

- [ ] Alerts configured and firing correctly
  ```bash
  # Verify alert rules are active
  ```

### Frontend Validation

- [ ] Frontend accessible
  - http://production-domain.com

- [ ] All pages loading
  - Login page
  - Dashboard
  - Tenant list
  - Warehouse list
  - Inbound notice list
  - Receipt list
  - QC task workbench

- [ ] API calls succeeding
  ```bash
  # Check browser console for errors
  # Check network tab for failed requests
  ```

- [ ] Authentication working
  - Login flow
  - Token refresh
  - Logout

## Rollback Procedures

### When to Rollback

Rollback immediately if:
- Critical functionality broken
- Data corruption detected
- Performance degradation > 50%
- Error rate > 5%
- Security vulnerability introduced

### Rollback Steps

#### 1. Rollback Services

```bash
# Rollback to previous version
kubectl rollout undo deployment/platform
kubectl rollout undo deployment/inbound
kubectl rollout undo deployment/ai-gateway
kubectl rollout undo deployment/operations
kubectl rollout undo deployment/gateway

# Verify rollback
kubectl rollout status deployment/platform
```

#### 2. Rollback Database (if needed)

```bash
# Stop services
kubectl scale deployment platform --replicas=0
kubectl scale deployment inbound --replicas=0
kubectl scale deployment ai-gateway --replicas=0

# Restore database backup
pg_restore -h production-host -U postgres -d UserDb -c userdb-backup.dump
pg_restore -h production-host -U postgres -d BusinessDb -c businessdb-backup.dump
pg_restore -h production-host -U postgres -d AiDb -c aidb-backup.dump

# Or apply rollback migration
psql -h production-host -U postgres -d UserDb -f userdb-rollback.sql
```

#### 3. Rollback Configuration

```bash
# Revert Nacos configuration
# Via Nacos console or API

# Revert environment variables
kubectl set env deployment/platform CONFIG=old-value
```

#### 4. Verify Rollback

- [ ] Services healthy
- [ ] Functional tests passing
- [ ] Error rates normal
- [ ] Performance acceptable

### Post-Rollback

- [ ] Notify stakeholders of rollback
- [ ] Document rollback reason
- [ ] Create incident report
- [ ] Schedule post-mortem
- [ ] Fix issues before next deployment

## Production Readiness Criteria

Before marking release as production-ready:

### Stability
- [ ] No critical bugs in last 7 days
- [ ] No data loss incidents
- [ ] No security vulnerabilities
- [ ] Uptime > 99.9% in staging

### Performance
- [ ] Load testing passed at 2x expected traffic
- [ ] Response times meet SLA
- [ ] Database queries optimized
- [ ] Cache hit rate > 80%

### Monitoring
- [ ] All dashboards operational
- [ ] Alerts configured and tested
- [ ] Log aggregation working
- [ ] Metrics collection active

### Documentation
- [ ] API documentation complete
- [ ] Runbooks updated
- [ ] Troubleshooting guide current
- [ ] Architecture diagrams accurate

### Operations
- [ ] Backup/restore tested
- [ ] Disaster recovery plan documented
- [ ] On-call rotation established
- [ ] Escalation procedures defined

### Compliance
- [ ] Security audit passed
- [ ] Data privacy requirements met
- [ ] Audit logging enabled
- [ ] Compliance reports generated

## Sign-off

- [ ] Development team lead approval
- [ ] QA team approval
- [ ] Operations team approval
- [ ] Security team approval
- [ ] Product owner approval

**Deployment Date**: _______________
**Deployed By**: _______________
**Approved By**: _______________

## Post-Deployment Tasks

- [ ] Monitor system for 24 hours
- [ ] Review error logs
- [ ] Check performance metrics
- [ ] Gather user feedback
- [ ] Update status page
- [ ] Send deployment summary to stakeholders
- [ ] Schedule retrospective meeting
- [ ] Document lessons learned
