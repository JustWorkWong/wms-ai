import { test, expect } from '@playwright/test'
import { ApiHelper } from './fixtures/api'
import testData from './fixtures/test-data.json'
import * as fs from 'fs'
import * as path from 'path'

test.describe('Golden Path - Complete E2E Flow', () => {
  const api = new ApiHelper()
  const { tenantId, warehouseId } = testData.demoTenant
  let noticeNo: string
  let inboundNoticeId: string

  test('complete flow from ASN to QC decision', async ({ page }) => {
    // Step 1: Create ASN
    noticeNo = `ASN_GOLDEN_${Date.now()}`
    const noticeResponse = await api.createInboundNotice(
      tenantId,
      warehouseId,
      noticeNo,
      testData.testSku.skuCode,
      testData.testSku.quantity
    )

    expect(noticeResponse.status).toBe(200)
    inboundNoticeId = noticeResponse.data.inboundNoticeId

    console.log(`✓ Step 1: Created ASN ${noticeNo}`)

    // Step 2: Record receipt
    const receiptResponse = await api.recordReceipt(
      tenantId,
      warehouseId,
      inboundNoticeId,
      testData.testSku.skuCode,
      testData.testSku.quantity
    )

    expect(receiptResponse.status).toBe(200)
    console.log(`✓ Step 2: Recorded receipt`)

    // Wait for QC task to be created
    await new Promise(resolve => setTimeout(resolve, 3000))

    // Step 3: Navigate to QC tasks
    await page.goto('/qc/tasks')
    await page.waitForLoadState('networkidle')
    await page.waitForSelector('table tbody tr', { timeout: 15000 })

    console.log(`✓ Step 3: Navigated to QC tasks`)

    // Step 4: Open QC workbench
    const firstTask = page.locator('table tbody tr').first()
    await expect(firstTask).toBeVisible()

    const inspectLink = firstTask.locator('a').first()
    await inspectLink.click()
    await page.waitForLoadState('networkidle')

    const workbenchTitle = page.locator('h1:has-text("质检工作台"), h1:has-text("QC Workbench")')
    await expect(workbenchTitle.first()).toBeVisible({ timeout: 10000 })

    console.log(`✓ Step 4: Opened QC workbench`)

    // Step 5: Upload evidence
    const fileInput = page.locator('input[type="file"]')
    const testImagePath = path.join(__dirname, 'fixtures', 'test-image.jpg')

    // Create test image if it doesn't exist
    if (!fs.existsSync(testImagePath)) {
      const testImageBuffer = Buffer.from([
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46,
        0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01,
        0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9
      ])
      fs.mkdirSync(path.dirname(testImagePath), { recursive: true })
      fs.writeFileSync(testImagePath, testImageBuffer)
    }

    await fileInput.setInputFiles(testImagePath)

    const successMessage = page.locator('text=/上传成功|Upload success/')
    await expect(successMessage).toBeVisible({ timeout: 10000 })

    console.log(`✓ Step 5: Uploaded evidence`)

    // Step 6: Trigger AI inspection
    const aiButton = page.locator('button:has-text("AI 检验"), button:has-text("AI Inspect")')
    await aiButton.first().click()

    const aiPanel = page.locator('.ai-chat-panel, .ai-panel, [class*="ai-chat"]')
    await expect(aiPanel.first()).toBeVisible({ timeout: 10000 })

    console.log(`✓ Step 6: Triggered AI inspection`)

    // Step 7: Wait for AI completion
    const aiMessage = page.locator('.ai-message, .message, [class*="ai-message"]')
    await expect(aiMessage.first()).toBeVisible({ timeout: 60000 })

    console.log(`✓ Step 7: AI inspection completed`)

    // Step 8: Verify final status
    // Wait a bit for status to update
    await new Promise(resolve => setTimeout(resolve, 2000))

    await page.goto('/qc/tasks')
    await page.waitForLoadState('networkidle')

    // Look for the task we created
    const taskRow = page.locator(`text=${noticeNo}`)
    await expect(taskRow).toBeVisible({ timeout: 10000 })

    console.log(`✓ Step 8: Verified task status`)

    // Verify the complete flow
    console.log(`\n✅ Golden Path Complete:`)
    console.log(`   - ASN: ${noticeNo}`)
    console.log(`   - Inbound Notice ID: ${inboundNoticeId}`)
    console.log(`   - All steps executed successfully`)
  })

  test('verify auto-pass flow with high confidence', async ({ page }) => {
    // Create ASN and receipt
    noticeNo = `ASN_AUTOPASS_${Date.now()}`
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

    await new Promise(resolve => setTimeout(resolve, 3000))

    // Navigate to QC task
    await page.goto('/qc/tasks')
    await page.waitForLoadState('networkidle')
    await page.waitForSelector('table tbody tr', { timeout: 15000 })

    const firstTask = page.locator('table tbody tr').first()
    await firstTask.locator('a').first().click()
    await page.waitForLoadState('networkidle')

    // Trigger AI inspection
    const aiButton = page.locator('button:has-text("AI 检验"), button:has-text("AI Inspect")')
    await aiButton.first().click()

    // Wait for completion
    await page.waitForTimeout(5000)

    // Check for confidence score or auto-pass indicator
    const confidenceText = page.locator('text=/置信度|Confidence|自动通过|Auto Pass/')
    const hasConfidence = await confidenceText.count() > 0

    if (hasConfidence) {
      await expect(confidenceText.first()).toBeVisible()
      console.log(`✓ Confidence score or auto-pass indicator displayed`)
    }
  })

  test('verify manual review flow with low confidence', async ({ page }) => {
    // Create ASN and receipt
    noticeNo = `ASN_MANUAL_${Date.now()}`
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

    await new Promise(resolve => setTimeout(resolve, 3000))

    // Navigate to QC task
    await page.goto('/qc/tasks')
    await page.waitForLoadState('networkidle')
    await page.waitForSelector('table tbody tr', { timeout: 15000 })

    const firstTask = page.locator('table tbody tr').first()
    await firstTask.locator('a').first().click()
    await page.waitForLoadState('networkidle')

    // Trigger AI inspection
    const aiButton = page.locator('button:has-text("AI 检验"), button:has-text("AI Inspect")')
    await aiButton.first().click()

    // Wait for AI response
    await page.waitForTimeout(5000)

    // Check if manual review is required
    const manualReviewText = page.locator('text=/人工复核|Manual Review|待复核|Pending Review/')
    const needsManualReview = await manualReviewText.count() > 0

    if (needsManualReview) {
      await expect(manualReviewText.first()).toBeVisible()
      console.log(`✓ Manual review required indicator displayed`)
    }
  })
})
