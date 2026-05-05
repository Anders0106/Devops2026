#!/bin/bash
set -euo pipefail
exec > /var/log/cloud-init-chirp.log 2>&1

# ---------------------------------------------------------------------------
# 1. System packages
# ---------------------------------------------------------------------------
apt-get update -qq
apt-get install -yq ca-certificates curl gnupg ufw

# ---------------------------------------------------------------------------
# 2. Install Docker (official repository)
# ---------------------------------------------------------------------------
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
  | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg

echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
  https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
  > /etc/apt/sources.list.d/docker.list

apt-get update -qq
apt-get install -yq docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

systemctl enable --now docker

# ---------------------------------------------------------------------------
# 3. Create deploy user, add to docker group
# ---------------------------------------------------------------------------
id -u ${deploy_user} &>/dev/null || useradd -m -s /bin/bash ${deploy_user}
usermod -aG docker ${deploy_user}

mkdir -p /home/${deploy_user}/.ssh
echo "${ssh_public_key}" >> /home/${deploy_user}/.ssh/authorized_keys
chmod 700 /home/${deploy_user}/.ssh
chmod 600 /home/${deploy_user}/.ssh/authorized_keys
chown -R ${deploy_user}:${deploy_user} /home/${deploy_user}/.ssh

# ---------------------------------------------------------------------------
# 4. UFW firewall — allow SSH, HTTP, HTTPS only
# ---------------------------------------------------------------------------
ufw --force reset
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable

