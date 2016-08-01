using SensorbergSDK;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.UI.Popups;
using SensorbergSDK.Internal.Data;
using SensorbergShowcase.Controls;

namespace SensorbergShowcase.Pages
{
    /// <summary>
    /// Code for the settings part of the main page. Here we also control the
    /// background task registration.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string KeyEnableActions = "enable_actions";
        private const string KeyApiKey = "api_key";
        private const string KeyEmail = "email";
        private const string KeyPassword = "password";
        private const string KeyBeaconId1 = "beacon_id_1";
        private const string KeyBeaconId2 = "beacon_id_2";
        private const string KeyBeaconId3 = "beacon_id_3";
        private const string KeyEnableBackgroundTask = "enable_backgroundtask";

        private ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private ApiKeyHelper _apiKeyHelper = new ApiKeyHelper();
        private bool _apiKeyWasJustSuccessfullyFetchedOrReset = false;

        #region Properties (API key, email, password, background task status etc.)

        public string Email
        {
            get
            {
                return (string)GetValue(EmailProperty);
            }
            private set
            {
                SetValue(EmailProperty, value);
            }
        }
        public static readonly DependencyProperty EmailProperty =
            DependencyProperty.Register("Email", typeof(string), typeof(MainPage),
                new PropertyMetadata(string.Empty));

        public string Password
        {
            get
            {
                return (string)GetValue(PasswordProperty);
            }
            private set
            {
                SetValue(PasswordProperty, value);
            }
        }
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register("Password", typeof(string), typeof(MainPage),
                new PropertyMetadata(string.Empty));

