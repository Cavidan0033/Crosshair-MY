#nullable disable

using System;
using System.Windows; // System.Windows.Forms yoxdur!

namespace Crosshair_MY
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                MainWindow crosshair = new MainWindow();
                ControlWindow menu = new ControlWindow(crosshair);
                this.MainWindow = menu;
                crosshair.Show();
                menu.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error: " + ex.Message, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}