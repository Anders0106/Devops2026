terraform {
  required_version = ">= 1.6.0"

  required_providers {
    digitalocean = {
      source  = "digitalocean/digitalocean"
      version = "~> 2.0"
    }
  }
}

provider "digitalocean" {
  token = var.do_token
}

# ---------------------------------------------------------------------------
# SSH key — import your public key so Terraform registers it with DO
# ---------------------------------------------------------------------------
resource "digitalocean_ssh_key" "chirp" {
  name       = "chirp-deploy-key"
  public_key = file(var.ssh_public_key_path)
}

# ---------------------------------------------------------------------------
# Droplet — single-node Docker Swarm host
# ---------------------------------------------------------------------------
resource "digitalocean_droplet" "chirp" {
  name   = "chirp-prod"
  region = var.region
  size   = var.droplet_size
  image  = "ubuntu-24-04-x64"

  ssh_keys = [digitalocean_ssh_key.chirp.fingerprint]

  # cloud-init script bootstraps Docker, initialises Swarm, and deploys the stack
  user_data = templatefile("${path.module}/cloud-init.sh.tpl", {
    deploy_user       = var.deploy_user
    ssh_public_key    = file(var.ssh_public_key_path)
    github_image_repo = var.github_image_repo
    grafana_password  = var.grafana_admin_password
    db_password       = var.db_password
  })

  tags = ["chirp", "production"]
}

# ---------------------------------------------------------------------------
# Firewall — only expose SSH + HTTP + HTTPS; all monitoring ports stay local
# ---------------------------------------------------------------------------
resource "digitalocean_firewall" "chirp" {
  name = "chirp-prod-fw"

  droplet_ids = [digitalocean_droplet.chirp.id]

  # Inbound — public traffic
  inbound_rule {
    protocol         = "tcp"
    port_range       = "22"
    source_addresses = ["0.0.0.0/0", "::/0"]
  }

  inbound_rule {
    protocol         = "tcp"
    port_range       = "80"
    source_addresses = ["0.0.0.0/0", "::/0"]
  }

  inbound_rule {
    protocol         = "tcp"
    port_range       = "443"
    source_addresses = ["0.0.0.0/0", "::/0"]
  }

  # Outbound — allow all
  outbound_rule {
    protocol              = "tcp"
    port_range            = "1-65535"
    destination_addresses = ["0.0.0.0/0", "::/0"]
  }

  outbound_rule {
    protocol              = "udp"
    port_range            = "1-65535"
    destination_addresses = ["0.0.0.0/0", "::/0"]
  }

  outbound_rule {
    protocol              = "icmp"
    destination_addresses = ["0.0.0.0/0", "::/0"]
  }
}

# ---------------------------------------------------------------------------
# (Optional) DNS A-record — uncomment if you manage a domain in DO
# ---------------------------------------------------------------------------
# resource "digitalocean_domain" "chirp" {
#   name = var.domain_name
# }
#
# resource "digitalocean_record" "chirp_root" {
#   domain = digitalocean_domain.chirp.name
#   type   = "A"
#   name   = "@"
#   value  = digitalocean_droplet.chirp.ipv4_address
#   ttl    = 300
# }
#
# resource "digitalocean_record" "chirp_www" {
#   domain = digitalocean_domain.chirp.name
#   type   = "A"
#   name   = "www"
#   value  = digitalocean_droplet.chirp.ipv4_address
#   ttl    = 300
# }
