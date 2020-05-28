using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace OpenBudget.Presentation.Windows.Controls.BudgetEditor
{
    public delegate void CategoryRowEditorOpenedEventHandler(object sender, CategoryRowEditorOpenedEventArgs e);

    public class CategoryRowEditorOpenedEventArgs : RoutedEventArgs
    {
        public PopupAdorner PopupAdorner { get; private set; }

        public CategoryRowEditorOpenedEventArgs(PopupAdorner popupAdorner, RoutedEvent routedEvent) : base(routedEvent)
        {
            this.PopupAdorner = popupAdorner;
        }
    }
}
