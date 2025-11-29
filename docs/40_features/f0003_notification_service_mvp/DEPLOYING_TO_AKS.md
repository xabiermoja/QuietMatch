# Deploying NotificationService to Azure Kubernetes Service (AKS)

**Complete guide** from zero to production deployment on Azure Kubernetes Service.

## 📋 Table of Contents

1. [Key Concepts](#key-concepts)
2. [Prerequisites](#prerequisites)
3. [Architecture Overview](#architecture-overview)
4. [Step-by-Step Deployment](#step-by-step-deployment)
5. [Kubernetes Manifests](#kubernetes-manifests)
6. [Secrets Management](#secrets-management)
7. [Configuration Management](#configuration-management)
8. [Networking & Ingress](#networking--ingress)
9. [Message Broker Options](#message-broker-options)
10. [Monitoring & Logging](#monitoring--logging)
11. [CI/CD Pipeline](#cicd-pipeline)
12. [Cost Optimization](#cost-optimization)
13. [Troubleshooting](#troubleshooting)

---

## Key Concepts

### What is AKS?

**Azure Kubernetes Service (AKS)** is Microsoft's managed Kubernetes offering. It handles:
- Kubernetes control plane (free!)
- Node management and upgrades
- Azure integration (networking, storage, identity)
- Autoscaling

**You pay only for:** Worker nodes (VMs where your containers run)

### Core Kubernetes Concepts for This Service

**Pod:**
- Smallest deployable unit
- Contains one or more containers
- Our NotificationService runs in a Pod
- Example: 1 Pod = 1 instance of NotificationService

**Deployment:**
- Manages Pods
- Handles rolling updates
- Ensures desired number of replicas
- Example: "Run 3 replicas of NotificationService"

**Service:**
- Stable network endpoint for Pods
- Load balances across replicas
- Types: ClusterIP (internal), LoadBalancer (external)
- Example: `notification-service.default.svc.cluster.local`

**ConfigMap:**
- Non-sensitive configuration
- Example: Template paths, feature flags

**Secret:**
- Sensitive configuration
- Example: SendGrid API key, database passwords
- **Azure Key Vault integration recommended**

**Ingress:**
- HTTP/HTTPS routing
- SSL termination
- Example: `https://api.quietmatch.com/notifications`

**Namespace:**
- Logical cluster isolation
- Example: `dev`, `staging`, `production`

### Why Kubernetes for NotificationService?

✅ **Scalability** - Auto-scale based on message queue depth
✅ **Resilience** - Self-healing, automatic restarts
✅ **Zero-downtime deployments** - Rolling updates
✅ **Resource efficiency** - Pack multiple services on same nodes
✅ **Cloud portability** - Works on Azure, AWS, GCP
✅ **Declarative config** - Infrastructure as code

---

## Prerequisites

### Azure Account & Tools

**1. Azure Subscription**
```bash
# Check if you have access
az account show
```

**2. Azure CLI**
```bash
# Install (macOS)
brew install azure-cli

# Login
az login

# Set subscription
az account set --subscription "YOUR_SUBSCRIPTION_ID"
```

**3. kubectl (Kubernetes CLI)**
```bash
# Install
brew install kubectl

# Verify
kubectl version --client
```

**4. Docker**
```bash
# Check
docker --version
```

**5. Helm (optional, for easier deployments)**
```bash
# Install
brew install helm

# Verify
helm version
```

### SendGrid API Key

Same as local development - get free API key from https://app.sendgrid.com/

### Azure Container Registry (ACR)

We'll push Docker images here (Azure's private Docker registry).

---

## Architecture Overview

### What We're Deploying

```
┌─────────────────────────────────────────────────────────────┐
│                         AKS Cluster                         │
│                                                             │
│  ┌────────────────────────────────────────────────────┐   │
│  │              Ingress Controller                     │   │
│  │          (nginx or Azure App Gateway)               │   │
│  │  https://api.quietmatch.com/notifications          │   │
│  └───────────────────┬─────────────────────────────────┘   │
│                      │                                      │
│  ┌───────────────────▼─────────────────────────────────┐   │
│  │         NotificationService Deployment              │   │
│  │                                                      │   │
│  │  ┌─────────┐  ┌─────────┐  ┌─────────┐            │   │
│  │  │  Pod 1  │  │  Pod 2  │  │  Pod 3  │ (replicas) │   │
│  │  │ Port    │  │ Port    │  │ Port    │            │   │
│  │  │ 8080    │  │ 8080    │  │ 8080    │            │   │
│  │  └─────────┘  └─────────┘  └─────────┘            │   │
│  │                                                      │   │
│  │  ConfigMap: Email__Provider=SendGrid                │   │
│  │  Secret: SendGrid API Key (from Key Vault)         │   │
│  └──────────────────┬───────────────────────────────────┘   │
│                     │                                       │
│  ┌──────────────────▼──────────────────────────────────┐   │
│  │      Azure Service Bus (or RabbitMQ Pod)            │   │
│  │      Queues: notification-service                   │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
                      │
                      ▼
          ┌───────────────────────┐
          │   Azure Key Vault     │
          │   (SendGrid API Key)  │
          └───────────────────────┘
                      │
                      ▼
          ┌───────────────────────┐
          │   SendGrid API        │
          │   (Email Delivery)    │
          └───────────────────────┘
```

### Azure Resources Needed

| Resource | Purpose | Approx. Cost (US East) |
|----------|---------|------------------------|
| AKS Cluster | Run containers | $70-150/month (2-3 nodes) |
| Azure Container Registry | Store Docker images | $5/month (Basic tier) |
| Azure Key Vault | Secrets management | $0.03/10k operations |
| Azure Service Bus | Message queue (optional) | $10/month (Basic tier) |
| Application Gateway | Ingress (optional) | $125/month + data |
| Azure Monitor | Logging/metrics | Pay-per-GB ingested |

**Total estimated:** $150-300/month for production setup

---

## Step-by-Step Deployment

### Step 1: Create AKS Cluster

```bash
# Set variables
RESOURCE_GROUP="rg-quietmatch-prod"
CLUSTER_NAME="aks-quietmatch-prod"
LOCATION="eastus"
ACR_NAME="acrquietmatchprod"  # Must be globally unique

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Create Azure Container Registry (for Docker images)
az acr create \
  --resource-group $RESOURCE_GROUP \
  --name $ACR_NAME \
  --sku Basic

# Create AKS cluster
az aks create \
  --resource-group $RESOURCE_GROUP \
  --name $CLUSTER_NAME \
  --node-count 2 \
  --node-vm-size Standard_B2s \
  --enable-managed-identity \
  --attach-acr $ACR_NAME \
  --generate-ssh-keys

# This takes 5-10 minutes
```

**What this creates:**
- AKS cluster with 2 worker nodes
- Managed identity for accessing ACR
- Virtual network for the cluster
- Load balancer for external access

### Step 2: Connect to AKS Cluster

```bash
# Get credentials
az aks get-credentials \
  --resource-group $RESOURCE_GROUP \
  --name $CLUSTER_NAME

# Verify connection
kubectl get nodes

# Expected output:
# NAME                                STATUS   ROLES   AGE   VERSION
# aks-nodepool1-12345678-vmss000000   Ready    agent   5m    v1.28.3
# aks-nodepool1-12345678-vmss000001   Ready    agent   5m    v1.28.3
```

### Step 3: Build and Push Docker Image

```bash
# Login to ACR
az acr login --name $ACR_NAME

# Build image (from NotificationService directory)
cd ~/code/QuietMatch/src/Services/Notification

docker build -t notification-service:latest .

# Tag for ACR
docker tag notification-service:latest \
  $ACR_NAME.azurecr.io/notification-service:v1.0.0

# Push to ACR
docker push $ACR_NAME.azurecr.io/notification-service:v1.0.0

# Verify
az acr repository list --name $ACR_NAME --output table
```

### Step 4: Create Azure Key Vault for Secrets

```bash
# Create Key Vault
KEYVAULT_NAME="kv-quietmatch-prod"  # Must be globally unique

az keyvault create \
  --resource-group $RESOURCE_GROUP \
  --name $KEYVAULT_NAME \
  --location $LOCATION

# Store SendGrid API key
az keyvault secret set \
  --vault-name $KEYVAULT_NAME \
  --name "SendGridApiKey" \
  --value "SG.your_actual_api_key_here"

# Grant AKS access to Key Vault (using managed identity)
# Get AKS managed identity
AKS_IDENTITY=$(az aks show \
  --resource-group $RESOURCE_GROUP \
  --name $CLUSTER_NAME \
  --query identityProfile.kubeletidentity.clientId -o tsv)

# Grant access
az keyvault set-policy \
  --name $KEYVAULT_NAME \
  --object-id $AKS_IDENTITY \
  --secret-permissions get list
```

### Step 5: Install Azure Key Vault Provider for Secrets Store CSI Driver

This allows Kubernetes pods to read secrets from Azure Key Vault.

```bash
# Enable CSI driver on AKS
az aks enable-addons \
  --resource-group $RESOURCE_GROUP \
  --name $CLUSTER_NAME \
  --addons azure-keyvault-secrets-provider

# Verify installation
kubectl get pods -n kube-system -l app=secrets-store-csi-driver
```

### Step 6: Create Kubernetes Namespace

```bash
# Create production namespace
kubectl create namespace production

# Set as default for convenience
kubectl config set-context --current --namespace=production
```

### Step 7: Deploy NotificationService

See [Kubernetes Manifests](#kubernetes-manifests) section below for YAML files.

```bash
# Apply all manifests
kubectl apply -f k8s/

# Expected output:
# configmap/notification-config created
# secretproviderclass.secrets-store.csi.x-k8s.io/azure-keyvault created
# deployment.apps/notification-service created
# service/notification-service created
# horizontalpodautoscaler.autoscaling/notification-service-hpa created
```

### Step 8: Verify Deployment

```bash
# Check pods are running
kubectl get pods -l app=notification-service

# Expected:
# NAME                                    READY   STATUS    RESTARTS   AGE
# notification-service-5d7c8f9b4d-abc12   1/1     Running   0          30s
# notification-service-5d7c8f9b4d-def34   1/1     Running   0          30s

# Check logs
kubectl logs -l app=notification-service --tail=50

# Should see:
# ✅ Email Provider: SendGrid (from: noreply@quietmatch.com)
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://[::]:8080

# Check service
kubectl get service notification-service

# Port-forward to test locally
kubectl port-forward svc/notification-service 5003:80

# Test in another terminal
curl http://localhost:5003/health
```

---

## Kubernetes Manifests

Create a `k8s/` directory in your NotificationService folder:

```bash
mkdir -p ~/code/QuietMatch/src/Services/Notification/k8s
```

### 1. ConfigMap (notification-configmap.yaml)

Stores non-sensitive configuration.

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: notification-config
  namespace: production
data:
  # Email provider selection
  Email__Provider: "SendGrid"

  # SendGrid configuration (non-sensitive)
  Email__SendGrid__FromEmail: "noreply@quietmatch.com"
  Email__SendGrid__FromName: "QuietMatch"

  # Template configuration
  Templates__BasePath: "/app/Templates"

  # RabbitMQ connection (if using RabbitMQ pod in cluster)
  RabbitMQ__Host: "rabbitmq-service"
  RabbitMQ__Port: "5672"
  RabbitMQ__Username: "guest"
  RabbitMQ__Password: "guest"

  # Or Azure Service Bus (recommended for production)
  # AzureServiceBus__ConnectionString: "set-via-secret"

  # ASP.NET Core settings
  ASPNETCORE_ENVIRONMENT: "Production"
  ASPNETCORE_URLS: "http://+:8080"
```

### 2. Secret Provider (secret-provider.yaml)

Links Azure Key Vault to Kubernetes.

```yaml
apiVersion: secrets-store.csi.x-k8s.io/v1
kind: SecretProviderClass
metadata:
  name: azure-keyvault
  namespace: production
spec:
  provider: azure
  parameters:
    usePodIdentity: "false"
    useVMManagedIdentity: "true"
    userAssignedIdentityID: ""  # Leave empty for system-assigned identity
    keyvaultName: "kv-quietmatch-prod"  # Your Key Vault name
    cloudName: ""
    objects: |
      array:
        - |
          objectName: SendGridApiKey
          objectType: secret
          objectVersion: ""
    tenantId: "YOUR_TENANT_ID"  # Get from: az account show --query tenantId -o tsv
  secretObjects:
  - secretName: sendgrid-api-key
    type: Opaque
    data:
    - objectName: SendGridApiKey
      key: apiKey
```

### 3. Deployment (notification-deployment.yaml)

Main application deployment.

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: notification-service
  namespace: production
  labels:
    app: notification-service
    version: v1.0.0
spec:
  replicas: 2  # Start with 2 replicas
  selector:
    matchLabels:
      app: notification-service
  template:
    metadata:
      labels:
        app: notification-service
        version: v1.0.0
    spec:
      containers:
      - name: notification-service
        image: acrquietmatchprod.azurecr.io/notification-service:v1.0.0
        imagePullPolicy: Always
        ports:
        - containerPort: 8080
          name: http
          protocol: TCP

        # Environment variables from ConfigMap
        envFrom:
        - configMapRef:
            name: notification-config

        # Sensitive environment variables from Secret
        env:
        - name: Email__SendGrid__ApiKey
          valueFrom:
            secretKeyRef:
              name: sendgrid-api-key
              key: apiKey

        # Health checks
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 30
          timeoutSeconds: 5
          failureThreshold: 3

        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
          timeoutSeconds: 3
          failureThreshold: 3

        # Resource limits (important for cost control!)
        resources:
          requests:
            cpu: 100m      # 0.1 CPU cores
            memory: 128Mi  # 128 MB
          limits:
            cpu: 500m      # 0.5 CPU cores max
            memory: 512Mi  # 512 MB max

        # Mount secrets from Key Vault
        volumeMounts:
        - name: secrets-store
          mountPath: "/mnt/secrets-store"
          readOnly: true

      volumes:
      - name: secrets-store
        csi:
          driver: secrets-store.csi.k8s.io
          readOnly: true
          volumeAttributes:
            secretProviderClass: "azure-keyvault"
```

### 4. Service (notification-service.yaml)

Exposes the deployment internally.

```yaml
apiVersion: v1
kind: Service
metadata:
  name: notification-service
  namespace: production
  labels:
    app: notification-service
spec:
  type: ClusterIP  # Internal only (use Ingress for external access)
  selector:
    app: notification-service
  ports:
  - port: 80
    targetPort: 8080
    protocol: TCP
    name: http
```

### 5. Horizontal Pod Autoscaler (notification-hpa.yaml)

Auto-scales based on CPU usage.

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: notification-service-hpa
  namespace: production
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: notification-service
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70  # Scale up when CPU > 70%
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80  # Scale up when Memory > 80%
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300  # Wait 5 min before scaling down
      policies:
      - type: Percent
        value: 50
        periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 0  # Scale up immediately
      policies:
      - type: Percent
        value: 100
        periodSeconds: 30
```

### 6. Ingress (notification-ingress.yaml)

Exposes service to the internet.

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: notification-ingress
  namespace: production
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod  # For SSL
    nginx.ingress.kubernetes.io/rewrite-target: /$2
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - api.quietmatch.com
    secretName: quietmatch-tls
  rules:
  - host: api.quietmatch.com
    http:
      paths:
      - path: /notifications(/|$)(.*)
        pathType: Prefix
        backend:
          service:
            name: notification-service
            port:
              number: 80
```

---

## Secrets Management

### Best Practice: Azure Key Vault

**Why not Kubernetes Secrets?**
- Kubernetes Secrets are base64-encoded (NOT encrypted!)
- Visible to anyone with cluster access
- Stored in etcd (needs encryption at rest)

**Azure Key Vault Benefits:**
- Hardware security modules (HSM) backed
- Audit logging (who accessed what secret when)
- Automatic rotation
- Centralized secret management
- Compliance (SOC 2, HIPAA, etc.)

### How It Works

```
┌─────────────────┐
│  Kubernetes Pod │
│                 │
│  ┌───────────┐  │
│  │ Container │──┼──┐
│  └───────────┘  │  │
│                 │  │
│  Volume Mount:  │  │
│  /mnt/secrets/  │  │
└─────────────────┘  │
                     │ CSI Driver
                     ▼
          ┌──────────────────┐
          │ Azure Key Vault  │
          │                  │
          │ SendGridApiKey   │
          │ DbPassword       │
          └──────────────────┘
```

### Setup Steps

**1. Create Key Vault Secret**
```bash
az keyvault secret set \
  --vault-name $KEYVAULT_NAME \
  --name "SendGridApiKey" \
  --value "SG.your_api_key_here"
```

**2. Enable AKS Managed Identity Access**
```bash
# Get AKS identity
AKS_IDENTITY=$(az aks show \
  --resource-group $RESOURCE_GROUP \
  --name $CLUSTER_NAME \
  --query identityProfile.kubeletidentity.objectId -o tsv)

# Grant read access
az keyvault set-policy \
  --name $KEYVAULT_NAME \
  --object-id $AKS_IDENTITY \
  --secret-permissions get list
```

**3. Create SecretProviderClass** (see YAML above)

**4. Mount in Pod** (see Deployment YAML above)

### Verify Secret Access

```bash
# Exec into pod
kubectl exec -it deployment/notification-service -- /bin/sh

# Check mounted secret
ls /mnt/secrets-store
# Should show: SendGridApiKey

cat /mnt/secrets-store/SendGridApiKey
# Should show your API key
```

---

## Configuration Management

### Strategy: 12-Factor App Principles

**1. ConfigMaps for Non-Sensitive Config**
- Email provider selection
- Feature flags
- Template paths
- Log levels

**2. Secrets for Sensitive Config**
- API keys
- Connection strings
- Passwords

**3. Environment-Specific Overlays**

Use Kustomize or Helm for managing multiple environments:

```
k8s/
├── base/
│   ├── deployment.yaml
│   ├── service.yaml
│   └── kustomization.yaml
├── overlays/
│   ├── dev/
│   │   ├── configmap.yaml  # Email__Provider: Console
│   │   └── kustomization.yaml
│   ├── staging/
│   │   ├── configmap.yaml  # Email__Provider: SendGrid
│   │   ├── replica-patch.yaml  # replicas: 2
│   │   └── kustomization.yaml
│   └── production/
│       ├── configmap.yaml  # Email__Provider: SendGrid
│       ├── replica-patch.yaml  # replicas: 5
│       └── kustomization.yaml
```

**Deploy to dev:**
```bash
kubectl apply -k k8s/overlays/dev
```

**Deploy to production:**
```bash
kubectl apply -k k8s/overlays/production
```

### Configuration Updates

```bash
# Update ConfigMap
kubectl edit configmap notification-config

# Restart pods to pick up changes
kubectl rollout restart deployment/notification-service
```

---

## Networking & Ingress

### Internal vs External Access

**ClusterIP (Internal):**
- Default service type
- Only accessible from within cluster
- Use for service-to-service communication

**LoadBalancer (External):**
- Creates Azure Load Balancer (costs ~$20/month)
- Gets public IP
- Good for simple setups

**Ingress (Recommended for Production):**
- Single load balancer for many services
- Path-based routing: `/notifications`, `/users`, `/matches`
- SSL termination
- Better cost efficiency

### Install NGINX Ingress Controller

```bash
# Add Helm repo
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

# Install NGINX Ingress
helm install nginx-ingress ingress-nginx/ingress-nginx \
  --namespace ingress-nginx \
  --create-namespace \
  --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"=/healthz

# Get external IP (takes 2-3 minutes)
kubectl get svc -n ingress-nginx

# Output:
# NAME                                 TYPE           EXTERNAL-IP
# nginx-ingress-ingress-nginx-controller   LoadBalancer   20.62.xxx.xxx
```

### Configure DNS

Point your domain to the ingress external IP:

```bash
# Get IP
INGRESS_IP=$(kubectl get svc -n ingress-nginx nginx-ingress-ingress-nginx-controller \
  -o jsonpath='{.status.loadBalancer.ingress[0].ip}')

echo "Create DNS A record:"
echo "api.quietmatch.com -> $INGRESS_IP"
```

In your DNS provider (Cloudflare, Route53, etc.):
```
Type: A
Name: api.quietmatch.com
Value: <INGRESS_IP>
TTL: 300
```

### Install Cert-Manager for SSL

```bash
# Install cert-manager
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# Create Let's Encrypt issuer
cat <<EOF | kubectl apply -f -
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: admin@quietmatch.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
EOF
```

Now your Ingress will automatically get SSL certificates!

---

## Message Broker Options

### Option 1: RabbitMQ Pod in AKS (Simple)

**Pros:**
- Familiar (same as local dev)
- No additional Azure costs
- Quick setup

**Cons:**
- Stateful (needs persistent volume)
- Manual backups
- Not highly available by default

**Deploy RabbitMQ:**

```yaml
# rabbitmq-deployment.yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: rabbitmq
spec:
  serviceName: rabbitmq
  replicas: 1
  selector:
    matchLabels:
      app: rabbitmq
  template:
    metadata:
      labels:
        app: rabbitmq
    spec:
      containers:
      - name: rabbitmq
        image: rabbitmq:3.13-management-alpine
        ports:
        - containerPort: 5672
          name: amqp
        - containerPort: 15672
          name: management
        env:
        - name: RABBITMQ_DEFAULT_USER
          value: "admin"
        - name: RABBITMQ_DEFAULT_PASS
          valueFrom:
            secretKeyRef:
              name: rabbitmq-secret
              key: password
        volumeMounts:
        - name: rabbitmq-data
          mountPath: /var/lib/rabbitmq
  volumeClaimTemplates:
  - metadata:
      name: rabbitmq-data
    spec:
      accessModes: ["ReadWriteOnce"]
      storageClassName: managed-premium
      resources:
        requests:
          storage: 10Gi
---
apiVersion: v1
kind: Service
metadata:
  name: rabbitmq-service
spec:
  selector:
    app: rabbitmq
  ports:
  - port: 5672
    name: amqp
  - port: 15672
    name: management
```

### Option 2: Azure Service Bus (Recommended for Production)

**Pros:**
- Fully managed (no maintenance)
- Highly available (99.9% SLA)
- Built-in monitoring
- Scales automatically
- RBAC integration

**Cons:**
- Azure-specific (vendor lock-in)
- Costs ~$10-100/month
- Requires code changes (MassTransit supports it!)

**Create Azure Service Bus:**

```bash
# Create namespace
SERVICE_BUS_NS="sb-quietmatch-prod"

az servicebus namespace create \
  --resource-group $RESOURCE_GROUP \
  --name $SERVICE_BUS_NS \
  --location $LOCATION \
  --sku Standard

# Create queue
az servicebus queue create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $SERVICE_BUS_NS \
  --name notification-service

# Get connection string
az servicebus namespace authorization-rule keys list \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $SERVICE_BUS_NS \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString -o tsv

# Store in Key Vault
az keyvault secret set \
  --vault-name $KEYVAULT_NAME \
  --name "ServiceBusConnectionString" \
  --value "<connection_string_from_above>"
```

**Update Program.cs:**

MassTransit already supports Azure Service Bus! Just change configuration:

```csharp
// Instead of RabbitMQ:
x.UsingAzureServiceBus((context, cfg) =>
{
    cfg.Host(connectionString);  // From configuration

    cfg.ReceiveEndpoint("notification-service", e =>
    {
        e.ConfigureConsumer<UserRegisteredConsumer>(context);
        e.ConfigureConsumer<ProfileCompletedConsumer>(context);
    });
});
```

**Update ConfigMap:**

```yaml
data:
  MessageBroker__Type: "AzureServiceBus"
  # Connection string comes from Secret
```

---

## Monitoring & Logging

### Azure Monitor & Application Insights

**1. Create Application Insights:**

```bash
# Create Log Analytics Workspace
WORKSPACE_NAME="law-quietmatch-prod"

az monitor log-analytics workspace create \
  --resource-group $RESOURCE_GROUP \
  --workspace-name $WORKSPACE_NAME \
  --location $LOCATION

# Create Application Insights
APPINSIGHTS_NAME="ai-quietmatch-prod"

az monitor app-insights component create \
  --app $APPINSIGHTS_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --workspace $WORKSPACE_NAME

# Get instrumentation key
APPINSIGHTS_KEY=$(az monitor app-insights component show \
  --app $APPINSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey -o tsv)

# Store in Key Vault
az keyvault secret set \
  --vault-name $KEYVAULT_NAME \
  --name "ApplicationInsightsKey" \
  --value "$APPINSIGHTS_KEY"
```

**2. Add Application Insights to NotificationService:**

```bash
# Add NuGet package
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["ApplicationInsights:InstrumentationKey"]);
```

**3. View Logs:**

- Azure Portal → Application Insights → Logs
- Query with KQL (Kusto Query Language):

```kql
traces
| where timestamp > ago(1h)
| where customDimensions.CategoryName contains "NotificationService"
| order by timestamp desc
| limit 100
```

### Container Insights (AKS Monitoring)

```bash
# Enable Container Insights on AKS
az aks enable-addons \
  --resource-group $RESOURCE_GROUP \
  --name $CLUSTER_NAME \
  --addons monitoring \
  --workspace-resource-id $(az monitor log-analytics workspace show \
    --resource-group $RESOURCE_GROUP \
    --workspace-name $WORKSPACE_NAME \
    --query id -o tsv)
```

**View Metrics:**
- Azure Portal → AKS Cluster → Insights
- See: CPU, Memory, Pod count, Network I/O
- Set alerts for high CPU/memory

### Prometheus & Grafana (Alternative)

For more control, deploy Prometheus + Grafana in cluster:

```bash
# Add Helm repos
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

# Install kube-prometheus-stack (Prometheus + Grafana + Alertmanager)
helm install monitoring prometheus-community/kube-prometheus-stack \
  --namespace monitoring \
  --create-namespace

# Get Grafana password
kubectl get secret -n monitoring monitoring-grafana \
  -o jsonpath="{.data.admin-password}" | base64 --decode

# Port-forward Grafana
kubectl port-forward -n monitoring svc/monitoring-grafana 3000:80

# Open http://localhost:3000
# Username: admin
# Password: <from above>
```

---

## CI/CD Pipeline

### Azure DevOps Pipeline (azure-pipelines.yml)

```yaml
trigger:
  branches:
    include:
    - main
  paths:
    include:
    - src/Services/Notification/*

variables:
  acrName: 'acrquietmatchprod'
  imageName: 'notification-service'
  aksResourceGroup: 'rg-quietmatch-prod'
  aksClusterName: 'aks-quietmatch-prod'
  k8sNamespace: 'production'

stages:
- stage: Build
  jobs:
  - job: BuildAndPush
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - task: Docker@2
      displayName: 'Build Docker Image'
      inputs:
        command: build
        repository: $(imageName)
        dockerfile: src/Services/Notification/Dockerfile
        tags: |
          $(Build.BuildId)
          latest

    - task: Docker@2
      displayName: 'Push to ACR'
      inputs:
        command: push
        containerRegistry: '$(acrName)'
        repository: $(imageName)
        tags: |
          $(Build.BuildId)
          latest

- stage: Test
  dependsOn: Build
  jobs:
  - job: UnitTests
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - task: DotNetCoreCLI@2
      displayName: 'Run Unit Tests'
      inputs:
        command: test
        projects: 'src/Services/Notification/**/*.Tests.Unit.csproj'
        arguments: '--configuration Release'

- stage: Deploy
  dependsOn: Test
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
  jobs:
  - deployment: DeployToAKS
    environment: production
    pool:
      vmImage: 'ubuntu-latest'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureCLI@2
            displayName: 'Get AKS Credentials'
            inputs:
              azureSubscription: 'AzureServiceConnection'
              scriptType: bash
              scriptLocation: inlineScript
              inlineScript: |
                az aks get-credentials \
                  --resource-group $(aksResourceGroup) \
                  --name $(aksClusterName) \
                  --overwrite-existing

          - task: KubernetesManifest@0
            displayName: 'Deploy to AKS'
            inputs:
              action: deploy
              namespace: $(k8sNamespace)
              manifests: |
                src/Services/Notification/k8s/configmap.yaml
                src/Services/Notification/k8s/deployment.yaml
                src/Services/Notification/k8s/service.yaml
                src/Services/Notification/k8s/hpa.yaml
              containers: |
                $(acrName).azurecr.io/$(imageName):$(Build.BuildId)
```

### GitHub Actions (alternative)

```yaml
# .github/workflows/deploy-notification-service.yml
name: Deploy NotificationService to AKS

on:
  push:
    branches: [main]
    paths:
      - 'src/Services/Notification/**'

env:
  ACR_NAME: acrquietmatchprod
  IMAGE_NAME: notification-service
  AKS_RESOURCE_GROUP: rg-quietmatch-prod
  AKS_CLUSTER_NAME: aks-quietmatch-prod
  K8S_NAMESPACE: production

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3

    - name: Azure Login
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Build and Push to ACR
      run: |
        az acr login --name ${{ env.ACR_NAME }}

        cd src/Services/Notification
        docker build -t ${{ env.IMAGE_NAME }}:${{ github.sha }} .
        docker tag ${{ env.IMAGE_NAME }}:${{ github.sha }} \
          ${{ env.ACR_NAME }}.azurecr.io/${{ env.IMAGE_NAME }}:${{ github.sha }}
        docker push ${{ env.ACR_NAME }}.azurecr.io/${{ env.IMAGE_NAME }}:${{ github.sha }}

    - name: Set up kubectl
      uses: azure/setup-kubectl@v3

    - name: Get AKS credentials
      run: |
        az aks get-credentials \
          --resource-group ${{ env.AKS_RESOURCE_GROUP }} \
          --name ${{ env.AKS_CLUSTER_NAME }}

    - name: Deploy to AKS
      run: |
        kubectl set image deployment/notification-service \
          notification-service=${{ env.ACR_NAME }}.azurecr.io/${{ env.IMAGE_NAME }}:${{ github.sha }} \
          -n ${{ env.K8S_NAMESPACE }}

        kubectl rollout status deployment/notification-service -n ${{ env.K8S_NAMESPACE }}
```

---

## Cost Optimization

### AKS Cluster Sizing

**Development:**
```bash
--node-vm-size Standard_B2s  # 2 vCPU, 4GB RAM, ~$30/month
--node-count 1
```

**Production:**
```bash
--node-vm-size Standard_D2s_v3  # 2 vCPU, 8GB RAM, ~$70/month
--node-count 3  # High availability
--enable-cluster-autoscaler
--min-count 2
--max-count 10
```

### Resource Requests & Limits

**Critical:** Set appropriate resource requests/limits to pack more pods per node.

```yaml
resources:
  requests:
    cpu: 100m     # Guaranteed 0.1 CPU
    memory: 128Mi # Guaranteed 128 MB
  limits:
    cpu: 500m     # Max 0.5 CPU
    memory: 512Mi # Max 512 MB
```

**Example calculation:**
- Node: Standard_B2s (2 vCPU, 4GB RAM)
- Pod requests: 100m CPU, 128Mi memory
- Can fit: ~15-20 pods per node (accounting for system overhead)

### Spot Instances (70-80% discount!)

For non-critical workloads:

```bash
az aks nodepool add \
  --resource-group $RESOURCE_GROUP \
  --cluster-name $CLUSTER_NAME \
  --name spotpool \
  --priority Spot \
  --eviction-policy Delete \
  --spot-max-price -1 \
  --enable-cluster-autoscaler \
  --min-count 1 \
  --max-count 5 \
  --node-vm-size Standard_D2s_v3
```

### Azure Service Bus vs RabbitMQ Costs

**RabbitMQ in AKS:**
- Cost: Node costs only (~$30-70/month)
- Management overhead: You manage

**Azure Service Bus:**
- Basic tier: $10/month (12.5M operations)
- Standard tier: $10/month base + $0.05/million operations
- Premium tier: $670/month (dedicated resources)

**Recommendation:** Use Basic tier for production (<12.5M messages/month).

---

## Troubleshooting

### Pods Not Starting

```bash
# Check pod status
kubectl get pods -n production

# Describe pod (shows events)
kubectl describe pod <pod-name> -n production

# Check logs
kubectl logs <pod-name> -n production
```

**Common issues:**

1. **ImagePullBackOff** → ACR access issue
   ```bash
   # Grant AKS access to ACR
   az aks update \
     --resource-group $RESOURCE_GROUP \
     --name $CLUSTER_NAME \
     --attach-acr $ACR_NAME
   ```

2. **CrashLoopBackOff** → Application error
   ```bash
   # Check logs
   kubectl logs <pod-name> -n production --previous
   ```

3. **Pending** → Insufficient resources
   ```bash
   # Check node resources
   kubectl top nodes

   # Scale up node pool
   az aks nodepool scale \
     --resource-group $RESOURCE_GROUP \
     --cluster-name $CLUSTER_NAME \
     --name nodepool1 \
     --node-count 3
   ```

### Secrets Not Available

```bash
# Check SecretProviderClass
kubectl describe secretproviderclass azure-keyvault -n production

# Check if secret mounted
kubectl exec <pod-name> -n production -- ls /mnt/secrets-store

# Check AKS identity has Key Vault access
az keyvault show-policy \
  --name $KEYVAULT_NAME \
  --object-id $AKS_IDENTITY
```

### Ingress Not Working

```bash
# Check ingress controller
kubectl get pods -n ingress-nginx

# Check ingress resource
kubectl describe ingress notification-ingress -n production

# Check external IP
kubectl get svc -n ingress-nginx

# Test from within cluster
kubectl run test-pod --image=curlimages/curl -i --rm --restart=Never -- \
  curl http://notification-service.production.svc.cluster.local/health
```

### High Memory Usage

```bash
# Check resource usage
kubectl top pods -n production

# Check resource limits
kubectl describe pod <pod-name> -n production | grep -A 5 Limits

# Increase memory limit in deployment
kubectl edit deployment notification-service -n production
# Update resources.limits.memory: 1Gi
```

---

## Summary

### Quick Deploy Checklist

- [ ] Create AKS cluster
- [ ] Create Azure Container Registry (ACR)
- [ ] Build and push Docker image
- [ ] Create Azure Key Vault
- [ ] Store SendGrid API key in Key Vault
- [ ] Enable Key Vault CSI driver on AKS
- [ ] Create namespace: `kubectl create namespace production`
- [ ] Apply ConfigMap
- [ ] Apply SecretProviderClass
- [ ] Apply Deployment
- [ ] Apply Service
- [ ] Apply HPA
- [ ] Install ingress controller
- [ ] Apply Ingress
- [ ] Configure DNS
- [ ] Install cert-manager for SSL
- [ ] Set up monitoring (Application Insights)
- [ ] Create CI/CD pipeline
- [ ] Test deployment

### Essential Commands

```bash
# Connect to cluster
az aks get-credentials --resource-group $RG --name $CLUSTER

# Check deployments
kubectl get all -n production

# View logs
kubectl logs -f deployment/notification-service -n production

# Port forward for testing
kubectl port-forward svc/notification-service 5003:80 -n production

# Update deployment
kubectl set image deployment/notification-service \
  notification-service=acrname.azurecr.io/notification-service:v2.0.0

# Scale manually
kubectl scale deployment/notification-service --replicas=5

# Rollback deployment
kubectl rollout undo deployment/notification-service
```

### Next Steps

1. **Set up staging environment** - Test before production
2. **Configure alerts** - CPU, memory, error rate
3. **Implement backup strategy** - For stateful data
4. **Set up Azure Front Door** - Global load balancing, WAF
5. **Enable Azure Policy** - Enforce security standards
6. **Configure RBAC** - Limit cluster access

---

**Congratulations!** 🎉 You now have a production-grade AKS deployment of your NotificationService!