# ---------------------------------------------------------------------------
# 5. Initialise Docker Swarm (single-node manager)
# ---------------------------------------------------------------------------
PUBLIC_IP=$(curl -s http://169.254.169.254/metadata/v1/interfaces/public/0/ipv4/address)
docker swarm init --advertise-addr "$PUBLIC_IP" || true

# ---------------------------------------------------------------------------
# 6. Write deployment artefacts under /opt/chirp
# ---------------------------------------------------------------------------
mkdir -p /opt/chirp/monitoring/dashboards
cd /opt/chirp

# --- Caddyfile ---
cat > /opt/chirp/Caddyfile <<'CADDY'
:80 {
	reverse_proxy chirp-web:80 {
		lb_policy ip_hash
	}
}
CADDY

# --- Prometheus config ---
cat > /opt/chirp/monitoring/prometheus.yml <<'PROM'
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: prometheus
    static_configs:
      - targets: ["localhost:9090"]

  - job_name: chirp-web
    static_configs:
      - targets: ["chirp-web:80"]
    metrics_path: /metrics
    scrape_interval: 15s

  - job_name: postgres
    static_configs:
      - targets: ["postgres-exporter:9187"]
    scrape_interval: 15s
PROM

# --- Loki config ---
cat > /opt/chirp/monitoring/loki-config.yaml <<'LOKI'
auth_enabled: false

server:
  http_listen_port: 3100

ingester:
  lifecycler:
    address: 127.0.0.1
    ring:
      kvstore:
        store: inmemory
      replication_factor: 1
    final_sleep: 0s
  chunk_idle_period: 5m
  chunk_retain_period: 30s

schema_config:
  configs:
    - from: 2024-01-01
      store: tsdb
      object_store: filesystem
      schema: v13
      index:
        prefix: index_
        period: 24h

storage_config:
  tsdb_shipper:
    active_index_directory: /loki/index
    cache_location: /loki/index_cache
  filesystem:
    directory: /loki/chunks

limits_config:
  reject_old_samples: true
  reject_old_samples_max_age: 168h

compactor:
  working_directory: /loki/compactor
LOKI

# --- Promtail config ---
cat > /opt/chirp/monitoring/promtail-config.yaml <<'PROMTAIL'
server:
  http_listen_port: 9080
  grpc_listen_port: 0

positions:
  filename: /tmp/positions.yaml

clients:
  - url: http://loki:3100/loki/api/v1/push

scrape_configs:
  - job_name: containers
    static_configs:
      - targets:
          - localhost
        labels:
          job: containerlogs
          __path__: /var/lib/docker/containers/*/*log
    pipeline_stages:
      - json:
          expressions:
            output: log
            stream: stream
            attrs:
      - json:
          expressions:
            tag: attrs.tag
          source: attrs
      - labels:
          stream:
          tag:
      - output:
          source: output
PROMTAIL

# --- Grafana datasources ---
cat > /opt/chirp/monitoring/datasources.yml <<'GDS'
apiVersion: 1
datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
  - name: Loki
    type: loki
    access: proxy
    url: http://loki:3100
GDS

# --- docker-compose.swarm.yml ---
cat > /opt/chirp/docker-compose.swarm.yml <<SWARM
services:
  caddy:
    image: caddy:2-alpine
    depends_on:
      - chirp-web
    ports:
      - "80:80"
      - "443:443"
    configs:
      - source: caddyfile
        target: /etc/caddy/Caddyfile
    volumes:
      - caddy-data:/data
      - caddy-config:/config
    deploy:
      replicas: 1
      restart_policy:
        condition: on-failure

  chirp-web:
    image: ${github_image_repo}:latest
    user: "1000:1000"
    depends_on:
      - db
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=chirp;Username=postgres;Password=${db_password}
    volumes:
      - chirp-data:/app/Assets
    deploy:
      replicas: 1
      update_config:
        order: start-first
        parallelism: 1
        delay: 10s
        failure_action: rollback
      rollback_config:
        order: stop-first
        parallelism: 1
      restart_policy:
        condition: on-failure
        delay: 5s
      labels:
        - shepherd.enable=true

  db:
    image: postgres:17-alpine
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=${db_password}
      - POSTGRES_DB=chirp
    volumes:
      - chirp-db:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5
    deploy:
      replicas: 1
      restart_policy:
        condition: on-failure

  postgres-exporter:
    image: prometheuscommunity/postgres-exporter:v0.15.0
    user: "65534:65534"
    environment:
      - DATA_SOURCE_URI=db:5432/chirp?sslmode=disable
      - DATA_SOURCE_USER=postgres
      - DATA_SOURCE_PASS=${db_password}
    depends_on:
      - db
    deploy:
      replicas: 1
      restart_policy:
        condition: on-failure

  shepherd:
    image: containrrr/shepherd
    environment:
      - SLEEP_TIME=5m
      - ROLLBACK_ON_FAILURE=true
      - FILTER_SERVICES=label=shepherd.enable=true
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    deploy:
      replicas: 1
      placement:
        constraints:
          - node.role == manager
      restart_policy:
        condition: on-failure

  prometheus:
    image: prom/prometheus:v2.52.0
    user: "65534:65534"
    command:
      - --config.file=/etc/prometheus/prometheus.yml
      - --storage.tsdb.path=/prometheus
      - --web.enable-lifecycle
    ports:
      - "127.0.0.1:9090:9090"
    configs:
      - source: prometheus_config
        target: /etc/prometheus/prometheus.yml
    volumes:
      - prometheus-data:/prometheus
    deploy:
      replicas: 1
      restart_policy:
        condition: on-failure

  loki:
    image: grafana/loki:3.6.0
    user: "0"
    command: -config.file=/etc/loki/loki-config.yaml
    ports:
      - "127.0.0.1:3100:3100"
    configs:
      - source: loki_config
        target: /etc/loki/loki-config.yaml
    volumes:
      - loki-data:/loki
    deploy:
      replicas: 1
      restart_policy:
        condition: on-failure

  promtail:
    image: grafana/promtail:3.6.0
    command: -config.file=/etc/promtail/promtail-config.yaml
    configs:
      - source: promtail_config
        target: /etc/promtail/promtail-config.yaml
    volumes:
      - /var/lib/docker/containers:/var/lib/docker/containers:ro
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - /var/log:/var/log:ro
    depends_on:
      - loki
    deploy:
      mode: global
      restart_policy:
        condition: on-failure
      placement:
        constraints:
          - node.platform.os == linux

  grafana:
    image: grafana/grafana:11.2.0
    user: "472:472"
    environment:
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=${grafana_password}
      - GF_USERS_ALLOW_SIGN_UP=false
      - GF_SERVER_HTTP_PORT=3000
    ports:
      - "127.0.0.1:3000:3000"
    configs:
      - source: grafana_datasources
        target: /etc/grafana/provisioning/datasources/datasources.yml
    volumes:
      - grafana-data:/var/lib/grafana
      - /opt/chirp/monitoring/dashboards:/etc/grafana/provisioning/dashboards
    deploy:
      replicas: 1
      restart_policy:
        condition: on-failure

configs:
  caddyfile:
    file: /opt/chirp/Caddyfile
  prometheus_config:
    file: /opt/chirp/monitoring/prometheus.yml
  grafana_datasources:
    file: /opt/chirp/monitoring/datasources.yml
  loki_config:
    file: /opt/chirp/monitoring/loki-config.yaml
  promtail_config:
    file: /opt/chirp/monitoring/promtail-config.yaml

volumes:
  caddy-data:
  caddy-config:
  chirp-data:
  chirp-db:
  prometheus-data:
  grafana-data:
  loki-data:
SWARM

# ---------------------------------------------------------------------------
# 7. Deploy the stack
# ---------------------------------------------------------------------------
chown -R ${deploy_user}:${deploy_user} /opt/chirp

docker stack deploy \
  --compose-file /opt/chirp/docker-compose.swarm.yml \
  --with-registry-auth \
  chirp

echo "Bootstrap complete — stack deployed."
