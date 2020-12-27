using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenBudget.Presentation.Windows.Controls
{
    public class DashedBorder : Decorator
    {
        static DashedBorder()
        {

        }
        public DashedBorder()
        {
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
            SnapsToDevicePixels = false;
            UseLayoutRounding = true;
        }

        public Thickness BorderThickness
        {
            get { return (Thickness)GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register("BorderThickness", typeof(Thickness), typeof(DashedBorder), new PropertyMetadata(new Thickness(1.0)));

        public Brush BorderBrush
        {
            get { return (Brush)GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register("BorderBrush", typeof(Brush), typeof(DashedBorder), new PropertyMetadata(Brushes.Black, OnBorderBrushChanged));

        private static void OnBorderBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DashedBorder dashedBorder && e.NewValue is Brush borderBrush)
            {
                double dpiFactor = dashedBorder.GetDpiFactor();
                dashedBorder.UpdatePen(dpiFactor);
            }
        }

        private Pen _dashedPen = null;
        private double _dpiFactor;

        private double GetDpiFactor()
        {
            var source = PresentationSource.FromVisual(this);
            double dpiFactor = 1.0;
            if (source != null)
            {
                var matrix = source.CompositionTarget.TransformToDevice;
                dpiFactor = 1 / matrix.M11;
            }

            return dpiFactor;
        }

        private void UpdatePen(double dpiFactor)
        {
            _dpiFactor = dpiFactor;

            _dashedPen = new Pen();
            _dashedPen.DashStyle = new DashStyle(new[] { 1.0, 2.0 }, 0.0);
            _dashedPen.DashCap = PenLineCap.Square;
            _dashedPen.Brush = BorderBrush;
            _dashedPen.Thickness = dpiFactor;
            _dashedPen.Freeze();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double dpiFactor = GetDpiFactor();
            if (_dashedPen == null || dpiFactor != _dpiFactor)
            {
                UpdatePen(dpiFactor);
            }

            double halfThickness = _dashedPen.Thickness / 2;

            if (BorderThickness.Top > 0.0)
                dc.DrawLine(_dashedPen, new Point(0.0, halfThickness), new Point(base.ActualWidth, halfThickness));

            if (BorderThickness.Right > 0.0)
                dc.DrawLine(_dashedPen, new Point(base.ActualWidth - halfThickness, 1.0), new Point(base.ActualWidth - halfThickness, base.ActualHeight));

            if (BorderThickness.Bottom > 0.0)
                dc.DrawLine(_dashedPen, new Point(0.0, ActualHeight - halfThickness), new Point(base.ActualWidth, ActualHeight - halfThickness));

            if (BorderThickness.Left > 0.0)
                dc.DrawLine(_dashedPen, new Point(halfThickness, ActualHeight), new Point(halfThickness, 0.0));

        }
    }
}
