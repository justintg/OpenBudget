﻿using OpenBudget.Presentation.Windows.Controls.TransactionGrid;
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
    public class PopupAdorner : Adorner
    {
        private VisualCollection _visuals;
        private ContentPresenter _presenter;

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
            _presenter = new ContentPresenter();
            _visuals.Add(_presenter);
            _presenter.Content = content;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _presenter.Measure(constraint);
            return _presenter.DesiredSize;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            double yPos = 0;
            if (ShouldOpenOnTop())
            {
                yPos = -(_presenter.DesiredSize.Height);
            }
            else
            {
                yPos = AdornedElement.DesiredSize.Height;
            }
            // -(_presenter.DesiredSize.Height);
            _presenter.Arrange(new Rect(0, yPos, finalSize.Width, finalSize.Height));
            return _presenter.RenderSize;
        }

        private bool ShouldOpenOnTop()
        {
            FrameworkElement container = AdornedElement.FindParent<VirtualizingStackPanel>();
            if (container == null)
                return true;

            Point relativeLocation = AdornedElement.TranslatePoint(new Point(0, 0), container);
            double remainingHeight = container.ActualHeight - (relativeLocation.Y + (AdornedElement as FrameworkElement).ActualHeight);
            if (remainingHeight < _presenter.DesiredSize.Height)
                return true;
            else
                return false;
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

        /// <summary>
        /// Brings the popup into view.
        /// </summary>
        public void Show()
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(AdornedElement);
            adornerLayer.Add(this);
        }
        /// <summary>
        /// Removes the popup into view.
        /// </summary>
        public void Hide()
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(AdornedElement);
            adornerLayer.Remove(this);
        }
    }
}
