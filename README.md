# Skyline

Skyline is a personal configuration templates repository for **Mihomo** and **Shadowrocket**, used to manage reusable proxy configuration templates for desktop, NAS, and iOS-style client environments.

This repository does **not** provide proxy nodes, subscription services, or any third-party network service. It only stores configuration templates.

## Repository Structure

```text
skyline/
├── README.md
├── .gitignore
├── LICENSE
│
├── mihomo/
│   ├── desktop/
│   │   ├── config.yaml
│   │   └── README.md
│   └── nas/
│       ├── config.yaml
│       └── README.md
│
└── shadowrocket/
    ├── config.conf
    └── README.md
```

## Contents

### Mihomo

The `mihomo/` directory contains configuration templates for Mihomo-based clients.

```text
mihomo/
├── desktop/
│   ├── config.yaml
│   └── README.md
└── nas/
    ├── config.yaml
    └── README.md
```

* `mihomo/desktop/`
  Configuration template for desktop clients, such as Clash Party, Clash Verge, FlClash, and other Mihomo-compatible clients.
* `mihomo/nas/`
  Configuration template for NAS, server, home gateway, or always-on deployment environments.

Mihomo supports configuration modules such as proxies, proxy-providers, proxy-groups, rule-providers, and rules. In this repository, the main templates are designed around replacing or modifying the proxy-related sections before use.

### Shadowrocket

The `shadowrocket/` directory contains a Shadowrocket configuration template.

```text
shadowrocket/
├── config.conf
└── README.md
```

This template is intended for Shadowrocket on Apple platforms. After importing the configuration, users can add their own proxy subscriptions or nodes inside the Shadowrocket app and modify the proxy group names according to their own usage.

## Usage

### 1. Using Mihomo Templates

Choose the template according to your usage environment.

#### Desktop

Use:

```text
mihomo/desktop/config.yaml
```

Recommended for:

* Clash Party
* Clash Verge
* FlClash
* Other Mihomo-compatible desktop clients

Usage steps:

1. Download `mihomo/desktop/config.yaml`.
2. Open the configuration file with a text editor.
3. Replace the contents of the following sections with your own configuration:

```yaml
proxies:
  # Replace with your own proxy nodes
proxy-providers:
  # Replace with your own subscription providers
```

4. Import the modified `config.yaml` into your Mihomo-compatible client.
5. Select the appropriate proxy group in the client.
6. Test connectivity and rule matching.

#### NAS

Use:

```text
mihomo/nas/config.yaml
```

Recommended for:

* NAS deployment
* Docker deployment
* Home server deployment
* Home gateway or LAN proxy scenarios

Usage steps:

1. Download `mihomo/nas/config.yaml`.
2. Replace the `proxies` and `proxy-providers` sections with your own node or subscription configuration.
3. Adjust local network related settings according to your environment, such as:

```yaml
allow-lan:
external-controller:
mixed-port:
tun:
dns:
```

4. Deploy the configuration to your NAS or server.
5. Restart the Mihomo service or container.
6. Check logs and client connectivity.

### 2. Using Shadowrocket Template

Use:

```text
shadowrocket/config.conf
```

Usage methods:

#### Method 1: Import the file manually

1. Download `shadowrocket/config.conf`.
2. Open Shadowrocket.
3. Import the configuration file into the app.
4. Add your own subscription nodes in Shadowrocket.
5. Modify the proxy group names in the configuration if needed.
6. Select the desired proxy group and test the connection.

#### Method 2: Import from URL

1. Upload or host the `config.conf` file in a location accessible by URL.
2. Open Shadowrocket.
3. Use URL-based configuration download or update.
4. Import the configuration.
5. Add your own subscription nodes.
6. Modify the related proxy group names if necessary.

## Important Notes

### Do Not Commit Sensitive Information

Do not commit the following information to this repository:

* Real proxy subscription URLs
* Proxy node passwords
* API tokens
* Dashboard secrets
* Personal access tokens
* Private server addresses
* Private configuration overrides

Recommended practice:

* Commit templates.
* Keep real secrets locally.

## Configuration Principles

This repository follows these principles:

1. Keep templates clean and reusable.
2. Separate public templates from private subscriptions.
3. Avoid storing real node information in Git.
4. Keep Mihomo desktop and NAS configurations separate.
5. Keep Shadowrocket configuration simple and easy to import.
6. Use clear proxy group names for long-term maintenance.

## Recommended Workflow

### For Mihomo

```text
Download template
        ↓
Replace proxies / proxy-providers
        ↓
Adjust DNS / TUN / rules if needed
        ↓
Import into client
        ↓
Test and use
```

### For Shadowrocket

```text
Download or import config.conf
        ↓
Add subscription nodes in Shadowrocket
        ↓
Modify proxy group names if needed
        ↓
Select proxy group
        ↓
Test and use
```

## Disclaimer

This repository is only used for storing personal configuration templates.

The author does not provide, sell, or maintain any proxy service, subscription service, or network access service. Users are responsible for ensuring that their own configuration and network usage comply with local laws, regulations, and service terms.

## License

This repository is licensed under the GNU General Public License v3.0.
