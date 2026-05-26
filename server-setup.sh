 # 1. Install Docker + Compose plugin
  curl -fsSL https://get.docker.com | sh
  sudo usermod -aG docker $USER   # re-login after this

  # 2. Clone the repo to the deploy location
  sudo git clone https://github.com/YOUR_USERNAME/infinity-codex /opt/infinity-codex
  cd /opt/infinity-codex

  # 3. Copy and fill in your secrets
  cp .env.example .env
  nano .env   # fill in Discord OAuth, domain, etc.

  # 4. Create the data directory (SQLite lives here)
  mkdir data

  # 5. Get your TLS cert BEFORE starting nginx with SSL config
  # (certbot standalone briefly listens on port 80)
  sudo apt install certbot
  sudo certbot certonly --standalone -d YOUR_DOMAIN

  # 6. Replace YOUR_DOMAIN in nginx.conf
  sed -i 's/YOUR_DOMAIN/your-actual-domain.com/g' nginx.conf

  # 7. Start the stack
  docker compose up -d

  # 8. Auto-renew certs — add to crontab
  echo "0 12 * * * root certbot renew --quiet && docker compose -f /opt/infinity-codex/docker-compose.yml exec nginx nginx -s reload" | sudo tee /etc/cron.d/certbot-renew

  # 9. Register the self-hosted GitHub Actions runner
  # Go to: GitHub repo → Settings → Actions → Runners → New self-hosted runner
  # Follow the Linux x64 instructions — they give you the exact token and commands
  # Then install as a service so it survives reboots:
  sudo ./svc.sh install && sudo ./svc.sh start