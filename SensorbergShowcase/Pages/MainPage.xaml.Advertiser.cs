using SensorbergSDK;
using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SensorbergSDK.Internal.Utils;

namespace SensorbergShowcase.Pages
{
    /// <summary>
    /// Provides the main page implementation related to publishing BLE
    /// advertisements with the device.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static readonly string DefaultBeaconId1 = "73676723-7400-0000-ffff-0000ffff0001";
        private static readonly string DefaultBeaconId2 = "4";
        private static readonly string DefaultBeaconId3 = "2";
        private const char HexStringSeparator = '-';
        private const int BeaconId1LengthWithoutDashes = 32;

        #region Properties

        public string BeaconId1
        {
            get
            {
                return (string)GetValue(BeaconId1Property);
            }
            private set
            {
                SetValue(BeaconId1Property, value);
            }
        }
        public static readonly DependencyProperty BeaconId1Property =
            DependencyProperty.Register("BeaconId1", typeof(string), typeof(MainPage),
                new PropertyMetadata(DefaultBeaconId1));

        public string BeaconId2
        {
            get
            {
                return (string)GetValue(BeaconId2Property);
            }
            private set
            {
                SetValue(BeaconId2Property, value);
            }
        }
        public static readonly DependencyProperty BeaconId2Property =
            DependencyProperty.Register("BeaconId2", typeof(string), typeof(MainPage),
                new PropertyMetadata(DefaultBeaconId2));

        public string BeaconId3
        {
            get
            {
                return (string)GetValue(BeaconId3Property);
            }
            private set
            {
                SetValue(BeaconId3Property, value);
            }
        }
        public static readonly DependencyProperty BeaconId3Property =
            DependencyProperty.Register("BeaconId3", typeof(string), typeof(MainPage),
                new PropertyMetadata(DefaultBeaconId3));

        public bool IsAdvertisingStarted
        {
            get
            {
                return (bool)GetValue(IsAdvertisingStartedroperty);
            }
            private set
            {
                SetValue(IsAdvertisingStartedroperty, value);
            }
        }
        public static readonly DependencyProperty IsAdvertisingStartedroperty =
            DependencyProperty.Register("IsAdvertisingStarted", typeof(bool), typeof(MainPage), null);

        #endregion

        private Advertiser _advertiser;

        /// <summary>
        /// Validates the entered beacon IDs.
        /// </summary>
        /// <returns>True, if the values are valid, false otherwise.</returns>
        private bool ValuesForAdvertisementAreValid()
        {
            bool valid = false;

            if (!string.IsNullOrEmpty(BeaconId1))
            {
                string beaconId1WithoutDashes = string.Join("", BeaconId1.Split(HexStringSeparator));
                bool isValidHex = System.Text.RegularExpressions.Regex.IsMatch(beaconId1WithoutDashes, @"\A\b[0-9a-fA-F]+\b\Z");

                if (isValidHex && beaconId1WithoutDashes.Length == BeaconId1LengthWithoutDashes)
                {
                    try
                    {
                        int.Parse(BeaconId2);
                        int.Parse(BeaconId3);
                        valid = true;
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return valid;
        }

        private async void OnToggleAdvertizingButtonClickedAsync(object sender, RoutedEventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (_advertiser.IsStarted)
                {
                    _advertiser.Stop();
                }
                else
                {
                    if (ValuesForAdvertisementAreValid())
                    {
                        _advertiser.BeaconId1 = BeaconId1;

                        try
                        {
                            int id2 = int.Parse(BeaconId2);
                            if (id2 > ushort.MaxValue)
                            {
                                ShowInformationalMessageDialogAsync("The major id is to long, it should be between 0 and "+ ushort.MaxValue);
                                return;
                            }
                            int id3 = int.Parse(BeaconId3);
                            if (id3 > ushort.MaxValue)
                            {
                                ShowInformationalMessageDialogAsync("The minor id is to long, it should be between 0 and " + ushort.MaxValue);
                                return;
                            }
                            _advertiser.BeaconId2 = (ushort)id2;
                            _advertiser.BeaconId3 = (ushort)id3;

                            _advertiser.Start();

                            SaveApplicationSettings();
                        }
                        catch (Exception ex)
                        {
                            ShowInformationalMessageDialogAsync(ex.ToString(), "Failed to start advertiser");
                        }
                    }
                    else
                    {
                        ShowInformationalMessageDialogAsync(
                            "At least one of the entered values is invalid. The length of the beacon ID 1 (without dashes, which are ignored) must be "
                            + BeaconId1LengthWithoutDashes + " characters.", "Check advertiser values");
                    }
                }

                IsAdvertisingStarted = _advertiser.IsStarted;
            });
        }

        private void OnAdvertisingTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox)
            {
                TextBox textBox = sender as TextBox;
                string textBoxName = textBox.Name.ToLower();
                string text = textBox.Text;

                if (textBoxName.StartsWith("beaconid1"))
                {
                    int oldTextLength = text.Length;
                    int oldCaretPosition = textBox.SelectionStart;

                    BeaconId1 = BeaconFactory.FormatUuid(text);

                    int newCaretPosition = oldCaretPosition + (BeaconId1.Length - oldTextLength);

                    if (newCaretPosition > 0 && newCaretPosition <= BeaconId1.Length)
                    {
                        textBox.SelectionStart = newCaretPosition;
                    }
                }
                else if (textBoxName.StartsWith("beaconId2"))
                {
                    BeaconId2 = text;
                }
                else if (textBoxName.StartsWith("beaconId3"))
                {
                    BeaconId3 = text;
                }

                SaveApplicationSettings(KeyBeaconId1);
            }
        }
    }
}
