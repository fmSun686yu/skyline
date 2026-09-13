# Mihomo Desktop Configuration

This directory contains the Mihomo desktop configuration template **v1.2.3** for clients on **Windows** and **macOS**.

It is designed for Mihomo-compatible desktop clients such as:

* Clash Party
* Clash Verge / Clash Verge Rev
* FlClash
* Other Mihomo-based desktop clients

This template does **not** include real proxy nodes, credentials, or subscription URLs. The SOCKS5 node and six HTTP subscription providers are placeholders; replace or remove them before importing the configuration into your client.

The current defaults use rule mode, prefer IPv4, enable local process detection on Windows/macOS, and enable TUN with a mixed stack, automatic routing, interface detection, strict routing, and DNS hijacking.

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

Then replace or remove the example entries in the following sections:

```yaml
proxies:
  # Replace or remove the example SOCKS5 node
proxy-providers:
  # Replace or remove my-subscription-1 through my-subscription-6
```

The included SOCKS5 node is used as the second-hop exit in the example chain. Replace its server, port, and credentials before use:

```yaml
proxies:
  - name: USA_Static_Native_ISP
    type: socks5
    dialer-proxy: chain-hop1-entry
    server: socks5.example.com
    port: 1080
    username: "YOUR_SOCKS5_USERNAME"
    password: "YOUR_SOCKS5_PASSWORD"
    ip-version: ipv4-prefer
    udp: true
    # interface-name: eth0
    tfo: false
    mptcp: false
```

Delete `username` and `password` if the server does not require authentication. Keep `dialer-proxy` only when you want this node to connect through `chain-hop1-entry`; otherwise remove it and adjust the related proxy groups for your own topology. `interface-name` remains optional and is commented out by default.

Mihomo supports `proxy-providers` with different provider types such as `http`, `file`, and `inline`. For an HTTP subscription provider, the `url` field is required, and `interval` controls the update interval in seconds. See the official Mihomo proxy provider documentation for details.

Example:

```yaml
proxy-providers:
  my-subscription-1:
    type: http
    url: "https://provider-1.example.com/api/v1/client/subscribe?token=YOUR_SUBSCRIPTION_TOKEN_1"
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
      udp: true
      ip-version: ipv4-prefer
```

The `override` block enables UDP for imported nodes and makes them prefer IPv4, matching the template's IPv4-oriented defaults. Do not commit real subscription URLs, tokens, proxy server addresses, usernames, or passwords to a public repository.

### 3. Check Proxy Groups

After replacing `proxies` or `proxy-providers`, make sure your proxy groups reference the correct provider names.

Example:

```yaml
proxy-groups:
  - name: Manually Select Nodes
    type: select
    use:
      - my-subscription-1
      - my-subscription-2
      - my-subscription-3
      - my-subscription-4
      - my-subscription-5
      - my-subscription-6
```

If your provider is named `my-subscription-1`, then the proxy group should also use:

```yaml
use:
  - my-subscription-1
```

If the names do not match, the client may fail to load the configuration correctly.

The template defines `my-subscription-1` through `my-subscription-6`. All six are enabled by default in `Manually Select Nodes`, `Auto Select Nodes`, and `Fallback`. Configure all six providers, or remove each unused provider from `proxy-providers` and from all three `use` lists.

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
ipv6:
find-process-mode:
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

The common template leaves `redir-port` and `tproxy-port` commented out. macOS can use the redir TCP listener only with matching system redirection rules; tproxy is Linux/Android-only. Neither listener is required for TUN. HTTP 7890, SOCKS 7891 and mixed 7892 remain available.

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

The template uses these IPv4-oriented DNS defaults:

```yaml
ipv6: false

dns:
  enable: true
  prefer-h3: false
  ipv6: false
  enhanced-mode: fake-ip
  fake-ip-range: 198.18.0.1/16
  # fake-ip-range6: fdfe:dcba:9876::1/64
```

Mihomo DNS supports options such as `enable`, `listen`, `enhanced-mode`, `fake-ip-range`, `fake-ip-filter`, `nameserver`, `fallback`, and `nameserver-policy`. The official documentation notes that `enhanced-mode` supports `fake-ip` and `redir-host`, with `redir-host` as the default.

For desktop clients, the recommended approach is:

