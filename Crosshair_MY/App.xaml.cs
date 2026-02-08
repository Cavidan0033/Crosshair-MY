using System.Globalization;
using System.Windows;

namespace Crosshair_MY
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Dil ayarları (vacib deyil, amma qalsa yaxşıdır)
            SetLanguage("az");

            // Proqram əsas pəncərə bağlananda tam bağlansın
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;

            base.OnStartup(e);

            // === DÜZƏLİŞ BURADADIR ===
            // Sadəcə MainWindow-u (Crosshair) açırıq.
            // ControlWindow-u (Menyu) MainWindow özü açacaq.

            MainWindow crosshair = new MainWindow();
            this.MainWindow = crosshair;
            crosshair.Show();

            // DİQQƏT: Buradan "ControlWindow control = new..." sətrini SİLDİK!
            // Artıq ikinci pəncərə açılmayacaq.
        }

        public static void SetLanguage(string cultureCode)
        {
            try
            {
                var culture = new CultureInfo(cultureCode);
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch { }
        }
    }
}