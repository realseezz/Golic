using Godot;
using Golic.Core;

namespace Golic.Presentation;

public partial class WireView : Line2D
{
    public PinView SourcePin { get; private set; } = null!;
    public PinView TargetPin { get; private set; } = null!;

    [Export] public Color LowColor = new(0.35f, 0.4f, 0.5f, 0.8f);
    [Export] public Color HighColor = new(0.1f, 0.95f, 0.25f, 1.0f);
    [Export] public Color DeletePendingColor = new(1.0f, 0.2f, 0.2f, 1.0f);
    [Export] public Color SelectedColor = new(1.0f, 0.65f, 0.15f, 1.0f); // Seçiliyken altın/turuncu
    [Export] public float WireWidth = 3.5f;

    public bool IsDeletePending { get; set; } = false;
    public bool IsSelected { get; set; } = false;
    public CircuitNet? Net { get; set; }

    // Üst üste binen kabloların ayrılması için eğrilik/offset değeri
    public float CurveOffset { get; set; } = 0f;

    public void Setup(PinView source, PinView target)
    {
        SourcePin = source;
        TargetPin = target;

        Width = WireWidth;
        DefaultColor = LowColor;
        JointMode = LineJointMode.Round;
        BeginCapMode = LineCapMode.Round;
        EndCapMode = LineCapMode.Round;
        ZIndex = -1;
    }

    public override void _Process(double delta)
    {
        if (SourcePin == null || TargetPin == null) return;

        Vector2 p0 = SourcePin.GlobalPosition;
        Vector2 p2 = TargetPin.GlobalPosition;

        ClearPoints();

        // Ortogonal (Manhattan) Rotalama:
        // p0 -> pMid1 -> pMid2 -> p2
        // Çıkış pini genellikle sağda (X pozitif), giriş pini solda.
        // Aynı koridordan geçen kablolar CurveOffset kadar ötelenerek paralel gider.
        
        float midX = (p0.X + p2.X) * 0.5f + CurveOffset;
        
        // Eğer hedef solda kalıyorsa (geri dönüş hattı) engeli aşmak için Y ekseninde de bükülme
        if (p2.X < p0.X + 20f)
        {
            float detourY = Mathf.Max(p0.Y, p2.Y) + 32f + Mathf.Abs(CurveOffset);
            AddPoint(p0);
            AddPoint(new Vector2(p0.X + 16f, p0.Y));
            AddPoint(new Vector2(p0.X + 16f, detourY));
            AddPoint(new Vector2(p2.X - 16f, detourY));
            AddPoint(new Vector2(p2.X - 16f, p2.Y));
            AddPoint(p2);
        }
        else
        {
            // Standart ileri yönlü S-kablo (Resimdeki gibi)
            AddPoint(p0);
            AddPoint(new Vector2(midX, p0.Y));
            AddPoint(new Vector2(midX, p2.Y));
            AddPoint(p2);
        }

        if (IsDeletePending)
        {
            DefaultColor = DeletePendingColor;
        }
        else if (IsSelected)
        {
            DefaultColor = SelectedColor;
        }
        else
        {
            // Kendi output pininin değerine göre renk (High = Parlak Yeşil, Low = Koyu Gri)
            Pin? outPin = SourcePin.LogicPin.Type == PinType.Output ? SourcePin.LogicPin
                        : TargetPin.LogicPin.Type == PinType.Output ? TargetPin.LogicPin
                        : null;
            DefaultColor = (outPin != null && outPin.State == LogicState.High) ? HighColor : LowColor;
        }

    }

    public bool IsNearPoint(Vector2 worldPos, float threshold = 22f)
    {
        if (SourcePin == null || TargetPin == null) return false;

        // Line üzerindeki segmentleri kontrol et
        int pointCount = GetPointCount();
        if (pointCount < 2) return false;

        for (int i = 0; i < pointCount - 1; i++)
        {
            Vector2 a = GetPointPosition(i);
            Vector2 b = GetPointPosition(i + 1);

            float lengthSq = a.DistanceSquaredTo(b);
            if (Mathf.IsZeroApprox(lengthSq))
            {
                if (a.DistanceTo(worldPos) <= threshold) return true;
                continue;
            }

            float t = Mathf.Clamp((worldPos - a).Dot(b - a) / lengthSq, 0f, 1f);
            Vector2 proj = a + t * (b - a);
            if (proj.DistanceTo(worldPos) <= threshold) return true;
        }

        return false;
    }
}
