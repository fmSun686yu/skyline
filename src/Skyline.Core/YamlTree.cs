using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace Skyline.Core;

internal sealed class ConfigurationException(IssueCode code) : Exception(code.ToString())
{
    internal IssueCode Code { get; } = code;
}

internal static class YamlTree
{
    private static readonly HashSet<string> StandardTags = new(StringComparer.Ordinal)
    {
        "tag:yaml.org,2002:str", "tag:yaml.org,2002:int", "tag:yaml.org,2002:float",
        "tag:yaml.org,2002:bool", "tag:yaml.org,2002:null", "tag:yaml.org,2002:map", "tag:yaml.org,2002:seq"
    };
    internal static YamlMappingNode Parse(string text)
    {
        if (text.Length > 1024 * 1024) throw new ConfigurationException(IssueCode.InvalidYaml);
        var parser = new Parser(new StringReader(text));
        var depth = 0;
        while (parser.MoveNext())
        {
            if (parser.Current is AnchorAlias || parser.Current is NodeEvent n && (!n.Anchor.IsEmpty || (!n.Tag.IsEmpty && !StandardTags.Contains(n.Tag.ToString()))))
                throw new ConfigurationException(IssueCode.InvalidYaml);
            if (parser.Current is MappingStart or SequenceStart && ++depth > 64) throw new ConfigurationException(IssueCode.InvalidYaml);
            if (parser.Current is MappingEnd or SequenceEnd) depth--;
        }
        var stream = new YamlStream();
        stream.Load(new StringReader(text)); // YamlMappingNode rejects duplicate keys.
        if (stream.Documents.Count != 1 || stream.Documents[0].RootNode is not YamlMappingNode root)
            throw new ConfigurationException(IssueCode.InvalidYaml);
        CheckKeys(root);
        return root;
    }

    private static void CheckKeys(YamlNode node)
    {
        if (node is YamlMappingNode map)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pair in map.Children)
            {
                if (pair.Key is not YamlScalarNode key || key.Value is null || key.Value == "<<" || !keys.Add(key.Value))
                    throw new ConfigurationException(IssueCode.InvalidYaml);
                CheckKeys(pair.Value);
            }
        }
        else if (node is YamlSequenceNode seq) foreach (var child in seq.Children) CheckKeys(child);
    }

    internal static YamlNode Get(YamlMappingNode map, string key) => map.Children.TryGetValue(new YamlScalarNode(key), out var value)
        ? value : throw new ConfigurationException(IssueCode.ContractMismatch);
    internal static YamlMappingNode Map(YamlNode node) => node as YamlMappingNode ?? throw new ConfigurationException(IssueCode.ContractMismatch);
    internal static YamlSequenceNode Seq(YamlNode node) => node as YamlSequenceNode ?? throw new ConfigurationException(IssueCode.ContractMismatch);
    internal static string Str(YamlNode node) => node is YamlScalarNode s && s.Value is not null
        ? s.Value : throw new ConfigurationException(IssueCode.ContractMismatch);
    internal static string Str(YamlMappingNode map, string key) => Str(Get(map, key));
    internal static bool Has(YamlMappingNode map, string key) => map.Children.ContainsKey(new YamlScalarNode(key));
    internal static void Set(YamlMappingNode map, string key, YamlNode value) => map.Children[new YamlScalarNode(key)] = value;
    internal static void Remove(YamlMappingNode map, string key) => map.Children.Remove(new YamlScalarNode(key));
    internal static YamlScalarNode Text(string value) => new(value) { Style = ScalarStyle.DoubleQuoted };
    internal static YamlScalarNode Integer(int value) => new(value.ToString(CultureInfo.InvariantCulture)) { Style = ScalarStyle.Plain };
    internal static IEnumerable<YamlMappingNode> Maps(YamlMappingNode root, string key) => Seq(Get(root, key)).Children.Select(Map);
    internal static YamlMappingNode Named(YamlMappingNode root, string key, string name)
    {
        var matches = Maps(root, key).Where(m => Str(m, "name") == name).ToArray();
        if (matches.Length != 1) throw new ConfigurationException(IssueCode.ContractMismatch);
        return matches[0];
    }
    internal static string Write(YamlMappingNode root)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture) { NewLine = "\n" };
        new YamlStream(new YamlDocument(root)).Save(writer, false);
        return writer.ToString().Replace("\r\n", "\n");
    }

    // YAML scalar spelling and type class are preserved, while harmless quoting changes are allowed.
    internal static string Kind(YamlScalarNode scalar)
    {
        if (!scalar.Tag.IsEmpty)
            return scalar.Tag.ToString() switch
            {
                "tag:yaml.org,2002:str" => "string",
                "tag:yaml.org,2002:bool" => "bool",
                "tag:yaml.org,2002:null" => "null",
                "tag:yaml.org,2002:int" or "tag:yaml.org,2002:float" => "number",
                _ => throw new ConfigurationException(IssueCode.InvalidYaml)
            };
        if (scalar.Style != ScalarStyle.Plain && scalar.Style != ScalarStyle.Any) return "string";
        var s = scalar.Value ?? "";
        if (s is "" or "~" or "null" or "Null" or "NULL") return "null";
        if (s is "true" or "True" or "TRUE" or "false" or "False" or "FALSE") return "bool";
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            || s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || s.StartsWith("0o", StringComparison.OrdinalIgnoreCase)
            || s is ".inf" or "-.inf" or ".nan") return "number";
        return "string";
    }
    internal static bool Equal(YamlNode a, YamlNode b)
    {
        if (a is YamlScalarNode sa && b is YamlScalarNode sb) return sa.Value == sb.Value && Kind(sa) == Kind(sb);
        if (a is YamlSequenceNode qa && b is YamlSequenceNode qb)
            return qa.Children.Count == qb.Children.Count && qa.Children.Zip(qb.Children).All(p => Equal(p.First, p.Second));
        if (a is YamlMappingNode ma && b is YamlMappingNode mb)
            return ma.Children.Count == mb.Children.Count && ma.Children.All(p => mb.Children.TryGetValue(p.Key, out var v) && Equal(p.Value, v));
        return false;
    }
}
