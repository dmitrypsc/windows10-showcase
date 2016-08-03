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
using Windows.UI.Core;
using SensorbergSDK;
using Windows.UI.Xaml;
using SensorbergSDK.Internal.Data;

namespace SensorbergShowcase.Model
{
    public class MainPageModel : INotifyPropertyChanged
    {
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
        public ObservableCollection<string> ResolvedActions { get; } = new ObservableCollection<string>();

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