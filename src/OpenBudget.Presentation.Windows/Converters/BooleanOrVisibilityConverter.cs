using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace OpenBudget.Presentation.Windows.Converters
{
    public abstract class BooleanOrConverter<T> : IMultiValueConverter
    {
        public T True { get; private set; }
        public T False { get; private set; }

        public BooleanOrConverter(T trueValue, T falseValue)
        {
            True = trueValue;
            False = falseValue;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = false;
            foreach (object objValue in values)
            {
                if (objValue is bool boolValue)
                {
                    result = result || boolValue;
                    if (result) break;
                }
                else
                {
                    throw new NotSupportedException("Values passed to the BooleanOrConverter must be boolean.");
                }
            }

            return result ? True : False;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanOrVisibilityConverter : BooleanOrConverter<Visibility>
    {
        public BooleanOrVisibilityConverter() : base(Visibility.Visible, Visibility.Collapsed)
        {
        }
    }

}
