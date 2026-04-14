# WmsAi E2E Tests

End-to-end tests for the WMS AI system using Playwright.

## Prerequisites

- Node.js 18+ installed
- Aspire AppHost running (all services up)
- Vue frontend running on port 5173
- Backend API running on port 5000
- Seed data loaded in databases

## Installation

```bash
npm install
npx playwright install chromium
```

## Running Tests

### Run all tests
```bash
npm test
```

### Run tests with UI mode (interactive)
```bash
npm run test:ui
```

### Run tests in headed mode (see browser)
```bash
npm run test:headed
```

### Debug tests
```bash
npm run test:debug
```

### View test report
```bash
npm run test:report
```

### Run specific test file
```bash
npx playwright test tests/01-platform-setup.spec.ts
```

### Run tests matching a pattern
```bash
npx playwright test --grep "golden path"
```

## Test Structure

```
tests/
├── fixtures/
│   ├── api.ts              # API helper for backend calls
│   ├── test-data.json      # Test data configuration
│   └── test-image.jpg      # Sample image for evidence upload
├── 01-platform-setup.spec.ts       # Platform tenant/user tests
├── 02-inbound-operations.spec.ts   # ASN and receipt tests
├── 03-qc-workflow.spec.ts          # QC task and AI inspection tests
├── 04-manual-review.spec.ts        # Manual review decision tests
└── 05-golden-path.spec.ts          # Complete end-to-end flow
```

## Test Scenarios

### 1. Platform Setup (01-platform-setup.spec.ts)
- Create tenant and warehouse
- Create user and assign membership
- Display tenant list

### 2. Inbound Operations (02-inbound-operations.spec.ts)
- Create ASN (Advance Shipping Notice)
- Record receipt
- Display inbound notices and receipts

### 3. QC Workflow (03-qc-workflow.spec.ts)
- Display QC task after receipt
- Upload evidence images
- Trigger AI inspection
- Display confidence score

### 4. Manual Review (04-manual-review.spec.ts)
- Display manual review page
- Submit accept decision
- Submit reject decision
- Display review history

### 5. Golden Path (05-golden-path.spec.ts)
- Complete flow from ASN to QC decision
- Verify auto-pass flow with high confidence
- Verify manual review flow with low confidence

## Configuration

Edit `playwright.config.ts` to customize:
- Base URL for frontend
- Timeout settings
- Browser configuration
- Screenshot/video capture settings

## API Helper

The `ApiHelper` class provides methods for:
- Creating tenants and users
- Creating inbound notices
- Recording receipts
- Getting QC tasks
- Uploading evidence
- Triggering AI inspection
- Submitting QC decisions

## Test Data

Test data is configured in `tests/fixtures/test-data.json`:
- Demo tenant credentials
- Test SKU information
- Test user details

## Troubleshooting

### Tests fail with timeout
- Ensure all services are running
- Increase timeout in `playwright.config.ts`
- Check network connectivity

### Cannot find elements
- Verify frontend is running on port 5173
- Check element selectors match actual UI
- Use `--headed` mode to see what's happening

### API calls fail
- Ensure backend is running on port 5000
- Check API endpoints are correct
- Verify tenant/warehouse IDs exist

## CI/CD Integration

Tests can be run in CI/CD pipelines. See `.github/workflows/e2e-tests.yml` for GitHub Actions example.

## Best Practices

1. Use API helpers for data setup (faster than UI)
2. Use UI for verification (user-visible behavior)
3. Each test should be independent
4. Clean up test data after tests
5. Use explicit waits for async operations
6. Capture screenshots on failure for debugging
