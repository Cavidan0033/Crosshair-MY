using System.Windows;

namespace Crosshair_MY
{
    public partial class EcoModeDialog : Window
    {
        public EcoModeDialog()
        {
            InitializeComponent();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Pəncərəni bağla
        }
    }
}