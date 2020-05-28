using ControlzEx.Standard;
using MahApps.Metro.Controls;
using OpenBudget.Application.ViewModels.TransactionGrid;
using OpenBudget.Presentation.Windows.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenBudget.Presentation.Windows.Controls.TransactionGrid
{
    public class TransactionGrid : ItemsControl
    {
        static TransactionGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TransactionGrid), new FrameworkPropertyMetadata(typeof(TransactionGrid)));
            FocusableProperty.OverrideMetadata(typeof(TransactionGrid), new FrameworkPropertyMetadata(true));
        }

        private ScrollBar _verticalScrollBar;
        private ScrollViewer _contentScrollViewer;
        private ScrollViewer _headerScrollViewer;
        private ItemsControl _headerRow;
        private TransactionGridRow _addTransactionRow;

        public TransactionGrid()
        {
            this.DataContextChanged += TransactionGrid_DataContextChanged;
            this.AddHandler(TransactionGridRow.RowSelectedEvent, new RoutedEventHandler(OnRowSelected));
            this.AddHandler(TransactionGridRow.RowMultiSelectedEvent, new RoutedEventHandler(OnRowMultiSelected));
            this.AddHandler(TransactionGridRow.RowUnSelectedEvent, new RoutedEventHandler(OnRowUnSelected));
            this.KeyDown += TransactionGrid_KeyDown;
        }

        private void TransactionGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (SelectedRow?.IsEditing == true)
                {
                    SelectedRow.CancelEditCommand.Execute(null);
                }
            }
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!IsKeyboardFocusWithin)
            {
                Focus();
            }
            base.OnMouseLeftButtonUp(e);
        }


        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            HandleUpDownNavigation(e);
            base.OnPreviewKeyDown(e);
        }

        private void HandleUpDownNavigation(KeyEventArgs e)
        {
            if (!(e.Key == Key.Down || e.Key == Key.Up)) return;
            if (SelectedRow?.IsEditing == true) return;
            if (IsAdding) return;

            e.Handled = true;
            int currentIndex = TransactionRows.IndexOf(SelectedRow);

            if (e.Key == Key.Down)
            {
                int nextIndex = currentIndex + 1;
                if (nextIndex < Items.Count)
                {
                    VirtualizingStackPanel panel = this.GetChildOfType<VirtualizingStackPanel>();
                    panel.BringIndexIntoViewPublic(nextIndex);
                    var nextItem = this.ItemContainerGenerator.ContainerFromIndex(nextIndex) as DependencyObject;
                    var nextRow = nextItem.GetChildOfType<TransactionGridRow>();
                    nextRow.IsSelected = true;
                }
            }
            else if (e.Key == Key.Up)
            {
                int lastIndex = currentIndex - 1;
                if (lastIndex >= 0)
                {
                    VirtualizingStackPanel panel = this.GetChildOfType<VirtualizingStackPanel>();
                    panel.BringIndexIntoViewPublic(lastIndex);
                    var lastItem = this.ItemContainerGenerator.ContainerFromIndex(lastIndex) as DependencyObject;
                    var lastRow = lastItem.GetChildOfType<TransactionGridRow>();
                    lastRow.IsSelected = true;
                }
            }
        }

        public TransactionGridRowViewModel SelectedRow
        {
            get
            {
                if (SelectedRows?.Count == 1)
                {
                    return SelectedRows[0];
                }

                return null;
            }
        }

        public IList<TransactionGridRowViewModel> SelectedRows
        {
            get { return (IList<TransactionGridRowViewModel>)GetValue(SelectedRowsProperty); }
            set { SetValue(SelectedRowsProperty, value); }
        }

        public static readonly DependencyProperty SelectedRowsProperty =
            DependencyProperty.Register("SelectedRows", typeof(IList<TransactionGridRowViewModel>), typeof(TransactionGrid), new PropertyMetadata(null));

        public bool IsAdding
        {
            get { return (bool)GetValue(IsAddingProperty); }
            set { SetValue(IsAddingProperty, value); }
        }

        public static readonly DependencyProperty IsAddingProperty =
            DependencyProperty.Register("IsAdding", typeof(bool), typeof(TransactionGrid), new PropertyMetadata(false, OnIsAddingChanged));

        private static void OnIsAddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        public ICommand RowSelectedCommand
        {
            get { return (ICommand)GetValue(RowSelectedCommandProperty); }
            set { SetValue(RowSelectedCommandProperty, value); }
        }

        public static readonly DependencyProperty RowSelectedCommandProperty =
            DependencyProperty.Register("RowSelectedCommand", typeof(ICommand), typeof(TransactionGrid), new PropertyMetadata(null));

        public ICommand RowMultiSelectedCommand
        {
            get { return (ICommand)GetValue(RowMultiSelectedCommandProperty); }
            set { SetValue(RowMultiSelectedCommandProperty, value); }
        }

        public static readonly DependencyProperty RowMultiSelectedCommandProperty =
            DependencyProperty.Register("RowMultiSelectedCommand", typeof(ICommand), typeof(TransactionGrid), new PropertyMetadata(null));

        public ICommand RowUnSelectedCommand
        {
            get { return (ICommand)GetValue(RowUnSelectedCommandProperty); }
            set { SetValue(RowUnSelectedCommandProperty, value); }
        }

        public static readonly DependencyProperty RowUnSelectedCommandProperty =
            DependencyProperty.Register("RowUnSelectedCommand", typeof(ICommand), typeof(TransactionGrid), new PropertyMetadata(null));

        private void OnRowMultiSelected(object sender, RoutedEventArgs e)
        {
            var row = e.OriginalSource as TransactionGridRow;
            RowMultiSelectedCommand?.Execute(row.ViewModel);
        }

        private void OnRowUnSelected(object sender, RoutedEventArgs e)
        {
            var row = e.OriginalSource as TransactionGridRow;
            RowUnSelectedCommand?.Execute(row.ViewModel);
        }

        private void OnRowSelected(object sender, RoutedEventArgs e)
        {
            var row = e.OriginalSource as TransactionGridRow;
            RowSelectedCommand?.Execute(row.ViewModel);
        }

        private void TransactionGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ResetScrollValues();
            SyncWidthToViewModel();
        }

        private void SyncWidthToViewModel()
        {
            if (_contentScrollViewer != null && this.DataContext is TransactionGridViewModel viewModel)
            {
                viewModel.SetVisibleWidth(_contentScrollViewer.ActualWidth);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _verticalScrollBar = GetTemplateChild("PART_VerticalScrollBar") as ScrollBar;
            _verticalScrollBar.LargeChange = 10;
            _verticalScrollBar.SmallChange = 2;

            _headerScrollViewer = GetTemplateChild("PART_HeaderScrollViewer") as ScrollViewer;
            _contentScrollViewer = GetTemplateChild("PART_ContentScrollViewer") as ScrollViewer;
            _headerRow = GetTemplateChild("PART_HeaderRow") as ItemsControl;
            _addTransactionRow = GetTemplateChild("PART_AddTransactionRow") as TransactionGridRow;

            BindScrollBarToContent();
            _contentScrollViewer.SizeChanged += ContentScrollViewer_SizeChanged;
        }

        private void ContentScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SyncWidthToViewModel();
        }

        private void ResetScrollValues()
        {
            if (_contentScrollViewer != null)
            {
                _contentScrollViewer.ScrollToHome();
            }

            if (_headerScrollViewer != null)
            {
                _headerScrollViewer.ScrollToHome();
            }
        }

        private void BindScrollBarToContent()
        {
            _verticalScrollBar.SetBinding(ScrollBar.MaximumProperty, (new Binding("ScrollableHeight") { Source = _contentScrollViewer, Mode = BindingMode.OneWay }));
            _verticalScrollBar.SetBinding(ScrollBar.ViewportSizeProperty, (new Binding("ViewportHeight") { Source = _contentScrollViewer, Mode = BindingMode.OneWay }));

            _verticalScrollBar.Scroll += (sender, e) =>
            {
                _contentScrollViewer.ScrollToVerticalOffset(e.NewValue);
            };

            _contentScrollViewer.ScrollChanged += (sender, e) =>
            {
                if (e.OriginalSource != _contentScrollViewer) return;

                if (SelectedRow?.IsEditing == true && SelectedRow.IsAdding != true)
                {
                    LockEditingRowToView();
                }

                _verticalScrollBar.Value = e.VerticalOffset;
                _headerScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);

                if (e.ViewportHeight >= e.ExtentHeight)
                {
                    _verticalScrollBar.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _verticalScrollBar.Visibility = Visibility.Visible;
                }
            };

            _contentScrollViewer.ScrollToHome();
        }

        public IList<TransactionGridRowViewModel> TransactionRows
        {
            get { return (IList<TransactionGridRowViewModel>)GetValue(TransactionRowsProperty); }
            set { SetValue(TransactionRowsProperty, value); }
        }

        private void LockEditingRowToView()
        {
            var container = this.GetChildOfType<VirtualizingStackPanel>();
            var transactionGridRow = this.ItemContainerGenerator.ContainerFromItem(SelectedRow).GetChildOfType<TransactionGridRow>();
            if (transactionGridRow == null || container == null) return;

            Point relativeLocation = transactionGridRow.TranslatePoint(new Point(0, 0), container);
            if (relativeLocation.Y < 0)
            {
                transactionGridRow.BringIntoView();
                return;
            }
            if (relativeLocation.Y + transactionGridRow.ActualHeight > container.ActualHeight)
            {
                transactionGridRow.BringIntoView();
                return;
            }
        }

        // Using a DependencyProperty as the backing store for TransactionRows.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TransactionRowsProperty =
            DependencyProperty.Register("TransactionRows", typeof(IList<TransactionGridRowViewModel>), typeof(TransactionGrid), new PropertyMetadata(OnTransactionRowsChanged));

        private static void OnTransactionRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as TransactionGrid;

            grid.ItemsSource = e.NewValue as IList<TransactionGridRowViewModel>;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
        }
    }
}
