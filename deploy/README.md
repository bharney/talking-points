# Talking Points Helm Chart

This Helm chart deploys the Talking Points application to Kubernetes, including:

- Frontend (Next.js)
- Backend (.NET Core)
- Service Account for Azure Workload Identity
- Optional ingress with TLS support
- Network policies for security

## Prerequisites

- Kubernetes cluster (AKS recommended)
- Helm 3.x
- ACR access for the AKS cluster
- Azure Workload Identity configured for the backend identity
- NGINX Ingress Controller and TLS secret if ingress is enabled

## Installation

1. **Install the chart:**

   ```bash
   helm install talking-points ./deploy --namespace talking-points --create-namespace
   ```

2. **Install with custom values:**

   ```bash
   helm install talking-points ./deploy -f custom-values.yaml --namespace talking-points --create-namespace
   ```

3. **Upgrade the release:**
   ```bash
   helm upgrade talking-points ./deploy --namespace talking-points
   ```

## Configuration

The following table lists the configurable parameters and their default values:

| Parameter                               | Description                          | Default                                                  |
| --------------------------------------- | ------------------------------------ | -------------------------------------------------------- |
| `frontend.image.repository`             | Frontend image repository            | `starterpackregistry.azurecr.io/talking-points-frontend` |
| `frontend.image.tag`                    | Frontend image tag                   | `latest`                                                 |
| `frontend.service.type`                 | Frontend service type                | `LoadBalancer`                                           |
| `backend.image.repository`              | Backend image repository             | `starterpackregistry.azurecr.io/talking-points-backend`  |
| `backend.image.tag`                     | Backend image tag                    | `latest`                                                 |
| `serviceAccount.annotations`            | Backend workload identity annotation | `{}`                                                     |
| `ingress.enabled`                       | Enable ingress                       | `false`                                                  |
| `networkPolicy.enabled`                 | Enable network policy                | `false`                                                  |
| `azureWorkloadIdentity.webhook.enabled` | Install chart-managed webhook        | `false`                                                  |

## Azure Workload Identity

The backend Deployment is prepared to use Azure Workload Identity through the configured service account annotation. The chart-managed webhook is disabled by default.

- The backend pod carries the `azure.workload.identity/use: "true"` label.
- The frontend pod does not receive the backend identity.

If you explicitly want the chart to attempt to install the webhook resources, you can enable it:

```bash
helm install talking-points ./deploy \
  --set azureWorkloadIdentity.webhook.enabled=true
```

**Prerequisites for Azure Workload Identity:**

1. AKS cluster with Workload Identity enabled
2. Managed identity or application identity for the backend workload
3. Federated identity credential configured between the backend service account and Azure AD app

## TLS Setup

Before deploying with ingress enabled, create a TLS secret:

```bash
kubectl create secret tls talking-points-tls \
  --cert=tls.crt --key=tls.key --namespace=talking-points
```

## Uninstallation

```bash
helm uninstall talking-points
```

## Examples

### Deploy with different image tags:

```bash
helm install talking-points ./deploy --namespace talking-points --create-namespace \
  --set frontend.image.tag=v1.2.0 \
  --set backend.image.tag=v1.2.0
```

### Deploy without network policy:

```bash
helm install talking-points ./deploy --namespace talking-points --create-namespace \
  --set networkPolicy.enabled=false
```

### Deploy with custom API URL:

```bash
helm install talking-points ./deploy --namespace talking-points --create-namespace \
  --set frontend.env.NEXT_PUBLIC_API_URL="http://my-backend:8080"
```

## GitHub Actions Deployment

The repository includes a workflow that:

- Builds the frontend and backend images
- Pushes both images to ACR with the commit SHA and `latest`
- Runs `helm lint`
- Deploys the Helm release to AKS with `helm upgrade --install`

Set these repository variables before enabling the workflow:

- `ACR_NAME`
- `AKS_CLUSTER_NAME`
- `AKS_RESOURCE_GROUP`
- `AKS_NAMESPACE`
- `HELM_RELEASE_NAME`
- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `BACKEND_WORKLOAD_CLIENT_ID`

Set this repository secret:

- `APPLICATIONINSIGHTS_CONNECTION_STRING`
