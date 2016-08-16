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
using SensorbergShowcase.Model;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace SensorbergShowcase.Controls
{
    public sealed partial class ScannerControl : UserControl
    {
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof(MainPageModel), typeof(ScannerControl), new PropertyMetadata(default(MainPageModel)));

        public ScannerControl()
        {
            InitializeComponent();
        }

        public MainPageModel Model
        {
            get { return (MainPageModel) GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }
    }
}
