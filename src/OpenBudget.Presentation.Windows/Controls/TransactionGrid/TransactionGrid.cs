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
        }

        private ScrollBar _verticalScrollBar;
        private ScrollBar _horizontalScrollBar;
        private ScrollViewer _contentScrollViewer;
        private ScrollViewer _headerScrollViewer;
        private ItemsControl _headerRow;
        private StackPanel _headerPanel;

        public TransactionGrid()
        {
            this.DataContextChanged += TransactionGrid_DataContextChanged;
            this.AddHandler(TransactionGridRow.RowSelectedEvent, new RoutedEventHandler(OnRowSelected));
            this.PreviewKeyDown += TransactionGrid_PreviewKeyDown;
            this.KeyDown += TransactionGrid_KeyDown;
        }

        private void TransactionGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (SelectedRow != null && SelectedRow.IsEditing)
                {
                    SelectedRowViewModel.CancelEditCommand.Execute(null);
                }
            }
        }

        private void TransactionGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HandleUpDownNavigation(e);
        }

        private void HandleUpDownNavigation(KeyEventArgs e)
        {
            if (!(e.Key == Key.Down || e.Key == Key.Up)) return;
            if (SelectedRow == null) return;
            if (SelectedRow.IsEditing) return;

            e.Handled = true;
            int currentIndex = TransactionRows.IndexOf(SelectedRowViewModel);

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

        private static void OnRowSelected(object sender, RoutedEventArgs e)
        {
            var grid = sender as TransactionGrid;
            var row = e.OriginalSource as TransactionGridRow;
            grid.SelectedRow = row;
        }

        public TransactionGridRow SelectedRow
        {
            get { return (TransactionGridRow)GetValue(SelectedRowProperty); }
            set { SetValue(SelectedRowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedRow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedRowProperty =
            DependencyProperty.Register("SelectedRow", typeof(TransactionGridRow), typeof(TransactionGrid), new PropertyMetadata(null, OnSelectedRowChanged));

        private static void OnSelectedRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as TransactionGrid;

            var oldRow = e.OldValue as TransactionGridRow;
            if (oldRow != null)
            {
                oldRow.IsSelected = false;
            }

            var newRow = e.NewValue as TransactionGridRow;
            var newViewModel = newRow.DataContext as TransactionGridRowViewModel;
            grid.SelectedRowViewModel = newViewModel;
        }

        public TransactionGridRowViewModel SelectedRowViewModel
        {
            get { return (TransactionGridRowViewModel)GetValue(SelectedRowViewModelProperty); }
            set { SetValue(SelectedRowViewModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedRowViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedRowViewModelProperty =
            DependencyProperty.Register("SelectedRowViewModel", typeof(TransactionGridRowViewModel), typeof(TransactionGrid), new PropertyMetadata(null));

        private void TransactionGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ResetScrollValues();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _verticalScrollBar = GetTemplateChild("PART_VerticalScrollBar") as ScrollBar;
            _horizontalScrollBar = GetTemplateChild("PART_HorizontalScrollBar") as ScrollBar;
            _headerScrollViewer = GetTemplateChild("PART_HeaderScrollViewer") as ScrollViewer;
            _contentScrollViewer = GetTemplateChild("PART_ContentScrollViewer") as ScrollViewer;
            _headerRow = GetTemplateChild("PART_HeaderRow") as ItemsControl;

            BindScrollBarToContent();
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

            _horizontalScrollBar.SetBinding(ScrollBar.MaximumProperty, (new Binding("ScrollableWidth") { Source = _contentScrollViewer, Mode = BindingMode.OneWay }));
            _horizontalScrollBar.SetBinding(ScrollBar.ViewportSizeProperty, (new Binding("ViewportWidth") { Source = _contentScrollViewer, Mode = BindingMode.OneWay }));

            /*_headerRow.Loaded += (sender, e) =>
            {
                _headerPanel = _headerRow.GetChildOfType<StackPanel>();
                //_horizontalScrollBar.SetBinding(ScrollBar.MaximumProperty, (new Binding("ActualWidth") { Source = _headerPanel, Mode = BindingMode.OneWay }));
            };*/

            _verticalScrollBar.Scroll += (sender, e) =>
            {
                _contentScrollViewer.ScrollToVerticalOffset(e.NewValue);
            };

            _contentScrollViewer.ScrollChanged += (sender, e) =>
            {
                if (e.OriginalSource != _contentScrollViewer) return;

                //Lock an editing row from being scrolled out of view
                if (SelectedRow != null && SelectedRow.IsEditing)
                {
                    var container = SelectedRow.FindParent<VirtualizingStackPanel>();
                    Point relativeLocation = SelectedRow.TranslatePoint(new Point(0, 0), container);
                    if (relativeLocation.Y < 0)
                    {
                        SelectedRow.BringIntoView();
                        return;
                    }
                    if (relativeLocation.Y + SelectedRow.ActualHeight > container.ActualHeight)
                    {
                        SelectedRow.BringIntoView();
                        return;
                    }
                }

                _verticalScrollBar.Value = e.VerticalOffset;
                _horizontalScrollBar.Value = e.HorizontalOffset;
                _headerScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);

                if (e.ViewportWidth > e.ExtentWidth)
                {
                    _horizontalScrollBar.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _horizontalScrollBar.Visibility = Visibility.Visible;
                }

                if (e.ViewportHeight >= e.ExtentHeight)
                {
                    _verticalScrollBar.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _verticalScrollBar.Visibility = Visibility.Visible;
                }
            };

            _horizontalScrollBar.Scroll += (sender, e) =>
            {
                _contentScrollViewer.ScrollToHorizontalOffset(e.NewValue);
                _headerScrollViewer.ScrollToHorizontalOffset(e.NewValue);
            };

            _contentScrollViewer.ScrollToHome();
        }

        public ObservableCollection<TransactionGridRowViewModel> TransactionRows
        {
            get { return (ObservableCollection<TransactionGridRowViewModel>)GetValue(TransactionRowsProperty); }
            set { SetValue(TransactionRowsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TransactionRows.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TransactionRowsProperty =
            DependencyProperty.Register("TransactionRows", typeof(ObservableCollection<TransactionGridRowViewModel>), typeof(TransactionGrid), new PropertyMetadata(OnTransactionRowsChanged));

        private static void OnTransactionRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as TransactionGrid;

            grid.ItemsSource = e.NewValue as ObservableCollection<TransactionGridRowViewModel>;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
        }
    }
}
