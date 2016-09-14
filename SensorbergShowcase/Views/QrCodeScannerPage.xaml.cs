/*
 * This class has been adapted from Windows 10 sample project:
 * https://github.com/Microsoft/Windows-universal-samples/tree/master/Samples/CameraGetPreviewFrame
 */

//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using SensorbergShowcase.Utils;
using ZXing;
using ZXing.Common;

namespace SensorbergShowcase.Views
{
    /// <summary>
    /// Page for scanning API keys from QR codes.
    /// </summary>
    public sealed partial class QrCodeScannerPage : Page
    {
        // Rotation metadata to apply to the preview stream and recorded videos (MF_MT_VIDEO_ROTATION)
        // Reference: http://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868174.aspx
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private readonly SimpleOrientationSensor _orientationSensor = SimpleOrientationSensor.GetDefault();
        private readonly DisplayRequest _displayRequest = new DisplayRequest();
        private readonly SystemMediaTransportControls _systemMediaControls = SystemMediaTransportControls.GetForCurrentView();
        private MediaCapture _mediaCapture;
        private BarcodeReader mBarcodeReader;
        private ContinuousAutoFocus _continuousAutoFocus;
        private SimpleOrientation _deviceOrientation = SimpleOrientation.NotRotated;
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;
        private bool _isInitialized;
        private bool _isPreviewing;
        private bool _isExternalCamera;
        private bool _mirroringPreview;
        private bool _analyzingFrame;

        /// <summary>
        /// Contains the scanned QR code. Null, if no code was scanned.
        /// 
        /// This is the primary means to provide the result for the main page, since Frame.GoBack
        /// won't take a parameter.
        /// </summary>
        public static string ScannedQrCode { get; private set; }

