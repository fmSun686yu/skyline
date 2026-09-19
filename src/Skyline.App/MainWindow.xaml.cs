using System.Windows;

namespace Skyline.App;

public partial class MainWindow : Window
{
    public MainWindow() : this(new ShellViewModel()) { }

    public MainWindow(ShellViewModel model)
    {
        InitializeComponent();
        DataContext = model;
    }
}
