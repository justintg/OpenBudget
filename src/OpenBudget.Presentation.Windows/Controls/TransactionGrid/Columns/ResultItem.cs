using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenBudget.Presentation.Windows.Controls.TransactionGrid.Columns
{
    /// </summary>
    public class ResultItem : ContentControl
    {
        static ResultItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ResultItem), new FrameworkPropertyMetadata(typeof(ResultItem)));
        }

        public ResultItem()
        {
            this.PreviewMouseLeftButtonUp += ResultItem_PreviewMouseLeftButtonUp;
        }

        private void ResultItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            IsSelected = true;
            RaiseResultItemClicked();
        }

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(ResultItem), new PropertyMetadata(false, OnIsSelectedChange));

        private static void OnIsSelectedChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = d as ResultItem;
            if ((bool)e.NewValue)
            {
                item.RaiseResultItemSelected();
            }
        }

        public static readonly RoutedEvent ResultItemSelectedEvent =
               EventManager.RegisterRoutedEvent("ResultItemSelected", RoutingStrategy.Bubble,
               typeof(RoutedEventHandler), typeof(ResultItem));

        public event RoutedEventHandler ResultItemSelected
        {
            add { AddHandler(ResultItemSelectedEvent, value); }
            remove { RemoveHandler(ResultItemSelectedEvent, value); }
        }

        protected virtual void RaiseResultItemSelected()
        {
            RoutedEventArgs args = new RoutedEventArgs(ResultItem.ResultItemSelectedEvent);
            RaiseEvent(args);
        }

        public static readonly RoutedEvent ResultItemClickedEvent =
               EventManager.RegisterRoutedEvent("ResultItemClicked", RoutingStrategy.Bubble,
               typeof(RoutedEventHandler), typeof(ResultItem));

        public event RoutedEventHandler ResultItemClicked
        {
            add { AddHandler(ResultItemClickedEvent, value); }
            remove { RemoveHandler(ResultItemClickedEvent, value); }
        }

        protected virtual void RaiseResultItemClicked()
        {
            RoutedEventArgs args = new RoutedEventArgs(ResultItem.ResultItemClickedEvent);
            RaiseEvent(args);
        }

    }
}
