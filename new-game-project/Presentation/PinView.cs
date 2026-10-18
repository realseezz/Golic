using Godot;
using Golic.Core;
using System;

namespace Golic.Presentation;

public partial class PinView : Node2D
{
	[Export] public float Radius = 7f;
	[Export] public Color LowColor = new(0.35f, 0.35f, 0.4f);
	[Export] public Color HighColor = new(0.1f, 0.95f, 0.25f);
	[Export] public Color OutlineColor = Colors.White;
	[Export] public Color SelectedColor = new(0.2f, 0.8f, 1.0f);

	public Pin LogicPin { get; private set; } = null!;
	public bool IsSelected { get; set; } = false;

	public static event Action<PinView>? PinTapped;

	public static void TriggerPinTap(PinView pin)
	{
		PinTapped?.Invoke(pin);
	}

	public void Initialize(Pin pin)
	{
		LogicPin = pin;
	}

	public override void _Process(double delta)
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (LogicPin == null) return;

		Color fillColor = LogicPin.State == LogicState.High ? HighColor : LowColor;
		DrawCircle(Vector2.Zero, Radius, fillColor);

		if (IsSelected)
		{
			// Seçili pinin etrafında parlak mavi halka
			DrawArc(Vector2.Zero, Radius + 4f, 0, Mathf.Tau, 32, SelectedColor, 2.5f);
		}
		else
		{
			DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 24, OutlineColor, 1.5f);
		}
	}
}
