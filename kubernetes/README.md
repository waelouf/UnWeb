# Kubernetes Deployment Guide

Deploy UnWeb on Kubernetes with path-based ingress routing.

## Prerequisites

- Kubernetes cluster 1.21+ (minikube, GKE, EKS, AKS, etc.)
- kubectl configured for your cluster
- nginx ingress controller installed
- (Optional) cert-manager for TLS certificates

## Quick Start

Deploy everything with one command:

```bash
kubectl apply -f https://raw.githubusercontent.com/waelouf/unweb/main/kubernetes/all-in-one.yaml
```

**Before deploying**, update the ingress hostname:

1. Download the all-in-one.yaml file
2. Replace `unweb.example.com` with your actual domain
3. Apply: `kubectl apply -f all-in-one.yaml`

## Architecture

UnWeb Kubernetes deployment includes:

- **Frontend Deployment**: 2 replicas, nginx serving Vue 3 SPA
- **Backend Deployment**: 2 replicas, ASP.NET Core API
- **Frontend Service**: ClusterIP service exposing frontend pods
- **Backend Service**: ClusterIP service exposing backend pods
- **Ingress**: Path-based routing with TLS support
  - `/api/*` → Backend Service
  - `/*` → Frontend Service
- **ConfigMap**: Application configuration

## Deployment Options

### Option 1: All-in-One Deployment

Recommended for quick setup - deploys all resources from a single file.

```bash
# Download the file
curl -O https://raw.githubusercontent.com/waelouf/unweb/main/kubernetes/all-in-one.yaml

# Edit the ingress hostname
# Replace "unweb.example.com" with your domain

# Deploy
kubectl apply -f all-in-one.yaml
```

### Option 2: Individual Manifests

Recommended for customization - deploy each resource separately.

Deploy in this order:

```bash
kubectl apply -f configmap.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
kubectl apply -f ingress.yaml
```

## Configuration

### Update Ingress Hostname

Edit the ingress resource (in `ingress.yaml` or `all-in-one.yaml`):

```yaml
spec:
  rules:
  - host: unweb.yourdomain.com  # Change this to your domain
```

### Update Docker Images

To use specific versions instead of `:latest`:

Edit `deployment.yaml`:

```yaml
containers:
- name: frontend
  image: waelouf/unweb-frontend:v1.0.0  # Specific version
- name: backend
  image: waelouf/unweb-backend:v1.0.0   # Specific version
```

### Adjust Resource Limits

Edit resource requests/limits in `deployment.yaml`:

```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

### Change Replica Count

Edit `deployment.yaml`:

```yaml
spec:
  replicas: 3  # Increase for higher availability
```

### Configure Max File Size

Edit `configmap.yaml`:

```yaml
data:
  appsettings.json: |
    {
      "ConversionSettings": {
        "MaxFileSizeBytes": 10485760  # 10MB instead of 5MB
      }
    }
```

## Monitoring

### Check Deployment Status

```bash
kubectl get deployments
kubectl get pods
kubectl get services
kubectl get ingress
```

### View Detailed Status

```bash
# Deployment details
kubectl describe deployment unweb-frontend
kubectl describe deployment unweb-backend

# Pod details
kubectl describe pod <pod-name>

# Ingress details
kubectl describe ingress unweb-ingress
```

### View Logs

```bash
# Frontend logs (all replicas)
kubectl logs -f deployment/unweb-frontend

# Backend logs (all replicas)
kubectl logs -f deployment/unweb-backend

# Specific pod
kubectl logs -f <pod-name>

# Previous container logs (if pod crashed)
kubectl logs -f <pod-name> --previous
```

### Check Health

Port-forward to test locally:

```bash
# Frontend
kubectl port-forward service/unweb-frontend-service 8081:80
# Then visit: http://localhost:8081/health

# Backend
kubectl port-forward service/unweb-backend-service 8080:80
# Then visit: http://localhost:8080/health
```

## Scaling

### Manual Scaling

```bash
# Scale frontend
kubectl scale deployment unweb-frontend --replicas=3

# Scale backend
kubectl scale deployment unweb-backend --replicas=5
```

### Horizontal Pod Autoscaler

Create HPA for automatic scaling based on CPU:

```bash
kubectl autoscale deployment unweb-backend --cpu-percent=70 --min=2 --max=10
kubectl autoscale deployment unweb-frontend --cpu-percent=70 --min=2 --max=5
```

View HPA status:

```bash
kubectl get hpa
```

## Updating

### Update to New Image Version

```bash
# Update frontend
kubectl set image deployment/unweb-frontend frontend=waelouf/unweb-frontend:v1.1.0

# Update backend
kubectl set image deployment/unweb-backend backend=waelouf/unweb-backend:v1.1.0
```

### Rollback if Needed

```bash
# View rollout history
kubectl rollout history deployment/unweb-frontend
kubectl rollout history deployment/unweb-backend

# Rollback to previous version
kubectl rollout undo deployment/unweb-frontend
kubectl rollout undo deployment/unweb-backend