        public bool IsValidatingOrFetchingApiKey
        {
            get
            {
                return (bool)GetValue(IsValidatingOrFetchingApiKeyProperty);
            }
            private set
            {
                SetValue(IsValidatingOrFetchingApiKeyProperty, value);
            }
        }
        public static readonly DependencyProperty IsValidatingOrFetchingApiKeyProperty =
            DependencyProperty.Register("IsValidatingOrFetchingApiKey", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));



        public bool IsScannerAvailable
        {
            get { return (bool)GetValue(IsScannerAvailableProperty); }
            set { SetValue(IsScannerAvailableProperty, value); }
        }

        public static readonly DependencyProperty IsScannerAvailableProperty =
            DependencyProperty.Register("IsScannerAvailable", typeof(bool), typeof(MainPage),
                new PropertyMetadata(true));

        #endregion

        private void LoadApplicationSettings()
        {
            Logger.Debug("LoadApplicationSettings");
            if (_localSettings.Values.ContainsKey(KeyEnableActions))
            {
                Model.AreActionsEnabled = (bool)_localSettings.Values[KeyEnableActions];
            }

            if (_localSettings.Values.ContainsKey(KeyApiKey))
            {
                Model.ApiKey = _localSettings.Values[KeyApiKey].ToString();
            }
            else
            {
                Model.ApiKey = SDKManager.DemoApiKey;
            }

            if (_localSettings.Values.ContainsKey(KeyEmail))
            {
                Email = _localSettings.Values[KeyEmail].ToString();
            }

            if (_localSettings.Values.ContainsKey(KeyPassword))
            {
                Password = _localSettings.Values[KeyPassword].ToString();
            }

            if (_localSettings.Values.ContainsKey(KeyBeaconId1))
            {
                BeaconId1 = _localSettings.Values[KeyBeaconId1].ToString();
            }

            if (_localSettings.Values.ContainsKey(KeyBeaconId2))
            {
                BeaconId2 = _localSettings.Values[KeyBeaconId2].ToString();
            }

            if (_localSettings.Values.ContainsKey(KeyBeaconId3))
            {
                BeaconId3 = _localSettings.Values[KeyBeaconId3].ToString();
            }

            if (_localSettings.Values.ContainsKey(KeyEnableBackgroundTask))
            {
                Model.ShouldRegisterBackgroundTask = (bool)_localSettings.Values[KeyEnableBackgroundTask];
            }

            Model.IsBackgroundTaskRegistered = _sdkManager.IsBackgroundTaskRegistered;
        }

        /// <summary>
        /// Saves the application settings.
        /// </summary>
        /// <param name="key">If empty or null, will save all settings. Otherwise will save the 
        /// specific settings related to the given key.</param>
        private void SaveApplicationSettings(string key = null)
        {
            Logger.Debug("SaveApplicationSettings Key={0}", key);
            if (string.IsNullOrEmpty(key) || key.Equals(KeyEnableActions))
            {
                _localSettings.Values[KeyEnableActions] = Model.AreActionsEnabled;
            }

            if (string.IsNullOrEmpty(key) || key.Equals(KeyEnableBackgroundTask))
            {
                _localSettings.Values[KeyEnableBackgroundTask] = Model.ShouldRegisterBackgroundTask;
            }


            if (string.IsNullOrEmpty(key) || key.Equals(KeyApiKey))
            {
                _localSettings.Values[KeyApiKey] = Model.ApiKey;
                _localSettings.Values[KeyEmail] = Email;
                _localSettings.Values[KeyPassword] = Password;
            }

            if (string.IsNullOrEmpty(key) || key.Equals(KeyBeaconId1))
            {
                _localSettings.Values[KeyBeaconId1] = BeaconId1;
                _localSettings.Values[KeyBeaconId2] = BeaconId2;
                _localSettings.Values[KeyBeaconId3] = BeaconId3;
            }
        }

        private async Task TryToReinitializeSDK()
        {
            Logger.Debug("TryToReinitializeSDK {0}", _sdkManager != null);
            if (_sdkManager != null)
            {
                _sdkManager.Deinitialize(false);
                _sdkManager.BeaconActionResolved -= OnBeaconActionResolvedAsync;
                _sdkManager.FailedToResolveBeaconAction -= OnFailedToResolveBeaconAction;
                await _sdkManager.InitializeAsync(new SdkConfiguration()
                {
                    ManufacturerId = ManufacturerId,
                    BeaconCode = BeaconCode,
                    ApiKey = Model.ApiKey,
                    BackgroundAdvertisementClassName = "SensorbergShowcaseBackgroundTask.SensorbergShowcaseAdvertisementBackgroundTask",
                    BackgroundTimerClassName = "SensorbergShowcaseBackgroundTask.SensorbergShowcaseTimedBackgrundTask"
                });
                _sdkManager.StartScanner();


                _sdkManager.BeaconActionResolved += OnBeaconActionResolvedAsync;
                _sdkManager.FailedToResolveBeaconAction += OnFailedToResolveBeaconAction;

            }
        }

        /// <summary>
        /// Validates the given API key.
        /// </summary>
        /// <param name="displayResultDialogInCaseOfFailure">If true, will display a result dialog in case of an error.</param>
        /// <returns>The API key validation result.</returns>
        private async Task<ApiKeyValidationResult> ValidateApiKeyAsync(bool displayResultDialogInCaseOfFailure = false)
        {
            IsValidatingOrFetchingApiKey = true;

            ApiKeyValidationResult result = await _apiKeyHelper.ValidateApiKey(Model.ApiKey);

            if (result == ApiKeyValidationResult.Valid)
            {
                Model.IsApiKeyValid = true;
                await TryToReinitializeSDK();
            }
            else
            {
                Model.IsApiKeyValid = false;

                if (displayResultDialogInCaseOfFailure)
                {
                    string message = App.ResourceLoader.GetString("unknownApiKeyValidationError/Text");

                    switch (result)
                    {
                        case ApiKeyValidationResult.Invalid:
                            message = App.ResourceLoader.GetString("invalidApiKey/Text");
                            break;
                        case ApiKeyValidationResult.NetworkError:
                            message = App.ResourceLoader.GetString("apiKeyValidationFailedDueToNetworkError/Text");
                            break;
                    }

                    ShowInformationalMessageDialogAsync(message, App.ResourceLoader.GetString("apiKeyNotValidated/Text"));
                }
            }

            IsValidatingOrFetchingApiKey = false;
            return result;
        }

        private async void OnValidateApiKeyButtonClicked(object sender, RoutedEventArgs e)
        {
            await ValidateApiKeyAsync(true);
        }

        private async void OnFetchApiKeyButtonClickedAsync(object sender, RoutedEventArgs e)
        {
            IsValidatingOrFetchingApiKey = true;

            if (!string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password))
            {
                NetworkResult result = await _apiKeyHelper.FetchApiKeyAsync(Email, Password);

                if (result == NetworkResult.Success)
                {
                    _apiKeyWasJustSuccessfullyFetchedOrReset = true;
                    Model.Applications = _apiKeyHelper.Applications;
                    if (_apiKeyHelper.Applications?.Count > 1)
                    {
                        Model.ShowApiKeySelection = _apiKeyHelper.Applications.Count > 1;
                    }
                    else
                    {
                        Model.ApiKey = _apiKeyHelper.ApiKey;
                        Model.IsApiKeyValid = true;
                        SaveApplicationSettings();
                    }
                }
                else
                {
                    string message = App.ResourceLoader.GetString("unknownFetchApiKeyError/Text");

                    switch (result)
                    {
                        case NetworkResult.NetworkError:
                            message = App.ResourceLoader.GetString("failedToFetchApiKeyDueToNetworkError/Text");
                            break;
                        case NetworkResult.AuthenticationFailed:
                            message = App.ResourceLoader.GetString("authenticationFailedForFetchingApiKey/Text");
                            break;
                        case NetworkResult.ParsingError:
                            message = App.ResourceLoader.GetString("failedToParseServerResponse/Text");
                            break;
                        case NetworkResult.NoWindowsCampains:
                            message = App.ResourceLoader.GetString("noWindowsCampaignsAvailable/Text");
                            break;
                    }

                    ShowInformationalMessageDialogAsync(message, App.ResourceLoader.GetString("couldNotFetchApiKey/Text"));
                }
            }

            IsValidatingOrFetchingApiKey = false;
        }

        private void OnResetToDemoApiKeyButtonClicked(object sender, RoutedEventArgs e)
        {
            _apiKeyWasJustSuccessfullyFetchedOrReset = true;
            Model.ApiKey = SDKManager.DemoApiKey;
            Model.IsApiKeyValid = true;
        }

        private void OnScanApiQrCodeButtonClicked(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(QrCodeScannerPage));
        }

        private void OnEnableActionsSwitchToggled(object sender, RoutedEventArgs e)
        {
            SaveApplicationSettings(KeyEnableActions);
        }

        private async void OnEnableBackgroundTaskSwitchToggledAsync(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch && (sender as ToggleSwitch).IsOn)
            {
                if (string.IsNullOrEmpty(_sdkManager.Configuration.ApiKey))
                {
                    _sdkManager.Configuration.ApiKey = SDKManager.DemoApiKey;
                }

                BackgroundTaskRegistrationResult result = await _sdkManager.RegisterBackgroundTaskAsync();

                if (!result.Success)
                {
                    string exceptionMessage = string.Empty;

                    if (result.Exception != null)
                    {
                        exceptionMessage = ": " + result.Exception.Message;
                    }

                    (sender as ToggleSwitch).IsOn = false;

                    ShowInformationalMessageDialogAsync(exceptionMessage, App.ResourceLoader.GetString("failedToRegisterBackgroundTask/Text"));
                }
            }
            else
            {
                _sdkManager.UnregisterBackgroundTask();
            }
            SaveApplicationSettings(KeyEnableBackgroundTask);

            Model.IsBackgroundTaskRegistered = _sdkManager.IsBackgroundTaskRegistered;
        }

        private async void OnSettingsTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox)
            {
                string textBoxName = (sender as TextBox).Name.ToLower();
                string text = (sender as TextBox).Text;

                if (textBoxName.StartsWith("apikey"))
                {
                    Model.ApiKey = text;

                    if (_apiKeyWasJustSuccessfullyFetchedOrReset)
                    {
                        _apiKeyWasJustSuccessfullyFetchedOrReset = false;
                    }
                    else
                    {
                        Model.IsApiKeyValid = false;
                    }

                    await _sdkManager.InvalidateCacheAsync();

                    if (Model.ApiKey.Length > 25)
                    {
                        await ValidateApiKeyAsync();
                    }
                }
                else if (textBoxName.StartsWith("email"))
                {
                    Email = text;
                }

                SaveApplicationSettings(KeyApiKey);
            }
        }

        private void OnPasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox)
            {
                Password = (sender as PasswordBox).Password;
                SaveApplicationSettings(KeyApiKey);
            }
        }
    }
}