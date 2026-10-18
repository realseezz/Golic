using Godot;
using Golic.Core;
using System.Collections.Generic;

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
        ZIndex = 0;
    }

    public List<Vector2> GetBaseWaypoints()
    {
        List<Vector2> baseWaypoints = new();
        if (SourcePin == null || TargetPin == null) return baseWaypoints;

        Vector2 p0 = SourcePin.GlobalPosition;
        Vector2 p2 = TargetPin.GlobalPosition;

        if (p2.X < p0.X + 20f)
        {
            float detourY = Mathf.Max(p0.Y, p2.Y) + 36f;
            baseWaypoints.Add(p0);
            baseWaypoints.Add(new Vector2(p0.X + 16f, p0.Y));
            baseWaypoints.Add(new Vector2(p0.X + 16f, detourY));
            baseWaypoints.Add(new Vector2(p2.X - 16f, detourY));
            baseWaypoints.Add(new Vector2(p2.X - 16f, p2.Y));
            baseWaypoints.Add(p2);
        }
        else
        {
            float midX = (p0.X + p2.X) * 0.5f;
            baseWaypoints.Add(p0);
            baseWaypoints.Add(new Vector2(midX, p0.Y));
            baseWaypoints.Add(new Vector2(midX, p2.Y));
            baseWaypoints.Add(p2);
        }
        return baseWaypoints;
    }

    public override void _Process(double delta)
    {
        if (SourcePin == null || TargetPin == null) return;

        ClearPoints();

        // 1. Temel Ortogonal Hat köşe noktaları
        List<Vector2> baseWaypoints = GetBaseWaypoints();

        // 2. Kesişen kabloların üzerinden köprü atlama (Hop-over bridge)
        var controller = GetParent() as WiringController;
        List<Vector2> finalPoints = new();

        for (int i = 0; i < baseWaypoints.Count - 1; i++)
        {
            Vector2 segStart = baseWaypoints[i];
            Vector2 segEnd = baseWaypoints[i + 1];

            bool isHorizontal = Mathf.Abs(segStart.Y - segEnd.Y) < 1f;

            if (isHorizontal && controller != null)
            {
                float minX = Mathf.Min(segStart.X, segEnd.X);
                float maxX = Mathf.Max(segStart.X, segEnd.X);
                float y = segStart.Y;

                // Bu yatay segmente rastlayan yabancı kablo dikey segmentlerini bul
                List<float> hopsX = new();
                foreach (var other in controller.Wires)
                {
                    if (other == this) continue;
                    // Eğer aynı pini paylaşıyorlarsa bu elektriksel birleşmedir, köprü atlamaz
                    if (other.SourcePin == SourcePin || other.SourcePin == TargetPin ||
                        other.TargetPin == SourcePin || other.TargetPin == TargetPin)
                        continue;

                    var otherWaypoints = other.GetBaseWaypoints();
                    for (int j = 0; j < otherWaypoints.Count - 1; j++)
                    {
                        Vector2 o1 = otherWaypoints[j];
                        Vector2 o2 = otherWaypoints[j + 1];

                        // Diğer segment dikey mi?
                        if (Mathf.Abs(o1.X - o2.X) < 1f)
                        {
                            float ox = o1.X;
                            float oMinY = Mathf.Min(o1.Y, o2.Y);
                            float oMaxY = Mathf.Max(o1.Y, o2.Y);

                            if (ox > minX + 8f && ox < maxX - 8f && y >= oMinY && y <= oMaxY)
                            {
                                hopsX.Add(ox);
                            }
                        }
                    }
                }

                if (hopsX.Count > 0)
                {
                    // Soldan sağa veya sağdan sola yönüne göre sırala
                    if (segStart.X < segEnd.X)
                        hopsX.Sort();
                    else
                        hopsX.Sort((a, b) => b.CompareTo(a));

                    Vector2 currentPos = segStart;
                    finalPoints.Add(currentPos);

                    float hopRadius = 10f; // Daha belirgin şematik köprü
                    foreach (float hx in hopsX)
                    {
                        float dir = (segEnd.X >= segStart.X) ? 1f : -1f;
                        Vector2 beforeHop = new(hx - dir * hopRadius, y);
                        finalPoints.Add(beforeHop);

                        // Yarım daire kavis (köprü yukarıya doğru belirgin kabarır)
                        int arcSegments = 10;
                        for (int k = 1; k < arcSegments; k++)
                        {
                            float angle = Mathf.Pi - (Mathf.Pi * k / arcSegments); // Pi'den 0'a
                            float arcX = hx - Mathf.Cos(angle) * hopRadius * dir;
                            float arcY = y - Mathf.Sin(angle) * (hopRadius * 1.15f);
                            finalPoints.Add(new Vector2(arcX, arcY));
                        }

                        Vector2 afterHop = new(hx + dir * hopRadius, y);
                        finalPoints.Add(afterHop);
                        currentPos = afterHop;
                    }
                    continue; // Segment sonu bir sonraki adımda eklenecek
                }
            }

            finalPoints.Add(segStart);
        }
        finalPoints.Add(baseWaypoints[^1]);

        foreach (var pt in finalPoints)
        {
            AddPoint(pt);
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
