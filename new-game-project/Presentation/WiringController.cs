using Godot;
using Golic.Core;
using System.Collections.Generic;

namespace Golic.Presentation;

public partial class WiringController : Node2D
{
	[Export] public Color PreviewWireColor = new(0.2f, 0.8f, 1.0f, 0.7f);
	[Export] public float WireWidth = 3.0f;

	private Line2D _previewLine = null!;
	private PinView? _selectedPin = null;

	private readonly List<WireView> _wires = new();
	public IReadOnlyList<WireView> Wires => _wires;

	public override void _Ready()
	{
		ZIndex = 5;
		_previewLine = new Line2D
		{
			Width = WireWidth,
			DefaultColor = PreviewWireColor,
			JointMode = Line2D.LineJointMode.Round,
			BeginCapMode = Line2D.LineCapMode.Round,
			EndCapMode = Line2D.LineCapMode.Round,
			ZIndex = 10,
			Visible = false
		};
		AddChild(_previewLine);
	}

	public override void _Process(double delta)
	{
		if (_selectedPin != null && _previewLine.Visible)
		{
			_previewLine.SetPointPosition(0, _selectedPin.GlobalPosition);
			_previewLine.SetPointPosition(1, GetGlobalMousePosition());
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		// Şematik Elektriksel Bağlantı Noktaları (Junction Dots •):
		// 1) Ortak pin bağlantıları
		Dictionary<PinView, int> pinWireCount = new();
		foreach (var wire in _wires)
		{
			if (wire.SourcePin != null)
			{
				pinWireCount.TryGetValue(wire.SourcePin, out int sc);
				pinWireCount[wire.SourcePin] = sc + 1;
			}
			if (wire.TargetPin != null)
			{
				pinWireCount.TryGetValue(wire.TargetPin, out int tc);
				pinWireCount[wire.TargetPin] = tc + 1;
			}
		}

		foreach (var (pin, count) in pinWireCount)
		{
			if (count > 1)
			{
				Vector2 localPos = ToLocal(pin.GlobalPosition);
				Color dotColor = pin.LogicPin.State == LogicState.High ? new Color(0.1f, 0.95f, 0.25f) : new Color(0.35f, 0.4f, 0.5f);
				DrawCircle(localPos, 5f, dotColor);
				DrawArc(localPos, 5f, 0, Mathf.Tau, 16, Colors.White, 1.2f);
			}
		}

		// 2) Kabloların hat üzerindeki T-Kavşakları (Aynı hedef pine veya kaynağa giden hatların birleşme noktaları)
		List<Vector2> junctionPoints = new();
		for (int i = 0; i < _wires.Count; i++)
		{
			var w1 = _wires[i];
			var pts1 = w1.GetBaseWaypoints();

			for (int j = i + 1; j < _wires.Count; j++)
			{
				var w2 = _wires[j];
				// Sadece elektriksel olarak bağlı (ortak pini olan) kablolar junction oluşturur
				bool sharesPin = w1.SourcePin == w2.SourcePin || w1.SourcePin == w2.TargetPin ||
								 w1.TargetPin == w2.SourcePin || w1.TargetPin == w2.TargetPin;
				if (!sharesPin) continue;

				var pts2 = w2.GetBaseWaypoints();

				// w1'in köşe noktalarından biri w2'nin bir segmentinin üzerindeyse (T-junction)
				foreach (var p in pts1)
				{
					if (IsPointOnSegments(p, pts2, out Vector2 jPt))
					{
						if (!IsNearAnyPin(jPt))
							junctionPoints.Add(jPt);
					}
				}

				// w2'nin köşe noktalarından biri w1'in bir segmentinin üzerindeyse
				foreach (var p in pts2)
				{
					if (IsPointOnSegments(p, pts1, out Vector2 jPt))
					{
						if (!IsNearAnyPin(jPt))
							junctionPoints.Add(jPt);
					}
				}
			}
		}

		// Noktaları tekilleştirip çiz
		List<Vector2> uniqueJunctions = new();
		foreach (var jp in junctionPoints)
		{
			bool alreadyDrawn = false;
			foreach (var u in uniqueJunctions)
			{
				if (u.DistanceTo(jp) < 4f)
				{
					alreadyDrawn = true;
					break;
				}
			}
			if (!alreadyDrawn)
			{
				uniqueJunctions.Add(jp);
				Vector2 localPos = ToLocal(jp);
				DrawCircle(localPos, 5f, Colors.White);
				DrawCircle(localPos, 3.5f, new Color(0.12f, 0.14f, 0.18f));
				DrawCircle(localPos, 2.5f, Colors.White);
			}
		}
	}

	private bool IsPointOnSegments(Vector2 pt, List<Vector2> waypoints, out Vector2 matchedPt)
	{
		matchedPt = pt;
		for (int i = 0; i < waypoints.Count - 1; i++)
		{
			Vector2 a = waypoints[i];
			Vector2 b = waypoints[i + 1];

			float minX = Mathf.Min(a.X, b.X) - 1f;
			float maxX = Mathf.Max(a.X, b.X) + 1f;
			float minY = Mathf.Min(a.Y, b.Y) - 1f;
			float maxY = Mathf.Max(a.Y, b.Y) + 1f;

			// Nokta bu segmentin içinde ve hizasında mı?
			if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
			{
				// Yatay segment üzerinde mi?
				if (Mathf.Abs(a.Y - b.Y) < 1.5f && Mathf.Abs(pt.Y - a.Y) < 2f)
				{
					matchedPt = new Vector2(pt.X, a.Y);
					return true;
				}
				// Dikey segment üzerinde mi?
				if (Mathf.Abs(a.X - b.X) < 1.5f && Mathf.Abs(pt.X - a.X) < 2f)
				{
					matchedPt = new Vector2(a.X, pt.Y);
					return true;
				}
			}
		}
		return false;
	}

	private bool IsNearAnyPin(Vector2 pt, float threshold = 12f)
	{
		foreach (var w in _wires)
		{
			if (w.SourcePin != null && w.SourcePin.GlobalPosition.DistanceTo(pt) < threshold) return true;
			if (w.TargetPin != null && w.TargetPin.GlobalPosition.DistanceTo(pt) < threshold) return true;
		}
		return false;
	}

	public void HandlePinTap(PinView pinView)
	{
		if (_selectedPin == null)
		{
			SelectPin(pinView);
		}
		else if (_selectedPin == pinView)
		{
			DeselectPin();
		}
		else
		{
			ConnectPins(_selectedPin, pinView);
			DeselectPin();
		}
	}

	public WireView? GetWireAt(Vector2 worldPos, float threshold = 22f)
	{
		foreach (var wire in _wires)
		{
			if (wire.IsNearPoint(worldPos, threshold))
			{
				return wire;
			}
		}
		return null;
	}

	public void RemoveAllWiresConnectedTo(GateView gate)
	{
		var toRemove = new List<WireView>();
		foreach (var wire in _wires)
		{
			if (gate.InputPinViews.Contains(wire.SourcePin) || gate.InputPinViews.Contains(wire.TargetPin) ||
				gate.OutputPinViews.Contains(wire.SourcePin) || gate.OutputPinViews.Contains(wire.TargetPin))
			{
				toRemove.Add(wire);
			}
		}

		foreach (var wire in toRemove)
		{
			DeleteWire(wire);
		}
	}

	public void DeselectPin()
	{
		if (_selectedPin != null)
		{
			_selectedPin.IsSelected = false;
			_selectedPin = null;
		}
		_previewLine.Visible = false;
	}

	private void SelectPin(PinView pinView)
	{
		_selectedPin = pinView;
		_selectedPin.IsSelected = true;

		_previewLine.ClearPoints();
		_previewLine.AddPoint(_selectedPin.GlobalPosition);
		_previewLine.AddPoint(GetGlobalMousePosition());
		_previewLine.Visible = true;
	}

	private void ConnectPins(PinView a, PinView b)
	{
		if (a.LogicPin.Owner == b.LogicPin.Owner) return;
		if (a.LogicPin.Type == b.LogicPin.Type) return;

		// Aynı iki pin arasında zaten bağlantı varsa yeniden oluşturma
		foreach (var existing in _wires)
		{
			if ((existing.SourcePin == a && existing.TargetPin == b) ||
				(existing.SourcePin == b && existing.TargetPin == a))
				return;
		}



		var manager = GetTree().CurrentScene as CircuitManager ?? GetParent<CircuitManager>();
		if (manager == null) return;

		// Her yeni kablo bağlantısı için ayrı, bağımsız bir hat (Net) oluşturulur.
		// Böylece bir input'a bağlanan farklı kaynaklar birbirinin net'ine karışıp kısa devre yapmaz.
		var net = new CircuitNet($"NET_{System.Guid.NewGuid().ToString()[..6]}");
		manager.Simulator.RegisterNet(net);

		net.AddPin(a.LogicPin);
		net.AddPin(b.LogicPin);

		var wireView = new WireView();
		wireView.Setup(a, b);
		wireView.Net = net;

		AddChild(wireView);
		_wires.Add(wireView);
	}

	public void DeleteWire(WireView wire)
	{
		var manager = GetTree().CurrentScene as CircuitManager ?? GetParent<CircuitManager>();
		var pinA = wire.SourcePin.LogicPin;
		var pinB = wire.TargetPin.LogicPin;

		var net = wire.Net ?? pinA.ConnectedNet ?? pinB.ConnectedNet;
		if (net != null)
		{
			net.RemovePin(pinA);
			net.RemovePin(pinB);

			if (pinA.Type == PinType.Input && pinA.ConnectedNets.Count == 0) pinA.State = LogicState.Low;
			if (pinB.Type == PinType.Input && pinB.ConnectedNets.Count == 0) pinB.State = LogicState.Low;

			if (manager != null)
			{
				manager.Simulator.UnregisterNet(net);
			}
		}

		_wires.Remove(wire);
		wire.QueueFree();
	}
}
