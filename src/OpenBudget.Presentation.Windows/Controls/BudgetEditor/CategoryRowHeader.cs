using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace OpenBudget.Presentation.Windows.Controls.BudgetEditor
{
    public class CategoryRowHeader : Control
    {
        private FrameworkElement _popupContent;
        private PopupAdorner _popup;

        static CategoryRowHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CategoryRowHeader), new FrameworkPropertyMetadata(typeof(CategoryRowHeader)));
        }

        public CategoryRowHeader()
        {
        }

        public string CategoryName
        {
            get { return (string)GetValue(CategoryNameProperty); }
            set { SetValue(CategoryNameProperty, value); }
        }

        public static readonly DependencyProperty CategoryNameProperty =
            DependencyProperty.Register("CategoryName", typeof(string), typeof(CategoryRowHeader), new PropertyMetadata(null));


        public DataTemplate PopupTemplate
        {
            get { return (DataTemplate)GetValue(PopupTemplateProperty); }
            set { SetValue(PopupTemplateProperty, value); }
        }

        public static readonly DependencyProperty PopupTemplateProperty =
            DependencyProperty.Register("PopupTemplate", typeof(DataTemplate), typeof(CategoryRowHeader), new PropertyMetadata(null));

        public override void OnApplyTemplate()
        {
            _popupContent = PopupTemplate.LoadContent() as FrameworkElement;
            _popup = new PopupAdorner(this, _popupContent);

            _popupContent.SetBinding(FrameworkElement.DataContextProperty, new Binding("DataContext") { Source = this });
            _popup.SetBinding(PopupAdorner.IsOpenProperty, new Binding("IsPopupOpen") { Source = this, Mode = BindingMode.TwoWay });
        }

        public static RoutedEvent CategoryRowEditorOpenedEvent
            = EventManager.RegisterRoutedEvent("CategoryRowEditorOpened", RoutingStrategy.Bubble, typeof(CategoryRowEditorOpenedEventHandler), typeof(CategoryRowHeader));

        private void RaiseCategoryRowEditorOpened()
        {
            CategoryRowEditorOpenedEventArgs args = new CategoryRowEditorOpenedEventArgs(_popup, CategoryRowEditorOpenedEvent);
            RaiseEvent(args);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!IsPopupOpen)
            {
                IsPopupOpen = true;
                e.Handled = true;
            }
        }

        public bool IsPopupOpen
        {
            get { return (bool)GetValue(IsPopupOpenProperty); }
            set { SetValue(IsPopupOpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsPopupOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPopupOpenProperty =
            DependencyProperty.Register("IsPopupOpen", typeof(bool), typeof(CategoryRowHeader), new PropertyMetadata(false, OnIsPopupOpenChanged));

        private static void OnIsPopupOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var header = d as CategoryRowHeader;
            bool newValue = (bool)e.NewValue;
            if (newValue)
            {
                header._popup.Show();
                header.RaiseCategoryRowEditorOpened();
            }
            else
            {
                header._popup.Hide();
            }
        }
    }
}