        /// <summary>
        /// Checks if the device has a camera device.
        /// </summary>
        /// <returns>True, if the device has a camera. False otherwise.</returns>
        public static async Task<bool> DoesDeviceHaveCamera()
        {
            return (await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Back) != null);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public QrCodeScannerPage()
        {
            InitializeComponent();

            // Do not cache the state of the UI when suspending/navigating
            NavigationCacheMode = NavigationCacheMode.Disabled;

            // Useful to know when to initialize/clean up the camera
            Application.Current.Suspending += OnApplicationSuspendingAsync;
            Application.Current.Resuming += OnApplicationResumingAsync;

            mBarcodeReader = new BarcodeReader
            {
                Options = new DecodingOptions
                {
                    PossibleFormats = new BarcodeFormat[] {BarcodeFormat.QR_CODE, BarcodeFormat.CODE_128},
                    TryHarder = true
                }
            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ScannedQrCode = null;
            await InitializeAndStartScanningAsync();
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            await CleanupAsync();
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// Initializes the UI, camera and start scanning for QR codes.
        /// </summary>
        private async Task InitializeAndStartScanningAsync()
        {
            SetupUi();
            await InitializeCameraAsync();

            if (_isInitialized)
            {
                FocusControl focusControl = _mediaCapture.VideoDeviceController.FocusControl;

                if (focusControl != null)
                {
                    _continuousAutoFocus = await ContinuousAutoFocus.StartAsync(focusControl);
                }
            }

            StartScanningForQrCodeAsync();
        }

        private async Task CleanupAsync()
        {
            if (_continuousAutoFocus != null)
            {
                _continuousAutoFocus.Dispose();
            }

            await CleanupCameraAsync();
            CleanupUi();
        }

        /// <summary>
        /// Attempts to lock the page orientation and registers the event handlers for orientation sensors.
        /// </summary>
        /// <returns></returns>
        private void SetupUi()
        {
            // Attempt to lock page to portrait orientation to prevent the CaptureElement from
            // rotating, as this gives a better experience
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            // Populate orientation variables with the current state
            _displayOrientation = _displayInformation.CurrentOrientation;

            if (_orientationSensor != null)
            {
                _deviceOrientation = _orientationSensor.GetCurrentOrientation();
            }

            SetEventHandlers(true);
        }

        /// <summary>
        /// Releases the orientation lock and unregisters the event handlers.
        /// </summary>
        private void CleanupUi()
        {
            SetEventHandlers(false);
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
        }

        /// <summary>
        /// Registers/unregisters event handlers for orientation sensors.
        /// </summary>
        /// <param name="register">If true, will register the event handlers. If false, will unregister.</param>
        private void SetEventHandlers(bool register)
        {
            if (register)
            {
                if (_orientationSensor != null)
                {
                    _orientationSensor.OrientationChanged += OnOrientationSensorOrientationChanged;
                }

                _displayInformation.OrientationChanged += OnDisplayInformationOrientationChangedAsync;
                _systemMediaControls.PropertyChanged += OnSystemMediaControlsPropertyChanged;
            }
            else
            {
                if (_orientationSensor != null)
                {
                    _orientationSensor.OrientationChanged -= OnOrientationSensorOrientationChanged;
                }

                _displayInformation.OrientationChanged -= OnDisplayInformationOrientationChangedAsync;
                _systemMediaControls.PropertyChanged -= OnSystemMediaControlsPropertyChanged;
            }
        }

        /// <summary>
        /// Initializes the MediaCapture, registers events, gets camera device information for
        /// mirroring and rotating, starts preview and unlocks the UI
        /// </summary>
        /// <returns></returns>
        private async Task InitializeCameraAsync()
        {
            System.Diagnostics.Debug.WriteLine("QrCodeScannerPage.InitializeCameraAsync");

            if (_mediaCapture == null)
            {
                // Attempt to get the back camera if one is available, but use any camera device if not
                var cameraDevice = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Back);

                if (cameraDevice == null)
                {
                    System.Diagnostics.Debug.WriteLine("QrCodeScannerPage.InitializeCameraAsync: No camera device found!");
                    return;
                }

                // Create MediaCapture and its settings
                _mediaCapture = new MediaCapture();

                // Register for a notification when video recording has reached the maximum time
                // and when something goes wrong
                _mediaCapture.Failed += OnMediaCaptureFailedAsync;

                MediaCaptureInitializationSettings mediaCaptureInitializationSettings =
                    new MediaCaptureInitializationSettings();
                mediaCaptureInitializationSettings.VideoDeviceId = cameraDevice.Id;
                mediaCaptureInitializationSettings.StreamingCaptureMode = StreamingCaptureMode.Video;

                // Initialize MediaCapture
                try
                {
                    await _mediaCapture.InitializeAsync(mediaCaptureInitializationSettings);
                    _isInitialized = true;
                }
                catch (UnauthorizedAccessException e)
                {
                    System.Diagnostics.Debug.WriteLine("QrCodeScannerPage.InitializeCameraAsync: {0}", e.ToString());
                }

                // If initialization succeeded, start the preview
                if (_isInitialized)
                {
                    // Figure out where the camera is located
                    if (cameraDevice.EnclosureLocation == null
                        || cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        // No information on the location of the camera, assume it's an external
                        // camera, not integrated on the device
                        _isExternalCamera = true;
                    }
                    else
                    {
                        // Camera is fixed on the device
                        _isExternalCamera = false;

                        // Only mirror the preview if the camera is on the front panel
                        _mirroringPreview =
                        (cameraDevice.EnclosureLocation.Panel
                         == Windows.Devices.Enumeration.Panel.Front);
                    }

                    await StartPreviewAsync();
                }
            }
        }

        /// <summary>
        /// Cleans up the camera resources (after stopping any video recording and/or preview if necessary) and unregisters from MediaCapture events
        /// </summary>
        /// <returns></returns>
        private async Task CleanupCameraAsync()
        {
            System.Diagnostics.Debug.WriteLine("QrCodeScannerPage.CleanupCameraAsync");

            if (_isInitialized)
            {
                if (_isPreviewing)
                {
                    // The call to stop the preview is included here for completeness, but can be
                    // safely removed if a call to MediaCapture.Dispose() is being made later,
                    // as the preview will be automatically stopped at that point
                    await StopPreviewAsync();
                }

                _isInitialized = false;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.Failed -= OnMediaCaptureFailedAsync;
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }

        /// <summary>
        /// Starts the preview and adjusts it for for rotation and mirroring after making a request
        /// to keep the screen on
        /// </summary>
        /// <returns></returns>
        private async Task StartPreviewAsync()
        {
            // Prevent the device from sleeping while the preview is running
            _displayRequest.RequestActive();

            // Set the preview source in the UI and mirror it if necessary
            captureElement.Source = _mediaCapture;
            captureElement.FlowDirection = _mirroringPreview
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;

            // Start the preview
            await _mediaCapture.StartPreviewAsync();
            _isPreviewing = true;

            // Initialize the preview to the current orientation
            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        /// <summary>
        /// Stops the preview and deactivates a display request, to allow the screen to go into
        /// power saving modes
        /// </summary>
        /// <returns></returns>
        private async Task StopPreviewAsync()
        {
            _isPreviewing = false;
            await _mediaCapture.StopPreviewAsync();

            // Use the dispatcher because this method is sometimes called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                captureElement.Source = null;

                // Allow the device screen to sleep now that the preview is stopped
                _displayRequest.RequestRelease();
            });
        }

        /// <summary>
        /// Takes a preview frame and scans it for a QR code.
        /// </summary>
        /// <returns>The scan result or null if no code found.</returns>
        private async Task<Result> AnalyzePreviewFrameAsync()
        {
            Result result = null;

            if (!_analyzingFrame)
            {
                _analyzingFrame = true;
                var previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
                var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int) previewProperties.Width, (int) previewProperties.Height);

                using (var currentFrame = await _mediaCapture.GetPreviewFrameAsync(videoFrame))
                {
                    SoftwareBitmap previewFrame = currentFrame.SoftwareBitmap;
                    WriteableBitmap writeableBitmap = new WriteableBitmap(previewFrame.PixelWidth, previewFrame.PixelHeight);
                    previewFrame.CopyToBuffer(writeableBitmap.PixelBuffer);
                    result = mBarcodeReader.Decode(writeableBitmap);
                }

                _analyzingFrame = false;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("QrCodeScannerPage.AnalyzePreviewFrameAsync: Still analyzing the previous frame");
            }

