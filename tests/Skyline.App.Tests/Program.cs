using System.Globalization;
using Skyline.App;
using Skyline.Core;

var checks = 0;
var failures = 0;
void Check(string name, Action action)
{
    checks++;
    try { action(); Console.WriteLine($"PASS {name}"); }
    catch (Exception error) { failures++; Console.WriteLine($"FAIL {name}: {error}"); }
}
void Assert(bool value) { if (!value) throw new Exception("Assertion failed."); }
Check("shell shows verified templates and S02 boundary", () =>
{
    var model = new ShellViewModel();
    Assert(model.IsReady && model.Templates.Count == 2);
    Assert(model.Templates[0].Version == "Template v1.2.3");
    Assert(model.Templates[1].Version == "Template v2.1.2");
    Assert(model.Stage == "S02 / CORE PREVIEW");
    Assert(model.VersionLabel.Contains("0.1.0-s02"));
    Assert(model.BoundaryBody.Contains("later approved stages"));
});
foreach (var code in Enum.GetValues<TemplateError>())
    Check($"safe English error presentation: {code}", () =>
    {
        var model = new ShellViewModel(() => throw new TemplateCatalogException(code));
        Assert(!model.IsReady && model.Templates.Count == 0);
        Assert(model.StatusTitle == "Built-in templates unavailable");
        Assert(model.StatusBody.Contains("Download a complete copy"));
        Assert(model.Diagnostic == $"Diagnostic: {code}");
    });
foreach (var language in new[] { "zh-CN", "de-DE", "ja-JP" })
    Check($"English default independent of system UI culture: {language}", () =>
    {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
        var model = new ShellViewModel();
        Assert(model.ProductName == "Skyline Configurator");
        Assert(model.StatusTitle == "Built-in templates verified");
        Assert(model.VersionLabel.EndsWith("English (United States)"));
    });
Console.WriteLine($"{checks - failures}/{checks} passed.");
return failures == 0 ? 0 : 1;
