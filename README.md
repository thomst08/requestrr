[![Paypal](https://img.shields.io/badge/Paypal-Donate-success?style=for-the-badge&logo=paypal)](https://www.paypal.com/donate/?business=QT2Y72ABMYJNG&no_recurring=0&currency_code=AUD) 
[![Discord](https://img.shields.io/discord/674782527139086350?color=7289DA&label=Discord&style=for-the-badge&logo=discord)](https://discord.gg/atjrUen5fJ)
[![DockerHub](https://img.shields.io/badge/Docker-Hub-%23099cec?style=for-the-badge&logo=docker)](https://hub.docker.com/r/thomst08/requestrr)
[![DockerHub](https://img.shields.io/badge/GitHub-Repo-lightgrey?style=for-the-badge&logo=github)](https://github.com/thomst08/requestrr/)


Requestrr 
=================

![logo](https://i.imgur.com/0UzLYvw.png)

Requestrr is a chatbot used to simplify using services like Sonarr/Radarr/Lidarr/Overseerr/Ombi via the use of chat!  

### Features

- Ability to request content via Discord using slash commands, buttons and more!
- Users can get notified when their requests complete
- Sonarr (V2-V4) & Radarr (V2-V5) integration with support for multiple instance via Overseerr (only for 4k/1080p)
- Lidarr (V1-V2) intergration
- Overseerr integration with support for per user permissions/quotas and issue submission
- Ombi (V3/V4) integration with support for per user roles/quotas and issue submission
- Fully configurable via a web portal

<br />

Installation & Configuration
==================

Refer to the Wiki for detailed steps:
https://github.com/thomst08/requestrr/wiki

<br />

Docker Set-up & Start
==================

Open a command prompt/terminal and then use the following command create and start the container:

```
    docker run -d \
      --name requestrr \
      -p 4545:4545 \
      -v path to config:/root/config \
      --restart=unless-stopped \
      thomst08/requestrr
```

You can also choose to run the container as a different user. See [docker run](https://docs.docker.com/engine/reference/run/#user) reference for how to set the user for your container.

Then simply access the web portal at http://youraddress:4545/ to create your admin account, then you can configure everything through the web portal. <br />
Once you have configured the bot and invited it to your Discord server, simply type **/help** to see all available commands.

If you just need commands to quickly setup Requestrr with no issues, use the following commands:

```
mkdir /opt/Requestrr
mkdir /opt/Requestrr/config
docker run -d \
  --name requestrr \
  -p 4545:4545 \
  -v /opt/Requestrr/config:/root/config \
  --restart=unless-stopped \
  thomst08/requestrr
```

<br />

Environment Variables
==================

Requestrr supports the following environment variables to help you customize your deployment:

#### `REQUESTRR_PORT`

* **Description**: Sets the port the application listens on **inside** the container.
* **Default**: `4545`
* **Example**: `-e REQUESTRR_PORT=5000`

#### `REQUESTRR_BASEURL`

* **Description**: Defines a base URL path for Requestrr. Useful when deploying behind a reverse proxy with a subpath (e.g. `/requestrr`).
* **Default**: `/`
* **Example**: `-e REQUESTRR_BASEURL=/requestrr`

#### Example Docker Command with Environment Variables

```bash
docker run -d \
  --name requestrr \
  -p 5000:5000 \
  -v /opt/Requestrr/config:/root/config \
  -e REQUESTRR_PORT=5000 \
  -e REQUESTRR_BASEURL=/requestrr \
  --restart=unless-stopped \
  thomst08/requestrr
```

> ⚠️ **Note**: When setting `REQUESTRR_BASEURL`, make sure it matches your reverse proxy config if you're serving Requestrr under a subpath.

<br />

Build Instructions
==================

Refer to the Wiki for detailed steps on how to build:
https://github.com/thomst08/requestrr/wiki/Build-Instructions

<br>

Thank you list
==============

Thank you goes out to the following people:
- [@darkalfx]( https://github.com/darkalfx ) - Creator of Requestrr, without this person, Requestrr would not exist.
