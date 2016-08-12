using SensorbergSDK;
using SensorbergShowcase.Utils;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Email;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using MetroLog;
using SensorbergShowcase.Model;

namespace SensorbergShowcase.Pages
{
    /// <summary>
    /// Construction and navigation related main page implementation reside here.
    /// We also manage the global main page components here such as the dialog
    /// control.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /*
         * Insert the manufacturer ID and beacon code for filtering beacons below.
         */
        private const ushort ManufacturerId = 0x004c;
        private const ushort BeaconCode = 0x0215;

        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<MainPage>();
        private const int SettingsPivotIndex = 2;

        private SDKManager _sdkManager;
        private bool _appIsOnForeground;
        public MainPageModel Model { get; } = new MainPageModel();
        public ICommand SendLogsCommand { get; } = new SendLogsCommand();


        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            double displaySize = DeviceUtils.ResolveDisplaySizeInInches();
            System.Diagnostics.Debug.WriteLine("Display size is " + displaySize + " inches");
            Model.IsBigScreen = displaySize > 6d ? true : false;

            hub.Background.Opacity = 0.6d;
            pivot.Background.Opacity = 0.6d;
            
            SizeChanged += OnMainPageSizeChanged;

            MainPageScanner(); // Scanner specific construction

            DataContext = this;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Logger.Debug("MainPage.OnNavigatedTo");
            base.OnNavigatedTo(e);

            if (_sdkManager == null)
            {
                _sdkManager = SDKManager.Instance();
                _sdkManager.ScannerStatusChanged += OnScannerStatusChangedAsync;
                _sdkManager.LayoutValidityChanged += OnBeaconLayoutValidityChangedAsync;
                _sdkManager.BackgroundFiltersUpdated += OnBackgroundFiltersUpdatedAsync;
                _sdkManager.BeaconActionResolved += OnBeaconActionResolvedAsync;
                _sdkManager.FailedToResolveBeaconAction += OnFailedToResolveBeaconAction;
                await TryToReinitializeSDK();
            }

            LoadApplicationSettings();

            if (QrCodeScannerPage.ScannedQrCode != null)
            {
                Logger.Debug("MainPage.OnNavigatedTo: Applying the scanned API key: " + QrCodeScannerPage.ScannedQrCode);

                if (pivot.Visibility == Visibility.Visible)
                {
                    pivot.SelectedIndex = SettingsPivotIndex;
                }

                Model.ApiKey = QrCodeScannerPage.ScannedQrCode;
                SaveApplicationSettings(KeyApiKey);
            }

            ValidateApiKeyAsync().ConfigureAwait(false); // Do not await

            if (_advertiser == null)
            {
                _advertiser = new Advertiser();
                _advertiser.ManufacturerId = ManufacturerId;
                _advertiser.BeaconCode = BeaconCode;
            }

            SetScannerSpecificEvents(true);

            Window.Current.VisibilityChanged += OnVisibilityChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Logger.Debug("MainPage.OnNavigatedFrom");

            Window.Current.VisibilityChanged -= OnVisibilityChanged;

//            _sdkManager.ScannerStatusChanged -= OnScannerStatusChangedAsync;
//            _sdkManager.LayoutValidityChanged -= OnBeaconLayoutValidityChangedAsync;
//            _sdkManager.BackgroundFiltersUpdated -= OnBackgroundFiltersUpdatedAsync;
//            _sdkManager.BeaconActionResolved -= OnBeaconActionResolvedAsync;
//            _sdkManager.FailedToResolveBeaconAction -= OnFailedToResolveBeaconAction;

            SetScannerSpecificEvents(false);

            if (_sdkManager.IsScannerStarted)
            {
                _sdkManager.StopScanner();
            }

            SaveApplicationSettings();

            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// Helper method for showing informational message dialogs, which do not require command handling.
        /// </summary>
        /// <param name="message">The message to show.</param>
        /// <param name="title">The title of the message dialog.</param>
        private async void ShowInformationalMessageDialogAsync(string message, string title = null)
        {
            MessageDialog messageDialog = (title == null) ? new MessageDialog(message) : new MessageDialog(message, title);
            messageDialog.Commands.Add(new UICommand(App.ResourceLoader.GetString("ok/Text")));
            await messageDialog.ShowAsync();
        }

        /// <summary>
        /// Creates and displays a message dialog containing the current status of the SDK and Bluetooth.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnCheckStatusButtonClickedAsync(object sender, RoutedEventArgs e)
        {
            string statusAsContentString = App.ResourceLoader.GetString("bluetooth/Text") + ": ";

            if (await DeviceUtils.GetIsBluetoothSupportedAsync())
            {
                if (await DeviceUtils.GetIsBluetoothEnabledAsync())
                {
                    statusAsContentString += App.ResourceLoader.GetString("enabled/Text");
                }
                else
                {
                    statusAsContentString += App.ResourceLoader.GetString("disabled/Text");
                }
            }
            else
            {
                statusAsContentString += App.ResourceLoader.GetString("notSupported/Text");
            }

            statusAsContentString +=
                "\n" + App.ResourceLoader.GetString("apiKey/Text") + ": "
                + (Model.IsApiKeyValid ? App.ResourceLoader.GetString("valid/Text") : App.ResourceLoader.GetString("invalid/Text"))
                + "\n" + App.ResourceLoader.GetString("beaconLayout/Text") + ": "
                + (IsLayoutValid ? App.ResourceLoader.GetString("valid/Text") : App.ResourceLoader.GetString("invalid/Text"));

            MessageDialog messageDialog = new MessageDialog(
                statusAsContentString,
                App.ResourceLoader.GetString("status/Text"));

            await messageDialog.ShowAsync();
        }

        private async void OnBackgroundFiltersUpdatedAsync(object sender, EventArgs e)
        {
            MessageDialog messageDialog = new MessageDialog(
                App.ResourceLoader.GetString("backgroundTaskFiltersUpdated/Text"),
                App.ResourceLoader.GetString("updateSuccessful/Text"));
            await messageDialog.ShowAsync();
        }

        /// <summary>
        /// Called when the app is brought on foreground or put in background.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnVisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            _appIsOnForeground = e.Visible;
            _sdkManager.OnApplicationVisibilityChanged(sender, e);

            if (_appIsOnForeground)
            {
                SetScannerSpecificEvents(true);
            }
            else
            {
                SetScannerSpecificEvents(false);
            }
        }

        private void OnMainPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Model.IsBigScreen)
            {
                Model.BeaconDetailsControlWidth = 350d;
            }
            else
            {
                if (layoutGrid.ActualWidth > 0)
                {
                    Model.BeaconDetailsControlWidth = layoutGrid.ActualWidth - 40d;
                }
            }
        }
    }
    public class SendLogsCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public async void Execute(object parameter)
        {
            var emailMessage = new EmailMessage();
            emailMessage.Subject = emailMessage.Body = "Logs from " + Package.Current.DisplayName;
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                Stream s = await LogManagerFactory.DefaultLogManager.GetCompressedLogs();
                MemoryStream m = new MemoryStream();
                await s.CopyToAsync(m);
                byte[] b = m.ToArray();
                InMemoryRandomAccessStream mem = new InMemoryRandomAccessStream();
                await mem.WriteAsync(b.AsBuffer());

                emailMessage.Attachments.Add(new EmailAttachment("logs.zip", RandomAccessStreamReference.CreateFromStream(mem)));
                await EmailManager.ShowComposeNewEmailAsync(emailMessage);
            }
            else
            {
                await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder);
            }
        }
    }
}
