using MahApps.Metro.Controls;
using OpenBudget.Presentation.Windows.Controls.TransactionGrid;
using OpenBudget.Presentation.Windows.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace OpenBudget.Presentation.Windows.Controls
{

    public enum PopupOpenPreference
    {
        Bottom,
        Top
    }

    public enum PopupOpenPosition
    {
        Bottom,
        Top
    }

    public interface IPopupPositionCallback
    {
        void PopupPositionChanged(PopupOpenPosition position);
    }

    public class PopupAdorner : Adorner
    {
        private VisualCollection _visuals;
        private FrameworkElement _content;

        /// <summary>
        /// Creates a new popup adorner with the specified content.
        /// </summary>
        /// <param name="adornedElement">The UIElement that will be adorned</param>
        /// <param name="content">The content that will be display inside the popup</param>
        /// <param name="offset">The popup position in regards to the adorned element</param>
        public PopupAdorner(UIElement adornedElement, UIElement content)
            : base(adornedElement)
        {
            _visuals = new VisualCollection(this);
            if (content is FrameworkElement visual)
            {
                _content = visual;
                _visuals.Add(visual);
            }
            else
            {
                var presenter = new ContentPresenter();
                _content = presenter;
                presenter.Content = content;
                _visuals.Add(_content);
            }
            AddLogicalChild(content);
            //AddVisualChild(content);
        }

        public PopupAdorner(UIElement adornedElement, UIElement content, IPopupPositionCallback positionCallback)
            : this(adornedElement, content)
        {
            _positionCallback = positionCallback;
        }

        private IPopupPositionCallback _positionCallback;

        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            private set { SetValue(IsOpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(PopupAdorner), new PropertyMetadata(false));

        protected override Size MeasureOverride(Size constraint)
        {
            _content.Measure(constraint);
            return _content.DesiredSize;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            double yPos = 0;
            double xPos = 0;
            if (ShouldOpenOnTop())
            {
                OpenPosition = PopupOpenPosition.Top;
                yPos = -(_content.DesiredSize.Height);
            }
            else
            {
                OpenPosition = PopupOpenPosition.Bottom;
                var adornedElement = AdornedElement as FrameworkElement;
                yPos = adornedElement.ActualHeight;
            }
            //yPos += _content.Margin.Top;
            //xPos += _content.Margin.Left;
            _content.Arrange(new Rect(xPos, yPos, finalSize.Width, finalSize.Height));
            return finalSize;
        }

        private bool ShouldOpenOnTop()
        {
            FrameworkElement container = AdornedElement.FindParent<VirtualizingStackPanel>();
            if (container == null)
            {
                container = AdornedElement.FindParent<Window>();
            }

            if (container != null)
            {
                Point relativeLocation = AdornedElement.TranslatePoint(new Point(0, 0), container);
                if (_openPreference == PopupOpenPreference.Top)
                {
                    //double heightAbove = container.ActualHeight - (relativeLocation.Y + (AdornedElement as FrameworkElement).ActualHeight);
                    double heightAbove = relativeLocation.Y;
                    if (heightAbove >= _content.DesiredSize.Height)
                        return true;
                    else
                        return false;
                }
                else if (_openPreference == PopupOpenPreference.Bottom)
                {
                    double heightBelow = container.ActualHeight - (relativeLocation.Y + (AdornedElement as FrameworkElement).ActualHeight);
                    if (heightBelow >= _content.DesiredSize.Height)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            return true;
        }

        protected override Visual GetVisualChild(int index)
        {
            return _visuals[index];
        }
        protected override int VisualChildrenCount
        {
            get
            {
                return _visuals.Count;
            }
        }

        private PopupOpenPreference _openPreference = PopupOpenPreference.Top;
        private AdornerLayer _adornerLayer = null;

        private AdornerLayer FindAdornerLayer()
        {
            //var window = Window.GetWindow(AdornedElement);
            //return window.FindChild<AdornerLayer>();

            ScrollViewer parent = AdornedElement.FindParent<ScrollViewer>();
            if (parent != null)
            {
                return AdornerLayer.GetAdornerLayer(parent);
            }

            return AdornerLayer.GetAdornerLayer(AdornedElement);
        }

        /// <summary>
        /// Brings the popup into view.
        /// </summary>
        public void Show(PopupOpenPreference openPreference = PopupOpenPreference.Top)
        {
            if (!IsOpen)
            {
                _openPreference = openPreference;
                if (_adornerLayer == null)
                {
                    _adornerLayer = FindAdornerLayer();
                }
                _adornerLayer.Add(this);
                IsOpen = true;
            }
        }
        /// <summary>
        /// Removes the popup into view.
        /// </summary>
        public void Hide()
        {
            if (IsOpen)
            {
                _adornerLayer.Remove(this);
                IsOpen = false;
            }
        }

        public PopupOpenPosition OpenPosition
        {
            get { return (PopupOpenPosition)GetValue(OpenPositionProperty); }
            set { SetValue(OpenPositionProperty, value); }
        }

        public static readonly DependencyProperty OpenPositionProperty =
            DependencyProperty.Register("OpenPosition", typeof(PopupOpenPosition), typeof(PopupAdorner), new PropertyMetadata(PopupOpenPosition.Bottom, OnOpenPositionChanged));

        private static void OnOpenPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PopupAdorner popupAdorner && e.NewValue is PopupOpenPosition openPosition)
            {
                popupAdorner._positionCallback?.PopupPositionChanged(openPosition);
            }
        }
    }
}