* Keep the default DNS settings if you are not sure.
* IPv6 resolution and the IPv6 fake-IP range are disabled by default. If you enable IPv6, review both the top-level `ipv6` setting and the settings under `dns` together.
* Use `fake-ip` only when the client and your system environment support it properly.
* Add domains to `fake-ip-filter` if some local services, LAN devices, or special applications do not work correctly.
* The sample private-domain DNS address under `nameserver-policy` is `192.168.50.1`; replace it with your router or local DNS address.
* The two generic STUN entries in `fake-ip-filter` are commented out by default. Uncomment them only if your environment needs STUN domains to bypass fake-IP.
* Check `proxy-server-nameserver` if proxy provider updates fail because provider domain names cannot be resolved.

## TUN Mode Notes

This desktop template enables TUN mode by default:

```yaml
tun:
  enable: true
  stack: mixed
  auto-route: true
  auto-detect-interface: true
  strict-route: true
  dns-hijack:
    - any:53
    - tcp://any:53
  udp-timeout: 300
  endpoint-independent-nat: false
```

The mixed stack uses the system stack for TCP and gVisor for UDP. Automatic routing and interface detection are enabled, and ordinary UDP/TCP DNS traffic on port 53 is sent to Mihomo. On Windows, `strict-route: true` is intended to reduce DNS bypass on systems with multiple network adapters.

Recommended practice:

* TUN mode is enabled by default and may require additional system permissions.
* If this is your first setup, you can temporarily disable TUN and test normal system proxy mode first.
* On Windows and macOS, make sure the client has the required network permissions.
* If TUN mode causes network issues, disable it and check the client logs. You can also test whether `strict-route` or DNS hijacking conflicts with other VPN, DNS, or security software.

`udp-timeout: 300` means 300 seconds of idle UDP NAT mapping lifetime, not a five-minute maximum for all active UDP connections.

## Process Detection Notes

The template enables process detection by default:

```yaml
find-process-mode: always
```

`always` is retained to attempt local process identification for connection details and process rules. Recognition depends on platform, permissions and connection type, and can fail. It does not add process routing rules automatically. Windows and macOS process names/paths may differ; no Edge-specific process rule is currently defined.

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
* Instagram
* Facebook
* Twitch
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

## Deployment Checklist and Local DNS

Before importing a private deployment copy:

1. Replace the residential server, integer port and credentials.
2. Fill all six enabled subscriptions. To disable one, disable both its provider and its `use` entries in `Manually Select Nodes`, `Auto Select Nodes`, and `Fallback`.
3. Replace `192.168.50.1` in `dns.nameserver-policy` with a DNS server that resolves private names in the **target deployment network**. Do not copy the generator computer's DNS settings into a different NAS network.
4. Ensure that this upstream does not forward the same queries back to Mihomo and create a resolution loop.

Changing only one subscription URL leaves the other enabled examples unresolved. Removing only a provider leaves invalid references; removing only its references leaves an unnecessary enabled provider.

The commented `+.lan` and `+.home.arpa` policies are optional examples; replace their addresses too when enabling them. `.local` discovery commonly involves mDNS and cannot be guaranteed by ordinary DNS policies alone. Fake-IP exceptions return real addresses but do not themselves select DIRECT or prove WebRTC/UDP behavior.

`fallback` also uses response filtering; it is not only a timeout backup. The empty `fallback-filter` is commented out. Omitting a custom filter does not disable the core's default fallback filtering.

## Residential Chain Health Check

The existing exit group now performs a non-lazy check every ten minutes:

```yaml
- name: chain-hop2-exit
  type: select
  proxies:
    - USA_Static_Native_ISP
  url: https://www.gstatic.com/generate_204
  interval: 600
  timeout: 10000
  lazy: false
  expected-status: 204
```

This is an item under `proxy-groups`, not an additional top-level section or a provider `health-check` block. Keep `dialer-proxy: chain-hop1-entry` on the residential node.

```text
Mihomo -> currently selected entry node -> residential SOCKS5 exit -> test website
```

`interval` is in seconds; `timeout` is in milliseconds. `lazy: false` enables periodic checks even while the residential exit is idle. Checks consume a small amount of subscription and residential traffic; they cannot complete while the core is stopped, the device sleeps or networking is unavailable.

Testing the residential node uses its configured first hop. Testing a subscription node checks that entry only. The check does not enumerate every entry/exit combination. After an entry change, refresh the result because history may describe the previous entry. Node DNS resolution and subscription downloads retain their own configured paths.

A successful result means that this request reached the test URL through the current chain and received HTTP 204. It does not prove the exit IP's residential status, fixed address, UDP support or availability of every business website. Failure can arise from the entry, residential authentication, DNS, connectivity, timeout or the test website itself.

The group remains `select`: no direct bypass, automatic entry search, notification service or residential failover is added. The chosen entry group retains its existing selection behavior. Clients may display the result differently.

