# Changelog

All notable changes to UnWeb will be documented in this file.

## [1.1.0] - 2025-12-29

### Added

- **URL Fetching**: New `POST /api/convert/url` endpoint to convert web pages by URL
  - Fetch HTML from any public URL and convert to markdown
  - 60-second timeout for URL fetching
  - 10MB content size limit
  - Content-Type validation (text/html only)
- **Security Features**:
  - SSRF (Server-Side Request Forgery) protection
  - Blocks private IP ranges (10.x.x.x, 172.16-31.x.x, 192.168.x.x, 127.x.x.x)
  - Blocks localhost and loopback addresses
  - Protocol validation (HTTP/HTTPS only)
- **Comprehensive Error Handling**:
  - HTTP 400 for invalid URLs and unsupported protocols
  - HTTP 403 for blocked private IPs
  - HTTP 413 for oversized content
  - HTTP 415 for non-HTML content types
  - HTTP 502 for network failures
  - HTTP 504 for timeouts
- **Integration Tests**: 7 new tests covering URL endpoint validation and security
- **UI Enhancements**:
  - Link tab now enabled with Beta badge
  - New logo design featuring `</>` symbol with "UnWeb" branding
  - Gradient opacity effects on logo elements

### Changed

- Updated from .NET 8 to .NET 10
- Input modes changed from "Dual" to "Triple" (paste, upload, URL)
- Logo updated from bracket symbols to HTML closing tag `</>`

## [1.0.0] - 2025-12-27

### Initial Public Release

#### Features

- **HTML to Markdown Conversion**: Convert HTML to CommonMark markdown with high accuracy
- **Dual Input Modes**:
  - Paste HTML directly into web interface
  - Upload .html/.htm files (max 5MB)
- **Smart Content Extraction**: Automatic detection of main content using:
  - Semantic HTML5 tags (`<main>`, `<article>`, `[role='main']`)
  - Content scoring algorithm for non-semantic HTML
  - Excludes navigation, footer, sidebar elements
- **Simple Web Interface**: Two-panel Vue 3 UI with real-time conversion
- **Export Options**: Download as .md file or copy to clipboard
- **RESTful API**: Two endpoints for programmatic conversion
  - `POST /api/convert/paste` - Convert from JSON body
  - `POST /api/convert/upload` - Convert from file upload
- **Health Monitoring**: Health check endpoint for container orchestration

#### Deployment

- **Docker Support**: Pre-built images on Docker Hub
  - Frontend: nginx 1.27-alpine serving Vue 3 SPA
  - Backend: ASP.NET Core .NET 8 API
- **Docker Compose**: Ready-to-use configurations
  - Build from source option
  - Pre-built images option from Docker Hub
- **Kubernetes**: Complete manifests with:
  - 2 replica deployments for high availability
  - Path-based ingress routing (`/api/*` → backend, `/*` → frontend)
  - Resource limits and health probes
  - ConfigMap for configuration management
  - All-in-one deployment file for quick setup

**Content Extraction Algorithm:**
- Prioritizes semantic HTML5 tags
- Scores content based on text length and paragraph density
- Excludes navigation, footer, sidebar elements
- Penalizes high link density
