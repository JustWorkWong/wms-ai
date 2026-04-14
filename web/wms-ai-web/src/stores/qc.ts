import { defineStore } from 'pinia'
import { qcApi } from '@/api/qc'
import type { QcTask } from '@/types/qc'

export const useQcStore = defineStore('qc', {
  state: () => ({
    tasks: [] as QcTask[],
    currentTask: null as QcTask | null,
    loading: false,
    error: null as string | null
  }),

  actions: {
    async fetchTasks() {
      this.loading = true
      this.error = null
      try {
        const response = await qcApi.getTasks()
        this.tasks = response.data
      } catch (error) {
        this.error = 'Failed to fetch tasks'
        console.error(error)
      } finally {
        this.loading = false
      }
    },

    async fetchTask(id: string) {
      this.loading = true
      this.error = null
      try {
        const response = await qcApi.getTask(id)
        this.currentTask = response.data
      } catch (error) {
        this.error = 'Failed to fetch task'
        console.error(error)
      } finally {
        this.loading = false
      }
    },

    clearCurrentTask() {
      this.currentTask = null
    }
  }
})
