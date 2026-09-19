using YamlDotNet.RepresentationModel;
using static Skyline.Core.YamlTree;

namespace Skyline.Core;

internal static class TemplateContractValidator
{
    internal static void Validate(YamlMappingNode root, TemplateDescriptor template)
    {
        void Require(bool valid) { if (!valid) throw new ConfigurationException(IssueCode.ContractMismatch); }
        Require(template.ContractVersion == 1 && template.Id is "desktop" or "nas"
            && template.EntryGroup == "chain-hop1-entry" && template.ExitGroup == "chain-hop2-exit"
            && template.ResidentialProxyName == "USA_Static_Native_ISP"
            && template.RequiredEnvironmentFields.SequenceEqual(new[] { "localDns" })
            && template.SubscriptionSlots.SequenceEqual(Enumerable.Range(1, 6).Select(i => $"my-subscription-{i}")));
        ConfigurationValidator.Validate(root);
        var residential = Named(root, "proxies", template.ResidentialProxyName);
        Require(Str(residential, "type") == "socks5" && Str(residential, "dialer-proxy") == template.EntryGroup);
        foreach (var key in new[] { "server", "username", "password" }) Require(Get(residential, key) is YamlScalarNode value && Kind(value) == "string");
        Require(Get(residential, "port") is YamlScalarNode port && Kind(port) == "number"
            && int.TryParse(port.Value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var portValue) && portValue is >= 1 and <= 65535);
        var entry = Named(root, "proxy-groups", template.EntryGroup);
        Require(Str(entry, "type") == "select" && Seq(Get(entry, "proxies")).Children.Select(Str)
            .SequenceEqual(new[] { "Manually Select Nodes", "Auto Select Nodes", "Fallback" }));
        var exit = Named(root, "proxy-groups", template.ExitGroup);
        Require(Str(exit, "type") == "select" && Seq(Get(exit, "proxies")).Children.Select(Str).SequenceEqual(new[] { template.ResidentialProxyName }));
        Require(Str(exit, "url") == "https://www.gstatic.com/generate_204" && Str(exit, "interval") == "600"
            && Str(exit, "timeout") == "10000" && Str(exit, "lazy") == "false" && Str(exit, "expected-status") == "204");
        Require(Seq(Get(Named(root, "proxy-groups", "AI_non_cn"), "proxies")).Children.Select(Str).SequenceEqual(new[] { template.ExitGroup }));
        var providers = Map(Get(root, "proxy-providers"));
        Require(providers.Children.Keys.Select(Str).ToHashSet(StringComparer.Ordinal).SetEquals(template.SubscriptionSlots));
        foreach (var slot in template.SubscriptionSlots)
        {
            var provider = Map(Get(providers, slot));
            Require(Str(provider, "type") == "http" && Get(provider, "url") is YamlScalarNode);
            var directory = template.Id == "desktop" ? "proxy_providers" : "proxy-providers";
            Require(Str(provider, "path") == $"./{directory}/{slot}.yaml");
            _ = Map(Get(provider, "health-check"));
            _ = Map(Get(provider, "override"));
        }
        foreach (var group in new[] { "Manually Select Nodes", "Auto Select Nodes", "Fallback" })
            Require(Seq(Get(Named(root, "proxy-groups", group), "use")).Children.Select(Str).SequenceEqual(template.SubscriptionSlots));
        Require(Seq(Get(Map(Get(Map(Get(root, "dns")), "nameserver-policy")), "geosite:private")).Children.Count == 1);
        Require(Get(root, "secret") is YamlScalarNode secret && Kind(secret) == "string");
        if (template.Id == "nas") Require(Seq(Get(root, "authentication")).Children.Count == 1
            && Seq(Get(root, "authentication")).Children[0] is YamlScalarNode auth && Kind(auth) == "string");
        else Require(!Has(root, "authentication"));
        CheckTypes(root);
    }

    private static void CheckTypes(YamlMappingNode root)
    {
        void Type(YamlMappingNode map, string key, string expected, bool optional = false)
        {
            if (optional && !Has(map, key)) return;
            if (Get(map, key) is not YamlScalarNode scalar || Kind(scalar) != expected)
                throw new ConfigurationException(IssueCode.ContractMismatch);
            if (expected == "number" && (!long.TryParse(scalar.Value, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var number) || number < 0))
                throw new ConfigurationException(IssueCode.ContractMismatch);
        }
        foreach (var key in new[] { "ipv6", "allow-lan" }) Type(root, key, "bool");
        foreach (var key in new[] { "port", "socks-port", "mixed-port" }) Type(root, key, "number", true);
        Type(root, "external-controller", "string");
        var tun = Map(Get(root, "tun"));
        Type(tun, "enable", "bool");
        Type(tun, "stack", "string");
        foreach (var key in new[] { "auto-route", "auto-redirect", "strict-route" }) Type(tun, key, "bool", true);
        var dns = Map(Get(root, "dns"));
        Type(dns, "enable", "bool");
        Type(dns, "ipv6", "bool");
        Type(dns, "listen", "string");
        foreach (var node in Maps(root, "proxies")) Type(node, "udp", "bool", true);
        foreach (var group in Maps(root, "proxy-groups"))
        {
            foreach (var key in new[] { "interval", "timeout", "expected-status" }) Type(group, key, "number", true);
            Type(group, "lazy", "bool", true);
        }
        foreach (var provider in Map(Get(root, "proxy-providers")).Children.Values.Select(Map))
        {
            Type(provider, "interval", "number");
            var health = Map(Get(provider, "health-check"));
            Type(health, "enable", "bool");
            Type(health, "lazy", "bool");
            Type(health, "url", "string");
            foreach (var key in new[] { "interval", "timeout", "expected-status" }) Type(health, key, "number");
        }
    }
}
