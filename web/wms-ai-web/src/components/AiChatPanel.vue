<template>
  <div class="ai-chat-panel">
    <div class="messages" ref="messagesContainer">
      <div
        v-for="msg in messages"
        :key="msg.id"
        :class="['message', msg.role]"
      >
        <div class="message-content">{{ msg.content }}</div>
        <div class="message-time">{{ formatTime(msg.timestamp) }}</div>
      </div>
    </div>

    <div class="input-area">
      <input
        v-model="userInput"
        @keyup.enter="sendMessage"
        placeholder="输入消息..."
        :disabled="!sessionId"
      />
      <button @click="sendMessage" :disabled="!sessionId || !userInput.trim()">
        发送
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import { aiApi } from '@/api/ai'
import type { AiStreamEvent } from '@/types/ai'

const props = defineProps<{
  sessionId: string | null
}>()

const emit = defineEmits<{
  suggestion: [suggestion: any]
}>()

interface Message {
  id: string
  role: 'user' | 'assistant'
  content: string
  timestamp: string
}

const messages = ref<Message[]>([])
const userInput = ref('')
const messagesContainer = ref<HTMLElement>()
let eventSource: EventSource | null = null

onMounted(() => {
  if (props.sessionId) {
    connectToStream()
  }
})

onUnmounted(() => {
  eventSource?.close()
})

const connectToStream = () => {
  if (!props.sessionId) return

  const streamUrl = aiApi.getStreamUrl(props.sessionId)
  eventSource = new EventSource(streamUrl)

  eventSource.addEventListener('message', (e) => {
    const event: AiStreamEvent = JSON.parse(e.data)
    if (event.content) {
      messages.value.push({
        id: crypto.randomUUID(),
        role: 'assistant',
        content: event.content,
        timestamp: new Date().toISOString()
      })
      scrollToBottom()
    }
  })

  eventSource.addEventListener('status', (e) => {
    const event: AiStreamEvent = JSON.parse(e.data)
    if (event.status === 'completed' && event.suggestion) {
      emit('suggestion', event.suggestion)
    }
  })

  eventSource.onerror = (error) => {
    console.error('SSE Error:', error)
  }
}

const sendMessage = async () => {
  if (!userInput.value.trim() || !props.sessionId) return

  const messageContent = userInput.value
  messages.value.push({
    id: crypto.randomUUID(),
    role: 'user',
    content: messageContent,
    timestamp: new Date().toISOString()
  })

  userInput.value = ''
  scrollToBottom()

  try {
    await aiApi.sendMessage(props.sessionId, messageContent)
  } catch (error) {
    console.error('Failed to send message:', error)
  }
}

const scrollToBottom = async () => {
  await nextTick()
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
  }
}

const formatTime = (timestamp: string) => {
  return new Date(timestamp).toLocaleTimeString('zh-CN', {
    hour: '2-digit',
    minute: '2-digit'
  })
}
</script>

<style scoped>
.ai-chat-panel {
  display: flex;
  flex-direction: column;
  height: 500px;
  border: 1px solid #ddd;
  border-radius: 8px;
  background: #fff;
}

.messages {
  flex: 1;
  overflow-y: auto;
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.message {
  display: flex;
  flex-direction: column;
  max-width: 70%;
}

.message.user {
  align-self: flex-end;
}

.message.user .message-content {
  background: #1890ff;
  color: white;
  border-radius: 12px 12px 0 12px;
}

.message.assistant {
  align-self: flex-start;
}

.message.assistant .message-content {
  background: #f0f0f0;
  color: #333;
  border-radius: 12px 12px 12px 0;
}

.message-content {
  padding: 0.75rem 1rem;
  word-wrap: break-word;
}

.message-time {
  font-size: 0.75rem;
  color: #999;
  margin-top: 0.25rem;
  padding: 0 0.5rem;
}

.input-area {
  display: flex;
  padding: 1rem;
  border-top: 1px solid #ddd;
  gap: 0.5rem;
}

.input-area input {
  flex: 1;
  padding: 0.5rem 1rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
}

.input-area input:focus {
  outline: none;
  border-color: #1890ff;
}

.input-area button {
  padding: 0.5rem 1.5rem;
  background: #1890ff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
}

.input-area button:hover:not(:disabled) {
  background: #40a9ff;
}

.input-area button:disabled {
  background: #d9d9d9;
  cursor: not-allowed;
}
</style>
