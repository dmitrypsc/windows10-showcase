using SensorbergSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SensorbergSDK.Internal.Services;

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

        private async void OnBeaconLayoutValidityChangedAsync(object sender, bool e)
        {
            if (Model.ShouldRegisterBackgroundTask && e)
            {
                if (ServiceManager.LayoutManager.Layout != null)
                {
                    IList<string> ids = ServiceManager.LayoutManager.Layout.AccountBeaconId1S;
                    if (ids.Count > 0)
                    {
                        Model.SdkManager.Configuration.BackgroundBeaconUuidSpace = ids[0];
                    }
                }

                await Model.SdkManager.RegisterBackgroundTaskAsync();
            }
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Model.IsLayoutValid = e;
            });
        }

        /// <summary>
        /// Displays a dialog and a toast notification corresponding to the given beacon action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnBeaconActionResolvedAsync(object sender, BeaconAction e)
        {
            Logger.Debug("OnBeaconActionResolvedAsync (enabled {0}) Action {1}", Model.AreActionsEnabled, e);
            if (!Model.AreActionsEnabled)
            {
                return;
            }
            Model.ActionResolved(e);
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
