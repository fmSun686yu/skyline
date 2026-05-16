# Shadowrocket Configuration

This directory contains a Shadowrocket configuration template for Apple platforms.

It is designed for personal Shadowrocket usage on iOS, iPadOS, and macOS environments where users want to keep a reusable configuration file for proxy groups, basic routing behavior, host mappings, and URL rewrite rules.

This template does **not** include real proxy nodes, subscription URLs, or any third-party network service. You need to add your own proxy subscriptions or nodes in Shadowrocket after importing the configuration.

## File Structure

```text
Shadowrocket/
├── skyline_sr.conf
└── README.md
```

## File Description

| File | Description |
| --- | --- |
| `skyline_sr.conf` | Shadowrocket configuration template |
| `README.md` | Usage guide for this template |

## Suitable Scenarios

This template is intended for:

* Shadowrocket on iOS
* Shadowrocket on iPadOS
* Shadowrocket on macOS, if available in your environment
* Personal proxy group management
* Reusable routing and rewrite configuration
* Importing a local or URL-based Shadowrocket config

## Configuration Overview

The template contains the following major sections:

```ini
[General]
[Proxy Group]
[Rule]
[Host]
[URL Rewrite]
```

### General

The `[General]` section controls basic Shadowrocket behavior, including bypass rules, DNS behavior, IPv6, ICMP reply behavior, UDP fallback behavior, and local network exclusions.

Important fields include:

```ini
bypass-system = true
skip-proxy = 192.168.0.0/16, 10.0.0.0/8, 172.16.0.0/12, localhost, *.local, captive.apple.com
tun-excluded-routes = 10.0.0.0/8, 100.64.0.0/10, 127.0.0.0/8, 169.254.0.0/16, 172.16.0.0/12, 192.0.0.0/24, 192.0.2.0/24, 192.88.99.0/24, 192.168.0.0/16, 198.51.100.0/24, 203.0.113.0/24, 224.0.0.0/4, 255.255.255.255/32, 239.255.255.250/32
dns-server = system
fallback-dns-server = system
ipv6 = true
prefer-ipv6 = false
dns-direct-system = false
icmp-auto-reply = true
always-reject-url-rewrite = false
private-ip-answer = true
dns-direct-fallback-proxy = false
udp-policy-not-supported-behaviour = REJECT
use-local-host-item-for-proxy = false
```

For most users, the default values can be kept first. If local network devices, AirPlay, NAS, router management pages, or captive portal checks do not work correctly, review `skip-proxy` and `tun-excluded-routes`.

### Proxy Group

The `[Proxy Group]` section defines how traffic is assigned to different proxy groups.

The template includes groups such as:

```text
Chain-Hop-1
Chain-Hop-2
Manually Select Nodes
Auto Select Nodes
Fallback
AI_non_cn
Apple
Microsoft
YouTube
Google
GitHub
Telegram
OneDrive
Twitter
Instagram
Facebook
```

These groups are placeholders and routing targets. After importing the configuration, make sure your real subscription nodes or manually added nodes match the group names used by the template.

The template references subscription placeholders from `MY-SUBSCRIPTION-1` to `MY-SUBSCRIPTION-7` in `Manually Select Nodes`, `Auto Select Nodes`, and `Fallback`.

The template also includes chain-style groups:

```ini
Chain-Hop-2 = select,USA-SAN FRANCISCO-STATIC NATIVE ISP
Chain-Hop-1 = select,MANUALLY SELECT NODES,policy-select-name=MANUALLY SELECT NODES
```

If you do not use chain-style routing, you can simplify these groups after import or select ordinary subscription nodes in the service groups.

### Rule

The `[Rule]` section is used to define routing rules.

The template includes built-in rules and remote `RULE-SET` entries for common routing targets.

Current rule categories include:

* Private domains and private IP ranges, routed to `DIRECT`
* Non-China AI services, routed to `AI_non_cn`
* YouTube, routed to `YouTube`
* Apple service rule sets, routed to `Apple`
* Telegram, routed to `Telegram`
* X / Twitter, routed to `Twitter`
* Instagram, routed to `Instagram`
* China mainland IP ranges, routed to `DIRECT`
* Unmatched traffic, routed to `MANUALLY SELECT NODES`

