using OpenBudget.Application.ViewModels.TransactionGrid;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace OpenBudget.Presentation.Windows.Controls.TransactionGrid
{
    public class TransactionGridRow : ItemsControl
    {
        static TransactionGridRow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TransactionGridRow), new FrameworkPropertyMetadata(typeof(TransactionGridRow)));
        }

        public TransactionGridRow()
        {
            this.DataContextChanged += (sender, e) => { OnDataContextChanged(e); };
        }

        public TransactionGridRowViewModel ViewModel { get; private set; }

        private void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is TransactionGridRowViewModel viewModel)
            {
                ViewModel = viewModel;
            }
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            if (!IsSelected)
            {
                IsSelected = true;
            }
            base.OnMouseRightButtonUp(e);
        }

        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            if (IsEditing)
            {
                e.Handled = true;
            }
            base.OnContextMenuOpening(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            bool ctrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
            if (!IsSelected)
            {
                if (ctrlPressed)
                {
                    RaiseRowMultiSelected();
                }
                else
                {
                    RaiseRowSelected();
                }
                e.Handled = true;
            }
            else if (IsSelected && !IsEditing)
            {
                if (ctrlPressed)
                {
                    RaiseRowUnSelected();
                    e.Handled = true;
                }
                else
                {
                    if (BeginEditCommand != null)
                    {
                        BeginEditCommand.Execute(null);
                        e.Handled = true;
                    }
                }
            }
            base.OnMouseLeftButtonUp(e);
        }

        public ObservableCollection<TransactionGridCellViewModel> Cells
        {
            get { return (ObservableCollection<TransactionGridCellViewModel>)GetValue(CellsProperty); }
            set { SetValue(CellsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Cells.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CellsProperty =
            DependencyProperty.Register("Cells", typeof(ObservableCollection<TransactionGridCellViewModel>), typeof(TransactionGridRow), new PropertyMetadata(OnCellsChanged));

        private static void OnCellsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var row = d as TransactionGridRow;
            var cells = e.NewValue as ObservableCollection<TransactionGridCellViewModel>;

            row.ItemsSource = cells;
        }

        public ICommand BeginEditCommand
        {
            get { return (ICommand)GetValue(BeginEditCommandProperty); }
            set { SetValue(BeginEditCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BeginEditCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BeginEditCommandProperty =
            DependencyProperty.Register("BeginEditCommand", typeof(ICommand), typeof(TransactionGridRow), new PropertyMetadata(null));

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(TransactionGridRow), new PropertyMetadata(false, OnIsSelectedChanged));

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public static readonly RoutedEvent RowSelectedEvent =
           EventManager.RegisterRoutedEvent("RowSelected", RoutingStrategy.Bubble,
           typeof(RoutedEventHandler), typeof(TransactionGridRow));

        public event RoutedEventHandler RowSelected
        {
            add { AddHandler(RowSelectedEvent, value); }
            remove { RemoveHandler(RowSelectedEvent, value); }
        }

        protected virtual void RaiseRowSelected()
        {
            RoutedEventArgs args = new RoutedEventArgs(TransactionGridRow.RowSelectedEvent);
            RaiseEvent(args);
        }

        public static readonly RoutedEvent RowUnSelectedEvent =
           EventManager.RegisterRoutedEvent("RowUnSelected", RoutingStrategy.Bubble,
           typeof(RoutedEventHandler), typeof(TransactionGridRow));

        public event RoutedEventHandler RowUnSelected
        {
            add { AddHandler(RowUnSelectedEvent, value); }
            remove { RemoveHandler(RowUnSelectedEvent, value); }
        }

        protected virtual void RaiseRowUnSelected()
        {
            RoutedEventArgs args = new RoutedEventArgs(TransactionGridRow.RowUnSelectedEvent);
            RaiseEvent(args);
        }

        public static readonly RoutedEvent RowMultiSelectedEvent =
           EventManager.RegisterRoutedEvent("RowMultiSelected", RoutingStrategy.Bubble,
           typeof(RoutedEventHandler), typeof(TransactionGridRow));

        public event RoutedEventHandler RowMultiSelected
        {
            add { AddHandler(RowSelectedEvent, value); }
            remove { RemoveHandler(RowSelectedEvent, value); }
        }

        protected virtual void RaiseRowMultiSelected()
        {
            RoutedEventArgs args = new RoutedEventArgs(TransactionGridRow.RowMultiSelectedEvent);
            RaiseEvent(args);
        }

        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsEditing.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register("IsEditing", typeof(bool), typeof(TransactionGridRow), new PropertyMetadata(false));

    }
}
