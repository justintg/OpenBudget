using OpenBudget.Application.ViewModels.BudgetEditor;
using OpenBudget.Presentation.Windows.Controls.BudgetEditor;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenBudget.Presentation.Windows.Converters
{
    public class BudgetEditorMonthMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var budgetEditor = values[1] as BudgetEditor;
            if(values[0] is bool boolValue)
            {
                if(boolValue)
                {
                    return new Thickness();
                }
                else
                {
                    return new Thickness(budgetEditor.MonthMarginLeft, 0, 0, 0);
                }
            }

            return new Thickness();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
