import { test, expect } from '@playwright/test'
import { ApiHelper } from './fixtures/api'

test.describe('Platform Setup', () => {
  const api = new ApiHelper()
  let testTenantId: string
  let testWarehouseId: string

  test.beforeEach(() => {
    testTenantId = `TENANT_E2E_${Date.now()}`
    testWarehouseId = `WH_E2E_${Date.now()}`
  })

  test('should create tenant and warehouse', async ({ page }) => {
    // Create tenant via API (faster than UI)
    const response = await api.createTenant(testTenantId, 'E2E Test Tenant', testWarehouseId)

    expect(response.status).toBe(200)
    expect(response.data).toHaveProperty('tenantId', testTenantId)

    // Navigate to tenant management page
    await page.goto('/platform/tenants')

    // Verify tenant appears in list
    await page.waitForLoadState('networkidle')
    const tenantRow = page.locator(`text=${testTenantId}`)
    await expect(tenantRow).toBeVisible({ timeout: 10000 })
  })

  test('should create user and assign membership', async ({ page }) => {
    // First create a tenant
    await api.createTenant(testTenantId, 'E2E Test Tenant', testWarehouseId)

    // Navigate to user management
    await page.goto('/platform/users')
    await page.waitForLoadState('networkidle')

    // Click create user button
    const createButton = page.locator('button:has-text("创建用户"), button:has-text("Create User")')
    await createButton.first().click()

    // Fill user form
    const userId = `user.e2e.${Date.now()}`
    await page.fill('input[name="userId"]', userId)
    await page.fill('input[name="username"]', userId)
    await page.fill('input[name="displayName"]', 'E2E Test User')

    // Submit form
    const submitButton = page.locator('button:has-text("提交"), button:has-text("Submit")')
    await submitButton.first().click()

    // Verify success message or user appears in list
    await page.waitForTimeout(2000)
    const userRow = page.locator(`text=${userId}`)
    await expect(userRow).toBeVisible({ timeout: 10000 })
  })

  test('should display tenant list', async ({ page }) => {
    await page.goto('/platform/tenants')
    await page.waitForLoadState('networkidle')

    // Verify page loaded
    const pageTitle = page.locator('h1, h2, .page-title')
    await expect(pageTitle).toBeVisible()

    // Verify table exists
    const table = page.locator('table')
    await expect(table).toBeVisible()
  })
})
