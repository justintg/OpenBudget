using OpenBudget.Application.ViewModels.TransactionGrid;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Presentation.Windows.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenBudget.Presentation.Windows.Controls.TransactionGrid.Columns
{
    public class ResultsColumn : Control
    {
        static ResultsColumn()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ResultsColumn), new FrameworkPropertyMetadata(typeof(ResultsColumn)));
        }

        private TextBox _searchTextBox;
        private ItemsControl _resultsItemControl;
        private PopupAdorner _dropDown;
        private FrameworkElement _dropDownContent;

        public ResultsColumn()
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            InitializeSearchTextBox();
            InitializeDropDown();
        }

        private void InitializeSearchTextBox()
        {
            _searchTextBox = GetTemplateChild("PART_SearchTextBox") as TextBox;
            _searchTextBox.PreviewKeyDown += SearchTextBox_PreviewKeyDown;
            _searchTextBox.GotKeyboardFocus += SearchTextBox_GotKeyboardFocus;
            _searchTextBox.PreviewMouseDown += SearchTextBox_PreviewMouseDown;
            _searchTextBox.LostKeyboardFocus += SearchTextBox_LostKeyboardFocus;
        }

        private void InitializeDropDown()
        {
            _dropDownContent = ResultsBoxTemplate.LoadContent() as FrameworkElement;
            _dropDown = new PopupAdorner(_searchTextBox, _dropDownContent);
            _resultsItemControl = _dropDownContent.FindName("PART_ResultsItemControl") as ItemsControl;
            _dropDownContent.SetBinding(FrameworkElement.DataContextProperty, new Binding("DataContext") { Source = this });
            _dropDownContent.AddHandler(ResultItem.ResultItemSelectedEvent, new RoutedEventHandler(OnResultItemSelected));
            _dropDownContent.AddHandler(ResultItem.ResultItemClickedEvent, new RoutedEventHandler(OnResultItemClicked));
            _dropDownContent.MouseDown += PopupContent_MouseDown;
        }

        private void SearchTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Check if the SearchTextBox doesnt have Keyboard focus before a click
            //If it doesn't we give it focus but stop additional side effects of the click
            //which allows the focus event to select the text without also placing the cursor
            if ((!_searchTextBox.IsKeyboardFocusWithin))
            {
                _searchTextBox.Focus();
                e.Handled = true;
            }
        }

        private void SearchTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _searchTextBox.SelectAll();
        }

        private void PopupContent_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Stop click events on the popup from bubbling up to the Adorner layer which may cause the control to lose focus
            e.Handled = true;
        }

        private void SearchTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (ResultsOpen)
            {
                ResultsOpen = false;
            }
        }

        private void OnResultItemClicked(object sender, RoutedEventArgs e)
        {
            ResultItem item = e.OriginalSource as ResultItem;
            SelectResultItemCommand?.Execute(item.DataContext);
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
            DependencyProperty.Register("SelectedResultItem", typeof(ResultItem), typeof(ResultsColumn), new PropertyMetadata(null, OnSelectedResultItemChanged));

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
            DependencyProperty.Register("ResultsBoxTemplate", typeof(DataTemplate), typeof(ResultsColumn), new PropertyMetadata(null));


        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (ResultsOpen)
                {
                    ResultsOpen = false;
                    e.Handled = true;
                }
            }
            base.OnPreviewKeyDown(e);
        }

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
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
            else if (e.Key == Key.Tab || e.Key == Key.Enter)
            {
                if (ResultsOpen)
                {
                    SelectResultItemCommand?.Execute(SelectedResultItem.DataContext);
                    if (e.Key == Key.Enter) e.Handled = true;
                }
            }
        }

        private IEnumerable<ResultItem> GetResultItems()
        {
            foreach (var category in Results)
            {
                var categoryContent = _resultsItemControl.ItemContainerGenerator.ContainerFromItem(category) as DependencyObject;
                var categoryItemControl = categoryContent.GetChildOfType<ItemsControl>();
                if (categoryItemControl == null) continue;
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
            DependencyProperty.Register("SearchText", typeof(string), typeof(ResultsColumn), new PropertyMetadata(null));

        public bool ResultsOpen
        {
            get { return (bool)GetValue(ResultsOpenProperty); }
            set { SetValue(ResultsOpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ResultsOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResultsOpenProperty =
            DependencyProperty.Register("ResultsOpen", typeof(bool), typeof(ResultsColumn), new PropertyMetadata(false, OnResultsOpenChanged));

        private static void OnResultsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var payeeColumn = d as ResultsColumn;
            if ((bool)e.NewValue)
            {
                payeeColumn._dropDown.Show();
                payeeColumn.SelectFirstResultItem();
            }
            else
            {
                payeeColumn._dropDown.Hide();
            }
        }

        public ICommand SelectResultItemCommand
        {
            get { return (ICommand)GetValue(SelectResultItemCommandProperty); }
            set { SetValue(SelectResultItemCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectResultItemCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectResultItemCommandProperty =
            DependencyProperty.Register("SelectResultItemCommand", typeof(ICommand), typeof(ResultsColumn), new PropertyMetadata(null));

        public ObservableCollection<ResultCategoryViewModel> Results
        {
            get { return (ObservableCollection<ResultCategoryViewModel>)GetValue(ResultsProperty); }
            set { SetValue(ResultsProperty, value); }
        }

        public string Watermark
        {
            get { return (string)GetValue(WatermarkProperty); }
            set { SetValue(WatermarkProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Watermark.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register("Watermark", typeof(string), typeof(ResultsColumn), new PropertyMetadata(null));


        // Using a DependencyProperty as the backing store for Results.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResultsProperty =
            DependencyProperty.Register("Results", typeof(ObservableCollection<ResultCategoryViewModel>), typeof(ResultsColumn), new PropertyMetadata(null, OnResultsChanged));

        private static void OnResultsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var payeeColumn = d as ResultsColumn;
            if (payeeColumn.ResultsOpen)
            {
                payeeColumn.SelectFirstResultItem();
            }
        }

        private void SelectFirstResultItem()
        {
            if (Results == null) return;

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
            {
                var items = GetResultItems().ToList();
                var firstItem = items.FirstOrDefault();
                if (firstItem != null)
                {
                    firstItem.IsSelected = true;
                }
            }));
        }
    }
}
