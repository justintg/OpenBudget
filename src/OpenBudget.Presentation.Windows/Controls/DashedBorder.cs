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
            SnapsToDevicePixels = true;
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
                dashedBorder._dashedPen = new Pen();
                dashedBorder._dashedPen.DashStyle = new DashStyle(new[] { 1.0, 2.0 }, 0.0);
                dashedBorder._dashedPen.DashCap = PenLineCap.Square;
                dashedBorder._dashedPen.Brush = borderBrush;
                dashedBorder._dashedPen.Thickness = 1.0;
                dashedBorder._dashedPen.Freeze();
            }
        }

        private Pen _dashedPen = null;

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (_dashedPen == null) return;

            if (BorderThickness.Top > 0.0)
                dc.DrawLine(_dashedPen, new Point(0.0, 1.0), new Point(base.ActualWidth, 1.0));


            if (BorderThickness.Right > 0.0)
                dc.DrawLine(_dashedPen, new Point(base.ActualWidth, 1.0), new Point(base.ActualWidth, base.ActualHeight));

            if (BorderThickness.Bottom > 0.0)
                dc.DrawLine(_dashedPen, new Point(1.0, ActualHeight), new Point(base.ActualWidth, base.ActualHeight));

            if (BorderThickness.Left > 0.0)
                dc.DrawLine(_dashedPen, new Point(1.0, ActualHeight), new Point(0.0, 1.0));

        }
    }
}
