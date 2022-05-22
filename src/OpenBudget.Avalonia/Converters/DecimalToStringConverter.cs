using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace OpenBudget.Avalonia.Converters
{
    public class DecimalToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decValue)
            {
                return decValue.ToString("C");
            }

            return "Converter Error - Value Type not Supported";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
