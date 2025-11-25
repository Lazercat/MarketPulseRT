terraform {
  required_version = ">= 1.6"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
  default_tags {
    tags = {
      Project = "MarketPulseRT"
      Env     = var.environment
    }
  }
}

locals {
  vpc_cidr = "10.10.0.0/16"
}

module "vpc" {
  source  = "terraform-aws-modules/vpc/aws"
  version = "~> 5.0"

  name = "marketpulse-vpc"
  cidr = local.vpc_cidr

  azs             = slice(data.aws_availability_zones.available.names, 0, 2)
  public_subnets  = ["10.10.1.0/24", "10.10.2.0/24"]
  private_subnets = ["10.10.11.0/24", "10.10.12.0/24"]

  enable_nat_gateway   = false
  enable_dns_hostnames = true
  enable_dns_support   = true
}

data "aws_availability_zones" "available" {}

module "ecs" {
  source  = "terraform-aws-modules/ecs/aws"
  version = "~> 5.8"

  cluster_name = "marketpulse-ecs"
}

resource "aws_ecs_task_definition" "market_data" {
  family                   = "market-data-service"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = 256
  memory                   = 512
  execution_role_arn       = var.ecs_execution_role_arn
  task_role_arn            = var.ecs_task_role_arn

  container_definitions = jsonencode([
    {
      name      = "market-data-service"
      image     = var.market_data_image
      essential = true
      portMappings = [
        {
          containerPort = 5001
          hostPort      = 5001
          protocol      = "tcp"
        }
      ]
      environment = [
        { name = "MarketData__Source", value = "Mock" },
        { name = "ASPNETCORE_URLS", value = "http://+:5001" },
        { name = "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2UNENCRYPTEDSUPPORT", value = "true" }
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-region"        = var.aws_region
          "awslogs-group"         = "/ecs/market-data-service"
          "awslogs-stream-prefix" = "ecs"
        }
      }
    }
  ])
}

resource "aws_ecs_task_definition" "api_gateway" {
  family                   = "api-gateway"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = 256
  memory                   = 512
  execution_role_arn       = var.ecs_execution_role_arn
  task_role_arn            = var.ecs_task_role_arn

  container_definitions = jsonencode([
    {
      name      = "api-gateway"
      image     = var.api_gateway_image
      essential = true
      portMappings = [
        {
          containerPort = 5100
          hostPort      = 5100
          protocol      = "tcp"
        }
      ]
      environment = [
        { name = "MarketDataService__GrpcUrl", value = "http://market-data-service:5001" },
        { name = "ASPNETCORE_URLS", value = "http://+:5100" },
        { name = "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2UNENCRYPTEDSUPPORT", value = "true" }
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-region"        = var.aws_region
          "awslogs-group"         = "/ecs/api-gateway"
          "awslogs-stream-prefix" = "ecs"
        }
      }
    }
  ])
}

module "lb" {
  source  = "terraform-aws-modules/alb/aws"
  version = "~> 9.9"

  name = "marketpulse-alb"

  load_balancer_type = "application"
  vpc_id             = module.vpc.vpc_id
  subnets            = module.vpc.public_subnets

  security_groups = [aws_security_group.alb.id]

  target_groups = [
    {
      name_prefix      = "api-"
      backend_protocol = "HTTP"
      backend_port     = 5100
      target_type      = "ip"
      health_check = {
        path                = "/health"
        matcher             = "200-399"
        healthy_threshold   = 2
        unhealthy_threshold = 2
        interval            = 15
      }
    }
  ]

  http_tcp_listeners = [
    {
      port               = 80
      protocol           = "HTTP"
      target_group_index = 0
    }
  ]
}

resource "aws_security_group" "alb" {
  name        = "marketpulse-alb-sg"
  description = "Allow HTTP"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "ecs_services" {
  name        = "marketpulse-ecs-sg"
  description = "Allow service to service"
  vpc_id      = module.vpc.vpc_id

  ingress {
    from_port       = 0
    to_port         = 65535
    protocol        = "tcp"
    security_groups = [aws_security_group.ecs_services.id]
  }

  ingress {
    from_port       = 5100
    to_port         = 5100
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

module "market_data_service" {
  source  = "terraform-aws-modules/ecs/aws//modules/service"
  version = "~> 5.8"

  name        = "market-data-service"
  cluster_arn = module.ecs.ecs_cluster_arn

  cpu    = 256
  memory = 512

  desired_count                      = 1
  launch_type                        = "FARGATE"
  platform_version                   = "1.4.0"
  assign_public_ip                   = true
  enable_execute_command             = false
  subnet_ids                         = module.vpc.public_subnets
  security_group_ids                 = [aws_security_group.ecs_services.id]
  force_new_deployment               = true
  deployment_minimum_healthy_percent = 50

  task_definition = aws_ecs_task_definition.market_data.arn
}

module "api_gateway_service" {
  source  = "terraform-aws-modules/ecs/aws//modules/service"
  version = "~> 5.8"

  name        = "api-gateway"
  cluster_arn = module.ecs.ecs_cluster_arn

  cpu    = 256
  memory = 512

  desired_count                      = 1
  launch_type                        = "FARGATE"
  platform_version                   = "1.4.0"
  assign_public_ip                   = true
  enable_execute_command             = false
  subnet_ids                         = module.vpc.public_subnets
  security_group_ids                 = [aws_security_group.ecs_services.id]
  force_new_deployment               = true
  deployment_minimum_healthy_percent = 50

  task_definition = aws_ecs_task_definition.api_gateway.arn

  load_balancer = [
    {
      target_group_arn = module.lb.target_group_arns[0]
      container_name   = "api-gateway"
      container_port   = 5100
    }
  ]
}
