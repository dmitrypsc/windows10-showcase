// Created by Kay Czarnotta on 14.07.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using SensorbergSDK;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MetroLog;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Services;

namespace SensorbergShowcase.Model
{
    public class MainPageModel : INotifyPropertyChanged
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<MainPageModel>();

        /*
         * Insert the manufacturer ID and beacon code for filtering beacons below.
         */
        private const ushort ManufacturerId = 0x004c;
        private const ushort BeaconCode = 0x0215;

        public const string KeyEnableActions = "enable_actions";
        public const string KeyApiKey = "api_key";
        public const string KeyEmail = "email";
        public const string KeyPassword = "password";
        public const string KeyBeaconId1 = "beacon_id_1";
        public const string KeyBeaconId2 = "beacon_id_2";
        public const string KeyBeaconId3 = "beacon_id_3";
        public const string KeyEnableBackgroundTask = "enable_backgroundtask";

        private bool? _isScanning;
        private BeaconDetailsModel _beaconModel;
        private double _beaconDetailsControlWidth;
        private bool _haveScannerSpecificEventsBeenHooked;
        private bool _beaconsInRange;
        private string _headerWithBeaconCount;
        private bool _isBigScreen;
        private bool _shouldRegisterBackgroundTask;
        private bool _isBackgroundTaskRegistered;
        private bool _areActionsEnabled;
        private List<SensorbergApplication> _applications;
        private SensorbergApplication _application;
        private string _apiKey;
        private bool _showApiKeySelection;
        private bool _isApiKeyValid;
        private string _beaconId3;
        private string _beaconId2;
        private string _beaconId1;
        private bool _isAdvertisingStarted;
        private ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private ApiKeyHelper _apiKeyHelper = new ApiKeyHelper();
        private bool _apiKeyWasJustSuccessfullyFetchedOrReset;
        private string _email;
        private string _password;
        private bool _isValidatingOrFetchingApiKey;
        private bool _isLayoutValid;
        public ObservableCollection<string> ResolvedActions { get; } = new ObservableCollection<string>();
        public SDKManager SdkManager { get; set; }

        public bool IsAdvertisingStarted
        {
            get { return _isAdvertisingStarted; }
            set
            {
                _isAdvertisingStarted = value;
                OnPropertyChanged();
            }
        }

        public string BeaconId1
        {
            get { return _beaconId1; }
            set
            {
                _beaconId1 = value;
                OnPropertyChanged();
            }
        }

        public string BeaconId2
        {
            get { return _beaconId2; }
            set
            {
                _beaconId2 = value;
                OnPropertyChanged();
            }
        }

        public string BeaconId3
        {
            get { return _beaconId3; }
            set
            {
                _beaconId3 = value;
                OnPropertyChanged();
            }
        }

        public bool IsBigScreen
        {
            get { return _isBigScreen; }
            set
            {
                _isBigScreen = value;
                OnPropertyChanged();
            }
        }

        public bool? IsScanning
        {
            get { return _isScanning; }
            set
            {
                _isScanning = value;
                OnPropertyChanged();
                OnPropertyChanged("IsScanningText");
            }
        }

        public string IsScanningText
        {
            get
            {
                return IsScanning.HasValue && IsScanning.Value
                    ? App.ResourceLoader.GetString("scanning/Text")
                    : App.ResourceLoader.GetString("scannerStopped/Text");
            }
        }


        public BeaconDetailsModel BeaconModel
        {
            get { return _beaconModel; }
            set
            {
                _beaconModel = value;
                OnPropertyChanged();
            }
        }

        public double BeaconDetailsControlWidth
        {
            get { return _beaconDetailsControlWidth; }
            set
            {
                _beaconDetailsControlWidth = value;
                OnPropertyChanged();
            }
        }

        public bool HaveScannerSpecificEventsBeenHooked
        {
            get { return _haveScannerSpecificEventsBeenHooked; }
            set
            {
                _haveScannerSpecificEventsBeenHooked = value;
                OnPropertyChanged();
            }
        }

        public bool BeaconsInRange
        {
            get { return _beaconsInRange; }
            set
            {
                _beaconsInRange = value;
                OnPropertyChanged();
            }
        }

        public string HeaderWithBeaconCount
        {
            get { return _headerWithBeaconCount; }
            set
            {
                _headerWithBeaconCount = value;
                OnPropertyChanged();
            }
        }

        public bool ShouldRegisterBackgroundTask
        {
            get { return _shouldRegisterBackgroundTask; }
            set
            {
                _shouldRegisterBackgroundTask = value;
                OnPropertyChanged();
            }
        }

        public bool IsBackgroundTaskRegistered
        {
            get { return _isBackgroundTaskRegistered; }
            set
            {
                _isBackgroundTaskRegistered = value;
                OnPropertyChanged();
            }
        }

        public bool AreActionsEnabled
        {
            get { return _areActionsEnabled; }
            set
            {
                _areActionsEnabled = value;
                OnPropertyChanged();
            }
        }

        public List<SensorbergApplication> Applications
        {
            get { return _applications; }
            set
            {
                _applications = value;
                OnPropertyChanged();
            }
        }

