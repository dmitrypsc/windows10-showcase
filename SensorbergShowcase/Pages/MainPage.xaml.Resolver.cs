using SensorbergSDK;
using System;
using System.Diagnostics;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SensorbergShowcase.Pages
{
    /// <summary>
    /// Implementation for handling resolved beacon events and possible error situations.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool _messageDialogIsOpen;

        public bool HaveResolverSpecificEventsBeenHooked
        {
            get;
            private set;
        }

        public bool IsLayoutValid
        {
            get
            {
                return (bool)GetValue(IsLayoutValidProperty);
            }
            private set
            {
                SetValue(IsLayoutValidProperty, value);
            }
        }
        public static readonly DependencyProperty IsLayoutValidProperty =
            DependencyProperty.Register("IsLayoutValid", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));

        /// <summary>
        /// Hooks/unhooks the resolver specific events. If already hooked/unhooked, does nothing.
        /// </summary>
        /// <param name="hook">If true, will hook the events. If false, will unhook.</param>
        private void SetResolverSpecificEvents(bool hook)
        {
            if (HaveResolverSpecificEventsBeenHooked != hook)
            {
                Debug.WriteLine("MainPage.SetResolverSpecificEvents: " + HaveResolverSpecificEventsBeenHooked + " -> " + hook);

                if (hook)
                {
                    _sdkManager.BeaconActionResolved += OnBeaconActionResolvedAsync;
                    _sdkManager.FailedToResolveBeaconAction += OnFailedToResolveBeaconAction;
                }
                else
                {
                    _sdkManager.BeaconActionResolved -= OnBeaconActionResolvedAsync;
                    _sdkManager.FailedToResolveBeaconAction -= OnFailedToResolveBeaconAction;
                }

                HaveResolverSpecificEventsBeenHooked = hook;
            }
        }

        private void OnCheckStatusButtonClicked(object sender, RoutedEventArgs e)
        {
            // TODO
        }

        private async void OnBeaconLayoutValidityChangedAsync(object sender, bool e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                IsLayoutValid = e;
            });
        }

        /// <summary>
        /// Displays a dialog and a toast notification corresponding to the given beacon action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnBeaconActionResolvedAsync(object sender, BeaconAction e)
        {
            if (_messageDialogIsOpen)
            {
                return;
            }

            MessageDialog messageDialog = e.ToMessageDialog();

            switch (e.Type)
            {
                case BeaconActionType.UrlMessage:
                case BeaconActionType.VisitWebsite:
                    messageDialog.Commands.Add(new UICommand(App.ResourceLoader.GetString("yes/Text"),
                        new UICommandInvokedHandler(async (command) =>
                        {
                            await Windows.System.Launcher.LaunchUriAsync(new Uri(e.Url));
                        })));

                    messageDialog.Commands.Add(new UICommand(App.ResourceLoader.GetString("no/Text")));


                    Debug.WriteLine("MainPage.OnBeaconActionResolvedAsync: Message dialog is open");
                    _messageDialogIsOpen = true;

                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        try
                        {
                            await messageDialog.ShowAsync();
                        }
                        catch
                        {
                            // For showing more than one message dialog at one time.
                        }
                    });

                    Debug.WriteLine("MainPage.OnBeaconActionResolvedAsync: Message dialog is closed");
                    _messageDialogIsOpen = false;
                    break;

                case BeaconActionType.InApp:
                    _messageDialogIsOpen = true;

                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        try
                        {
                            await messageDialog.ShowAsync();
                        }
                        catch
                        {
                            // For showing more than one message dialog at one time.
                        }
                    });

                    _messageDialogIsOpen = false;
                    break;
            }
        }

        /// <summary>
        /// Called when there was a failure in resolving the beacon action.
        /// 
        /// In most cases this event can be ignored. However, if you wish to act upon this,
        /// this is the place where to do it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The error message.</param>
        private void OnFailedToResolveBeaconAction(object sender, string e)
        {
            // No implementation
        }
    }
}
