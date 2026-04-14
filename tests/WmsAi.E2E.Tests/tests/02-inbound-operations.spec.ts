import { test, expect } from '@playwright/test'
import { ApiHelper } from './fixtures/api'
import testData from './fixtures/test-data.json'

test.describe('Inbound Operations', () => {
  const api = new ApiHelper()
  const { tenantId, warehouseId } = testData.demoTenant
  let inboundNoticeId: string
  let noticeNo: string

  test('should create ASN (Advance Shipping Notice)', async ({ page }) => {
    // Create ASN via API
    noticeNo = `ASN_E2E_${Date.now()}`
    const response = await api.createInboundNotice(
      tenantId,
      warehouseId,
      noticeNo,
      testData.testSku.skuCode,
      testData.testSku.quantity
    )

    expect(response.status).toBe(200)
    inboundNoticeId = response.data.inboundNoticeId

    // Navigate to inbound notices page
    await page.goto('/inbound/notices')
    await page.waitForLoadState('networkidle')

    // Verify ASN appears in list
    const noticeRow = page.locator(`text=${noticeNo}`)
    await expect(noticeRow).toBeVisible({ timeout: 10000 })
  })

  test('should record receipt', async ({ page }) => {
    // First create an ASN
    noticeNo = `ASN_E2E_${Date.now()}`
    const noticeResponse = await api.createInboundNotice(
      tenantId,
      warehouseId,
      noticeNo,
      testData.testSku.skuCode,
      testData.testSku.quantity
    )
    inboundNoticeId = noticeResponse.data.inboundNoticeId

    // Record receipt via API
    const receiptResponse = await api.recordReceipt(
      tenantId,
      warehouseId,
      inboundNoticeId,
      testData.testSku.skuCode,
      testData.testSku.quantity
    )

    expect(receiptResponse.status).toBe(200)

    // Navigate to receipts page
    await page.goto('/inbound/receipts')
    await page.waitForLoadState('networkidle')

    // Verify receipt appears in list
    const receiptRow = page.locator('text=/RCV_/')
    await expect(receiptRow.first()).toBeVisible({ timeout: 10000 })
  })

  test('should display inbound notices list', async ({ page }) => {
    await page.goto('/inbound/notices')
    await page.waitForLoadState('networkidle')

    // Verify page loaded
    const pageTitle = page.locator('h1, h2, .page-title')
    await expect(pageTitle).toBeVisible()

    // Verify table exists
    const table = page.locator('table')
    await expect(table).toBeVisible()
  })

  test('should display receipts list', async ({ page }) => {
    await page.goto('/inbound/receipts')
    await page.waitForLoadState('networkidle')

    // Verify page loaded
    const pageTitle = page.locator('h1, h2, .page-title')
    await expect(pageTitle).toBeVisible()

    // Verify table exists
    const table = page.locator('table')
    await expect(table).toBeVisible()
  })
})
