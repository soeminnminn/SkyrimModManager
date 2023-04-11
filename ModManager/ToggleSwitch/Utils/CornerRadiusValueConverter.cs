using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ModManager.ToggleSwitch.Utils
{
    public class CornerRadiusValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var radius = double.Parse($"{value}", culture);
            if (parameter != null)
            {
                radius *= double.Parse($"{parameter}", culture);
            }
            return new CornerRadius(radius);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
