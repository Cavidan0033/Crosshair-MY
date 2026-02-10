using System.Windows;

namespace Crosshair_MY
{
    public partial class HotkeyOverlay : Window
    {
        public HotkeyOverlay()
        {
            InitializeComponent();

            // Pəncərəni monitorun sağ üst küncünə yerləşdiririk
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 10;
            this.Top = 10;
        }
    }
}