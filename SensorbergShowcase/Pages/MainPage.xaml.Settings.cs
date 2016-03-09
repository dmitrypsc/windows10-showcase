using SensorbergSDK;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

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

        private ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private ApiKeyHelper _apiKeyHelper = new ApiKeyHelper();
        private bool _enableActionsSwitchToggledByUser = true;
        private bool _apiKeyWasJustSuccessfullyFetchedOrReset = false;

        #region Properties (API key, email, password, background task status etc.)

        public string ApiKey
        {
            get
            {
                return (string)GetValue(ApiKeyProperty);
            }
            private set
            {
                SetValue(ApiKeyProperty, value);
            }
        }
        public static readonly DependencyProperty ApiKeyProperty =
            DependencyProperty.Register("ApiKey", typeof(string), typeof(MainPage),
                new PropertyMetadata(SDKManager.DemoApiKey));

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

        public bool AreActionsEnabled
        {
            get
            {
                return (bool)GetValue(AreActionsEnabledProperty);
            }
            private set
            {
                SetValue(AreActionsEnabledProperty, value);
            }
        }
        public static readonly DependencyProperty AreActionsEnabledProperty =
            DependencyProperty.Register("AreActionsEnabled", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));

        public bool IsApiKeyValid
        {
            get
            {
                return (bool)GetValue(IsApiKeyValidProperty);
            }
            private set
            {
                SetValue(IsApiKeyValidProperty, value);

                if (value && ShouldActionsBeEnabled)
                {
                    TryToReinitializeSDK();
                }
            }
        }
        public static readonly DependencyProperty IsApiKeyValidProperty =
            DependencyProperty.Register("IsApiKeyValid", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));

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

        public bool IsBackgroundTaskRegistered
        {
            get
            {
                return (bool)GetValue(IsBackgroundTaskRegisteredProperty);
            }
            private set
            {
                SetValue(IsBackgroundTaskRegisteredProperty, value);
            }
        }
        public static readonly DependencyProperty IsBackgroundTaskRegisteredProperty =
            DependencyProperty.Register("IsBackgroundTaskRegistered", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));

        private bool ShouldActionsBeEnabled
        {
            get;
            set;
        }

        #endregion

        private void LoadApplicationSettings()
        {
            if (_localSettings.Values.ContainsKey(KeyEnableActions))
            {
                ShouldActionsBeEnabled = (bool)_localSettings.Values[KeyEnableActions];
            }

            if (_localSettings.Values.ContainsKey(KeyApiKey))
            {
                ApiKey = _localSettings.Values[KeyApiKey].ToString();
            }
            else
            {
                ApiKey = SDKManager.DemoApiKey;
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

            if (AreActionsEnabled != ShouldActionsBeEnabled)
            {
                _enableActionsSwitchToggledByUser = false;
                AreActionsEnabled = ShouldActionsBeEnabled;
            }

            IsBackgroundTaskRegistered = _sdkManager.IsBackgroundTaskRegistered;
        }

        /// <summary>
        /// Saves the application settings.
        /// </summary>
        /// <param name="key">If empty or null, will save all settings. Otherwise will save the 
        /// specific settings related to the given key.</param>
        private void SaveApplicationSettings(string key = null)
        {
            if (string.IsNullOrEmpty(key) || key.Equals(KeyEnableActions))
            {
                _localSettings.Values[KeyEnableActions] = ShouldActionsBeEnabled;
            }

            if (string.IsNullOrEmpty(key) || key.Equals(KeyApiKey))
            {
                _localSettings.Values[KeyApiKey] = ApiKey;
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

        private void TryToReinitializeSDK()
        {
            if (_sdkManager != null)
            {
                _sdkManager.Deinitialize(false);
                _sdkManager.InitializeAsync(ApiKey);
                SetResolverSpecificEvents(true);
            }

            if (!AreActionsEnabled)
            {
                _enableActionsSwitchToggledByUser = false;
                AreActionsEnabled = true;
            }
        }

        /// <summary>
        /// Validates the given API key.
        /// </summary>
        /// <param name="apiKey">The API key to validate.</param>
        /// <param name="displayResultDialogInCaseOfFailure">If true, will display a result dialog in case of an error.</param>
        /// <returns>The API key validation result.</returns>
        private async Task<ApiKeyValidationResult> ValidateApiKeyAsync(
            string apiKey, bool displayResultDialogInCaseOfFailure = false)
        {
            IsValidatingOrFetchingApiKey = true;

            ApiKeyValidationResult result = await _apiKeyHelper.ValidateApiKey(ApiKey);

            if (result == ApiKeyValidationResult.Valid)
            {
                IsApiKeyValid = true;
            }
            else
            {
                IsApiKeyValid = false;

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

        private void OnValidateApiKeyButtonClicked(object sender, RoutedEventArgs e)
        {
            ValidateApiKeyAsync(ApiKey, true);
        }

        private async void OnFetchApiKeyButtonClickedAsync(object sender, RoutedEventArgs e)
        {
            IsValidatingOrFetchingApiKey = true;

            if (!string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password))
            {
                FetchApiKeyResult result = await _apiKeyHelper.FetchApiKeyAsync(Email, Password);

                if (result == FetchApiKeyResult.Success)
                {
                    _apiKeyWasJustSuccessfullyFetchedOrReset = true;
                    ApiKey = _apiKeyHelper.ApiKey;
                    IsApiKeyValid = true;
                    SaveApplicationSettings();
                }
                else
                {
                    string message = App.ResourceLoader.GetString("unknownFetchApiKeyError/Text");

                    switch (result)
                    {
                        case FetchApiKeyResult.NetworkError:
                            message = App.ResourceLoader.GetString("failedToFetchApiKeyDueToNetworkError/Text");
                            break;
                        case FetchApiKeyResult.AuthenticationFailed:
                            message = App.ResourceLoader.GetString("authenticationFailedForFetchingApiKey/Text");
                            break;
                        case FetchApiKeyResult.ParsingError:
                            message = App.ResourceLoader.GetString("failedToParseServerResponse/Text");
                            break;
                        case FetchApiKeyResult.NoWindowsCampains:
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
            ApiKey = SDKManager.DemoApiKey;
            IsApiKeyValid = true;
        }

        private void OnScanApiQrCodeButtonClicked(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(QrCodeScannerPage));
        }

        private void OnEnableActionsSwitchToggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch)
            {
                ToggleSwitch enableActionsSwitch = sender as ToggleSwitch;

                if (enableActionsSwitch.IsOn)
                {
                    TryToReinitializeSDK();
                }
                else
                {
                    SetResolverSpecificEvents(false);
                    _sdkManager.Deinitialize(false);
                }

                if (_enableActionsSwitchToggledByUser)
                {
                    ShouldActionsBeEnabled = enableActionsSwitch.IsOn;
                    SaveApplicationSettings(KeyEnableActions);
                }
                else
                {
                    _enableActionsSwitchToggledByUser = true;
                }
            }
        }

        private async void OnEnableBackgroundTaskSwitchToggledAsync(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch && (sender as ToggleSwitch).IsOn)
            {
                if (string.IsNullOrEmpty(_sdkManager.ApiKey))
                {
                    _sdkManager.ApiKey = SDKManager.DemoApiKey;
                }

                BackgroundTaskRegistrationResult result = await _sdkManager.RegisterBackgroundTaskAsync();

                if (!result.success)
                {
                    string exceptionMessage = string.Empty;

                    if (result.exception != null)
                    {
                        exceptionMessage = ": " + result.exception.Message;
                    }

                    (sender as ToggleSwitch).IsOn = false;

                    ShowInformationalMessageDialogAsync(
                        exceptionMessage, App.ResourceLoader.GetString("failedToRegisterBackgroundTask/Text"));
                }
            }
            else
            {
                _sdkManager.UnregisterBackgroundTask();
            }

            IsBackgroundTaskRegistered = _sdkManager.IsBackgroundTaskRegistered;
        }

        private async void OnSettingsTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox)
            {
                string textBoxName = (sender as TextBox).Name.ToLower();
                string text = (sender as TextBox).Text;

                if (textBoxName.StartsWith("apikey"))
                {
                    ApiKey = text;

                    if (_apiKeyWasJustSuccessfullyFetchedOrReset)
                    {
                        _apiKeyWasJustSuccessfullyFetchedOrReset = false;
                    }
                    else
                    {
                        IsApiKeyValid = false;
                    }

                    await _sdkManager.InvalidateCacheAsync();
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