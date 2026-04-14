import { test, expect } from '@playwright/test'
import { ApiHelper } from './fixtures/api'
import testData from './fixtures/test-data.json'
import * as fs from 'fs'
import * as path from 'path'

test.describe('QC Workflow', () => {
  const api = new ApiHelper()
  const { tenantId, warehouseId } = testData.demoTenant
  let qcTaskId: string

  test.beforeEach(async () => {
    // Create ASN and receipt to generate QC task
    const noticeNo = `ASN_QC_${Date.now()}`
    const noticeResponse = await api.createInboundNotice(
      tenantId,
      warehouseId,
      noticeNo,
      testData.testSku.skuCode,
      testData.testSku.quantity
    )

    await api.recordReceipt(
      tenantId,
      warehouseId,
      noticeResponse.data.inboundNoticeId,
      testData.testSku.skuCode,
      testData.testSku.quantity
    )

    // Wait for QC task to be created
    await new Promise(resolve => setTimeout(resolve, 2000))
  })

  test('should display QC task after receipt', async ({ page }) => {
    await page.goto('/qc/tasks')
    await page.waitForLoadState('networkidle')

    // Wait for tasks to load
    await page.waitForSelector('table tbody tr', { timeout: 15000 })

    // Verify task exists
    const firstTask = page.locator('table tbody tr').first()
    await expect(firstTask).toBeVisible()

    // Click to open workbench
    const inspectLink = firstTask.locator('a:has-text("检验"), a:has-text("Inspect")')
    await inspectLink.first().click()

    // Verify workbench loaded
    await page.waitForLoadState('networkidle')
    await expect(page).toHaveURL(/\/qc\/tasks\//)

    // Verify workbench title
    const workbenchTitle = page.locator('h1:has-text("质检工作台"), h1:has-text("QC Workbench")')
    await expect(workbenchTitle.first()).toBeVisible({ timeout: 10000 })
  })

  test('should upload evidence', async ({ page }) => {
    // Navigate to QC tasks
    await page.goto('/qc/tasks')
    await page.waitForLoadState('networkidle')
    await page.waitForSelector('table tbody tr', { timeout: 15000 })

    // Open first task
    const firstTask = page.locator('table tbody tr').first()
    const inspectLink = firstTask.locator('a').first()
    await inspectLink.click()
    await page.waitForLoadState('networkidle')

    // Find file input
    const fileInput = page.locator('input[type="file"]')

    // Create a test image if it doesn't exist
    const testImagePath = path.join(__dirname, 'fixtures', 'test-image.jpg')
    if (!fs.existsSync(testImagePath)) {
      // Create a minimal JPEG file for testing
      const testImageBuffer = Buffer.from([
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46,
        0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01,
        0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9
      ])
      fs.writeFileSync(testImagePath, testImageBuffer)
    }

    // Upload image
    await fileInput.setInputFiles(testImagePath)

    // Wait for upload success message
    const successMessage = page.locator('text=/上传成功|Upload success/')
    await expect(successMessage).toBeVisible({ timeout: 10000 })

    // Verify evidence appears
    const evidenceItem = page.locator('.evidence-item, .evidence-image, img[src*="evidence"]')
    await expect(evidenceItem.first()).toBeVisible({ timeout: 10000 })
  })

  test('should trigger AI inspection', async ({ page }) => {
    // Navigate to QC tasks
    await page.goto('/qc/tasks')
    await page.waitForLoadState('networkidle')
    await page.waitForSelector('table tbody tr', { timeout: 15000 })

    // Open first task
    const firstTask = page.locator('table tbody tr').first()
    await firstTask.locator('a').first().click()
    await page.waitForLoadState('networkidle')

    // Click AI inspect button
    const aiButton = page.locator('button:has-text("AI 检验"), button:has-text("AI Inspect")')
    await aiButton.first().click()

    // Wait for AI chat panel to appear
    const aiPanel = page.locator('.ai-chat-panel, .ai-panel, [class*="ai-chat"]')
    await expect(aiPanel.first()).toBeVisible({ timeout: 10000 })

    // Wait for AI response (SSE streaming)
    const aiMessage = page.locator('.ai-message, .message, [class*="ai-message"]')
    await expect(aiMessage.first()).toBeVisible({ timeout: 30000 })
  })

  test('should display confidence score after AI inspection', async ({ page }) => {
    // Navigate to QC tasks
    await page.goto('/qc/tasks')
    await page.waitForLoadState('networkidle')
    await page.waitForSelector('table tbody tr', { timeout: 15000 })

    // Open first task
    const firstTask = page.locator('table tbody tr').first()
    await firstTask.locator('a').first().click()
    await page.waitForLoadState('networkidle')

    // Trigger AI inspection
    const aiButton = page.locator('button:has-text("AI 检验"), button:has-text("AI Inspect")')
    await aiButton.first().click()

    // Wait for completion
    const confidenceText = page.locator('text=/置信度|Confidence/')
    await expect(confidenceText.first()).toBeVisible({ timeout: 30000 })

    // Check if confidence score is displayed
    const confidenceValue = page.locator('text=/\\d+\\.\\d+%|\\d+%/')
    await expect(confidenceValue.first()).toBeVisible({ timeout: 5000 })
  })

  test('should navigate QC tasks list', async ({ page }) => {
    await page.goto('/qc/tasks')
    await page.waitForLoadState('networkidle')

    // Verify page loaded
    const pageTitle = page.locator('h1, h2, .page-title')
    await expect(pageTitle).toBeVisible()

    // Verify table exists
    const table = page.locator('table')
    await expect(table).toBeVisible()
  })
})
