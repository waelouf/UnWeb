<template>
  <div class="app-container">
    <!-- Header -->
    <header class="header">
      <div class="logo">
        <img src="/assets/unweb-icon.svg" alt="UnWeb" class="logo-icon" />
        <h1>Un<span class="highlight">Web</span></h1>
      </div>
      <p class="tagline">Convert HTML to Markdown - Un-webbing content, stripping web complexity</p>
    </header>

    <!-- Main Content -->
    <main class="main-content">
      <!-- Left Panel - Input -->
      <div class="panel input-panel">
        <div class="panel-header">
          <div class="tabs">
            <button
              :class="['tab', { active: activeTab === 'paste' }]"
              @click="activeTab = 'paste'"
            >
              Paste HTML
            </button>
            <button
              :class="['tab', { active: activeTab === 'upload' }]"
              @click="activeTab = 'upload'"
            >
              <span class="tab-label">
                <span class="beta-badge">Beta</span>
                <span class="tab-text">Upload File</span>
              </span>
            </button>
            <button
              :class="['tab', { active: activeTab === 'link' }]"
              @click="activeTab = 'link'"
            >
              <span class="tab-label">
                <span class="beta-badge">Beta</span>
                <span class="tab-text">Link</span>
              </span>
            </button>
          </div>
        </div>

        <div class="panel-body">
          <!-- Paste Tab -->
          <div v-if="activeTab === 'paste'" class="tab-content">
            <textarea
              v-model="htmlInput"
              placeholder="Paste your HTML content here..."
              @input="clearOutput"
            ></textarea>
          </div>

          <!-- Upload Tab -->
          <div v-else-if="activeTab === 'upload'" class="tab-content">
            <div
              class="upload-zone"
              :class="{ dragover: isDragging }"
              @drop.prevent="handleDrop"
              @dragover.prevent="isDragging = true"
              @dragleave="isDragging = false"
              @click="triggerFileInput"
            >
              <div v-if="!uploadedFile">
                <svg width="48" height="48" fill="none" stroke="currentColor" viewBox="0 0 24 24" style="margin: 0 auto 16px; color: #a0aec0;">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                </svg>
                <p style="color: #4a5568; margin-bottom: 8px;">Drag & drop your HTML file here</p>
                <p style="color: #a0aec0; font-size: 14px;">or click to browse</p>
                <p style="color: #cbd5e0; font-size: 12px; margin-top: 12px;">Supports .html and .htm files (max 5MB)</p>
              </div>
              <div v-else>
                <svg width="48" height="48" fill="none" stroke="currentColor" viewBox="0 0 24 24" style="margin: 0 auto 16px; color: #667eea;">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                <p style="color: #4a5568; font-weight: 600; margin-bottom: 8px;">{{ uploadedFile.name }}</p>
                <p style="color: #a0aec0; font-size: 14px;">{{ formatFileSize(uploadedFile.size) }}</p>
              </div>
            </div>
            <input
              ref="fileInput"
              type="file"
              accept=".html,.htm"
              style="display: none"
              @change="handleFileSelect"
            />
          </div>

          <!-- Link Tab -->
          <div v-else-if="activeTab === 'link'" class="tab-content">
            <div class="url-input-container">
              <label for="url-input">Enter URL to convert:</label>
              <input
                id="url-input"
                v-model="urlInput"
                type="url"
                placeholder="https://example.com/article"
                @input="clearOutput"
              />
              <p class="help-text">Paste a public URL to fetch and convert HTML to Markdown</p>
            </div>
          </div>
        </div>

        <div class="panel-footer">
          <button class="secondary" @click="clearAll">Clear</button>
          <button class="primary" :disabled="!canConvert || isLoading" @click="convertToMarkdown">
            {{ isLoading ? 'Converting...' : 'Convert to Markdown' }}
          </button>
        </div>
      </div>

      <!-- Right Panel - Output -->
      <div class="panel output-panel">
        <div class="panel-header">
          <h2>Markdown Output</h2>
        </div>

        <div class="panel-body">
          <!-- Error Alert -->
          <div v-if="error" class="alert error">
            <span>{{ error }}</span>
            <button @click="error = null">&times;</button>
          </div>

          <!-- Warning Alert -->
          <div v-if="warnings.length > 0" class="alert warning">
            <div>
              <div v-for="(warning, index) in warnings" :key="index">{{ warning }}</div>
            </div>
            <button @click="warnings = []">&times;</button>
          </div>

          <!-- Output Area -->
          <textarea
            v-model="markdownOutput"
            placeholder="Your markdown will appear here..."
            readonly
          ></textarea>
        </div>

        <div class="panel-footer">
          <button class="secondary" :disabled="!markdownOutput" @click="copyToClipboard">
            {{ copySuccess ? 'Copied!' : 'Copy to Clipboard' }}
          </button>
          <button class="primary" :disabled="!markdownOutput" @click="downloadMarkdown">
            Download .md
          </button>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

// State
const activeTab = ref('paste')
const htmlInput = ref('')
const uploadedFile = ref(null)
const urlInput = ref('')
const markdownOutput = ref('')
const warnings = ref([])
const error = ref(null)
const isLoading = ref(false)
const isDragging = ref(false)
const copySuccess = ref(false)
const fileInput = ref(null)

// Computed
const canConvert = computed(() => {
  if (activeTab.value === 'paste') {
    return htmlInput.value.trim().length > 0
  } else if (activeTab.value === 'upload') {
    return uploadedFile.value !== null
  } else if (activeTab.value === 'link') {
    return urlInput.value.trim().length > 0
  }
  return false
})

// Methods
const clearOutput = () => {
  markdownOutput.value = ''
  warnings.value = []
  error.value = null
}