### Manual Verification

1. Use a private configuration copy with valid subscription and residential information. Load it, check for parse/reference errors, update providers, and record the current mode and selections.
2. Set `chain-hop1-entry` to `Manually Select Nodes`, then select a concrete entry A. Check A's delay to establish an entry-only baseline.
3. Run the client's **Mihomo HTTP delay test** for `USA_Static_Native_ISP`. Its `dialer-proxy` keeps the full chain active. Record delay, timeout or authentication errors; delay is not simply the sum of two network pings.
4. During an interruptible test window, switch to `global`, select `chain-hop2-exit` or the residential node in `GLOBAL`, and keep entry A fixed. Query a trusted public-IP service through this Mihomo instance. Confirm the residential path in connection details and compare the result with the service provider's expected exit.
5. Restore `rule` and the original selections. Test residential-routed services, domestic direct services and local NAS/router access; inspect the matched rule and final node. A failure only in rule mode points first to routing or selections.
6. Test required UDP applications separately. HTTP/HTTPS delay and IP checks do not prove UDP support. Verify that UDP enters the core through a suitable SOCKS5 or TUN path and uses the intended exit; observe drops or interruptions.
7. Repeat with entry B, compare results, then restore all original settings. Keep addresses, credentials and sensitive logs out of the public repository.

On Windows/macOS, confirm that the browser enters the intended instance through system proxy, explicit proxy or TUN. For NAS testing, first use its explicit port 7892 and **NAS inbound** credentials. An HTTP proxy test does not cover arbitrary application UDP. Global-mode changes affect other new connections handled by that core; schedule NAS tests accordingly and open fresh connections after switching strategies.

The residential `server` may be an access gateway and need not equal the exit IP. An entry IP in the result suggests an incorrect group/path; a local broadband IP suggests bypass or DIRECT. Neither conclusion replaces inspection of the actual request path. A public-IP query alone cannot establish residential classification or long-term address stability.

| Entry | Entry test | Residential chain test | Exit IP | Business test | Time |
| --- | --- | --- | --- | --- | --- |
| A | Record locally | Record locally | Keep private | Record locally | Record locally |
| B | Record locally | Record locally | Keep private | Record locally | Record locally |

### Automatic Check Acceptance

In an isolated deployment copy, verify successful checks with a fixed valid entry, result updates while the exit is idle, and failure with intentionally incorrect residential credentials. Confirm there is no residential bypass, restore valid credentials, and check recovery. Switch entries and refresh the result. Complete public-IP and business verification manually as above; never perform fault injection on a shared live profile.

## First Start and Persistent Resources

A fresh installation needs subscription data, remote rule sets and the GEO data actually referenced by DNS/rules. MRS routing rules do not remove the `geosite:private` and `geosite:cn` DNS dependencies.

Provider paths are relative to the core's actual working/home directory (for example the directory configured by `-d`), not necessarily the YAML file's directory. The directory must be readable/writable as required. Persist the necessary configuration, provider/rule caches and selection/Fake-IP state when deploying NAS containers; inspect the service's real working directory and volume mapping.

Keep the existing download strategies and resource URLs. For failures, distinguish DNS errors, unreachable subscription/GitHub resources, content errors and filesystem permissions. Do not route a first subscription download through nodes that have not yet been obtained.

Verify a first start using an **independent empty working directory**, then a restart with its populated caches. Do not erase a live instance's cache for testing. Cached restart success is not evidence that a clean first download works.

## Changes in v1.2.3 (2026-09-13)

- D1/D3/D4: platform-specific listeners commented, `always` retained with corrected notes, UDP idle timeout reduced to 300 seconds.
- C1/C3/C4/C5/C6: local-DNS and six-subscription instructions; empty fallback filter commented; `localhost`, `+.local`, `+.home.arpa` added to Fake-IP exceptions; empty group node lists made explicit.
- C8/C9/C10: full residential chain checks, verification and cache documentation, synchronized comments and version.

Implementation follows the [approved v4 plan](../../docs/change-plans/2026-09-13-mihomo-template-optimization-v4.md). Template version numbers are independent of plan versions. See the plan's implementation record for static validation results. Live Windows/macOS/NAS routing, subscription access, credentials, exit IP and UDP behavior require deployment verification; no tested client/core version is claimed here.

## Disclaimer

This configuration template is only used for personal learning and configuration management.

This repository does not provide proxy services, subscription services, proxy nodes, or network access services. Users are responsible for their own configuration, subscriptions, and network usage.

## License

This project is licensed under the GNU General Public License v3.0.
