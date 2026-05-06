# ExcelBot

A Discord bot and web application for managing a Final Fantasy XIV Free Company. Combines an ASP.NET Core 9 backend, Vue 3 frontend, MongoDB, and a Discord.Net bot.

## Prerequisites

- A [Discord application](https://discord.com/developers/applications) with:
  - OAuth2 redirect URI set to `https://<your-domain>/callback`
  - Bot token
  - The bot invited to your server with appropriate permissions
- MongoDB 6+ instance
- Docker (for container-based deployment)

---

## Docker Compose

A `compose.yaml` is included in the repository and starts the application, MongoDB, and a mongo-express UI. Configuration is loaded from a `.env` file in the project root.

Create a `.env` file:

```bash
# ── MongoDB ────────────────────────────────────────────────────────────────────
MONGODB_CONNECTION_STRING=mongodb://root:secret@mongo:27017/
MONGO_INITDB_ROOT_USERNAME=root
MONGO_INITDB_ROOT_PASSWORD=secret

# ── mongo-express (database UI at :8081) ──────────────────────────────────────
ME_CONFIG_MONGODB_URL=mongodb://root:secret@mongo:27017/
ME_CONFIG_BASICAUTH_USERNAME=admin
ME_CONFIG_BASICAUTH_PASSWORD=secret

# ── Database ───────────────────────────────────────────────────────────────────
DATABASE__CONNECTIONSTRING=mongodb://root:secret@mongo:27017/
DATABASE__DATABASENAME=ExcelFC

# ── Discord OAuth ──────────────────────────────────────────────────────────────
OAUTHPROVIDERS__PROVIDERS__DISCORD__CLIENTID=your_discord_client_id
OAUTHPROVIDERS__PROVIDERS__DISCORD__CLIENTSECRET=your_discord_client_secret
OAUTHPROVIDERS__PROVIDERS__DISCORD__CALLBACK=/callback

# ── JWT (set to your public-facing URL) ───────────────────────────────────────
JWT__ISSUER=https://your-domain.com
JWT__AUDIENCE=https://your-domain.com

# ── Discord Bot ────────────────────────────────────────────────────────────────
DISCORDBOT__TOKEN=your_bot_token
DISCORDBOT__GUILDID=your_guild_id
DISCORDBOT__LOTTERYCHANNEL=channel_id
DISCORDBOT__ANNOUNCEMENTCHANNEL=channel_id
DISCORDBOT__EVENTSCHANNEL=channel_id
DISCORDBOT__UPCOMINGROSTERECHANNEL=channel_id
DISCORDBOT__HALLOFCLEARCHANNEL=channel_id
DISCORDBOT__LOGCHANNEL=0
DISCORDBOT__ADMINROLEIDS__0=admin_role_id
DISCORDBOT__MEMBERROLEIDS__0=member_role_id

# ── Lodestone ──────────────────────────────────────────────────────────────────
LODESTONE__FCID=your_fc_lodestone_id
# LODESTONE__BASEURL=https://na.finalfantasyxiv.com  # default
# LODESTONE__REQUESTDELAYMS=1000                      # default

# ── FFLogs ─────────────────────────────────────────────────────────────────────
FFLOGS__CLIENTID=your_fflogs_client_id
FFLOGS__CLIENTSECRET=your_fflogs_client_secret
# FFLOGS__TOKENURL=https://www.fflogs.com/oauth/token  # default
# FFLOGS__APIURL=https://www.fflogs.com/api/v2/client  # default
# FFLOGS__MEMBERSPERWAVE=10                             # default
# FFLOGS__DELAYBETWEENREQUESTSMS=500                   # default
```

All `SECTION__KEY` variables are loaded directly into the container via `env_file` and are picked up automatically by ASP.NET Core's configuration system (`__` maps to `:` as the section separator).

Then start the stack:

```bash
docker compose up -d
```

The application will be available at `http://localhost:8080` and mongo-express at `http://localhost:8081`.

To use the published image instead of building locally, change the `image` and remove the `build` block in `compose.yaml`:

```yaml
image: ghcr.io/your-org/excelbotcs:master
```

---

## Configuration Reference

All settings can be provided via environment variables using `__` as the section separator (e.g. `Database__ConnectionString`), or via `appsettings.json`. Values marked as optional have sensible defaults and can be omitted.

**Database**

| Variable | Required | Description |
|---|---|---|
| `Database__ConnectionString` | Yes | MongoDB connection string |
| `Database__DatabaseName` | Yes | MongoDB database name |

**Discord OAuth**

| Variable | Required | Description |
|---|---|---|
| `OAuthProviders__Providers__Discord__ClientId` | Yes | Discord OAuth application client ID |
| `OAuthProviders__Providers__Discord__ClientSecret` | Yes | Discord OAuth application client secret |
| `OAuthProviders__Providers__Discord__Callback` | Yes | OAuth redirect path — use `/callback` |

**JWT**

| Variable | Required | Description |
|---|---|---|
| `Jwt__Issuer` | Yes | Public base URL of the application (must match the Discord redirect URI host) |
| `Jwt__Audience` | Yes | Public base URL of the application |

RSA keys for JWT signing are generated automatically on first start and stored in MongoDB. No manual key management is needed.

**Discord Bot**

| Variable | Required | Description |
|---|---|---|
| `DiscordBot__Token` | Yes | Discord bot token |
| `DiscordBot__GuildId` | Yes | Discord server (guild) ID |
| `DiscordBot__LotteryChannel` | Yes | Channel ID for lottery announcements |
| `DiscordBot__AnnouncementChannel` | Yes | Channel ID for general announcements |
| `DiscordBot__EventsChannel` | Yes | Channel ID for event posts |
| `DiscordBot__UpcomingRosterChannel` | Yes | Channel ID for roster posts |
| `DiscordBot__HallOfClearsChannel` | Yes | Channel ID for clear announcements |
| `DiscordBot__LogChannel` | Yes | Channel ID for bot log messages (set to `0` to disable) |
| `DiscordBot__AdminRoleIds__0` | No | Discord role ID(s) granted admin access |
| `DiscordBot__MemberRoleIds__0` | No | Discord role ID(s) granted member access |

**Lodestone**

| Variable | Required | Default | Description |
|---|---|---|---|
| `Lodestone__FCId` | Yes | — | Lodestone Free Company ID for roster sync |
| `Lodestone__BaseUrl` | No | `https://na.finalfantasyxiv.com` | Lodestone base URL (change for other regions) |
| `Lodestone__RequestDelayMs` | No | `1000` | Delay between Lodestone HTTP requests in ms |

**FFLogs**

| Variable | Required | Default | Description |
|---|---|---|---|
| `FFLogs__ClientId` | Yes | — | FFLogs OAuth client ID |
| `FFLogs__ClientSecret` | Yes | — | FFLogs OAuth client secret |
| `FFLogs__TokenUrl` | No | `https://www.fflogs.com/oauth/token` | FFLogs token endpoint |
| `FFLogs__ApiUrl` | No | `https://www.fflogs.com/api/v2/client` | FFLogs GraphQL API endpoint |
| `FFLogs__MembersPerWave` | No | `10` | Members to sync per batch |
| `FFLogs__DelayBetweenRequestsMs` | No | `500` | Delay between FFLogs requests in ms |

---

## Kubernetes

### Secret

Store sensitive values in a Kubernetes Secret:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: excelbotcs-secret
type: Opaque
stringData:
  discord-client-secret: "YOUR_DISCORD_CLIENT_SECRET"
  discord-bot-token: "YOUR_BOT_TOKEN"
  mongo-connection-string: "mongodb://user:password@mongo-service:27017/"
```

### Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: excelbotcs
spec:
  replicas: 1
  selector:
    matchLabels:
      app: excelbotcs
  template:
    metadata:
      labels:
        app: excelbotcs
    spec:
      containers:
        - name: excelbotcs
          image: ghcr.io/your-org/excelbotcs:master
          ports:
            - containerPort: 8080
          env:
            - name: Database__ConnectionString
              valueFrom:
                secretKeyRef:
                  name: excelbotcs-secret
                  key: mongo-connection-string
            - name: Database__DatabaseName
              value: ExcelFC
            - name: OAuthProviders__Providers__Discord__ClientId
              value: "YOUR_DISCORD_CLIENT_ID"
            - name: OAuthProviders__Providers__Discord__ClientSecret
              valueFrom:
                secretKeyRef:
                  name: excelbotcs-secret
                  key: discord-client-secret
            - name: OAuthProviders__Providers__Discord__Callback
              value: /callback
            - name: Jwt__Issuer
              value: https://your-domain.com
            - name: Jwt__Audience
              value: https://your-domain.com
            - name: DiscordBot__Token
              valueFrom:
                secretKeyRef:
                  name: excelbotcs-secret
                  key: discord-bot-token
            - name: DiscordBot__GuildId
              value: "YOUR_GUILD_ID"
            - name: DiscordBot__AdminRoleIds__0
              value: "ADMIN_ROLE_ID"
            - name: DiscordBot__MemberRoleIds__0
              value: "MEMBER_ROLE_ID"
            - name: Lodestone__FCId
              value: "YOUR_FC_LODESTONE_ID"
```

### Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: excelbotcs
spec:
  selector:
    app: excelbotcs
  ports:
    - port: 80
      targetPort: 8080
```

Point an Ingress or load balancer at this Service. Make sure the host matches the `Jwt__Issuer`/`Jwt__Audience` values and the Discord OAuth redirect URI.

> **Note:** The Discord bot requires only one running instance. Set `replicas: 1` and avoid running multiple pods simultaneously, as each would connect to Discord independently.

---

## Container Images

Images are published to GitHub Container Registry on every push:

| Tag | When |
|---|---|
| `master` | Push to `master` branch |
| `pr-{n}` | Pull request |
| `1.2.3` / `1.2` | Git tag `v1.2.3` |
