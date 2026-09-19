using System.Globalization;
using System.Windows;

namespace Skyline.App;

public partial class App : Application
{
    public App()
    {
        // S01 has English resources only. Full language switching is planned for S05.
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");
    }
}
