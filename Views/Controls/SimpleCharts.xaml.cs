using System.Collections.Generic;
using Microsoft.Maui.Graphics;

namespace Coftea_Capstone.Views.Controls;

public partial class SimpleCharts : ContentView
{
    public SimpleCharts()
    {
        InitializeComponent();
    }
}

public class PieSlice
{
    public string Label { get; set; }
    public float Value { get; set; }
    public Color Color { get; set; }
}

public class SimplePieChartDrawable : IDrawable
{
    public IList<PieSlice> Slices { get; set; } = new List<PieSlice>();

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Slices == null || Slices.Count == 0) return;
        canvas.Antialias = true;
        float total = 0f;
        foreach (var s in Slices) total += s.Value;
        if (total <= 0) return;

        var center = new PointF(dirtyRect.Center.X, dirtyRect.Center.Y);
        float radius = MathF.Min(dirtyRect.Width, dirtyRect.Height) * 0.4f;
        float start = -90f;
        foreach (var s in Slices)
        {
            var sweep = (s.Value / total) * 360f;
            canvas.FillColor = s.Color;
			var path = CreatePieSlicePath(center, radius, start, start + sweep, 48);
			canvas.FillPath(path);
            start += sweep;
        }
    }

	private static PathF CreatePieSlicePath(PointF center, float radius, float startAngleDeg, float endAngleDeg, int segments)
	{
		var path = new PathF();
		path.MoveTo(center);

		float startRad = DegreesToRadians(startAngleDeg);
		float endRad = DegreesToRadians(endAngleDeg);
		if (endRad < startRad)
		{
			endRad += MathF.Tau; // ensure positive sweep
		}
		float sweepRad = endRad - startRad;
		int steps = Math.Max(1, segments);
		for (int i = 0; i <= steps; i++)
		{
			float t = (float)i / steps;
			float angle = startRad + sweepRad * t;
			float x = center.X + radius * MathF.Cos(angle);
			float y = center.Y + radius * MathF.Sin(angle);
			if (i == 0)
			{
				path.LineTo(x, y);
			}
			else
			{
				path.LineTo(x, y);
			}
		}
		path.Close();
		return path;
	}

	private static float DegreesToRadians(float degrees)
	{
		return degrees * (MathF.PI / 180f);
	}
}

public class BarItem
{
    public string Label { get; set; }
    public float Value { get; set; }
    public Color Color { get; set; }
}

public class SimpleBarChartDrawable : IDrawable
{
    public IList<BarItem> Items { get; set; } = new List<BarItem>();

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Items == null || Items.Count == 0) return;
        canvas.Antialias = true;
        float max = 0f;
        foreach (var i in Items) if (i.Value > max) max = i.Value;
        if (max <= 0) return;

        float padding = 20f;
        float availableWidth = dirtyRect.Width - padding * 2;
        float barWidth = availableWidth / (Items.Count * 2);
        float x = dirtyRect.Left + padding;
        float baseY = dirtyRect.Bottom - padding;
        float heightAvail = dirtyRect.Height - padding * 2;
        foreach (var i in Items)
        {
            float h = (i.Value / max) * heightAvail;
            canvas.FillColor = i.Color;
            canvas.FillRectangle(x, baseY - h, barWidth, h);
            x += barWidth * 2;
        }
    }
}


