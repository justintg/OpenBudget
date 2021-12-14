using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace OpenBudget.Avalonia.Converters
{
    public class DecimalToVisibilityConverter : IValueConverter
    {
        public bool Zero { get; set; } = false;
        public bool LessThanZero { get; set; } = false;
        public bool GreaterThanZero { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decValue)
            {
                if (decValue == 0M)
                {
                    return Zero;
                }
                else if (decValue > 0M)
                {
                    return GreaterThanZero;
                }
                else if (decValue < 0M)
                {
                    return LessThanZero;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
