using System;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Xaml.Controls;
using SensorbergShowcase.Views;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace SensorbergShowcase.Controls
{
    public sealed partial class AboutControl : UserControl
    {
        public string AppVersion
        {
            get
            {
                var versionExpression = new Regex("Version=(?<version>[0-9.]*)");
                var match = versionExpression.Match(typeof(MainPage).AssemblyQualifiedName);
                return "Version: " + (match.Success ? match.Groups["version"].Value : null);
            }
        }

        public ICommand OpenLinkCommand { get; } = new OpenLinkCommand();

        public AboutControl()
        {
            InitializeComponent();
        }
    }

    public class OpenLinkCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public async void Execute(object parameter)
        {
            await Launcher.LaunchUriAsync(new Uri(parameter.ToString()));
        }

        public event EventHandler CanExecuteChanged;
    }
}