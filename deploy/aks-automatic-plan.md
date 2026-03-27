# AKS Automatic Deployment Plan

## Goal

Deploy the frontend and backend as separate pods to the AKS Automatic cluster, publish both images to ACR from GitHub Actions, and deploy the release with Helm.

## Current State

- The repository already has separate Dockerfiles for the frontend and backend.
- The Helm chart is intended to create separate frontend and backend Deployments, but the templates are malformed and will not render.
- The existing GitHub Actions workflow pushes images to ACR but does not deploy to AKS.
- The backend reads `DefaultConnection` from Key Vault at startup, so the backend pod must run with an identity that can read secrets from the vault.

## Target Topology

- One frontend Deployment and Service.
- One backend Deployment and Service.
- Frontend exposed externally through a `LoadBalancer` service by default.
- Backend exposed internally through a `ClusterIP` service only.
- Backend pod uses Azure Workload Identity.
- Frontend pod does not receive the backend identity.
- Helm release deployed into a dedicated namespace.

## Helm Changes Required

1. Fix the Helm template syntax in the frontend, backend, service account, ingress, and network policy manifests.
2. Keep frontend and backend as separate Deployments and Services.
3. Move namespace selection to the Helm release namespace instead of a value hardcoded in `values.yaml`.
4. Disable ingress by default so the first AKS Automatic rollout does not depend on an ingress controller or TLS secret.
5. Disable the chart-managed Azure Workload Identity webhook by default. AKS should provide the cluster-side workload identity integration.
6. Disable network policy by default so outbound traffic to Key Vault, Azure SQL, Redis, Search, and Application Insights is not blocked during the first deployment.
7. Keep the frontend pointing to the backend service URL `http://talking-points-backend:8080` inside the cluster.

## GitHub Actions Changes Required

1. Build the frontend image from the repo root `Dockerfile`.
2. Build the backend image from `src/server/Dockerfile.prod`.
3. Authenticate to Azure with GitHub OIDC.
4. Log in to ACR and push both images tagged with the commit SHA and `latest`.
5. Set AKS context.
6. Run `helm lint`.
7. Run `helm upgrade --install` with image repositories, image tags, namespace, and backend identity values.
8. Wait for the release to become ready before the workflow completes.

## Required GitHub Repository Configuration

### Variables

- `ACR_NAME`
- `AKS_CLUSTER_NAME`
- `AKS_RESOURCE_GROUP`
- `AKS_NAMESPACE`
- `HELM_RELEASE_NAME`
- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `BACKEND_WORKLOAD_CLIENT_ID`

### Secrets

- `APPLICATIONINSIGHTS_CONNECTION_STRING`

## Azure Prerequisites

1. The GitHub Actions federated identity must be configured on the Azure app registration used by `azure/login`.
2. The AKS cluster must be attached to the target ACR or otherwise allowed to pull from it.
3. The backend managed identity must have access to Key Vault so startup can resolve `DefaultConnection`.
4. If ingress is enabled later, the cluster must have an ingress controller and a TLS secret available in the target namespace.

## Validation Sequence

1. Run `helm lint ./deploy`.
2. Run the workflow with `workflow_dispatch`.
3. Confirm two Deployments exist in the namespace.
4. Confirm the frontend Service has an external IP or hostname.
5. Confirm the backend Service is internal only.
6. Confirm the backend pod can read Key Vault secrets and start successfully.
7. Confirm the frontend can reach the backend through the cluster service name.

## Rollout Notes

- The first deployment should use the default `LoadBalancer` frontend service and keep ingress disabled.
- Once the basic AKS Automatic deployment is stable, ingress and tighter network policy rules can be added as a second pass.
