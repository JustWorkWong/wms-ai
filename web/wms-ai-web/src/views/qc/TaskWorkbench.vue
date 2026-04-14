<template>
  <div class="task-workbench">
    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="task" class="workbench-content">
      <div class="task-header">
        <h2>质检任务: {{ task.id }}</h2>
        <router-link to="/qc/tasks" class="back-btn">返回列表</router-link>
      </div>

      <div class="task-info-card">
        <h3>任务信息</h3>
        <div class="info-grid">
          <div class="info-item">
            <label>收货单号:</label>
            <span>{{ task.receiptId }}</span>
          </div>
          <div class="info-item">
            <label>SKU:</label>
            <span>{{ task.skuCode }}</span>
          </div>
          <div class="info-item">
            <label>数量:</label>
            <span>{{ task.quantity }}</span>
          </div>
          <div class="info-item">
            <label>状态:</label>
            <span :class="['status-badge', task.status]">
              {{ getStatusText(task.status) }}
            </span>
          </div>
        </div>
      </div>

      <div class="evidence-card">
        <h3>证据上传</h3>
        <EvidenceUploader @upload="handleEvidenceUpload" />
      </div>

      <div class="ai-card">
        <h3>AI 检验助手</h3>
        <AiChatPanel
          :session-id="sessionId"
          @suggestion="handleAiSuggestion"
        />
      </div>

      <div v-if="aiSuggestion" class="suggestion-card">
        <h3>AI 建议</h3>
        <div class="suggestion-content">
          <div class="suggestion-decision">
            建议: <strong>{{ aiSuggestion.suggestion === 'accept' ? '通过' : '拒绝' }}</strong>
          </div>
          <div class="suggestion-confidence">
            置信度: {{ (aiSuggestion.confidence * 100).toFixed(1) }}%
          </div>
          <div class="suggestion-reasoning">
            理由: {{ aiSuggestion.reasoning }}
          </div>
        </div>
      </div>

      <div class="decision-card">
        <h3>检验结论</h3>
        <div class="decision-buttons">
          <button @click="acceptTask" class="accept-btn" :disabled="submitting">
            通过
          </button>
          <button @click="rejectTask" class="reject-btn" :disabled="submitting">
            拒绝
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useQcStore } from '@/stores/qc'
import { useAuthStore } from '@/stores/auth'
import { storeToRefs } from 'pinia'
import { aiApi } from '@/api/ai'
import { qcApi } from '@/api/qc'
import EvidenceUploader from '@/components/EvidenceUploader.vue'
import AiChatPanel from '@/components/AiChatPanel.vue'
import type { AiSuggestion } from '@/types/qc'

const route = useRoute()
const router = useRouter()
const qcStore = useQcStore()
const authStore = useAuthStore()
const { currentTask: task, loading, error } = storeToRefs(qcStore)

const sessionId = ref<string | null>(null)
const aiSuggestion = ref<AiSuggestion | null>(null)
const submitting = ref(false)
const evidenceUrls = ref<string[]>([])

onMounted(async () => {
  const taskId = route.params.id as string
  await qcStore.fetchTask(taskId)

  if (task.value) {
    try {
      const response = await aiApi.createSession('inbound_inspection', task.value.id)
      sessionId.value = response.data.id
    } catch (err) {
      console.error('Failed to create AI session:', err)
    }
  }
})

const handleEvidenceUpload = async (files: File[]) => {
  if (!task.value) return

  try {
    const response = await qcApi.uploadEvidence(task.value.id, files)
    evidenceUrls.value = response.data.map(e => e.fileUrl)
    console.log('Evidence uploaded:', evidenceUrls.value)
  } catch (err) {
    console.error('Failed to upload evidence:', err)
  }
}

const handleAiSuggestion = (suggestion: AiSuggestion) => {
  aiSuggestion.value = suggestion
}

const acceptTask = async () => {
  if (!task.value) return

  submitting.value = true
  try {
    await qcApi.finalizeDecision(task.value.id, {
      taskId: task.value.id,
      decision: 'accept',
      evidenceUrls: evidenceUrls.value,
      inspectedBy: authStore.userId,
      inspectedAt: new Date().toISOString()
    })
    router.push('/qc/tasks')
  } catch (err) {
    console.error('Failed to accept task:', err)
  } finally {
    submitting.value = false
  }
}

const rejectTask = async () => {
  if (!task.value) return

  submitting.value = true
  try {
    await qcApi.finalizeDecision(task.value.id, {
      taskId: task.value.id,
      decision: 'reject',
      reason: aiSuggestion.value?.reasoning || '质量不合格',
      evidenceUrls: evidenceUrls.value,
      inspectedBy: authStore.userId,
      inspectedAt: new Date().toISOString()
    })
    router.push('/qc/tasks')
  } catch (err) {
    console.error('Failed to reject task:', err)
  } finally {
    submitting.value = false
  }
}

const getStatusText = (status: string) => {
  const statusMap: Record<string, string> = {
    pending: '待检验',
    in_progress: '检验中',
    completed: '已完成',
    rejected: '已拒绝'
  }
  return statusMap[status] || status
}
</script>

<style scoped>
.task-workbench {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
}

.loading,
.error {
  text-align: center;
  padding: 2rem;
  color: #666;
}

.error {
  color: #ff4d4f;
}

.workbench-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.task-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.task-header h2 {
  font-size: 24px;
  font-weight: 600;
  color: #333;
}

.back-btn {
  padding: 0.5rem 1rem;
  background: #f0f0f0;
  color: #333;
  text-decoration: none;
  border-radius: 4px;
  font-size: 14px;
}

.back-btn:hover {
  background: #e0e0e0;
}

.task-info-card,
.evidence-card,
.ai-card,
.suggestion-card,
.decision-card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.task-info-card h3,
.evidence-card h3,
.ai-card h3,
.suggestion-card h3,
.decision-card h3 {
  font-size: 18px;
  font-weight: 600;
  color: #333;
  margin-bottom: 1rem;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.info-item label {
  font-size: 14px;
  color: #666;
}

.info-item span {
  font-size: 16px;
  color: #333;
}

.status-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 500;
  display: inline-block;
  width: fit-content;
}

.status-badge.pending {
  background: #fff7e6;
  color: #fa8c16;
}

.status-badge.in_progress {
  background: #e6f7ff;
  color: #1890ff;
}

.status-badge.completed {
  background: #f6ffed;
  color: #52c41a;
}

.status-badge.rejected {
  background: #fff1f0;
  color: #ff4d4f;
}

.suggestion-content {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.suggestion-decision {
  font-size: 16px;
}

.suggestion-confidence {
  font-size: 14px;
  color: #666;
}

.suggestion-reasoning {
  font-size: 14px;
  color: #333;
  padding: 0.75rem;
  background: #f9f9f9;
  border-radius: 4px;
}

.decision-buttons {
  display: flex;
  gap: 1rem;
}

.accept-btn,
.reject-btn {
  padding: 0.75rem 2rem;
  border: none;
  border-radius: 4px;
  font-size: 16px;
  cursor: pointer;
  font-weight: 500;
}

.accept-btn {
  background: #52c41a;
  color: white;
}

.accept-btn:hover:not(:disabled) {
  background: #73d13d;
}

.reject-btn {
  background: #ff4d4f;
  color: white;
}

.reject-btn:hover:not(:disabled) {
  background: #ff7875;
}

.accept-btn:disabled,
.reject-btn:disabled {
  background: #d9d9d9;
  cursor: not-allowed;
}
</style>
