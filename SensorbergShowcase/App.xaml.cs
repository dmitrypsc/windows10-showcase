using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Microsoft.HockeyApp;
using Prism.Unity.Windows;

namespace SensorbergShowcase
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : PrismUnityApplication
    {
        private static ResourceLoader _resourceLoader;

        /// <summary>
        /// Resource loader for app-wide localization needs.
        /// </summary>
        public static ResourceLoader ResourceLoader
        {
            get
            {
                if (_resourceLoader == null)
                {
                    _resourceLoader = new ResourceLoader();
                }

                return _resourceLoader;
            }
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
//            LogManagerFactory.DefaultConfiguration.IsEnabled = false;
            HockeyClient.Current.Configure("d98620a9d3984f349f8685a25a3cfae1");
        }


        protected override async Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
        {
            NavigationService.Navigate("Main", null);
        }
    }
}
