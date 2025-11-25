# Terraform Skeleton – MarketPulse RT

This is a lightweight starting point to deploy the stack to AWS with minimal cost.

## What it does
- Creates a VPC with 2 subnets.
- Creates an ECS Fargate cluster.
- Creates task definitions for:
  - `market-data-service` (internal only)
  - `api-gateway` (fronted by an ALB)
- Provisions an ALB + target groups + listeners (HTTP by default; add TLS in production).
- Optionally provisions an S3 bucket + CloudFront for the static `web-dashboard` build.

## How to use
1. Install Terraform (>= 1.6) and AWS CLI with credentials configured.
2. Copy `terraform.tfvars.example` to `terraform.tfvars` and set the required values.
3. Initialize and review the plan:
   ```bash
   terraform init
   terraform plan
   ```
4. Apply:
   ```bash
   terraform apply
   ```

## Cost-conscious defaults
- Uses Fargate with small task sizes (0.25 vCPU / 0.5 GB) to keep baseline spend down.
- Single public ALB. Consider turning off when not in use, or replace with an API Gateway/Lambda pattern if you need lower idle cost.
- S3 + CloudFront for the frontend keeps hosting close to free at low traffic.

## TODO / Customize
- Add TLS certs via ACM and switch ALB listener to HTTPS (recommended).
- Add logging buckets for ALB and app logs.
- Swap to Azure/Bicep if you prefer; layout would be similar (App Service/Container Apps + Front Door + Storage Static Website).
