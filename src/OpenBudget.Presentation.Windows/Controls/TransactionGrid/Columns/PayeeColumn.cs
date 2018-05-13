using OpenBudget.Application.ViewModels.TransactionGrid;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure.Entities;
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
using System.Windows.Input;
using System.Windows.Media;

namespace OpenBudget.Presentation.Windows.Controls.TransactionGrid.Columns
{
    public class PayeeColumn : Control
    {
        static PayeeColumn()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PayeeColumn), new FrameworkPropertyMetadata(typeof(PayeeColumn)));
        }

        private TextBox _searchTextBox;
        private ItemsControl _resultsItemControl;
        private PopupAdorner _dropDown;
        private FrameworkElement _popupContent;

        public PayeeColumn()
        {
            this.PreviewKeyDown += PayeeColumn_PreviewKeyDown;
            this.GotFocus += PayeeColumn_OnGotFocus;
            this.LostFocus += PayeeColumn_OnLostFocus;
        }

        private void OnResultItemClicked(object sender, RoutedEventArgs e)
        {
            ResultItem item = e.OriginalSource as ResultItem;

        }

        private void PayeeColumn_OnLostFocus(object sender, RoutedEventArgs e)
        {
            var test = FocusManager.GetFocusedElement(this);
            if(!_popupContent.IsFocused && ResultsOpen)
            {
                ResultsOpen = false;
            }
        }

        private void OnResultItemSelected(object sender, RoutedEventArgs e)
        {
            ResultItem item = e.OriginalSource as ResultItem;
            SelectedResultItem = item;
        }

        public ResultItem SelectedResultItem
        {
            get { return (ResultItem)GetValue(SelectedResultItemProperty); }
            set { SetValue(SelectedResultItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedResultItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedResultItemProperty =
            DependencyProperty.Register("SelectedResultItem", typeof(ResultItem), typeof(PayeeColumn), new PropertyMetadata(null, OnSelectedResultItemChanged));

        private static void OnSelectedResultItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ResultItem old = e.OldValue as ResultItem;
            if (old != null)
                old.IsSelected = false;
        }

        public DataTemplate ResultsBoxTemplate
        {
            get { return (DataTemplate)GetValue(ResultsBoxTemplateProperty); }
            set { SetValue(ResultsBoxTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ResultsBoxTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResultsBoxTemplateProperty =
            DependencyProperty.Register("ResultsBoxTemplate", typeof(DataTemplate), typeof(PayeeColumn), new PropertyMetadata(null));

        private void PayeeColumn_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (_searchTextBox != null)
            {
                _searchTextBox.Focus();
                _searchTextBox.SelectAll();
            }
        }

        private void PayeeColumn_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (ResultsOpen)
                {
                    ResultsOpen = false;
                    e.Handled = true;
                }
            }
        }

        private void PayeeColumn_KeyDown(object sender, KeyEventArgs e)
        {

        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _searchTextBox = GetTemplateChild("PART_SearchTextBox") as TextBox;
            _searchTextBox.PreviewKeyDown += SearchTextBox_KeyDown;
            _popupContent = ResultsBoxTemplate.LoadContent() as FrameworkElement;
            _dropDown = new PopupAdorner(_searchTextBox, _popupContent);
            _resultsItemControl = _popupContent.FindName("PART_ResultsItemControl") as ItemsControl;
            _resultsItemControl.SetBinding(ItemsControl.DataContextProperty, new Binding("DataContext") { Source = this });
            _popupContent.AddHandler(ResultItem.ResultItemSelectedEvent, new RoutedEventHandler(OnResultItemSelected));
            _popupContent.AddHandler(ResultItem.ResultItemClickedEvent, new RoutedEventHandler(OnResultItemClicked));
            //_payeeResultsBox.MouseDoubleClick += ResultsBox_MouseDoubleClick;
            //_payeeResultsBox.KeyDown += ResultsBox_KeyDown;
        }

        private void ResultsBox_KeyDown(object sender, KeyEventArgs e)
        {
            /*if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                if (ResultsOpen)
                {
                    var payee = _payeeResultsBox.SelectedItem as Payee;
                    if (payee == null) return;

                    SelectedPayeeOrAccount = payee;
                    ResultsOpen = false;
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Up)
            {
                if (ResultsOpen)
                {
                    if (_payeeResultsBox.SelectedIndex == 0)
                    {
                        _searchTextBox.Focus();
                        e.Handled = true;
                    }
                }
            }*/
        }

        private void ResultsBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            /*var listItem = (e.OriginalSource as DependencyObject)?.FindParent<ListBoxItem>();
            if (listItem != null)
            {
                SelectedPayeeOrAccount = _payeeResultsBox.SelectedItem as Payee;
                ResultsOpen = false;
                e.Handled = true;
            }*/
        }

        void MoveToNextUIElement()
        {
            // Creating a FocusNavigationDirection object and setting it to a
            // local field that contains the direction selected.
            FocusNavigationDirection focusDirection = FocusNavigationDirection.Next;

            // MoveFocus takes a TraveralReqest as its argument.
            TraversalRequest request = new TraversalRequest(focusDirection);

            // Gets the element with keyboard focus.
            UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;

            // Change keyboard focus.
            if (elementWithFocus != null)
            {
                if (elementWithFocus.MoveFocus(request))
                {

                }
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (ResultsOpen)
                {
                    var items = GetResultItems().ToList();
                    var nextIndex = items.IndexOf(SelectedResultItem) + 1;
                    if (nextIndex < items.Count)
                    {
                        items[nextIndex].IsSelected = true;
                        e.Handled = true;
                    }
                }
            }
            else if (e.Key == Key.Up)
            {
                if (ResultsOpen)
                {
                    var items = GetResultItems().ToList();
                    var prevIndex = items.IndexOf(SelectedResultItem) - 1;
                    if (prevIndex >= 0)
                    {
                        items[prevIndex].IsSelected = true;
                        e.Handled = true;
                    }
                }
            }
            else if (e.Key == Key.Tab)
            {
                if (ResultsOpen)
                {

                }
            }
        }

        private IEnumerable<ResultItem> GetResultItems()
        {
            foreach (var category in Results)
            {
                var categoryContent = _resultsItemControl.ItemContainerGenerator.ContainerFromItem(category) as DependencyObject;
                var categoryItemControl = categoryContent.GetChildOfType<ItemsControl>();
                foreach (var item in category.Items)
                {
                    var itemContainer = categoryItemControl.ItemContainerGenerator.ContainerFromItem(item);
                    var resultItem = itemContainer.GetChildOfType<ResultItem>();
                    if (resultItem != null)
                        yield return resultItem;
                }
            }
        }

        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SearchText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register("SearchText", typeof(string), typeof(PayeeColumn), new PropertyMetadata(null));

        public bool ResultsOpen
        {
            get { return (bool)GetValue(ResultsOpenProperty); }
            set { SetValue(ResultsOpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ResultsOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResultsOpenProperty =
            DependencyProperty.Register("ResultsOpen", typeof(bool), typeof(PayeeColumn), new PropertyMetadata(false, OnResultsOpenChanged));

        private static void OnResultsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var payeeColumn = d as PayeeColumn;
            if ((bool)e.NewValue)
            {
                payeeColumn._dropDown.Show();
                payeeColumn.Dispatcher.Invoke(() =>
                {
                    var items = payeeColumn.GetResultItems().ToList();
                    var firstItem = items.FirstOrDefault();
                    if (firstItem != null)
                    {
                        firstItem.IsSelected = true;
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
            else
            {
                payeeColumn._dropDown.Hide();
                payeeColumn._searchTextBox.Focus();
            }
        }

        public ICommand SelectResultItemCommand
        {
            get { return (ICommand)GetValue(SelectResultItemCommandProperty); }
            set { SetValue(SelectResultItemCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectResultItemCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectResultItemCommandProperty =
            DependencyProperty.Register("SelectResultItemCommand", typeof(ICommand), typeof(PayeeColumn), new PropertyMetadata(null));

        public ObservableCollection<ResultCategoryViewModel> Results
        {
            get { return (ObservableCollection<ResultCategoryViewModel>)GetValue(ResultsProperty); }
            set { SetValue(ResultsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Results.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResultsProperty =
            DependencyProperty.Register("Results", typeof(ObservableCollection<ResultCategoryViewModel>), typeof(PayeeColumn), new PropertyMetadata(null));


    }
}
