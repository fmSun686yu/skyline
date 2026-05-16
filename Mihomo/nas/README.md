# Mihomo NAS Configuration

This directory contains a Mihomo configuration template for **NAS**, **Docker**, **home server**, and **home gateway** environments.

It is designed for always-on Mihomo deployments, especially scenarios where Mihomo runs as a service and other devices on the LAN use it as a proxy gateway.

This template does **not** provide proxy services, proxy nodes, subscription services, or any third-party network access service. You need to replace the proxy-related sections and sensitive values before deploying it.

## File Structure

```text
Mihomo/nas/
├── config.yaml
└── README.md
```

## File Description

| File | Description |
| --- | --- |
| `config.yaml` | Mihomo NAS configuration template |
| `README.md` | Usage guide for this template |

## Suitable Scenarios

This template is intended for:

* NAS deployment
* Docker deployment
* Home server deployment
* Home gateway or LAN proxy scenarios
* MetaCubeXD or other Mihomo dashboard management
* Always-on proxy routing environments

Compared with the desktop template, this NAS template focuses more on LAN access, service management, TUN routing, DNS handling, external controller access, and long-running stability.

## Usage

### 1. Download the Template

Download the following file:

```text
Mihomo/nas/config.yaml
```

You can either download it directly from GitHub or clone the whole repository locally.

### 2. Edit Proxy Configuration

Open `config.yaml` with a text editor, such as:

* Visual Studio Code
* Sublime Text
* Vim
* Nano
* Any plain text editor

Then replace the following sections with your own proxy nodes or subscription providers:

```yaml
proxies:
  # Replace this section with your own proxy nodes
proxy-providers:
  # Replace this section with your own subscription providers
```

For subscription-based usage, `proxy-providers` is usually recommended.

Example:

```yaml
proxy-providers:
  my-subscription-1:
    type: http
    url: "https://example.com/your-subscription-url"
    path: ./proxy-providers/my-subscription-1.yaml
    interval: 43200
    proxy: DIRECT
    health-check:
      enable: true
      url: https://www.gstatic.com/generate_204
      interval: 300
      timeout: 10000
      lazy: true
      expected-status: 204
```

Do not commit your real subscription URL to a public repository.

### 3. Check Proxy Groups

After replacing `proxies` or `proxy-providers`, make sure your proxy groups reference the correct provider names.

Example:

```yaml
proxy-groups:
  - name: Manually Select Nodes
    type: select
    use:
      - my-subscription-1
```

If your provider is named `my-subscription-1`, then the proxy group should also use:

```yaml
use:
  - my-subscription-1
```

If the names do not match, Mihomo may load the configuration but the related proxy group can be empty.

### 4. Replace Sensitive Values

Before deploying this template, replace all sensitive or environment-specific values.

Recommended fields to check:

```yaml
authentication:
secret:
proxies:
proxy-providers:
external-controller:
allow-lan:
bind-address:
lan-allowed-ips:
```

Important notes:

* `authentication` protects the HTTP/SOCKS/mixed proxy ports.
* `skip-auth-prefixes` lets trusted source IP ranges bypass proxy port authentication.
* `secret` protects the `external-controller` management API.
* `external-controller` should only be exposed to trusted networks.
* `allow-lan: true` allows other devices on the LAN to connect to Mihomo.
* `bind-address: "*"` listens on all interfaces, which is convenient but should be used carefully.

### 5. Configure Proxy Authentication

The template enables proxy port authentication for HTTP/SOCKS/mixed inbound connections.

Example:

```yaml
authentication:
  - "username1:password1"
  - "username2:password2"
skip-auth-prefixes:
  - 127.0.0.1/8
  - ::1/128
```

Recommended practice:

* Replace all example usernames and passwords before deployment.
* Keep `skip-auth-prefixes` limited to trusted local addresses.
* Remember that `authentication` protects proxy ports, not the `external-controller` API.
* Use `secret` for the dashboard and controller API.

### 6. Adjust LAN Settings

This NAS template is designed for LAN usage, so the following fields are important:

```yaml
mixed-port: 7892
allow-lan: true
bind-address: "*"
lan-allowed-ips:
  - 0.0.0.0/0
  - ::/0
```

If you only want specific devices to use the proxy, narrow `lan-allowed-ips` to your trusted LAN subnet or specific client addresses.

Example:

```yaml
lan-allowed-ips:
  - 192.168.2.0/24
```

