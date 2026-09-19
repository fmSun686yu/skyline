using System.Globalization;
using System.Resources;

namespace Skyline.App;

public static class Text
{
    private static readonly ResourceManager Resources = new("Skyline.App.Resources.Strings.Strings", typeof(Text).Assembly);
    public static string Get(string key) => Resources.GetString(key, CultureInfo.GetCultureInfo("en-US"))
        ?? throw new InvalidOperationException($"Missing English UI resource: {key}");
    public static string Format(string key, params object[] arguments) =>
        string.Format(CultureInfo.GetCultureInfo("en-US"), Get(key), arguments);
}
