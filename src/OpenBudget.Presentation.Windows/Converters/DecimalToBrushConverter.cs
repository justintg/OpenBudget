using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OpenBudget.Presentation.Windows.Converters
{
    public class DecimalToBrushConverter : IValueConverter
    {
        public Brush PositiveBrush { get; set; } = Brushes.Black;
        public Brush NegativeBrush { get; set; } = Brushes.Black;
        public Brush ZeroBrush { get; set; } = Brushes.Black;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is decimal decimalValue)
            {
                if(decimalValue > 0)
                {
                    return PositiveBrush;
                }
                else if(decimalValue < 0)
                {
                    return NegativeBrush;
                }
                else
                {
                    return ZeroBrush;
                }
            }

            throw new InvalidOperationException("This converter only supports decimal values");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
