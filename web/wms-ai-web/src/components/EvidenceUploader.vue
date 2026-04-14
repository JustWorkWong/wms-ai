<template>
  <div class="evidence-uploader">
    <input
      type="file"
      multiple
      accept="image/*"
      @change="handleFileSelect"
      ref="fileInput"
      style="display: none"
    />
    <button @click="triggerFileSelect" class="upload-btn">
      选择文件
    </button>

    <div v-if="files.length > 0" class="preview-list">
      <div v-for="(file, index) in files" :key="index" class="preview-item">
        <img :src="getPreviewUrl(file)" alt="preview" />
        <span class="file-name">{{ file.name }}</span>
        <button @click="removeFile(index)" class="remove-btn">×</button>
      </div>
    </div>

    <button
      v-if="files.length > 0"
      @click="uploadFiles"
      :disabled="uploading"
      class="submit-btn"
    >
      {{ uploading ? '上传中...' : '上传证据' }}
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const emit = defineEmits<{
  upload: [files: File[]]
}>()

const fileInput = ref<HTMLInputElement>()
const files = ref<File[]>([])
const uploading = ref(false)

const triggerFileSelect = () => {
  fileInput.value?.click()
}

const handleFileSelect = (event: Event) => {
  const target = event.target as HTMLInputElement
  if (target.files) {
    files.value = [...files.value, ...Array.from(target.files)]
  }
}

const removeFile = (index: number) => {
  files.value.splice(index, 1)
}

const getPreviewUrl = (file: File) => {
  return URL.createObjectURL(file)
}

const uploadFiles = () => {
  if (files.value.length === 0) return
  uploading.value = true
  emit('upload', files.value)
  setTimeout(() => {
    uploading.value = false
    files.value = []
  }, 1000)
}
</script>

<style scoped>
.evidence-uploader {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.upload-btn {
  padding: 0.5rem 1rem;
  background: #1890ff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  align-self: flex-start;
}

.upload-btn:hover {
  background: #40a9ff;
}

.preview-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  gap: 1rem;
}

.preview-item {
  position: relative;
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 0.5rem;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.preview-item img {
  width: 100%;
  height: 120px;
  object-fit: cover;
  border-radius: 4px;
}

.file-name {
  margin-top: 0.5rem;
  font-size: 12px;
  color: #666;
  text-align: center;
  word-break: break-all;
}

.remove-btn {
  position: absolute;
  top: 0.25rem;
  right: 0.25rem;
  width: 24px;
  height: 24px;
  background: rgba(0, 0, 0, 0.6);
  color: white;
  border: none;
  border-radius: 50%;
  cursor: pointer;
  font-size: 18px;
  line-height: 1;
  display: flex;
  align-items: center;
  justify-content: center;
}

.remove-btn:hover {
  background: rgba(0, 0, 0, 0.8);
}

.submit-btn {
  padding: 0.75rem 1.5rem;
  background: #52c41a;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  align-self: flex-start;
}

.submit-btn:hover:not(:disabled) {
  background: #73d13d;
}

.submit-btn:disabled {
  background: #d9d9d9;
  cursor: not-allowed;
}
</style>
