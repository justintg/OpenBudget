using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace OpenBudget.Presentation.Windows.Controls
{
    public delegate void PopupButtonOpenedEventHandler(object sender, PopupButtonOpenedEventArgs e);

    public class PopupButtonOpenedEventArgs : RoutedEventArgs
    {
        public PopupAdorner PopupAdorner { get; private set; }
        public PopupButton PopupButton { get; private set; }

        public PopupButtonOpenedEventArgs(PopupButton popupButton, PopupAdorner popupAdorner, RoutedEvent routedEvent) : base(routedEvent)
        {
            this.PopupButton = popupButton;
            this.PopupAdorner = popupAdorner;
        }
    }

    public class PopupButton : Button
    {
        static PopupButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PopupButton), new FrameworkPropertyMetadata(typeof(PopupButton)));
        }

        private PopupAdorner _popup;
        private FrameworkElement _popupContent;


        public PopupButton()
        {

        }

        public DataTemplate PopupTemplate
        {
            get { return (DataTemplate)GetValue(PopupTemplateProperty); }
            set { SetValue(PopupTemplateProperty, value); }
        }

        public static readonly DependencyProperty PopupTemplateProperty =
            DependencyProperty.Register("PopupTemplate", typeof(DataTemplate), typeof(PopupButton), new PropertyMetadata(null));

        public PopupOpenPreference OpenPreference
        {
            get { return (PopupOpenPreference)GetValue(OpenPreferenceProperty); }
            set { SetValue(OpenPreferenceProperty, value); }
        }

        public static readonly DependencyProperty OpenPreferenceProperty =
            DependencyProperty.Register("OpenPreference", typeof(PopupOpenPreference), typeof(PopupButton), new PropertyMetadata(PopupOpenPreference.Top));

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var popupContent = new ContentControl();
            popupContent.ContentTemplate = PopupTemplate;
            popupContent.SetBinding(ContentControl.ContentProperty, new Binding());
            _popupContent = popupContent;

            var contentPresenter = popupContent.FindChild<ContentPresenter>();
            _popup = new PopupAdorner(this, _popupContent);

            _popupContent.SetBinding(FrameworkElement.DataContextProperty, new Binding("DataContext") { Source = this });
        }

        protected override void OnClick()
        {
            IsPopupOpen = !IsPopupOpen;
        }

        private void FindFocus()
        {
            App.Current.Dispatcher.InvokeAsync(() =>
            {
                var contentPresenter = _popupContent.FindChild<ContentPresenter>();
                var focusElement = contentPresenter?.ContentTemplate?.FindName("focusElement", contentPresenter) as FrameworkElement;
                focusElement?.Focus();
            },
            DispatcherPriority.ContextIdle);
        }

        public bool IsPopupOpen
        {
            get { return (bool)GetValue(IsPopupOpenProperty); }
            set { SetValue(IsPopupOpenProperty, value); }
        }

        public static readonly DependencyProperty IsPopupOpenProperty =
            DependencyProperty.Register("IsPopupOpen", typeof(bool), typeof(PopupButton), new PropertyMetadata(false, OnIsPopupOpenChanged));


        private static void OnIsPopupOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var popupButton = d as PopupButton;
            bool newValue = (bool)e.NewValue;
            if (newValue)
            {
                popupButton._popup.Show(popupButton.OpenPreference);
                popupButton.RaiseCategoryRowEditorOpened();

                popupButton.FindFocus();
            }
            else
            {
                popupButton._popup.Hide();
            }
        }

        public static RoutedEvent PopupButtonOpenedEvent
            = EventManager.RegisterRoutedEvent("PopupButtonOpened", RoutingStrategy.Bubble, typeof(PopupButtonOpenedEventHandler), typeof(PopupButton));

        private void RaiseCategoryRowEditorOpened()
        {
            PopupButtonOpenedEventArgs args = new PopupButtonOpenedEventArgs(this, _popup, PopupButtonOpenedEvent);
            RaiseEvent(args);
        }
    }
}
