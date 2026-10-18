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

		// Çakışan kablo kontrolü:
		// 1) Ortak pin paylaşımı → aynı pine gelen/giden kablolar her zaman orada üst üste gelir
		// 2) Yolun %25, %50, %75 noktasında yakınlık → paralel/kesişen kablolar
		Vector2 aPos = a.GlobalPosition;
		Vector2 bPos = b.GlobalPosition;
		int overlapIndex = 0;
		foreach (var existing in _wires)
		{
			bool sharesPin = existing.SourcePin == a || existing.SourcePin == b ||
							 existing.TargetPin == a || existing.TargetPin == b;
			bool pathClose = false;
			for (int s = 1; s <= 3 && !pathClose; s++)
			{
				Vector2 sample = aPos.Lerp(bPos, s / 4f);
				if (existing.IsNearPoint(sample, 22f))
					pathClose = true;
			}
			if (sharesPin || pathClose)
				overlapIndex++;
		}

		if (overlapIndex > 0)
		{
			float sign = (overlapIndex % 2 == 1) ? 1f : -1f;
			float magnitude = Mathf.Ceil(overlapIndex / 2f) * 28f;
			wireView.CurveOffset = sign * magnitude;
		}

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
