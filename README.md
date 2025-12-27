# UnWeb - HTML to Markdown Converter

![UnWeb Logo](logo.svg)

**Un-webbing content, stripping web complexity**

UnWeb converts HTML to clean CommonMark markdown. Perfect for migrating content from HTML-based CMS (Confluence, SharePoint, etc.) to markdown-based systems.

## Features

- **Dual Input Modes**: Paste HTML directly or upload .html/.htm files
- **Smart Content Extraction**: Automatically detects main content from full webpages
- **CommonMark Output**: Clean, standard markdown format
- **Simple Interface**: Two-panel design for easy conversion
- **Download & Copy**: Export markdown or copy to clipboard
- **RESTful API**: Programmatic conversion support

## Tech Stack

- **Frontend**: Vue 3 + nginx (containerized)
- **Backend**: ASP.NET Core .NET 8 (containerized)
- **Libraries**: AngleSharp (HTML parsing), ReverseMarkdown (conversion)

## Quick Start

The fastest way to run UnWeb is with Docker Compose using pre-built images:

```bash
curl -O https://raw.githubusercontent.com/waelouf/unweb/main/docker/docker-compose.yml
docker-compose -f docker-compose.yml up
```

Access at `http://localhost:8081`

That's it! 🎉

## Deployment Options

### Option 1: Docker Compose (Recommended)

**Using pre-built images from Docker Hub:**
```bash
# Download docker-compose.registry.yml from this repo
docker-compose -f docker-compose.yml up
```


### Option 2: Kubernetes

**Quick deploy (all-in-one):**
```bash
kubectl apply -f https://raw.githubusercontent.com/waelouf/unweb/main/kubernetes/all-in-one.yaml
```

**Prerequisites:**
- Kubernetes cluster with nginx ingress controller
- Update `unweb.example.com` in ingress.yaml to your domain

See [kubernetes/README.md](kubernetes/README.md) for detailed Kubernetes deployment guide.

## Architecture

UnWeb uses a two-container architecture:

- **Frontend Container**: nginx serving Vue 3 SPA (port 80)
- **Backend Container**: ASP.NET Core API (port 8080)
- **Communication**: Path-based routing via Ingress/proxy (`/api/*` → backend, `/*` → frontend)

## API Endpoints

### POST `/api/convert/paste`

Convert HTML from JSON body.

**Request:**
```json
{
  "html": "<h1>Hello</h1><p>World</p>"
}
```

**Response:**
```json
{
  "markdown": "# Hello\n\nWorld",
  "warnings": []
}
```

### POST `/api/convert/upload`

Convert HTML from uploaded file (.html, .htm, max 5MB).

**Request:** multipart/form-data with `file` field

**Response:** Same as `/api/convert/paste`

### GET /health

Health check endpoint.

**Response:**
```json
{
  "status": "healthy"
}
```

## Configuration

Configure max file size in Kubernetes ConfigMap or backend environment:

```json
{
  "ConversionSettings": {
    "MaxFileSizeBytes": 5242880
  }
}
```

## Docker Images

Pre-built images available on Docker Hub:
- `waelouf/unweb-frontend:latest`
- `waelouf/unweb-backend:latest`


## Changelog

See [CHANGELOG.md](CHANGELOG.md) for release history.

## License

MIT

## Support

For issues and questions, please open an issue in this repository.
