using Godot;

namespace Golic.Presentation;

public partial class CameraController : Camera2D
{
	[Export] public float MinZoom = 0.2f;
	[Export] public float MaxZoom = 3.0f;
	[Export] public float ZoomFactor = 0.15f;

	private bool _isPanning = false;

	public override void _UnhandledInput(InputEvent @event)
	{
		// Yalnızca Orta Fare Tuşu (Tekerlek) ile Pan
		if (@event is InputEventMouseButton mouseBtn)
		{
			if (mouseBtn.ButtonIndex == MouseButton.Middle)
			{
				_isPanning = mouseBtn.Pressed;
			}
			else if (mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.WheelUp)
			{
				ApplyZoom(1.0f + ZoomFactor, mouseBtn.Position);
			}
			else if (mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.WheelDown)
			{
				ApplyZoom(1.0f - ZoomFactor, mouseBtn.Position);
			}
		}
		else if (@event is InputEventMouseMotion mouseMotion && _isPanning)
		{
			Position -= mouseMotion.Relative / Zoom.X;
		}
	}

	private void ApplyZoom(float factor, Vector2 screenPos)
	{
		Vector2 oldZoom = Zoom;
		Vector2 newZoom = (oldZoom * factor).Clamp(new Vector2(MinZoom, MinZoom), new Vector2(MaxZoom, MaxZoom));

		if (oldZoom == newZoom) return;

		Vector2 mouseWorldBefore = GetGlobalMousePosition();
		Zoom = newZoom;
		Vector2 mouseWorldAfter = GetGlobalMousePosition();

		Position += (mouseWorldBefore - mouseWorldAfter);
	}
}
