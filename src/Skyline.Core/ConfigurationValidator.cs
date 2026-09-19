using YamlDotNet.RepresentationModel;
using static Skyline.Core.YamlTree;

namespace Skyline.Core;

internal static class ConfigurationValidator
{
    private static readonly HashSet<string> BuiltIns = new(StringComparer.Ordinal) { "DIRECT", "REJECT", "REJECT-DROP", "PASS", "COMPATIBLE" };
    internal static void Validate(YamlMappingNode root)
    {
        var nodes = Maps(root, "proxies").Concat(Maps(root, "proxy-groups")).ToArray();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
            if (!names.Add(Str(node, "name")) || BuiltIns.Contains(Str(node, "name"))) throw new ConfigurationException(IssueCode.DuplicateName);
        void Reference(string target)
        {
            if (!names.Contains(target) && !BuiltIns.Contains(target)) throw new ConfigurationException(IssueCode.InvalidReference);
        }
        var providers = Map(Get(root, "proxy-providers"));
        var rules = Map(Get(root, "rule-providers"));
        var edges = names.ToDictionary(n => n, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            var name = Str(node, "name");
            if (Has(node, "proxies")) foreach (var target in Seq(Get(node, "proxies")).Children.Select(Str))
            {
                Reference(target);
                if (names.Contains(target)) edges[name].Add(target);
            }
            if (Has(node, "dialer-proxy"))
            {
                var target = Str(node, "dialer-proxy");
                Reference(target);
                if (names.Contains(target)) edges[name].Add(target);
            }
            if (Has(node, "use")) foreach (var target in Seq(Get(node, "use")).Children.Select(Str))
                if (!Has(providers, target)) throw new ConfigurationException(IssueCode.InvalidReference);
        }
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        void Visit(string name)
        {
            if (visited.Contains(name)) return;
            if (!visiting.Add(name)) throw new ConfigurationException(IssueCode.DependencyCycle);
            foreach (var target in edges[name]) Visit(target);
            visiting.Remove(name);
            visited.Add(name);
        }
        foreach (var name in names) Visit(name);

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers.Children.Values.Concat(rules.Children.Values).Select(Map))
        {
            if (Has(provider, "proxy")) Reference(Str(provider, "proxy"));
            if (Has(provider, "path") && !paths.Add(NormalizePath(Str(provider, "path"))))
                throw new ConfigurationException(IssueCode.CachePathCollision);
        }
        foreach (var rule in Seq(Get(root, "rules")).Children.Select(Str))
        {
            var parts = rule.Split(',');
            if (parts[0] == "RULE-SET" && parts.Length is 3 or 4)
            {
                if (!Has(rules, parts[1])) throw new ConfigurationException(IssueCode.InvalidReference);
                Reference(parts[2]);
            }
            else if (parts[0] == "MATCH" && parts.Length == 2) Reference(parts[1]);
            else throw new ConfigurationException(IssueCode.ContractMismatch); // Contract v1 rule grammar.
        }
    }

    private static string NormalizePath(string path)
    {
        var segments = new List<string>();
        foreach (var part in path.Replace('\\', '/').Split('/'))
        {
            if (part is "" or ".") continue;
            if (part == "..")
            {
                if (segments.Count == 0) throw new ConfigurationException(IssueCode.ContractMismatch);
                segments.RemoveAt(segments.Count - 1);
            }
            else segments.Add(part);
        }
        if (segments.Count == 0) throw new ConfigurationException(IssueCode.ContractMismatch);
        return string.Join('/', segments);
    }
}
