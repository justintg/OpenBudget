using OpenBudget.Application.ViewModels.TransactionGrid;
using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace OpenBudget.Presentation.Windows.Converters
{
    public class SplitTransactionVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if ((values[0].GetType().IsEnum) && Enum.IsDefined(typeof(ColumnType), values[0]) && values[1] is bool isSplit)
                {
                    ColumnType cellType = (ColumnType)values[0];
                    if (cellType == ColumnType.Transaction && isSplit)
                    {
                        return Visibility.Visible;
                    }
                    else
                    {
                        return Visibility.Collapsed;
                    }
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
            catch (InvalidOperationException)
            {
                return Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
