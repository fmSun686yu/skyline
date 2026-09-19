using System.Reflection;
using Skyline.Core;
using Skyline.Infrastructure;

namespace Skyline.App;

public sealed class ShellViewModel
{
    public string ProductName => Text.Get("ProductName");
    public string Stage => Text.Get("Stage");
    public string Subtitle => Text.Get("Subtitle");
    public string VersionLabel => Text.Format("VersionFormat",
        typeof(ShellViewModel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);
    public string TemplatesHeading => Text.Get("TemplatesHeading");
    public string BoundaryTitle => Text.Get("BoundaryTitle");
    public string BoundaryBody => Text.Get("BoundaryBody");
    public string Footer => Text.Get("Footer");
    public bool IsReady { get; }
    public string StatusTitle { get; }
    public string StatusBody { get; }
    public string Diagnostic { get; } = "";
    public IReadOnlyList<TemplateRow> Templates { get; } = Array.Empty<TemplateRow>();

    public ShellViewModel() : this(() => new EmbeddedTemplateCatalog()) { }

    public ShellViewModel(Func<ITemplateCatalog> createCatalog)
    {
        try
        {
            var catalog = createCatalog();
            Templates = catalog.Templates.Select(item => new TemplateRow(
                Text.Get(item.Id == "desktop" ? "DesktopName" : "NasName"),
                Text.Format("TemplateVersionFormat", item.Version),
                Text.Format("SlotsFormat", item.SubscriptionSlots.Count), item.Sha256)).ToArray();
            IsReady = true;
            StatusTitle = Text.Get("ReadyTitle");
            StatusBody = Text.Get("ReadyBody");
        }
        catch (TemplateCatalogException error)
        {
            StatusTitle = Text.Get("ErrorTitle");
            StatusBody = Text.Get("ErrorBody");
            Diagnostic = Text.Format("DiagnosticFormat", error.Code);
        }
    }
}

public sealed record TemplateRow(string Name, string Version, string Detail, string Hash)
{
    public string HashLabel => Text.Get("HashLabel");
}
