import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AiChatPanel from '../AiChatPanel.vue'

describe('AiChatPanel', () => {
  it('renders the chat panel', () => {
    const wrapper = mount(AiChatPanel, {
      props: { sessionId: 'test-session-123' }
    })
    expect(wrapper.find('.ai-chat-panel').exists()).toBe(true)
  })

  it('displays input area', () => {
    const wrapper = mount(AiChatPanel, {
      props: { sessionId: 'test-session-123' }
    })
    expect(wrapper.find('.input-area').exists()).toBe(true)
    expect(wrapper.find('input').exists()).toBe(true)
    expect(wrapper.find('button').exists()).toBe(true)
  })

  it('disables input when no session', () => {
    const wrapper = mount(AiChatPanel, {
      props: { sessionId: null }
    })
    const input = wrapper.find('input')
    expect(input.attributes('disabled')).toBeDefined()
  })
})