        public SensorbergApplication Application
        {
            get { return _application; }
            set
            {
                _application = value;
                OnPropertyChanged();
                if (_application != null)
                {
                    ApiKey = _application.AppKey;
                }
            }
        }

        public string ApiKey
        {
            get { return _apiKey; }
            set
            {
                _apiKey = value;
                OnPropertyChanged();
            }
        }

        public bool ShowApiKeySelection
        {
            get { return _showApiKeySelection; }
            set
            {
                _showApiKeySelection = value;
                OnPropertyChanged();
            }
        }

        public bool IsApiKeyValid
        {
            get { return _isApiKeyValid; }
            set
            {
                _isApiKeyValid = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get { return _email; }
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public bool IsValidatingOrFetchingApiKey
        {
            get { return _isValidatingOrFetchingApiKey; }
            set
            {
                _isValidatingOrFetchingApiKey = value;
                OnPropertyChanged();
            }
        }

        public bool IsLayoutValid
        {
            get { return _isLayoutValid; }
            set
            {
                _isLayoutValid = value;
                OnPropertyChanged();
            }
        }

        public bool IsScannerAvailable { get; set; }

        public Frame Frame { get; set; }

        /// <summary>
        /// Saves the application settings.
        /// </summary>
        /// <param name="key">If empty or null, will save all settings. Otherwise will save the 
        /// specific settings related to the given key.</param>
        public void SaveApplicationSettings(string key = null)
        {
            Logger.Debug("SaveApplicationSettings Key={0}", key);
            if (string.IsNullOrEmpty(key) || key.Equals(KeyEnableActions))
            {
                _localSettings.Values[KeyEnableActions] = AreActionsEnabled;
            }

            if (string.IsNullOrEmpty(key) || key.Equals(KeyEnableBackgroundTask))
            {
                _localSettings.Values[KeyEnableBackgroundTask] = ShouldRegisterBackgroundTask;
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

        public void LoadApplicationSettings()
        {
            Logger.Debug("LoadApplicationSettings");
            if (_localSettings.Values.ContainsKey(KeyEnableActions))
            {
                AreActionsEnabled = (bool) _localSettings.Values[KeyEnableActions];
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

            if (_localSettings.Values.ContainsKey(KeyEnableBackgroundTask))
            {
                ShouldRegisterBackgroundTask = (bool) _localSettings.Values[KeyEnableBackgroundTask];
            }

            IsBackgroundTaskRegistered = SdkManager.IsBackgroundTaskRegistered;
        }

        public async Task TryToReinitializeSDK()
        {
            Logger.Debug("TryToReinitializeSDK {0}", SdkManager != null);
            if (SdkManager != null)
            {
                SdkManager.Deinitialize(false);
                SdkConfiguration sdkConfiguration = new SdkConfiguration()
                {
                    ManufacturerId = ManufacturerId,
                    BeaconCode = BeaconCode,
                    ApiKey = ApiKey,
                    BackgroundAdvertisementClassName = "SensorbergShowcaseBackgroundTask.SensorbergShowcaseAdvertisementBackgroundTask",
                    BackgroundTimerClassName = "SensorbergShowcaseBackgroundTask.SensorbergShowcaseTimedBackgrundTask"
                };

                if (ServiceManager.LayoutManager.Layout != null)
                {
                    IList<string> ids = ServiceManager.LayoutManager.Layout.AccountBeaconId1S;
                    if (ids.Count > 0)
                    {
                        sdkConfiguration.BackgroundBeaconUuidSpace = ids[0];
                    }
                }
                await SdkManager.InitializeAsync(sdkConfiguration);
                SdkManager.StartScanner();
            }
        }



        /// <summary>
        /// Validates the given API key.
        /// </summary>
        /// <param name="displayResultDialogInCaseOfFailure">If true, will display a result dialog in case of an error.</param>
        /// <returns>The API key validation result.</returns>
        public async Task<ApiKeyValidationResult> ValidateApiKeyAsync(bool displayResultDialogInCaseOfFailure = false)
        {
            IsValidatingOrFetchingApiKey = true;

            ApiKeyValidationResult result = await new ApiKeyHelper().ValidateApiKey(ApiKey);

            if (result == ApiKeyValidationResult.Valid)
            {
                IsApiKeyValid = true;
                await TryToReinitializeSDK();
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

        /// <summary>
        /// Helper method for showing informational message dialogs, which do not require command handling.
        /// </summary>
        /// <param name="message">The message to show.</param>
        /// <param name="title">The title of the message dialog.</param>
        public async void ShowInformationalMessageDialogAsync(string message, string title = null)
        {
            MessageDialog messageDialog = (title == null) ? new MessageDialog(message) : new MessageDialog(message, title);
            messageDialog.Commands.Add(new UICommand(App.ResourceLoader.GetString("ok/Text")));
            await messageDialog.ShowAsync();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async void ActionResolved(BeaconAction beaconAction)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                string s = string.Format("Event received: {0}\nType: {3}\nEvent subject: {1}\nPayload: {2}", DateTime.Now, beaconAction.Subject, beaconAction.PayloadString, beaconAction.Type);
                ResolvedActions.Insert(0, s);
            });
        }
    }
}