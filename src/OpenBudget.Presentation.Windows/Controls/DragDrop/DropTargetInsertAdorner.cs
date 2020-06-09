/*
 * This code has been adapted from GongSolutions.WPF.DragDrop
 * Original source can be found here: https://github.com/punker76/gong-wpf-dragdrop
 */
using MahApps.Metro.Controls;
using OpenBudget.Presentation.Windows.Controls.BudgetEditor;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace OpenBudget.Presentation.Windows.Controls.DragDrop
{
    internal class DropTargetInsertAdorner : Adorner
    {
        static DropTargetInsertAdorner()
        {
            // Create the pen and triangle in a static constructor and freeze them to improve performance.
            const int triangleSize = 5;

            var firstLine = new LineSegment(new Point(0, -triangleSize), false);
            firstLine.Freeze();
            var secondLine = new LineSegment(new Point(0, triangleSize), false);
            secondLine.Freeze();

            var figure = new PathFigure { StartPoint = new Point(triangleSize, 0) };
            figure.Segments.Add(firstLine);
            figure.Segments.Add(secondLine);
            figure.Freeze();

            m_Triangle = new PathGeometry();
            m_Triangle.Figures.Add(figure);
            m_Triangle.Freeze();
        }

        public DropTargetInsertAdorner(UIElement adornedElement, AdornerLayer adornerLayer, bool insertBelow) : base(adornedElement)
        {
            this.IsHitTestVisible = false;
            this.AllowDrop = false;
            this.SnapsToDevicePixels = true;
            this.m_AdornerLayer = adornerLayer;
            this.m_AdornerLayer.Add(this);
            InsertBelow = insertBelow;
        }

        public Pen Pen { get; set; } = new Pen(Brushes.Gray, 2);
        public bool InsertBelow { get; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var size = AdornedElement.RenderSize;

            Point point1, point2;
            double rotation = 0;

            if (!InsertBelow)
            {
                point1 = new Point(0, 0);
                point2 = new Point(AdornedElement.RenderSize.Width, 0);
            }
            else
            {
                point1 = new Point(0, size.Height);
                point2 = new Point(AdornedElement.RenderSize.Width, size.Height);
            }

            drawingContext.DrawLine(this.Pen, point1, point2);
            this.DrawTriangle(drawingContext, point1, rotation);
            this.DrawTriangle(drawingContext, point2, 180 + rotation);
        }

        private void DrawTriangle(DrawingContext drawingContext, Point origin, double rotation)
        {
            drawingContext.PushTransform(new TranslateTransform(origin.X, origin.Y));
            drawingContext.PushTransform(new RotateTransform(rotation));

            drawingContext.DrawGeometry(this.Pen.Brush, null, m_Triangle);

            drawingContext.Pop();
            drawingContext.Pop();
        }

        public void Detach()
        {
            this.m_AdornerLayer.Remove(this);
        }

        private readonly AdornerLayer m_AdornerLayer;
        private static readonly PathGeometry m_Triangle;
    }
}
