# Talking Points Helm Chart

This Helm chart deploys the Talking Points application to Kubernetes, including:

- Frontend (Next.js)
- Backend (.NET Core)
- Service Account for Azure Workload Identity
- Ingress with TLS support
- Network policies for security

## Prerequisites

- Kubernetes cluster (AKS recommended)
- Helm 3.x
- NGINX Ingress Controller
- TLS secret for HTTPS (if using ingress)

## Installation

1. **Install the chart:**

   ```bash
   helm install talking-points ./deploy
   ```

2. **Install with custom values:**

   ```bash
   helm install talking-points ./deploy -f custom-values.yaml
   ```

3. **Upgrade the release:**
   ```bash
   helm upgrade talking-points ./deploy
   ```

## Configuration

The following table lists the configurable parameters and their default values:

| Parameter                   | Description               | Default                                                  |
| --------------------------- | ------------------------- | -------------------------------------------------------- |
| `global.namespace`          | Kubernetes namespace      | `default`                                                |
| `frontend.image.repository` | Frontend image repository | `starterpackregistry.azurecr.io/talking-points-frontend` |
| `frontend.image.tag`        | Frontend image tag        | `latest`                                                 |
| `backend.image.repository`  | Backend image repository  | `starterpackregistry.azurecr.io/talking-points-backend`  |
| `backend.image.tag`         | Backend image tag         | `latest`                                                 |
| `ingress.enabled`           | Enable ingress            | `true`                                                   |
| `networkPolicy.enabled`     | Enable network policy     | `true`                                                   |

## Azure Workload Identity

This chart automatically sets up Azure Workload Identity webhook when enabled. The webhook:

- Automatically injects Azure AD tokens into pods with the `azure.workload.identity/use: "true"` label
- Generates TLS certificates for secure webhook communication
- Configures the necessary RBAC permissions

Azure Workload Identity is enabled by default in this chart. To disable it:

```bash
helm install talking-points ./deploy \
  --set azureWorkloadIdentity.webhook.enabled=false
```

**Prerequisites for Azure Workload Identity:**

1. AKS cluster with Workload Identity enabled
2. Azure AD application/service principal
3. Federated identity credential configured between your service account and Azure AD app

## TLS Setup

Before deploying with ingress enabled, create a TLS secret:

```bash
kubectl create secret tls talking-points-tls \
  --cert=tls.crt --key=tls.key --namespace=default
```

## Uninstallation

```bash
helm uninstall talking-points
```

## Examples

### Deploy with different image tags:

```bash
helm install talking-points ./deploy \
  --set frontend.image.tag=v1.2.0 \
  --set backend.image.tag=v1.2.0
```

### Deploy without network policy:

```bash
helm install talking-points ./deploy \
  --set networkPolicy.enabled=false
```

### Deploy with custom API URL:

```bash
helm install talking-points ./deploy \
  --set frontend.env.NEXT_PUBLIC_API_URL="http://my-backend:8080"
```
