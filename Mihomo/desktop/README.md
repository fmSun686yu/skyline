# Mihomo Desktop Configuration

This directory contains a Mihomo configuration template for desktop clients on **Windows** and **macOS**.

It is designed for Mihomo-compatible desktop clients such as:

* Clash Party
* Clash Verge / Clash Verge Rev
* FlClash
* Other Mihomo-based desktop clients

This template does **not** include real proxy nodes or subscription URLs. You need to replace the proxy-related sections before importing it into your client.

## File Structure

```text
Mihomo/desktop/
├── config.yaml
└── README.md
```

## File Description

| File | Description |
| --- | --- |
| `config.yaml` | Mihomo desktop configuration template |
| `README.md` | Usage guide for this template |

## Usage

### 1. Download the Template

Download the following file:

```text
Mihomo/desktop/config.yaml
```

You can either download it directly from GitHub or clone the whole repository locally.

### 2. Edit Proxy Configuration

Open `config.yaml` with a text editor, such as:

* Visual Studio Code
* Sublime Text
* Notepad++
* Any plain text editor

Then replace the following sections with your own proxy nodes or subscription providers:

```yaml
proxies:
  # Replace this section with your own proxy nodes
proxy-providers:
  # Replace this section with your own subscription providers
```

Mihomo supports `proxy-providers` with different provider types such as `http`, `file`, and `inline`. For an HTTP subscription provider, the `url` field is required, and `interval` controls the update interval in seconds. See the official Mihomo proxy provider documentation for details.

Example:

```yaml
proxy-providers:
  my-subscription-1:
    type: http
    url: "https://example.com/your-subscription-url"
    path: ./proxy_providers/my-subscription-1.yaml
    interval: 10800
    proxy: DIRECT
    health-check:
      enable: true
      url: https://www.gstatic.com/generate_204
      interval: 600
      timeout: 5000
      lazy: true
      expected-status: 204
    override:
      additional-prefix: "[SUB-1]"
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
      - my-subscription-2
      - my-subscription-3
```

If your provider is named `my-subscription-1`, then the proxy group should also use:

```yaml
use:
  - my-subscription-1
```

If the names do not match, the client may fail to load the configuration correctly.

The current template defines subscription providers from `my-subscription-1` to `my-subscription-6`. The default proxy groups actively use `my-subscription-2` and `my-subscription-3`, while the other providers are available as placeholders that you can enable by uncommenting them in the relevant `use` lists.

### 4. Import into Desktop Client

Import the modified `config.yaml` into your Mihomo-compatible client.

Common workflow:

```text
Open client
    ↓
Profiles / Configs
    ↓
Import local config file
    ↓
Select config.yaml
    ↓
Enable the profile
```

Different clients may use slightly different menu names, but the general idea is the same: import the YAML configuration file as a local profile.

### 5. Select Proxy Mode

This desktop template is mainly designed for rule-based usage.

Common Mihomo modes include:

```yaml
mode: rule
```

Mihomo supports `rule`, `global`, and `direct` operation modes. `rule` means traffic is matched according to routing rules; `global` sends traffic through the selected global proxy group; `direct` sends traffic directly without proxy.

Recommended desktop setting:

```yaml
mode: rule
```

### 6. Test Connection

After importing the configuration:

1. Update proxy providers.
2. Select a proxy node or proxy group.
3. Enable system proxy or TUN mode if needed.
4. Open a browser and test connectivity.
5. Check client logs if something does not work.

## Recommended Editing Areas

In most cases, you only need to edit these parts:

```yaml
port:
socks-port:
mixed-port:
allow-lan:
bind-address:
external-controller:
secret:
tun:
dns:
proxies:
proxy-providers:
proxy-groups:
rule-providers:
rules:
```

Usually, you do not need to modify every section.

## Port and LAN Notes

This desktop template exposes local proxy ports and keeps LAN access disabled by default.

Important fields include:

```yaml
port: 7890
socks-port: 7891
mixed-port: 7892
allow-lan: false
bind-address: 127.0.0.1
```

Recommended practice:

* Keep `allow-lan: false` if the proxy is only used on your own computer.
* Keep `bind-address: 127.0.0.1` for better local-only safety.
* Enable LAN access only when you intentionally want other devices to use this desktop client as a proxy.

