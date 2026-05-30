# Deployment Guide

## What's already wired up

| Piece | File | Notes |
|---|---|---|
| Docker image build + push | `.github/workflows/deploy.yml` | Pushes to GHCR on every merge to `main` |
| Docker Compose stack | `docker-compose.yml` | App + nginx services |
| nginx + TLS | `nginx.conf` | HTTP→HTTPS redirect, Let's Encrypt certs |
| Environment template | `.env.example` | Copy to `.env` on server and fill in |
| Server bootstrap steps | `server-setup.sh` | Run once on a fresh server |
| Database reset + seed | `db/reset.py` | Run once after first deploy |

**Deploy pipeline:** push to `main` → GitHub Actions builds image → pushes to GHCR → self-hosted runner on EC2 pulls and restarts the stack.

---

## Step 1 — EC2 instance

Launch a new EC2 instance in the AWS console:

| Setting | Value |
|---|---|
| AMI | Ubuntu Server 24.04 LTS |
| Instance type | `t3.small` (1 vCPU, 2 GB) — `t3.micro` works but can be tight under load |
| Storage | 20 GB gp3 (default is fine) |
| Key pair | Create or select one — you'll need it for SSH |

**Security Group** — create a new one with these inbound rules:

| Type | Port | Source |
|---|---|---|
| SSH | 22 | Your IP only (not 0.0.0.0/0) |
| HTTP | 80 | 0.0.0.0/0, ::/0 |
| HTTPS | 443 | 0.0.0.0/0, ::/0 |

---

## Step 2 — Elastic IP

EC2 public IPs change on every reboot. An Elastic IP pins a static address to your instance.

1. EC2 console → **Elastic IPs** → **Allocate Elastic IP address**
2. Once allocated, **Actions → Associate** → select your instance
3. Note the Elastic IP — you'll use it in Route 53

---

## Step 3 — Domain + DNS (Route 53)

If buying the domain in Route 53:
1. **Route 53 → Registered domains → Register domain** — find and purchase your domain
2. Route 53 automatically creates a hosted zone for it

Then add an A record:
1. **Route 53 → Hosted zones → your domain → Create record**
2. Record type: **A**, value: your Elastic IP, TTL: 300, leave name blank (apex record)

DNS can take a few minutes to a few hours to propagate.

---

## Step 4 — Server bootstrap

SSH into your instance and run through `server-setup.sh`:

```bash
ssh -i your-key.pem ubuntu@YOUR_ELASTIC_IP

# Install Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
# Log out and back in so the group change takes effect

# Clone the repo
sudo git clone https://github.com/Firesofiso/infinity-codex /opt/infinity-codex
cd /opt/infinity-codex

# Set up the environment file
cp .env.example .env
nano .env   # fill in all values — see Step 5

# Create the SQLite data directory
mkdir data

# Get TLS cert (do this before starting nginx — certbot needs port 80 free)
sudo apt install certbot -y
sudo certbot certonly --standalone -d YOUR_DOMAIN

# Replace the domain placeholder in nginx.conf
sed -i 's/YOUR_DOMAIN/your-actual-domain.com/g' nginx.conf

# Start the stack
docker compose up -d

# Verify both containers are running
docker compose ps
```

---

## Step 5 — Fill in `.env`

| Variable | Value |
|---|---|
| `GITHUB_REPOSITORY` | `Firesofiso/infinity-codex` |
| `DiscordOAuth__ClientId` | Discord Developer Portal → Your App → OAuth2 |
| `DiscordOAuth__ClientSecret` | Same place |
| `DiscordOAuth__RedirectUri` | `https://YOUR_DOMAIN/auth/discord/callback` |
| `DiscordOAuth__RegistrationGuildId` | Right-click your Linkshell Discord server → Copy Server ID |
| `Frontend__BaseUrl` | `https://YOUR_DOMAIN/` |

**Also add your redirect URI in Discord:**
Discord Developer Portal → Your App → OAuth2 → Redirects → add `https://YOUR_DOMAIN/auth/discord/callback`

---

## Step 6 — GitHub Actions self-hosted runner

This lets GitHub Actions deploy directly to your EC2 instance.

1. GitHub repo → **Settings → Actions → Runners → New self-hosted runner**
2. Select **Linux x64** — copy and run the commands GitHub provides (they include a one-time token)
3. Install as a service so it survives reboots:

```bash
sudo ./svc.sh install
sudo ./svc.sh start
```

From this point on, every push to `main` deploys automatically.

---

## Step 7 — First database seed

```bash
cd /opt/infinity-codex
python3 db/reset.py
```

This applies all EF migrations and inserts the 39 imported members with their DKP balances. Safe to re-run — it's idempotent and backs up the existing `app.db` before doing anything.

---

## Step 8 — Verify

- [ ] `https://YOUR_DOMAIN` loads the app
- [ ] `http://YOUR_DOMAIN` redirects to HTTPS
- [ ] Discord login completes and lands back on the app
- [ ] `/app/roster` shows imported members with correct DKP balances
- [ ] Push a trivial commit to `main` — confirm the Actions runner deploys it

---

## Ongoing deployments

Nothing to do manually after initial setup. Every push to `main`:
1. GitHub Actions builds a new Docker image and pushes to GHCR
2. The self-hosted runner on EC2 pulls the new image and restarts the stack
3. EF migrations run automatically on app startup

The SQLite database lives in `/opt/infinity-codex/data/` and is mounted as a volume — it persists across every deployment.

---

## Cert renewal

Already automated by `server-setup.sh`. The cron job renews certs every 90 days and reloads nginx:

```
0 12 * * * root certbot renew --quiet && docker compose -f /opt/infinity-codex/docker-compose.yml exec nginx nginx -s reload
```