const clearAll = () => {
  htmlInput.value = ''
  uploadedFile.value = null
  urlInput.value = ''
  clearOutput()
}

const triggerFileInput = () => {
  fileInput.value?.click()
}

const handleFileSelect = (event) => {
  const file = event.target.files?.[0]
  if (file) {
    processFile(file)
  }
}

const handleDrop = (event) => {
  isDragging.value = false
  const file = event.dataTransfer.files?.[0]
  if (file) {
    processFile(file)
  }
}

const processFile = (file) => {
  // Validate file extension
  const allowedExtensions = ['.html', '.htm']
  const fileName = file.name.toLowerCase()
  const hasValidExtension = allowedExtensions.some(ext => fileName.endsWith(ext))

  if (!hasValidExtension) {
    error.value = 'Only .html and .htm files are allowed'
    return
  }

  // Validate file size (5MB)
  const maxSize = 5 * 1024 * 1024
  if (file.size > maxSize) {
    error.value = 'File size exceeds 5MB limit'
    return
  }

  uploadedFile.value = file
  clearOutput()
}

const formatFileSize = (bytes) => {
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

const convertToMarkdown = async () => {
  isLoading.value = true
  error.value = null
  warnings.value = []

  try {
    let response

    if (activeTab.value === 'paste') {
      // Paste mode
      response = await fetch('/api/convert/paste', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ html: htmlInput.value })
      })
    } else if (activeTab.value === 'upload') {
      // Upload mode
      const formData = new FormData()
      formData.append('file', uploadedFile.value)

      response = await fetch('/api/convert/upload', {
        method: 'POST',
        body: formData
      })
    } else if (activeTab.value === 'link') {
      // Link mode
      response = await fetch('/api/convert/url', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ url: urlInput.value })
      })
    }

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ error: 'Conversion failed' }))
      throw new Error(errorData.error || 'Conversion failed')
    }

    const data = await response.json()
    markdownOutput.value = data.markdown
    warnings.value = data.warnings || []
  } catch (err) {
    error.value = err.message || 'An error occurred during conversion'
  } finally {
    isLoading.value = false
  }
}

const copyToClipboard = async () => {
  try {
    await navigator.clipboard.writeText(markdownOutput.value)
    copySuccess.value = true
    setTimeout(() => {
      copySuccess.value = false
    }, 2000)
  } catch (err) {
    error.value = 'Failed to copy to clipboard'
  }
}

const downloadMarkdown = () => {
  const blob = new Blob([markdownOutput.value], { type: 'text/markdown' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'converted.md'
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}
</script>

<style scoped>
.app-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
}

.header {
  background: white;
  border-bottom: 1px solid #e2e8f0;
  padding: 20px 32px;
}

.logo {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 8px;
}

.logo-icon {
  width: 40px;
  height: 40px;
}

.logo h1 {
  font-size: 28px;
  font-weight: 700;
  color: #2d3748;
}

.logo .highlight {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.tagline {
  color: #718096;
  font-size: 14px;
  margin-left: 52px;
}

.main-content {
  flex: 1;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0;
  overflow: hidden;
}

.panel {
  display: flex;
  flex-direction: column;
  background: white;
  border-right: 1px solid #e2e8f0;
}

.output-panel {
  border-right: none;
}

.panel-header {
  padding: 20px 24px;
  border-bottom: 1px solid #e2e8f0;
}

.panel-header h2 {
  font-size: 18px;
  font-weight: 600;
  color: #2d3748;
}

.tabs {
  display: flex;
  gap: 8px;
}

.tab {
  padding: 8px 16px;
  background: transparent;
  color: #718096;
  border: none;
  border-bottom: 2px solid transparent;
  font-weight: 500;
  transition: all 0.2s;
}

.tab:hover {
  color: #4a5568;
}

.tab.active {
  color: #667eea;
  border-bottom-color: #667eea;
}

.panel-body {
  flex: 1;
  padding: 24px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.tab-content {
  height: 100%;
}

.panel-footer {
  padding: 20px 24px;
  border-top: 1px solid #e2e8f0;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

/* Tab label wrapper for flexbox layout */
.tab-label {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
}

/* Tab text - ensures proper spacing */
.tab-text {
  display: inline-block;
}

/* Beta badge - outlined style positioned above */
.beta-badge {
  display: inline-block;
  padding: 1px 4px;
  font-size: 9px;
  font-weight: 700;
  color: #c05621;
  border: 1px solid #c05621;
  border-radius: 3px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  line-height: 1;
}

/* Make badge color match active tab state */
.tab.active .beta-badge {
  color: #667eea;
  border-color: #667eea;
}

/* Soon badge - same style as beta badge */
.soon-badge {
  display: inline-block;
  padding: 1px 4px;
  font-size: 9px;
  font-weight: 700;
  color: #c05621;
  border: 1px solid #c05621;
  border-radius: 3px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  line-height: 1;
}

/* Disabled tab styling */
.tab:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  color: #a0aec0;
}

.tab:disabled:hover {
  color: #a0aec0;
}

/* Disabled tab badge styling */
.tab:disabled .soon-badge {
  color: #a0aec0;
  border-color: #a0aec0;
}

/* URL input container */
.url-input-container {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.url-input-container label {
  font-weight: 600;
  color: #2d3748;
  font-size: 14px;
}

.url-input-container input[type="url"] {
  padding: 12px;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
  font-size: 14px;
  font-family: inherit;
  transition: border-color 0.2s;
}

.url-input-container input[type="url"]:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.help-text {
  font-size: 13px;
  color: #718096;
  margin: 0;
}
</style>
