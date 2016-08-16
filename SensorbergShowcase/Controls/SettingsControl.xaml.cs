using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SensorbergSDK;
using SensorbergSDK.Internal.Services;
using SensorbergShowcase.Model;
using SensorbergShowcase.Pages;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace SensorbergShowcase.Controls
{
    public sealed partial class SettingsControl : UserControl
    {
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof(MainPageModel), typeof(SettingsControl), new PropertyMetadata(default(MainPageModel)));

        public SettingsControl()
        {
            InitializeComponent();
        }

        public MainPageModel Model
        {
            get { return (MainPageModel) GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }
        private void OnEnableActionsSwitchToggled(object sender, RoutedEventArgs e)
        {
            Model.SaveApplicationSettings(MainPageModel.KeyEnableActions);
        }
        private async void OnEnableBackgroundTaskSwitchToggledAsync(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch && (sender as ToggleSwitch).IsOn)
            {
                if (string.IsNullOrEmpty(Model.SdkManager.Configuration.ApiKey))
                {
                    Model.SdkManager.Configuration.ApiKey = SDKManager.DemoApiKey;
                }
                if (ServiceManager.LayoutManager.Layout != null)
                {
                    IList<string> ids = ServiceManager.LayoutManager.Layout.AccountBeaconId1S;
                    if (ids.Count > 0)
                    {
                        Model.SdkManager.Configuration.BackgroundBeaconUuidSpace = ids[0];
                    }
                }

                BackgroundTaskRegistrationResult result = await Model.SdkManager.RegisterBackgroundTaskAsync();

                if (!result.Success)
                {
                    string exceptionMessage = string.Empty;

                    if (result.Exception != null)
                    {
                        exceptionMessage = ": " + result.Exception.Message;
                    }

                    (sender as ToggleSwitch).IsOn = false;

                    Model.ShowInformationalMessageDialogAsync(exceptionMessage, App.ResourceLoader.GetString("failedToRegisterBackgroundTask/Text"));
                }
            }
            else
            {
                Model.SdkManager.UnregisterBackgroundTask();
            }
            Model.SaveApplicationSettings(MainPageModel.KeyEnableBackgroundTask);

            Model.IsBackgroundTaskRegistered = Model.SdkManager.IsBackgroundTaskRegistered;
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

                        Model.IsApiKeyValid = false;

                    await Model.SdkManager.InvalidateCacheAsync();

                    if (Model.ApiKey.Length > 25)
                    {
                        await Model.ValidateApiKeyAsync();
                    }
                }
                else if (textBoxName.StartsWith("email"))
                {
                    Model.Email = text;
                }

                Model.SaveApplicationSettings(MainPageModel.KeyApiKey);
            }
        }
        private void OnPasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox)
            {
                Model.Password = (sender as PasswordBox).Password;
                Model.SaveApplicationSettings(MainPageModel.KeyApiKey);
            }
        }
        private async void OnValidateApiKeyButtonClicked(object sender, RoutedEventArgs e)
        {
            await Model.ValidateApiKeyAsync(true);
        }


        private void OnResetToDemoApiKeyButtonClicked(object sender, RoutedEventArgs e)
        {
            Model.ApiKey = SDKManager.DemoApiKey;
            Model.IsApiKeyValid = true;
        }

        private void OnScanApiQrCodeButtonClicked(object sender, RoutedEventArgs e)
        {
            Model.Frame.Navigate(typeof(QrCodeScannerPage));
        }

        private async void OnFetchApiKeyButtonClickedAsync(object sender, RoutedEventArgs e)
        {
            Model.IsValidatingOrFetchingApiKey = true;

            if (!string.IsNullOrEmpty(Model.Email) && !string.IsNullOrEmpty(Model.Password))
            {
                ApiKeyHelper apiKeyHelper = new ApiKeyHelper();
                NetworkResult result = await apiKeyHelper.FetchApiKeyAsync(Model.Email, Model.Password);

                if (result == NetworkResult.Success)
                {
                    Model.Applications = apiKeyHelper.Applications;
                    if (apiKeyHelper.Applications?.Count > 1)
                    {
                        Model.ShowApiKeySelection = apiKeyHelper.Applications.Count > 1;
                    }
                    else
                    {
                        Model.ApiKey = apiKeyHelper.ApiKey;
                        Model.IsApiKeyValid = true;
                        Model.SaveApplicationSettings();
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

                    Model.ShowInformationalMessageDialogAsync(message, App.ResourceLoader.GetString("couldNotFetchApiKey/Text"));
                }
            }

            Model.IsValidatingOrFetchingApiKey = false;
        }
    }
}
