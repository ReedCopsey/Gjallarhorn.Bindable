using System.Windows;
using ElmInspiredOne;
using Gjallarhorn.Bindable;
using Gjallarhorn.Bindable.Framework;
using Gjallarhorn.Wpf.CSharp;
using Framework = Gjallarhorn.Wpf.Framework;

namespace WpfElmInspiredOneCSharp
{
    /// <inheritdoc />
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            Framework.RunApplication(Navigation.SingleView<Program.Model, Nav.SimpleNavigation<Program.Msg>, Program.Msg, MainWindow>(() => new MainWindow()), Program.applicationCore);
        }
    }
}
