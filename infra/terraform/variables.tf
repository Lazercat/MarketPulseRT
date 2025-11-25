variable "aws_region" {
  description = "AWS region to deploy into"
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Environment name (dev/stage/prod)"
  type        = string
  default     = "dev"
}

variable "market_data_image" {
  description = "Container image for market-data-service (e.g., ghcr.io/lazercat/market-data-service:tag)"
  type        = string
}

variable "api_gateway_image" {
  description = "Container image for api-gateway (e.g., ghcr.io/lazercat/api-gateway:tag)"
  type        = string
}

variable "ecs_execution_role_arn" {
  description = "IAM role ARN for ECS task execution"
  type        = string
}

variable "ecs_task_role_arn" {
  description = "IAM role ARN for ECS tasks"
  type        = string
}