The final rule is:

```ini
FINAL,MANUALLY SELECT NODES
```

This means unmatched traffic will use the `MANUALLY SELECT NODES` policy. Add your own domain, IP, GEOIP, or `RULE-SET` entries above the final rule according to your usage.

### Host

The `[Host]` section defines local host mappings.

Example:

```ini
localhost = 127.0.0.1
```

Usually, you do not need to modify this section unless you want to add specific local domain mappings.

### URL Rewrite

The `[URL Rewrite]` section contains URL rewrite rules.

Example:

```ini
^https?://(www.)?g.cn($|/.*) https://www.google.com$2 302
^https?://(www.)?google.cn($|/.*) https://www.google.com$2 302
```

Only keep or add rewrite rules that you understand. Incorrect rewrite rules may cause websites or apps to behave unexpectedly.

## Usage

### 1. Download the Template

Download the following file:

```text
Shadowrocket/skyline_sr.conf
```

You can either download it directly from GitHub or clone the whole repository locally.

### 2. Import into Shadowrocket

You can import the configuration manually or from a URL.

#### Method 1: Import the file manually

1. Download `skyline_sr.conf`.
2. Open Shadowrocket.
3. Import the configuration file into the app.
4. Add your own proxy subscriptions or proxy nodes.
5. Check proxy group names.
6. Select the desired proxy group.
7. Test connectivity.

#### Method 2: Import from URL

1. Upload or host `skyline_sr.conf` in a location accessible by URL.
2. Open Shadowrocket.
3. Use URL-based configuration download or update.
4. Import the configuration.
5. Add your own proxy subscriptions or proxy nodes.
6. Check proxy group names.
7. Select the desired proxy group.
8. Test connectivity.

## Add Proxy Subscriptions or Nodes

This template does not include real proxy nodes.

After importing the configuration, add your own proxy subscriptions or nodes in Shadowrocket.

Common workflow:

```text
Open Shadowrocket
-> Add subscription or proxy node
-> Update subscription
-> Open proxy group list
-> Select the correct node or group
-> Enable Shadowrocket
-> Test connectivity
```

If you use subscription names such as `MY-SUBSCRIPTION-1`, `MY-SUBSCRIPTION-2`, or similar placeholders, make sure they match the names referenced in `[Proxy Group]`.

The current template references:

```text
MY-SUBSCRIPTION-1
MY-SUBSCRIPTION-2
MY-SUBSCRIPTION-3
MY-SUBSCRIPTION-4
MY-SUBSCRIPTION-5
MY-SUBSCRIPTION-6
MY-SUBSCRIPTION-7
```

If your actual subscription names are different, update the `[Proxy Group]` entries or rename the subscriptions in Shadowrocket.

## Check Proxy Group Names

Proxy group and policy references should stay consistent after import.

For example, if the final rule uses:

```ini
FINAL,MANUALLY SELECT NODES
```

Then the policy shown by Shadowrocket should also exist with the same name.

If the template uses a group such as:

```ini
Google = select,CHAIN-HOP-2,MANUALLY SELECT NODES,AUTO SELECT NODES,FALLBACK
```

Then the referenced policy names should exist and be selectable in Shadowrocket.

If group names do not match, Shadowrocket may fail to route traffic as expected, or a group may appear empty.

This template uses uppercase policy references such as `CHAIN-HOP-2`, `MANUALLY SELECT NODES`, `AUTO SELECT NODES`, and `FALLBACK` inside service groups. Keep these references consistent with the policy names shown by Shadowrocket after import.

## Recommended Editing Areas

In most cases, you only need to review these parts:

```ini
[General]
[Proxy Group]
[Rule]
[Host]
[URL Rewrite]
```

Recommended fields or items to check:

* `skip-proxy`
* `tun-excluded-routes`
* `dns-server`
* `fallback-dns-server`
* `dns-direct-system`
* `private-ip-answer`
* `dns-direct-fallback-proxy`
* `udp-policy-not-supported-behaviour`
* Proxy group names
* Final routing policy
* Remote `RULE-SET` URLs
* URL rewrite rules

Usually, you do not need to modify every section.

## Local Network Notes

The template includes common private network ranges in `skip-proxy` and `tun-excluded-routes`.

