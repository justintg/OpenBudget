using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace OpenBudget.Presentation.Windows.Converters
{
    public class DecimalToVisibilityConverter : IValueConverter
    {
        public Visibility Zero { get; set; } = Visibility.Collapsed;
        public Visibility LessThanZero { get; set; } = Visibility.Collapsed;
        public Visibility GreaterThanZero { get; set; } = Visibility.Collapsed;

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
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