If you do not need other devices to connect to the NAS proxy service, set:

```yaml
allow-lan: false
```

### 7. Configure External Controller

The template includes an external controller for dashboard tools such as MetaCubeXD.

Example:

```yaml
external-controller: 0.0.0.0:9090
secret: "replace-with-your-own-strong-secret"
external-controller-cors:
  allow-origins:
    - "*"
  allow-private-network: true
```

Recommended practice:

* Replace `secret` with a strong private value.
* Use the same `secret` in your dashboard client.
* Avoid exposing the controller port to the public Internet.
* Restrict `external-controller-cors.allow-origins` if you use a known fixed dashboard origin.
* Limit access with firewall rules, Docker network settings, or reverse proxy authentication if needed.

If MetaCubeXD runs on another device or container, make sure it can reach the NAS IP and controller port.

### 8. Deploy to NAS or Docker

Copy the modified `config.yaml` to the directory used by your Mihomo service or container.

Common workflow:

```text
Edit config.yaml
-> Copy config.yaml to the NAS or Docker config directory
-> Start or restart the Mihomo service
-> Open dashboard or check logs
-> Update proxy providers and rule providers
-> Select proxy groups
-> Test client connectivity
```

For Docker deployments, make sure the container has the required network permissions for your selected features. TUN mode commonly requires additional capabilities and access to `/dev/net/tun`.

### 9. Update Providers

After Mihomo starts, update the following resources from your client dashboard or logs:

* Proxy providers
* Rule providers
* Geo data

If provider updates fail, check:

* Whether the NAS can access the Internet.
* Whether the subscription URL is valid.
* Whether DNS is working correctly.
* Whether `proxy-server-nameserver` and upstream DNS settings are suitable for your network.
* Whether the configured provider path is writable by the Mihomo process.

The current template defines subscription providers from `my-subscription-1` to `my-subscription-6`. The default proxy groups use these providers through the `use` lists under `Manually Select Nodes`, `Auto Select Nodes`, and `Fallback`.

## Runtime and GEO Notes

This NAS template includes always-on service settings for routing, process matching, connection behavior, and GEO data updates.

Important fields include:

```yaml
find-process-mode: off
mode: rule
log-level: info
ipv6: true
unified-delay: true
tcp-concurrent: true
geodata-mode: true
geo-auto-update: true
geo-update-interval: 24
geodata-loader: memconservative
global-ua: "clash.meta"
etag-support: false
```

Recommended practice:

* Keep `find-process-mode: off` for NAS, router, and container-style environments unless you specifically need process matching.
* Keep `mode: rule` if you want traffic to follow the rule list.
* Use `log-level: debug` only while troubleshooting, then switch back to `info`.
* Keep GEO auto-update enabled if your rule providers depend on current geosite or geoip data.
* If memory is limited, `geodata-loader: memconservative` is usually a reasonable NAS default.

## Recommended Editing Areas

In most cases, you should review these sections first:

```yaml
mixed-port:
allow-lan:
bind-address:
authentication:
skip-auth-prefixes:
external-controller:
external-controller-cors:
secret:
find-process-mode:
tcp-concurrent:
geodata-mode:
geo-auto-update:
tun:
sniffer:
dns:
proxies:
proxy-providers:
proxy-groups:
rule-providers:
rules:
```

Usually, you do not need to modify every section.

## TUN Mode Notes

This NAS template enables TUN mode by default.

Example:

```yaml
tun:
  enable: true
  stack: system
  device: mihomo
  auto-route: true
  auto-redirect: true
  auto-detect-interface: true
  strict-route: true
```

TUN mode can route more traffic through Mihomo, but it requires proper system and container permissions.

Recommended practice:

* Confirm basic proxy mode works before relying on TUN mode.
* Make sure the NAS or Docker container has TUN support.
* Keep LAN, router, NAS, multicast, and local service address ranges excluded from TUN routing.
* If LAN access breaks, check `route-exclude-address`, `dns.fake-ip-filter`, and `rules`.
* If the container fails to start, temporarily set `tun.enable` to `false` and check the logs.

## DNS Notes

This template includes a detailed DNS section for NAS and LAN environments.

Important fields include:

```yaml
dns:
  enable: true
  listen: 0.0.0.0:1253
  enhanced-mode: fake-ip
  fake-ip-range: 198.18.0.1/16
  fake-ip-filter:
```