These settings help keep LAN traffic direct, including:

* Router management pages
* NAS services
* Local DNS
* AirPlay or local discovery traffic
* Captive portal checks
* Other private network devices

If LAN devices or local services cannot be accessed after enabling Shadowrocket, review:

```ini
skip-proxy =
tun-excluded-routes =
dns-server =
fallback-dns-server =
```

## DNS Notes

The template uses system DNS by default:

```ini
dns-server = system
fallback-dns-server = system
dns-direct-system = false
dns-direct-fallback-proxy = false
private-ip-answer = true
```

This is a conservative default for compatibility. If you use custom DNS, encrypted DNS, or remote DNS behavior inside Shadowrocket, test local network access and app compatibility after changing these values.

If direct domains fail to resolve correctly, review `dns-direct-system` and `dns-direct-fallback-proxy`. If private IP responses are incorrectly treated as hijacking, review `private-ip-answer`.

## IPv6 Notes

The template enables IPv6 support:

```ini
ipv6 = true
prefer-ipv6 = false
```

Recommended practice:

* Keep `ipv6 = true` if your network supports IPv6.
* Keep `prefer-ipv6 = false` unless you specifically want IPv6 to be preferred.
* If some apps behave abnormally on IPv6 networks, test with IPv6 disabled or adjust your DNS settings.

## Security Notes

Do not commit the following information to this repository:

* Real subscription URLs
* Real proxy node addresses
* Real proxy node usernames or passwords
* API tokens
* Private server addresses
* Personal access tokens
* Private configuration overrides

Recommended local private file names:

```text
skyline_sr.local.conf
skyline_sr.private.conf
subscription.txt
secret.txt
```

These files should be ignored by `.gitignore`.

## Troubleshooting

### The config cannot be imported

Check:

* Whether the file extension is `.conf`
* Whether section names such as `[General]`, `[Proxy Group]`, and `[Rule]` are valid
* Whether each rule line uses the correct Shadowrocket syntax
* Whether there are unexpected invisible characters
* Whether the file was modified by a rich text editor instead of a plain text editor

### No proxy nodes appear

Check:

* Whether you have added subscriptions or nodes in Shadowrocket
* Whether the subscription update succeeded
* Whether the subscription URL is valid
* Whether the subscription provides nodes supported by Shadowrocket
* Whether node names or group references match the template

### Proxy group is empty

Check whether the proxy group references existing nodes, subscriptions, or policy groups.

Example:

```ini
Manually Select Nodes = select,MY-SUBSCRIPTION-1,MY-SUBSCRIPTION-2,MY-SUBSCRIPTION-3,MY-SUBSCRIPTION-4,MY-SUBSCRIPTION-5,MY-SUBSCRIPTION-6,MY-SUBSCRIPTION-7,use=true
```

If the referenced `MY-SUBSCRIPTION-*` entries do not exist in Shadowrocket, the group may not work as expected.

### Internet works but some websites do not

Check:

* `[Rule]`
* Final policy
* Selected proxy group
* Remote `RULE-SET` availability
* DNS settings
* URL rewrite rules
* Subscription node availability

### LAN devices or local services cannot be accessed

Check:

```ini
skip-proxy =
tun-excluded-routes =
dns-server =
fallback-dns-server =
```

If the issue only affects local devices, make sure your local subnet is included in `skip-proxy` and `tun-excluded-routes`.

### URL rewrite causes website issues

Check:

* Whether the rewrite rule matches too broadly
* Whether the target URL is correct
* Whether the rule should be removed temporarily for testing
* Whether Shadowrocket is applying rewrite rules even when global routing is disabled

## Recommended Workflow

```text
Download skyline_sr.conf
-> Import into Shadowrocket
-> Add subscription or proxy nodes
-> Update subscriptions
-> Check proxy group names
-> Select proxy group
-> Test Internet connectivity
-> Test local network access
-> Adjust rules or rewrite entries if needed
```

## Disclaimer

This configuration template is only used for personal learning and configuration management.

This repository does not provide proxy services, subscription services, proxy nodes, or network access services. Users are responsible for their own configuration, subscriptions, and network usage.

## License

This project is licensed under the GNU General Public License v3.0.
