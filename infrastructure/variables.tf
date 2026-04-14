variable "do_token" {
  description = "DigitalOcean personal access token"
  type        = string
  sensitive   = true
}

variable "region" {
  description = "DigitalOcean region slug (e.g. fra1, ams3, nyc1)"
  type        = string
  default     = "fra1"
}

variable "droplet_size" {
  description = "Droplet size slug — s-1vcpu-2gb is the smallest that runs the full stack comfortably"
  type        = string
  default     = "s-2vcpu-4gb"
}

variable "ssh_public_key_path" {
  description = "Path to the SSH public key to register on the droplet"
  type        = string
  default     = "~/.ssh/id_ed25519.pub"
}

variable "deploy_user" {
  description = "Non-root Linux user created on the droplet for deployments"
  type        = string
  default     = "deploy"
}

variable "github_image_repo" {
  description = "GHCR image reference for the Chirp web app (e.g. ghcr.io/org/devops2026)"
  type        = string
}

variable "grafana_admin_password" {
  description = "Grafana admin password"
  type        = string
  sensitive   = true
  default     = "changeme"
}

variable "db_password" {
  description = "PostgreSQL password used by the application"
  type        = string
  sensitive   = true
  default     = "postgres"
}

# variable "domain_name" {
#   description = "Domain name managed in DigitalOcean DNS (optional)"
#   type        = string
#   default     = ""
# }