            return result;
        }

        /// <summary>
        /// Starts scanning for QR codes from the preview stream.
        /// Scanning will not be stopped until a code has been successfully scanned or we navigate
        /// off this page.
        /// </summary>
        private async void StartScanningForQrCodeAsync()
        {
            if (_isInitialized && _isPreviewing)
            {
                Result result = await AnalyzePreviewFrameAsync();

                if (result != null)
                {
                    System.Diagnostics.Debug.WriteLine("QrCodeScannerPage.StartScanningForQrCodeAsync: QR code read: " + result.Text);
                    ScannedQrCode = result.Text;
                    Frame.GoBack(); // Will do the necessary cleanup in OnNavigatedFrom
                }
                else
                {
                    // Scan another frame
                    System.Diagnostics.Debug.WriteLine("QrCodeScannerPage.StartScanningForQrCodeAsync: Failed to decode the frame - trying the next frame");
                    StartScanningForQrCodeAsync();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("QrCodeScannerPage.StartScanningForQrCodeAsync: Invalid state - no preview frames available");
            }
        }

        /// <summary>
        /// Gets the current orientation of the UI in relation to the device
        /// (when AutoRotationPreferences cannot be honored) and applies a corrective rotation to the preview
        /// </summary>
        private async Task SetPreviewRotationAsync()
        {
            // Only need to update the orientation if the camera is mounted on the device
            if (_isExternalCamera)
            {
                return;
            }

            // Calculate which way and how far to rotate the preview
            int rotationDegrees = ConvertDisplayOrientationToDegrees(_displayOrientation);

            // The rotation direction needs to be inverted if the preview is being mirrored
            if (_mirroringPreview)
            {
                rotationDegrees = (360 - rotationDegrees)%360;
            }

            // Add rotation metadata to the preview stream to make sure the aspect ratio / dimensions match when rendering and getting preview frames
            var props = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, rotationDegrees);
            await _mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }

