using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace OpenBudget.Presentation.Windows.Converters
{
    public class CategoryMonthBorderThicknessConverter : IValueConverter
    {
        public CategoryMonthBorderThicknessConverter()
        {
        }

        public Thickness FirstMonthThickness { get; set; }
        public Thickness NotFirstMonthThickness { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolVal)
            {
                if (boolVal)
                {
                    return FirstMonthThickness;
                }
                else
                {
                    return NotFirstMonthThickness;
                }
            }
            else
            {
                return NotFirstMonthThickness;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
