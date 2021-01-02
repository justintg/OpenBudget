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

    public class PopupButton : Button, IPopupPositionCallback
    {
        static PopupButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PopupButton), new FrameworkPropertyMetadata(typeof(PopupButton)));
        }

        private PopupAdorner _popup;
        private BindingExpressionBase _contentBinding;
        private ContentControl _popupContent;


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



        public DataTemplate PopupTopTemplate
        {
            get { return (DataTemplate)GetValue(PopupTopTemplateProperty); }
            set { SetValue(PopupTopTemplateProperty, value); }
        }

        public static readonly DependencyProperty PopupTopTemplateProperty =
            DependencyProperty.Register("PopupTopTemplate", typeof(DataTemplate), typeof(PopupButton), new PropertyMetadata(null));

        public DataTemplate PopupBottomTemplate
        {
            get { return (DataTemplate)GetValue(PopupBottomTemplateProperty); }
            set { SetValue(PopupBottomTemplateProperty, value); }
        }

        public static readonly DependencyProperty PopupBottomTemplateProperty =
            DependencyProperty.Register("PopupBottomTemplate", typeof(DataTemplate), typeof(PopupButton), new PropertyMetadata(null));

        public Thickness PopupMargin
        {
            get { return (Thickness)GetValue(PopupMarginProperty); }
            set { SetValue(PopupMarginProperty, value); }
        }

        public static readonly DependencyProperty PopupMarginProperty =
            DependencyProperty.Register("PopupMargin", typeof(Thickness), typeof(PopupButton), new PropertyMetadata(new Thickness(0)));

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
            _contentBinding = popupContent.SetBinding(ContentControl.ContentProperty, new Binding());
            popupContent.SetBinding(ContentControl.MarginProperty, new Binding("PopupMargin") { Source = this });
            _popupContent = popupContent;

            var contentPresenter = popupContent.FindChild<ContentPresenter>();
            _popup = new PopupAdorner(this, _popupContent, this);

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

        public void PopupPositionChanged(PopupOpenPosition position)
        {
            var oldTemplate = _popupContent.ContentTemplate;
            var content = _popupContent.Content;
            if(position == PopupOpenPosition.Bottom && PopupBottomTemplate != null)
            {
                _popupContent.ContentTemplate = PopupBottomTemplate;
                BindingOperations.ClearBinding(_popupContent, ContentControl.ContentProperty);
                _contentBinding = _popupContent.SetBinding(ContentControl.ContentProperty, new Binding());
                _contentBinding.UpdateTarget();
            }
            else if(position == PopupOpenPosition.Top && PopupTopTemplate != null)
            {
                _popupContent.ContentTemplate = PopupTopTemplate;
                BindingOperations.ClearBinding(_popupContent, ContentControl.ContentProperty);
                _contentBinding = _popupContent.SetBinding(ContentControl.ContentProperty, new Binding());
                _contentBinding.UpdateTarget();
            }
            else
            {
                _popupContent.ContentTemplate = PopupTemplate;
                BindingOperations.ClearBinding(_popupContent, ContentControl.ContentProperty);
                _contentBinding = _popupContent.SetBinding(ContentControl.ContentProperty, new Binding());
                _contentBinding.UpdateTarget();
            }
            if(oldTemplate != _popupContent.ContentTemplate)
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    _popup.InvalidateMeasure();
                    _popup.InvalidateArrange();
                    _popup.InvalidateVisual();
                }, DispatcherPriority.ApplicationIdle);
            }
        }
    }
}
