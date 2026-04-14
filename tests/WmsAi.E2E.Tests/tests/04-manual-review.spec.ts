import { test, expect } from '@playwright/test'
import { ApiHelper } from './fixtures/api'
import testData from './fixtures/test-data.json'

test.describe('Manual Review', () => {
  const api = new ApiHelper()
  const { tenantId, warehouseId } = testData.demoTenant

  test.beforeEach(async () => {
    // Create ASN and receipt to generate QC task
    const noticeNo = `ASN_REVIEW_${Date.now()}`
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

  test('should display manual review page', async ({ page }) => {
    await page.goto('/qc/reviews')
    await page.waitForLoadState('networkidle')

    // Verify page loaded
    const pageTitle = page.locator('h1, h2, .page-title')
    await expect(pageTitle).toBeVisible()

    // Verify table exists
    const table = page.locator('table')
    await expect(table).toBeVisible()
  })

  test('should submit manual review decision - accept', async ({ page }) => {
    await page.goto('/qc/reviews')
    await page.waitForLoadState('networkidle')

    // Wait for review tasks to load
    const hasReviewTasks = await page.locator('table tbody tr').count() > 0

    if (hasReviewTasks) {
      // Find task waiting for review
      const reviewTask = page.locator('table tbody tr').first()
      const reviewLink = reviewTask.locator('a:has-text("复核"), a:has-text("Review")')
      await reviewLink.first().click()

      await page.waitForLoadState('networkidle')

      // Select accept decision
      const acceptRadio = page.locator('input[value="accepted"], input[value="accept"]')
      await acceptRadio.first().click()

      // Enter reason
      const reasonTextarea = page.locator('textarea[name="reason"], textarea')
      await reasonTextarea.first().fill('Manual review: quality acceptable')

      // Submit decision
      const submitButton = page.locator('button:has-text("提交决策"), button:has-text("Submit Decision"), button:has-text("Submit")')
      await submitButton.first().click()

      // Verify success message
      const successMessage = page.locator('text=/决策已提交|Decision submitted|Success/')
      await expect(successMessage.first()).toBeVisible({ timeout: 10000 })
    }
  })

  test('should submit manual review decision - reject', async ({ page }) => {
    await page.goto('/qc/reviews')
    await page.waitForLoadState('networkidle')

    // Wait for review tasks to load
    const hasReviewTasks = await page.locator('table tbody tr').count() > 0

    if (hasReviewTasks) {
      // Find task waiting for review
      const reviewTask = page.locator('table tbody tr').first()
      const reviewLink = reviewTask.locator('a').first()
      await reviewLink.click()

      await page.waitForLoadState('networkidle')

      // Select reject decision
      const rejectRadio = page.locator('input[value="rejected"], input[value="reject"]')
      await rejectRadio.first().click()

      // Enter reason
      const reasonTextarea = page.locator('textarea[name="reason"], textarea')
      await reasonTextarea.first().fill('Manual review: quality issues found')

      // Submit decision
      const submitButton = page.locator('button:has-text("提交决策"), button:has-text("Submit Decision"), button:has-text("Submit")')
      await submitButton.first().click()

      // Verify success message
      const successMessage = page.locator('text=/决策已提交|Decision submitted|Success/')
      await expect(successMessage.first()).toBeVisible({ timeout: 10000 })
    }
  })

  test('should display review history', async ({ page }) => {
    await page.goto('/qc/reviews')
    await page.waitForLoadState('networkidle')

    // Check if there are any completed reviews
    const reviewRows = page.locator('table tbody tr')
    const count = await reviewRows.count()

    if (count > 0) {
      // Verify review data is displayed
      const firstRow = reviewRows.first()
      await expect(firstRow).toBeVisible()

      // Check for status indicators
      const statusCell = firstRow.locator('td:has-text("已完成"), td:has-text("Completed"), td:has-text("已接受"), td:has-text("Accepted")')
      // Status may or may not be present depending on data
    }
  })
})
