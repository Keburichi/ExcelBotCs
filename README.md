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
# MongoDB
MONGODB_CONNECTION_STRING=mongodb://root:secret@mongo:27017/
MONGO_INITDB_ROOT_USERNAME=root
MONGO_INITDB_ROOT_PASSWORD=secret

# mongo-express (database UI at :8081)
ME_CONFIG_MONGODB_URL=mongodb://root:secret@mongo:27017/
ME_CONFIG_BASICAUTH_USERNAME=admin
ME_CONFIG_BASICAUTH_PASSWORD=secret

# Application
DATABASE_NAME=ExcelFC
DISCORD_TOKEN=your_bot_token
LOTTERY_CHANNEL=channel_id
ANNOUNCEMENT_CHANNEL=channel_id
```

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

All settings can be provided via environment variables using `__` as the section separator (e.g. `Database__ConnectionString`), or via `appsettings.json`.

| Variable | Description |
|---|---|
| `Database__ConnectionString` | MongoDB connection string |
| `Database__DatabaseName` | MongoDB database name |
| `OAuthProviders__Providers__Discord__ClientId` | Discord OAuth application client ID |
| `OAuthProviders__Providers__Discord__ClientSecret` | Discord OAuth application client secret |
| `OAuthProviders__Providers__Discord__Callback` | OAuth redirect path (use `/callback`) |
| `Jwt__Issuer` | Public base URL of the application |
| `Jwt__Audience` | Public base URL of the application |
| `DiscordBot__Token` | Discord bot token |
| `DiscordBot__GuildId` | Discord server (guild) ID |
| `DiscordBot__LotteryChannel` | Channel ID for lottery announcements |
| `DiscordBot__AnnouncementChannel` | Channel ID for general announcements |
| `DiscordBot__EventsChannel` | Channel ID for event posts |
| `DiscordBot__UpcomingRosterChannel` | Channel ID for roster posts |
| `DiscordBot__HallOfClearsChannel` | Channel ID for clear announcements |
| `DiscordBot__AdminRoleIds__0` | Discord role ID(s) granted admin access |
| `DiscordBot__MemberRoleIds__0` | Discord role ID(s) granted member access |
| `Lodestone__FCId` | Lodestone Free Company ID for roster sync |

RSA keys for JWT signing are generated automatically on first start and stored in MongoDB. No manual key management is needed.

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