        /// <summary>
        /// Attempts to find and return a device mounted on the panel specified, and on failure to
        /// find one it will return the first device listed
        /// </summary>
        /// <param name="desiredPanel">The desired panel on which the returned device should be
        /// mounted, if available</param>
        /// <returns></returns>
        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(
            Windows.Devices.Enumeration.Panel desiredPanel)
        {
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(
                x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);

            // If there is no device mounted on the desired panel, return the first device found
            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }

        /// <summary>
        /// Converts the given orientation of the app on the screen to the corresponding rotation in degrees
        /// </summary>
        /// <param name="orientation">The orientation of the app on the screen</param>
        /// <returns>An orientation in degrees</returns>
        private static int ConvertDisplayOrientationToDegrees(DisplayOrientations orientation)
        {
            switch (orientation)
            {
                case DisplayOrientations.Portrait:
                    return 90;
                case DisplayOrientations.LandscapeFlipped:
                    return 180;
                case DisplayOrientations.PortraitFlipped:
                    return 270;
                case DisplayOrientations.Landscape:
                default:
                    return 0;
            }
        }

        private async void OnApplicationSuspendingAsync(object sender, SuspendingEventArgs e)
        {
            if (Frame.CurrentSourcePageType == typeof(QrCodeScannerPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await CleanupAsync();
                deferral.Complete();
            }
        }

        private async void OnApplicationResumingAsync(object sender, object o)
        {
            if (Frame.CurrentSourcePageType == typeof(QrCodeScannerPage))
            {
                await InitializeAndStartScanningAsync();
            }
        }

        /// <summary>
        /// In the event of the app being minimized this method handles media property change
        /// events. If the app receives a mute notification, it is no longer in the foregroud.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void OnSystemMediaControlsPropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // Only handle this event if this page is currently being displayed
                if (args.Property == SystemMediaTransportControlsProperty.SoundLevel
                    && Frame.CurrentSourcePageType == typeof(QrCodeScannerPage))
                {
                    // Check to see if the app is being muted. If so, it is being minimized.
                    // Otherwise if it is not initialized, it is being brought into focus.
                    if (sender.SoundLevel == SoundLevel.Muted)
                    {
                        await CleanupAsync();
                    }
                    else if (!_isInitialized)
                    {
                        await InitializeAndStartScanningAsync();
                    }
                }
            });
        }

        /// <summary>
        /// Occurs each time the simple orientation sensor reports a new sensor reading.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="args">The event data.</param>
        private void OnOrientationSensorOrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            if (args.Orientation != SimpleOrientation.Faceup
                && args.Orientation != SimpleOrientation.Facedown)
            {
                // Only update the current orientation if the device is not parallel to the ground.
                // This allows users to take pictures of documents (FaceUp) or the ceiling
                // (FaceDown) in portrait or landscape, by first holding the device in the desired
                // orientation, and then pointing the camera either up or down, at the desired
                // subject.
                //
                // Note: This assumes that the camera is either facing the same way as the screen,
                //       or the opposite way. For devices with cameras mounted on other panels,
                //       this logic should be adjusted.
                _deviceOrientation = args.Orientation;
            }
        }

        /// <summary>
        /// This event will fire when the page is rotated, when the
        /// DisplayInformation.AutoRotationPreferences value set in the SetupUiAsync() method
        /// cannot be not honored.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="args">The event data.</param>
        private async void OnDisplayInformationOrientationChangedAsync(DisplayInformation sender, object args)
        {
            _displayOrientation = sender.CurrentOrientation;

            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        private async void OnMediaCaptureFailedAsync(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            System.Diagnostics.Debug.WriteLine("QrCodeScannerPage.OnMediaCaptureFailedAsync: (0x{0:X}) {1}",
                errorEventArgs.Code, errorEventArgs.Message);
            Frame.GoBack(); // Cleanup will happen in OnNavigatedFrom
        }
    }
}
