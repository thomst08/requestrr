[![Discord](https://img.shields.io/discord/674782527139086350?color=7289DA&label=Discord&style=for-the-badge&logo=discord)](https://discord.gg/atjrUen5fJ)


CRIOS Request
=================

![logo](https://i.imgur.com/0UzLYvw.png)

CRIOS Request is a chatbot used to simplify using services like Sonarr / Radarr / Lidarr / Overseerr / Jellyseerr / Ombi via Discord. It is the request-side companion to CRIOS Watchlist (followarr).

### Features

- Request media via Discord using slash commands, buttons, and dropdowns
- Users get notified when their requests are fulfilled
- Sonarr (V2-V4) & Radarr (V2-V5) integration, with support for multiple instances via Overseerr/Jellyseerr (4k/1080p)
- Lidarr (V1-V2) integration
- Overseerr / Jellyseerr integration with per-user permissions/quotas and issue submission — including multi-instance routing so different categories can dispatch to different servers (e.g. `req.crios.app` vs `req.crios.media`)
- Ombi (V3/V4) integration with per-user roles/quotas and issue submission
- Fully configurable through a CRIOS-themed web portal

<br />

Installation
==================

Build the container from this repository (the `dockerfile` lives next to the .NET project), publish it to your registry, and run:

```
docker run -d \
  --name crios-request \
  -p 4545:4545 \
  -v /path/to/config:/root/config \
  --restart=unless-stopped \
  your-registry/crios-request
```

Then access the web portal at `http://your-host:4545/` to create your admin account and finish configuration. Once the bot is invited to your Discord server, type **/help** to see the available commands.

<br />

Environment Variables
==================

#### `CRIOS_PORT`

* **Description**: Sets the port the application listens on **inside** the container.
* **Default**: `4545`
* **Example**: `-e CRIOS_PORT=5000`

#### `CRIOS_BASE_URL`

* **Description**: Base URL path, for deployments behind a reverse proxy with a subpath (e.g. `/crios`).
* **Default**: `/`
* **Example**: `-e CRIOS_BASE_URL=/crios`

#### Example with environment variables

```bash
docker run -d \
  --name crios-request \
  -p 5000:5000 \
  -v /opt/crios-request/config:/root/config \
  -e CRIOS_PORT=5000 \
  -e CRIOS_BASE_URL=/crios \
  --restart=unless-stopped \
  your-registry/crios-request
```

> ⚠️ When setting `CRIOS_BASE_URL`, make sure it matches your reverse proxy config if you're serving the app under a subpath.

<br />

Companion Projects
==================

- [followarr](https://github.com/crios-app/followarr) — CRIOS Watchlist, the Discord bot that notifies users when new episodes drop on Plex.
- [plex-patreon](https://github.com/crios-app/plex-patreon) — Patreon ↔ Plex linking and entitlement enforcement for the CRIOS platform.