## Dashboard Notes

The template includes a local controller for compatible dashboards and client UI integrations.

```yaml
external-controller: '127.0.0.1:9090'
secret: 'your_secret_here'
```

Recommended practice:

* Replace `secret` before using the configuration long term.
* Keep `external-controller` bound to `127.0.0.1` unless you need remote dashboard access.
* Do not expose the controller port to untrusted networks.

## DNS Notes

This template may include a DNS section such as:

```yaml
dns:
  enable: true
  enhanced-mode: fake-ip
  fake-ip-range: 198.18.0.1/16
  fake-ip-range6: fdfe:dcba:9876::1/64
```

Mihomo DNS supports options such as `enable`, `listen`, `enhanced-mode`, `fake-ip-range`, `fake-ip-filter`, `nameserver`, `fallback`, and `nameserver-policy`. The official documentation notes that `enhanced-mode` supports `fake-ip` and `redir-host`, with `redir-host` as the default.

For desktop clients, the recommended approach is:

* Keep the default DNS settings if you are not sure.
* Use `fake-ip` only when the client and your system environment support it properly.
* Add domains to `fake-ip-filter` if some local services, LAN devices, or special applications do not work correctly.
* Check `proxy-server-nameserver` if proxy provider updates fail because provider domain names cannot be resolved.

## TUN Mode Notes

This desktop template enables TUN mode by default:

```yaml
tun:
  enable: true
  stack: system
  mtu: 3000
```

TUN mode can route more application traffic through Mihomo, but it may require additional system permissions.

Recommended practice:

* Start with normal system proxy mode first.
* Enable TUN mode only after the basic proxy configuration works.
* On Windows and macOS, make sure the client has the required network permissions.
* If TUN mode causes network issues, disable it and check the client logs.

## Rule Provider Notes

The template uses remote `rule-providers` and references them in `rules`.

The rule sets cover:

* Private network traffic
* AI services outside China
* Google
* Apple
* Microsoft, OneDrive, and GitHub
* Telegram
* X / Twitter
* China mainland domains and IP ranges

If routing does not behave as expected, update rule providers in the client first, then check whether each `RULE-SET` name in `rules` matches a provider under `rule-providers`.

## Security Notes

Do not commit the following information to this repository:

* Real subscription URLs
* Real proxy node passwords
* API tokens
* Dashboard secrets
* Real dashboard `secret` values
* Private server addresses
* Personal access tokens

Recommended local private file names:

```text
config.local.yaml
config.private.yaml
subscription.txt
secret.yaml
```

These files should be ignored by `.gitignore`.

## Troubleshooting

### The config cannot be imported

Check:

* YAML indentation
* Missing `:` symbols
* Incorrect list indentation
* Unsupported fields in your client
* Whether `proxy-groups` references an existing proxy or provider

### No proxy nodes appear

Check:

* Whether `proxy-providers` has a valid `url`
* Whether the subscription URL is accessible
* Whether the provider name matches the name used in `proxy-groups`
* Whether the provider update failed in client logs

### Proxy group is empty

Check whether your proxy group uses the correct provider name:

```yaml
proxy-groups:
  - name: Manually Select Nodes
    type: select
    use:
      - my-subscription-1
```

The name under `use` must match the name under `proxy-providers`.

### Internet works but some websites do not

Check:

* `rules`
* `rule-providers`
* `dns`
* `fake-ip-filter`
* Selected proxy node
* Client logs

### LAN devices or local services cannot be accessed

Check:

```yaml
allow-lan:
bind-address:
dns.fake-ip-filter:
rules:
```

If you only use the configuration on your own desktop computer, you usually do not need to enable LAN access.

## Recommended Workflow

```text
Download config.yaml
        ↓
Replace proxies / proxy-providers
        ↓
Check proxy-groups
        ↓
Import into desktop client
        ↓
Update providers
        ↓
Select proxy group
        ↓
Test connectivity
```

## Disclaimer

This configuration template is only used for personal learning and configuration management.

This repository does not provide proxy services, subscription services, proxy nodes, or network access services. Users are responsible for their own configuration, subscriptions, and network usage.

## License

This project is licensed under the GNU General Public License v3.0.
