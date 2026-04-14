output "droplet_ip" {
  description = "Public IPv4 address of the Chirp production server"
  value       = digitalocean_droplet.chirp.ipv4_address
}

output "droplet_id" {
  description = "DigitalOcean Droplet ID"
  value       = digitalocean_droplet.chirp.id
}

output "ssh_command" {
  description = "SSH command to connect to the server"
  value       = "ssh ${var.deploy_user}@${digitalocean_droplet.chirp.ipv4_address}"
}
