using System;
using System.Globalization;
using System.Windows.Data;

namespace ModManager.ToggleSwitch.Utils
{
    public class ScalarValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var oldValue = double.Parse($"{value}", culture);
            if (parameter != null)
            {
                oldValue *= double.Parse($"{parameter}", culture);
            }
            return oldValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var oldValue = double.Parse($"{value}", culture);
            if (parameter != null)
            {
                oldValue /= double.Parse($"{parameter}", culture);
            }
            return oldValue;
        }
    }
}
