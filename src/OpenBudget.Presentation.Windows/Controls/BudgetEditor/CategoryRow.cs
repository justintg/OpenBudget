using OpenBudget.Application.ViewModels.BudgetEditor;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace OpenBudget.Presentation.Windows.Controls.BudgetEditor
{
    public class CategoryRow : ItemsControl
    {
        static CategoryRow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CategoryRow), new FrameworkPropertyMetadata(typeof(CategoryRow)));
        }

        public string CategoryName
        {
            get { return (string)GetValue(CategoryNameProperty); }
            set { SetValue(CategoryNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CategoryName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CategoryNameProperty =
            DependencyProperty.Register("CategoryName", typeof(string), typeof(CategoryRow), new PropertyMetadata(null));
    }
}
