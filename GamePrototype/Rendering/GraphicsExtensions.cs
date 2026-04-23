using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace RunnerGame.Rendering
{
    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF bounds, float radius)
        {
            using var path = CreateRoundedRectanglePath(bounds, radius);
            graphics.FillPath(brush, path);
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, RectangleF bounds, float radius)
        {
            using var path = CreateRoundedRectanglePath(bounds, radius);
            graphics.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedRectanglePath(RectangleF bounds, float radius)
        {
            float clampedRadius = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2f);
            float diameter = clampedRadius * 2f;
            var path = new GraphicsPath();

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