# Rollback to specific revision
kubectl rollout undo deployment/unweb-backend --to-revision=2
```

### Check Rollout Status

```bash
kubectl rollout status deployment/unweb-frontend
kubectl rollout status deployment/unweb-backend
```

## TLS/HTTPS Setup

### With cert-manager (Recommended)

The ingress includes cert-manager annotation for automatic TLS:

```yaml
annotations:
  cert-manager.io/cluster-issuer: letsencrypt-prod
```

**Install cert-manager:**

```bash
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml
```

**Create ClusterIssuer:**

Create `clusterissuer.yaml`:

```yaml
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: your-email@example.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
```

Apply:

```bash
kubectl apply -f clusterissuer.yaml
```

cert-manager will automatically provision and renew TLS certificates.

### Manual TLS Certificate

If not using cert-manager, create a TLS secret:

```bash
kubectl create secret tls unweb-tls \
  --cert=path/to/tls.crt \
  --key=path/to/tls.key
```

## Troubleshooting

### Pods Not Starting

**Check pod status:**

```bash
kubectl get pods
kubectl describe pod <pod-name>
```

**Common issues:**
- Image pull errors: Verify images exist on Docker Hub
- Resource limits: Check cluster has enough CPU/memory
- ConfigMap missing: Ensure configmap is created first

### Ingress Not Routing

**Check ingress controller:**

```bash
# Verify ingress controller is running
kubectl get pods -n ingress-nginx

# Check ingress controller logs
kubectl logs -n ingress-nginx <ingress-controller-pod>
```

**Check ingress details:**

```bash
kubectl describe ingress unweb-ingress
```

**Common issues:**
- Ingress class mismatch
- DNS not pointing to ingress IP
- Path rewrite rules incorrect

### Image Pull Errors

Verify images exist on Docker Hub:

```bash
docker pull waelouf/unweb-frontend:latest
docker pull waelouf/unweb-backend:latest
```

If using private registry, create image pull secret:

```bash
kubectl create secret docker-registry regcred \
  --docker-server=<your-registry-server> \
  --docker-username=<your-name> \
  --docker-password=<your-password>
```

### Cannot Access via Domain

1. **Check DNS**: Verify domain points to ingress IP
   ```bash
   kubectl get ingress unweb-ingress
   nslookup unweb.yourdomain.com
   ```

2. **Check ingress controller**: Ensure it's receiving traffic
   ```bash
   kubectl logs -n ingress-nginx <ingress-pod>
   ```

3. **Verify TLS certificate**: If using HTTPS
   ```bash
   kubectl get certificate
   kubectl describe certificate unweb-tls
   ```

## Cleanup

### Delete All Resources

**All-in-one deployment:**

```bash
kubectl delete -f all-in-one.yaml
```

**Individual manifests:**

```bash
kubectl delete -f ingress.yaml
kubectl delete -f service.yaml
kubectl delete -f deployment.yaml
kubectl delete -f configmap.yaml
```

### Delete Specific Resources

```bash
kubectl delete deployment unweb-frontend
kubectl delete deployment unweb-backend
kubectl delete service unweb-frontend-service
kubectl delete service unweb-backend-service
kubectl delete ingress unweb-ingress
kubectl delete configmap unweb-config
```

## Production Best Practices

1. **Use Specific Image Tags**: Avoid `:latest` in production
   ```yaml
   image: waelouf/unweb-frontend:v1.0.0
   ```

2. **Enable TLS**: Set up cert-manager with Let's Encrypt

3. **Set Resource Limits**: Prevent resource exhaustion
   ```yaml
   resources:
     limits:
       memory: "512Mi"
       cpu: "500m"
   ```

4. **Use Namespaces**: Isolate UnWeb resources
   ```bash
   kubectl create namespace unweb
   kubectl apply -f all-in-one.yaml -n unweb
   ```

5. **Configure Monitoring**: Use Prometheus, Grafana, or cloud-native tools

6. **Set Up Logging**: Centralized logging with ELK stack or cloud services

7. **Health Probes**: Already configured in deployment manifests

8. **Configure Network Policies**: Restrict pod-to-pod communication
   ```yaml
   apiVersion: networking.k8s.io/v1
   kind: NetworkPolicy
   metadata:
     name: unweb-network-policy
   spec:
     podSelector:
       matchLabels:
         app: unweb
     policyTypes:
     - Ingress
   ```

9. **Use LoadBalancer or NodePort**: For cloud deployments without ingress
   ```yaml
   spec:
     type: LoadBalancer  # or NodePort
   ```

10. **Regular Backups**: If storing conversion history (future feature)

## Advanced Configuration

### Use Different Ingress Class

Edit ingress annotation:

```yaml
metadata:
  annotations:
    kubernetes.io/ingress.class: "traefik"  # Instead of nginx
```

### Add Custom Annotations

For specific ingress controller features:

```yaml
metadata:
  annotations:
    nginx.ingress.kubernetes.io/rate-limit: "100"
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
```

### Multiple Environments

Deploy to different namespaces:

```bash
# Development
kubectl apply -f all-in-one.yaml -n unweb-dev

# Production
kubectl apply -f all-in-one.yaml -n unweb-prod
```

## Getting Help

- Check pod logs: `kubectl logs <pod-name>`
- Describe resources: `kubectl describe <resource-type> <resource-name>`
- Check events: `kubectl get events --sort-by='.lastTimestamp'`
- Kubernetes documentation: https://kubernetes.io/docs/
