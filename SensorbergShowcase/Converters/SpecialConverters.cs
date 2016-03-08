using System;
using Windows.UI.Xaml.Data;

namespace SensorbergShowcase.Converters
{
    public class BoolToScannerStateTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool valueAsBool = (value is bool && (bool)value);
            return valueAsBool
                ? App.ResourceLoader.GetString("scanning/Text")
                : App.ResourceLoader.GetString("scannerStopped/Text");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToAdvertisingButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool valueAsBool = (value is bool && (bool)value);
            return valueAsBool 
                ? App.ResourceLoader.GetString("stopAdvertising/Label")
                : App.ResourceLoader.GetString("startAdvertising/Label");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToBackgroundImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool valueAsBool = (value is bool && (bool)value);
            return valueAsBool
                ? "ms-appx:///Assets/Graphics/BigBackground.jpg"
                : "ms-appx:///Assets/Graphics/Background.jpg";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool valueAsBool = (value is bool && (bool)value);
            string parameterAsString = parameter as string;
            string[] colorsAsStringArray = parameterAsString.Split(',');
            string selectedColorAsString = "Black";

            if (colorsAsStringArray.Length > 1)
            {
                selectedColorAsString = valueAsBool ? colorsAsStringArray[0] : colorsAsStringArray[1];
                System.Diagnostics.Debug.WriteLine("BoolToColorStringConverter: Will return \"" + selectedColorAsString + "\"");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("BoolToColorStringConverter: Invalid parameter: " + parameter);
            }

            return selectedColorAsString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class RangeToImageUriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string uri = null;

            if (value is int)
            {
                int range = (int)value;
                uri = "ms-appx:///Assets/Graphics/range" + range.ToString() + ".png";
            }

            return uri;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