Recommended practice:

* Keep `fake-ip-filter` entries for LAN, router, NAS, IoT, NTP, STUN, and local service domains.
* Use `nameserver-policy` for private domains if your LAN relies on a router or local DNS server.
* If local devices or NAS services cannot be reached, check `fake-ip-filter`, `nameserver-policy`, and private address rules.
* If provider downloads fail, check `default-nameserver` and `proxy-server-nameserver`.

## Sniffer Notes

The template includes `sniffer` settings to help Mihomo identify real domain names from HTTP, TLS, and QUIC traffic.

Sniffer can improve rule matching in TUN, redir, TProxy, and fake-ip scenarios. However, some LAN devices, IoT devices, private services, or special applications may not work well with sniffing.

Recommended practice:

* Keep LAN and local domains in `skip-domain`.
* Keep private IP ranges in `skip-dst-address`.
* Add specific client IPs to `skip-src-address` if one LAN device behaves abnormally.
* If a service breaks after enabling TUN or sniffer, disable sniffer temporarily or add skip rules.

## Security Notes

Do not commit the following information to this repository:

* Real subscription URLs
* Real proxy node passwords
* API tokens
* Dashboard secrets
* Private server addresses
* Personal access tokens
* Real `authentication` usernames or passwords
* Real `external-controller` secrets
* Private dashboard origins or controller access details

Recommended local private file names:

```text
config.local.yaml
config.private.yaml
subscription.txt
secret.yaml
docker-compose.local.yaml
```

These files should be ignored by `.gitignore`.

## Troubleshooting

### Mihomo cannot start

Check:

* YAML indentation
* Missing `:` symbols
* Incorrect list indentation
* Unsupported fields in your Mihomo version
* Whether the config file path is mounted correctly
* Whether the container has permission to read `config.yaml`
* Whether TUN mode requires extra permissions

### Dashboard cannot connect

Check:

* Whether `external-controller` is listening on the correct address and port
* Whether `secret` matches the dashboard setting
* Whether `external-controller-cors` allows the dashboard origin
* Whether the NAS firewall allows the controller port
* Whether Docker port mapping includes the controller port
* Whether the dashboard is using the correct NAS IP address

### LAN devices cannot use the proxy

Check:

* Whether `allow-lan` is enabled
* Whether `bind-address` allows LAN access
* Whether `lan-allowed-ips` includes the client IP
* Whether `authentication` requires a username and password on the client
* Whether `skip-auth-prefixes` should include a trusted local source address
* Whether the NAS firewall allows the proxy port
* Whether Docker port mapping includes `mixed-port`
* Whether the client uses the correct NAS IP and proxy port

### Proxy providers do not update

Check:

* Whether the subscription URL is valid
* Whether the NAS can resolve DNS
* Whether the NAS can reach the subscription server
* Whether the provider path is writable
* Whether provider names match the names used in `proxy-groups`
* Whether client logs show download or parse errors

### Proxy group is empty

Check whether your proxy group uses the correct provider name:

```yaml
proxy-providers:
  my-subscription-1:

proxy-groups:
  - name: Manually Select Nodes
    type: select
    use:
      - my-subscription-1
```

The name under `use` must match the name under `proxy-providers`.

### LAN devices or local services cannot be accessed

Check:

```yaml
route-exclude-address:
dns.fake-ip-filter:
sniffer.skip-domain:
sniffer.skip-dst-address:
rules:
```

Common causes include fake-ip handling, TUN routing, sniffer behavior, or missing private network direct rules.

### Internet works but some websites do not

Check:

* `rules`
* `rule-providers`
* `dns`
* `fake-ip-filter`
* Selected proxy group
* Provider health check results
* Mihomo logs

## Recommended Workflow

```text
Download config.yaml
-> Replace proxies / proxy-providers
-> Replace authentication and controller secret
-> Adjust LAN / DNS / TUN settings
-> Deploy to NAS or Docker
-> Restart Mihomo
-> Update proxy providers, rule providers, and GEO data
-> Open dashboard
-> Select proxy groups
-> Test LAN clients and local services
```

## Disclaimer

This configuration template is only used for personal learning and configuration management.

This repository does not provide proxy services, subscription services, proxy nodes, or network access services. Users are responsible for their own configuration, subscriptions, and network usage.

## License

This project is licensed under the GNU General Public License v3.0.
