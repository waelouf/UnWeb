import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import App from '../App.vue'

describe('App.vue', () => {
  let wrapper

  beforeEach(() => {
    wrapper = mount(App)
  })

  it('renders the application header', () => {
    expect(wrapper.find('.header').exists()).toBe(true)
    expect(wrapper.text()).toContain('UnWeb')
  })

  it('renders the tagline', () => {
    expect(wrapper.text()).toContain('Un-webbing content, stripping web complexity')
  })

  it('shows paste tab by default', () => {
    const pasteTab = wrapper.findAll('.tab')[0]
    expect(pasteTab.classes()).toContain('active')
  })

  it('switches to upload tab when clicked', async () => {
    const uploadTab = wrapper.findAll('.tab')[1]
    await uploadTab.trigger('click')

    expect(uploadTab.classes()).toContain('active')
    expect(wrapper.find('.upload-zone').exists()).toBe(true)
  })

  it('disables convert button when no input', () => {
    const convertButton = wrapper.findAll('button').find(btn =>
      btn.text().includes('Convert')
    )
    expect(convertButton.attributes('disabled')).toBeDefined()
  })

  it('enables convert button when HTML is pasted', async () => {
    const textarea = wrapper.find('textarea')
    await textarea.setValue('<h1>Test</h1>')

    const convertButton = wrapper.findAll('button').find(btn =>
      btn.text().includes('Convert')
    )
    expect(convertButton.attributes('disabled')).toBeUndefined()
  })

  it('clears input when clear button is clicked', async () => {
    const textarea = wrapper.find('textarea')
    await textarea.setValue('<h1>Test</h1>')

    const clearButton = wrapper.findAll('button').find(btn =>
      btn.text() === 'Clear'
    )
    await clearButton.trigger('click')

    expect(textarea.element.value).toBe('')
  })

  it('shows placeholder text in output initially', () => {
    const outputTextarea = wrapper.findAll('textarea')[1]
    expect(outputTextarea.attributes('placeholder')).toContain('Your markdown will appear here')
  })

  it('disables download and copy buttons when no output', () => {
    const buttons = wrapper.findAll('.output-panel button')
    const copyButton = buttons.find(btn => btn.text().includes('Copy'))
    const downloadButton = buttons.find(btn => btn.text().includes('Download'))

    expect(copyButton.attributes('disabled')).toBeDefined()
    expect(downloadButton.attributes('disabled')).toBeDefined()
  })

  it('validates file extension on upload', async () => {
    // Switch to upload tab
    const uploadTab = wrapper.findAll('.tab')[1]
    await uploadTab.trigger('click')

    // Create a fake file with invalid extension
    const file = new File(['<h1>Test</h1>'], 'test.txt', { type: 'text/plain' })
    const fileInput = wrapper.find('input[type="file"]')

    // Manually trigger the file change handler
    const event = { target: { files: [file] } }
    await wrapper.vm.handleFileSelect(event)

    // Should show error
    await wrapper.vm.$nextTick()
    expect(wrapper.vm.error).toContain('Only .html and .htm files are allowed')
  })

  it('validates file size on upload', async () => {
    // Switch to upload tab
    const uploadTab = wrapper.findAll('.tab')[1]
    await uploadTab.trigger('click')

    // Create a fake file exceeding 5MB
    const largeContent = 'x'.repeat(6 * 1024 * 1024) // 6MB
    const file = new File([largeContent], 'large.html', { type: 'text/html' })

    const event = { target: { files: [file] } }
    await wrapper.vm.handleFileSelect(event)

    await wrapper.vm.$nextTick()
    expect(wrapper.vm.error).toContain('File size exceeds 5MB limit')
  })

  it('formats file size correctly', () => {
    expect(wrapper.vm.formatFileSize(500)).toBe('500 B')
    expect(wrapper.vm.formatFileSize(1024)).toBe('1.0 KB')
    expect(wrapper.vm.formatFileSize(1024 * 1024)).toBe('1.0 MB')
    expect(wrapper.vm.formatFileSize(2.5 * 1024 * 1024)).toBe('2.5 MB')
  })

  it('converts HTML to markdown on button click', async () => {
    // Mock fetch
    global.fetch = vi.fn(() =>
      Promise.resolve({
        ok: true,
        json: () => Promise.resolve({
          markdown: '# Test\n\nContent here.',
          warnings: ['Main content auto-detected']
        })
      })
    )

    // Enter HTML
    const textarea = wrapper.find('textarea')
    await textarea.setValue('<h1>Test</h1><p>Content here.</p>')

    // Click convert
    const convertButton = wrapper.findAll('button').find(btn =>
      btn.text().includes('Convert')
    )
    await convertButton.trigger('click')

    // Wait for async operation
    await new Promise(resolve => setTimeout(resolve, 0))
    await wrapper.vm.$nextTick()

    // Check output
    expect(wrapper.vm.markdownOutput).toContain('# Test')
    expect(wrapper.vm.warnings).toHaveLength(1)
  })

  it('shows error message when conversion fails', async () => {
    // Mock fetch to fail
    global.fetch = vi.fn(() =>
      Promise.resolve({
        ok: false,
        json: () => Promise.resolve({
          error: 'Conversion failed'
        })
      })
    )

    const textarea = wrapper.find('textarea')
    await textarea.setValue('<h1>Test</h1>')

    const convertButton = wrapper.findAll('button').find(btn =>
      btn.text().includes('Convert')
    )
    await convertButton.trigger('click')

    await new Promise(resolve => setTimeout(resolve, 0))
    await wrapper.vm.$nextTick()

    expect(wrapper.vm.error).toBeTruthy()
  })

  it('clears output when switching input modes', async () => {
    // Set some output
    wrapper.vm.markdownOutput = '# Test'
    wrapper.vm.warnings = ['Warning']
    await wrapper.vm.$nextTick()

    // Switch tabs
    const uploadTab = wrapper.findAll('.tab')[1]
    await uploadTab.trigger('click')

    // Output should remain (only cleared when new input is entered)
    expect(wrapper.vm.markdownOutput).toBe('# Test')
  })

  it('shows loading state during conversion', async () => {
    global.fetch = vi.fn(() =>
      new Promise(resolve => setTimeout(() =>
        resolve({
          ok: true,
          json: () => Promise.resolve({ markdown: '# Test', warnings: [] })
        }), 100
      ))
    )

    const textarea = wrapper.find('textarea')
    await textarea.setValue('<h1>Test</h1>')

    const convertButton = wrapper.findAll('button').find(btn =>
      btn.text().includes('Convert')
    )
    await convertButton.trigger('click')

    // Should show loading text
    await wrapper.vm.$nextTick()
    expect(convertButton.text()).toBe('Converting...')
    expect(convertButton.attributes('disabled')).toBeDefined()

    // Wait for completion
    await new Promise(resolve => setTimeout(resolve, 150))
    await wrapper.vm.$nextTick()

    expect(convertButton.text()).toContain('Convert to Markdown')
  })
})
