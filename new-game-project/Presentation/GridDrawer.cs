using Godot;

namespace Golic.Presentation;

public partial class GridDrawer : Node2D
{
    [Export] public int GridSize = 32;
    [Export] public Color LineColor = new(0.2f, 0.22f, 0.28f, 0.6f);
    [Export] public Color AxisColor = new(0.35f, 0.38f, 0.45f, 0.8f);

    public override void _Process(double delta)
    {
        // Kamera her hareket ettiğinde ızgarayı yeniden çiz
        QueueRedraw();
    }

    public override void _Draw()
    {
        var viewport = GetViewportRect();
        var canvasTransform = GetCanvasTransform();

        // Ekranın gördüğü dünya koordinat sınırları
        Vector2 topLeft = -canvasTransform.Origin / canvasTransform.Scale;
        Vector2 bottomRight = topLeft + (viewport.Size / canvasTransform.Scale);

        float startX = Mathf.Floor(topLeft.X / GridSize) * GridSize;
        float endX = Mathf.Ceil(bottomRight.X / GridSize) * GridSize;
        float startY = Mathf.Floor(topLeft.Y / GridSize) * GridSize;
        float endY = Mathf.Ceil(bottomRight.Y / GridSize) * GridSize;

        // Dikey çizgiler
        for (float x = startX; x <= endX; x += GridSize)
        {
            Color col = Mathf.IsZeroApprox(x) ? AxisColor : LineColor;
            DrawLine(new Vector2(x, topLeft.Y), new Vector2(x, bottomRight.Y), col, 1.0f);
        }

        // Yatay çizgiler
        for (float y = startY; y <= endY; y += GridSize)
        {
            Color col = Mathf.IsZeroApprox(y) ? AxisColor : LineColor;
            DrawLine(new Vector2(topLeft.X, y), new Vector2(bottomRight.X, y), col, 1.0f);
        }
    }
}
