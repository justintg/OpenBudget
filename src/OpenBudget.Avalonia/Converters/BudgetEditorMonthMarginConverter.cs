using Avalonia;
using Avalonia.Data.Converters;
using OpenBudget.Avalonia.Controls.BudgetEditor;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenBudget.Avalonia.Converters
{
    public class BudgetEditorMonthMarginConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            var budgetEditor = values[1] as BudgetEditor;
            if (budgetEditor == null) return new Thickness();
            if (values[0] is bool boolValue)
            {
                if (boolValue)
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
    }
}
