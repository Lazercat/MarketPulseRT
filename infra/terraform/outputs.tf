output "alb_dns_name" {
  value = module.lb.this_lb_dns_name
}

output "cluster_name" {
  value = module.ecs.ecs_cluster_name
}
