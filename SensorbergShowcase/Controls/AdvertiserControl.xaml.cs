using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SensorbergSDK;
using SensorbergShowcase.Model;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace SensorbergShowcase.Controls
{
    public sealed partial class AdvertiserControl : UserControl
    {
        private static readonly string DefaultBeaconId1 = "73676723-7400-0000-ffff-0000ffff0001";
        private static readonly string DefaultBeaconId2 = "4";
        private static readonly string DefaultBeaconId3 = "2";
        private const char HexStringSeparator = '-';
        private const int BeaconId1LengthWithoutDashes = 32;
        private Advertiser _advertiser;
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof(MainPageModel), typeof(AdvertiserControl), new PropertyMetadata(default(MainPageModel)));

        public AdvertiserControl()
        {
            InitializeComponent();
        }

        public MainPageModel Model
        {
            get { return (MainPageModel) GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }
        private async void OnToggleAdvertizingButtonClickedAsync(object sender, RoutedEventArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (_advertiser.IsStarted)
                {
                    _advertiser.Stop();
                }
                else
                {
                    if (ValuesForAdvertisementAreValid())
                    {
                        _advertiser.BeaconId1 = Model.BeaconId1;

                        try
                        {
                            int id2 = Int32.Parse(Model.BeaconId2);
                            if (id2 > UInt16.MaxValue)
                            {
                                Model.ShowInformationalMessageDialogAsync("The major id is to long, it should be between 0 and " + UInt16.MaxValue);
                                return;
                            }
                            int id3 = Int32.Parse(Model.BeaconId3);
                            if (id3 > UInt16.MaxValue)
                            {
                                Model.ShowInformationalMessageDialogAsync("The minor id is to long, it should be between 0 and " + UInt16.MaxValue);
                                return;
                            }
                            _advertiser.BeaconId2 = (ushort)id2;
                            _advertiser.BeaconId3 = (ushort)id3;

                            _advertiser.Start();

                            Model.SaveApplicationSettings();
                        }
                        catch (Exception ex)
                        {
                            Model.ShowInformationalMessageDialogAsync(ex.ToString(), "Failed to start advertiser");
                        }
                    }
                    else
                    {
                        Model.ShowInformationalMessageDialogAsync(
                            "At least one of the entered values is invalid. The length of the beacon ID 1 (without dashes, which are ignored) must be "
                            + BeaconId1LengthWithoutDashes + " characters.", "Check advertiser values");
                    }
                }

                Model.IsAdvertisingStarted = _advertiser.IsStarted;
            });
        }

        private void OnAdvertisingTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox)
            {
                //                TextBox textBox = sender as TextBox;
                //                string textBoxName = textBox.Name.ToLower();
                //                string text = textBox.Text;
                //
                //                if (textBoxName.StartsWith("beaconid1"))
                //                {
                //                    int oldTextLength = text.Length;
                //                    int oldCaretPosition = textBox.SelectionStart;
                //
                //                    BeaconId1 = BeaconFactory.FormatUuid(text);
                //
                //                    int newCaretPosition = oldCaretPosition + (BeaconId1.Length - oldTextLength);
                //
                //                    if (newCaretPosition > 0 && newCaretPosition <= BeaconId1.Length)
                //                    {
                //                        textBox.SelectionStart = newCaretPosition;
                //                    }
                //                }
                //                else if (textBoxName.StartsWith("beaconId2"))
                //                {
                //                    BeaconId2 = text;
                //                }
                //                else if (textBoxName.StartsWith("beaconId3"))
                //                {
                //                    BeaconId3 = text;
                //                }

                Model.SaveApplicationSettings(MainPageModel.KeyBeaconId1);
            }
        }
        /// <summary>
        /// Validates the entered beacon IDs.
        /// </summary>
        /// <returns>True, if the values are valid, false otherwise.</returns>
        private bool ValuesForAdvertisementAreValid()
        {
            bool valid = false;

            if (!String.IsNullOrEmpty(Model.BeaconId1))
            {
                string beaconId1WithoutDashes = string.Join("", Model.BeaconId1.Split(HexStringSeparator));
                bool isValidHex = Regex.IsMatch(beaconId1WithoutDashes, @"\A\b[0-9a-fA-F]+\b\Z");

                if (isValidHex && beaconId1WithoutDashes.Length == BeaconId1LengthWithoutDashes)
                {
                    try
                    {
                        int.Parse(Model.BeaconId2);
                        int.Parse(Model.BeaconId3);
                        valid = true;
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return valid;
        }
    }
}
