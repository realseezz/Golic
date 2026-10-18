using Godot;
using Golic.Core;
using Golic.Core.Gates;
using System.Collections.Generic;

namespace Golic.Presentation;

public partial class GateView : Node2D
{
	[Export] public int GridSize = 32;
	[Export] public Vector2I GridDimensions = new(3, 3); 
	[Export] public Color BodyColor = new(0.14f, 0.16f, 0.21f); // Koyu şematik arka plan
	[Export] public Color BorderColor = new(0.42f, 0.48f, 0.58f); // Standart şematik hat rengi
	[Export] public Color SelectedColor = new(1.0f, 0.65f, 0.15f); // Seçiliyken altın/turuncu
	[Export] public Color DeletePromptColor = new(1.0f, 0.2f, 0.2f); // Silme uyarısında kırmızı

	public Vector2 BodyPixelSize => new(GridDimensions.X * GridSize, GridDimensions.Y * GridSize);

	public CircuitComponent Component { get; private set; } = null!;
	public string DisplayName { get; set; } = "GATE";

	public readonly List<PinView> InputPinViews = new();
	public readonly List<PinView> OutputPinViews = new();

	public bool IsSelected { get; set; } = false;
	public bool IsDeletePending { get; set; } = false;

	public void Setup(CircuitComponent component, string displayName)
	{
		Component = component;
		DisplayName = displayName;

		CreatePins();
		SnapToGrid();
	}

	private void CreatePins()
	{
		float inSpacing = BodyPixelSize.Y / (Component.Inputs.Count + 1);
		for (int i = 0; i < Component.Inputs.Count; i++)
		{
			var pinView = new PinView();
			pinView.Initialize(Component.Inputs[i]);
			pinView.Position = new Vector2(0, inSpacing * (i + 1));
			AddChild(pinView);
			InputPinViews.Add(pinView);
		}

		float outSpacing = BodyPixelSize.Y / (Component.Outputs.Count + 1);
		for (int i = 0; i < Component.Outputs.Count; i++)
		{
			var pinView = new PinView();
			pinView.Initialize(Component.Outputs[i]);
			pinView.Position = new Vector2(BodyPixelSize.X, outSpacing * (i + 1));
			AddChild(pinView);
			OutputPinViews.Add(pinView);
		}
	}

	public override void _Process(double delta)
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		var rect = new Rect2(Vector2.Zero, BodyPixelSize);
		Color currentBorder = BorderColor;
		float borderWidth = 2.0f;

		if (IsDeletePending)
		{
			currentBorder = DeletePromptColor;
			borderWidth = 3.5f;
		}
		else if (IsSelected)
		{
			currentBorder = SelectedColor;
			borderWidth = 3.0f;
		}

		// 1. LAMBA / GÖSTERGE (Evrensel Şematik Sembol: Daire İçinde Çarpı 'X')
		if (Component is LedComponent led)
		{
			DrawLampSymbol(rect, led.IsLit, currentBorder, borderWidth);
			return;
		}

		// 2. ANAHTAR / SWITCH (Evrensel Şematik Sembol: Terminal Kontaklı SPST Anahtar)
		if (Component is SwitchComponent sw)
		{
			DrawSwitchSymbol(rect, sw.IsOn, currentBorder, borderWidth);
			return;
		}

		// 3. EVRENSEL STANDART KAPILAR (IEEE Std 91 / ANSI)
		DrawAnsiSymbol(DisplayName, rect, BodyColor, currentBorder, borderWidth);
	}

	/// <summary>
	/// Evrensel Lamba Sembolü: Daire içinde tam 'X'.
	/// Sinyal 1 olduğunda parlayan yeşil zemin ve parlak beyaz X çizgileri.
	/// </summary>
	private void DrawLampSymbol(Rect2 bounds, bool isLit, Color outlineColor, float strokeWidth)
	{
		// Arka plan panel çerçevesi (hafif şeffaf)
		DrawRect(bounds, new Color(0.12f, 0.14f, 0.18f, 0.85f), filled: true);
		DrawRect(bounds, outlineColor, filled: false, width: strokeWidth);

		Vector2 center = bounds.Size * 0.5f;
		float radius = Mathf.Min(bounds.Size.X, bounds.Size.Y) * 0.32f;

		// Lamba zemin rengi
		Color lampFill = isLit ? new Color(0.12f, 0.85f, 0.28f, 0.95f) : new Color(0.18f, 0.2f, 0.25f);
		DrawCircle(center, radius, lampFill);

		// Dış çember
		Color circleBorder = isLit ? Colors.White : outlineColor;
		DrawArc(center, radius, 0, Mathf.Tau, 36, circleBorder, strokeWidth + (isLit ? 1f : 0f));

		// Daire içindeki tam 'X' (45 derece çapraz çizgiler)
		float offset = radius * 0.7071f; // cos(45) * r
		Color crossColor = isLit ? new Color(1.0f, 1.0f, 1.0f, 0.95f) : new Color(0.4f, 0.45f, 0.52f);
		float crossWidth = strokeWidth + (isLit ? 0.8f : 0f);

		DrawLine(new Vector2(center.X - offset, center.Y - offset), new Vector2(center.X + offset, center.Y + offset), crossColor, crossWidth);
		DrawLine(new Vector2(center.X - offset, center.Y + offset), new Vector2(center.X + offset, center.Y - offset), crossColor, crossWidth);

		// Yanıyorsa dışarıya ekstra ışık ışıma halkası
		if (isLit)
		{
			DrawArc(center, radius + 5f, 0, Mathf.Tau, 36, new Color(0.2f, 1.0f, 0.4f, 0.45f), 2.0f);
		}

		// Sol pin terminal çizgisi
		DrawLine(new Vector2(0, center.Y), new Vector2(center.X - radius, center.Y), outlineColor, strokeWidth);
	}

	/// <summary>
	/// Evrensel SPST Anahtar Sembolü:
	/// İki terminal noktası. Açıkken (0) kol havada, kapalıyken (1) düz iletim hattı.
	/// </summary>
	private void DrawSwitchSymbol(Rect2 bounds, bool isOn, Color outlineColor, float strokeWidth)
	{
		DrawRect(bounds, new Color(0.12f, 0.14f, 0.18f, 0.85f), filled: true);
		DrawRect(bounds, outlineColor, filled: false, width: strokeWidth);

		float midY = bounds.Size.Y * 0.5f;
		float leftTerminalX = bounds.Size.X * 0.28f;
		float rightTerminalX = bounds.Size.X * 0.72f;
		float termRadius = 4f;

		// Terminallere gelen dış bağlantı telleri
		DrawLine(new Vector2(0, midY), new Vector2(leftTerminalX, midY), outlineColor, strokeWidth);
		DrawLine(new Vector2(rightTerminalX, midY), new Vector2(bounds.Size.X, midY), outlineColor, strokeWidth);

		// Sol ve Sağ terminal halkaları
		DrawCircle(new Vector2(leftTerminalX, midY), termRadius, BodyColor);
		DrawArc(new Vector2(leftTerminalX, midY), termRadius, 0, Mathf.Tau, 16, outlineColor, strokeWidth);

		DrawCircle(new Vector2(rightTerminalX, midY), termRadius, BodyColor);
		DrawArc(new Vector2(rightTerminalX, midY), termRadius, 0, Mathf.Tau, 16, outlineColor, strokeWidth);

		// Kontak Kolu (Switch Blade)
		if (isOn)
		{
			// Kapalı kontak: Terminalleri birbirine bağlayan düz yeşil hat
			Color closedColor = new Color(0.2f, 0.95f, 0.35f);
			DrawLine(new Vector2(leftTerminalX, midY), new Vector2(rightTerminalX, midY), closedColor, strokeWidth + 1.5f);
		}
		else
		{
			// Açık kontak: Havaya kalkmış 35 derece diyagonal kol
			Vector2 armStart = new(leftTerminalX, midY);
			float armLen = (rightTerminalX - leftTerminalX) * 1.05f;
			float angle = -Mathf.DegToRad(32f);
			Vector2 armEnd = armStart + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * armLen;

			DrawLine(armStart, armEnd, outlineColor, strokeWidth + 1f);
		}

		// Durum yazısı (ON / OFF)
		var font = ThemeDB.FallbackFont;
		string label = isOn ? "ON" : "OFF";
		Color lblCol = isOn ? new Color(0.2f, 0.95f, 0.35f) : new Color(0.6f, 0.65f, 0.72f);
		Vector2 strSize = font.GetStringSize(label, HorizontalAlignment.Center, -1, 13);
		DrawString(font, new Vector2((bounds.Size.X - strSize.X) * 0.5f, bounds.Size.Y - 8f), label, HorizontalAlignment.Center, -1, 13, lblCol);
	}

	/// <summary>
	/// IEEE Std 91 / ANSI Mantık Kapısı Sembolleri
	/// </summary>
	private void DrawAnsiSymbol(string gateType, Rect2 bounds, Color fillColor, Color outlineColor, float strokeWidth)
	{
		float w = bounds.Size.X;
		float h = bounds.Size.Y;
		float pad = 8f;
		float bubbleRadius = 5f;

		bool hasBubble = gateType is "NOT" or "NAND" or "NOR" or "XNOR";
		float mainRightX = hasBubble ? w - pad - bubbleRadius * 2f : w - pad;
		float leftX = pad + 4f;
		float topY = pad;
		float botY = h - pad;
		float midY = h * 0.5f;

		switch (gateType)
		{
			case "NOT":
			{
				// Inverter Üçgeni + Bubble
				Vector2[] triangle = {
					new(leftX, topY),
					new(leftX, botY),
					new(mainRightX, midY)
				};
				DrawColoredPolygon(triangle, fillColor);
				DrawPolyline(new[] { triangle[0], triangle[1], triangle[2], triangle[0] }, outlineColor, strokeWidth);

				// Invert Bubble
				Vector2 bubbleCenter = new(mainRightX + bubbleRadius, midY);
				DrawCircle(bubbleCenter, bubbleRadius, fillColor);
				DrawArc(bubbleCenter, bubbleRadius, 0, Mathf.Tau, 24, outlineColor, strokeWidth);

				// Çıkış pini uzantı teli
				DrawLine(new Vector2(bubbleCenter.X + bubbleRadius, midY), new Vector2(w, midY), outlineColor, strokeWidth);
				break;
			}

			case "BUFFER":
			{
				// Sade Üçgen (Sürücü)
				Vector2[] triangle = {
					new(leftX, topY),
					new(leftX, botY),
					new(w - pad, midY)
				};
				DrawColoredPolygon(triangle, fillColor);
				DrawPolyline(new[] { triangle[0], triangle[1], triangle[2], triangle[0] }, outlineColor, strokeWidth);
				break;
			}

			case "AND":
			case "NAND":
			{
				// D-Şeklinde: Düz sol kenar, paralel üst ve alt yatay kenar, sağ yarım daire kubbe
				float arcStartX = leftX + (mainRightX - leftX) * 0.45f;
				var points = new List<Vector2>
				{
					new(leftX, topY),
					new(arcStartX, topY)
				};

				int arcSteps = 16;
				for (int i = 0; i <= arcSteps; i++)
				{
					float angle = -Mathf.Pi / 2f + (Mathf.Pi * i / arcSteps);
					float rx = (mainRightX - arcStartX);
					float ry = (botY - topY) * 0.5f;
					points.Add(new Vector2(arcStartX + Mathf.Cos(angle) * rx, midY + Mathf.Sin(angle) * ry));
				}

				points.Add(new Vector2(arcStartX, botY));
				points.Add(new Vector2(leftX, botY));

				DrawColoredPolygon(points.ToArray(), fillColor);
				points.Add(points[0]);
				DrawPolyline(points.ToArray(), outlineColor, strokeWidth);

				if (hasBubble)
				{
					Vector2 bubbleCenter = new(mainRightX + bubbleRadius, midY);
					DrawCircle(bubbleCenter, bubbleRadius, fillColor);
					DrawArc(bubbleCenter, bubbleRadius, 0, Mathf.Tau, 24, outlineColor, strokeWidth);
					DrawLine(new Vector2(bubbleCenter.X + bubbleRadius, midY), new Vector2(w, midY), outlineColor, strokeWidth);
				}
				break;
			}

			case "OR":
			case "NOR":
			{
				// Hilal / Yay şeklinde içe kavisli sırt ve sivri ok ucu
				var points = new List<Vector2>();
				int steps = 16;

				// Üst eğri
				for (int i = 0; i <= steps; i++)
				{
					float t = i / (float)steps;
					Vector2 p0 = new(leftX, topY);
					Vector2 p1 = new(mainRightX * 0.7f, topY + (midY - topY) * 0.2f);
					Vector2 p2 = new(mainRightX, midY);
					points.Add(p0.Lerp(p1, t).Lerp(p1.Lerp(p2, t), t));
				}

				// Alt eğri
				for (int i = 0; i <= steps; i++)
				{
					float t = i / (float)steps;
					Vector2 p0 = new(mainRightX, midY);
					Vector2 p1 = new(mainRightX * 0.7f, botY - (botY - midY) * 0.2f);
					Vector2 p2 = new(leftX, botY);
					points.Add(p0.Lerp(p1, t).Lerp(p1.Lerp(p2, t), t));
				}

				// Sol iç yay kavis
				for (int i = 0; i <= steps; i++)
				{
					float t = i / (float)steps;
					Vector2 p0 = new(leftX, botY);
					Vector2 p1 = new(leftX + (mainRightX - leftX) * 0.35f, midY);
					Vector2 p2 = new(leftX, topY);
					points.Add(p0.Lerp(p1, t).Lerp(p1.Lerp(p2, t), t));
				}

				DrawColoredPolygon(points.ToArray(), fillColor);
				points.Add(points[0]);
				DrawPolyline(points.ToArray(), outlineColor, strokeWidth);

				if (hasBubble)
				{
					Vector2 bubbleCenter = new(mainRightX + bubbleRadius, midY);
					DrawCircle(bubbleCenter, bubbleRadius, fillColor);
					DrawArc(bubbleCenter, bubbleRadius, 0, Mathf.Tau, 24, outlineColor, strokeWidth);
					DrawLine(new Vector2(bubbleCenter.X + bubbleRadius, midY), new Vector2(w, midY), outlineColor, strokeWidth);
				}
				break;
			}

			case "XOR":
			case "XNOR":
			{
				float xorShift = 8f;
				var points = new List<Vector2>();
				int steps = 16;

				float xorLeft = leftX + xorShift;
				for (int i = 0; i <= steps; i++)
				{
					float t = i / (float)steps;
					Vector2 p0 = new(xorLeft, topY);
					Vector2 p1 = new(mainRightX * 0.7f, topY + (midY - topY) * 0.2f);
					Vector2 p2 = new(mainRightX, midY);
					points.Add(p0.Lerp(p1, t).Lerp(p1.Lerp(p2, t), t));
				}

				for (int i = 0; i <= steps; i++)
				{
					float t = i / (float)steps;
					Vector2 p0 = new(mainRightX, midY);
					Vector2 p1 = new(mainRightX * 0.7f, botY - (botY - midY) * 0.2f);
					Vector2 p2 = new(xorLeft, botY);
					points.Add(p0.Lerp(p1, t).Lerp(p1.Lerp(p2, t), t));
				}

				for (int i = 0; i <= steps; i++)
				{
					float t = i / (float)steps;
					Vector2 p0 = new(xorLeft, botY);
					Vector2 p1 = new(xorLeft + (mainRightX - xorLeft) * 0.35f, midY);
					Vector2 p2 = new(xorLeft, topY);
					points.Add(p0.Lerp(p1, t).Lerp(p1.Lerp(p2, t), t));
				}

				DrawColoredPolygon(points.ToArray(), fillColor);
				points.Add(points[0]);
				DrawPolyline(points.ToArray(), outlineColor, strokeWidth);

				// XOR ekstra arka yay hattı
				var backArc = new List<Vector2>();
				for (int i = 0; i <= steps; i++)
				{
					float t = i / (float)steps;
					Vector2 p0 = new(leftX, topY);
					Vector2 p1 = new(leftX + (mainRightX - xorLeft) * 0.35f, midY);
					Vector2 p2 = new(leftX, botY);
					backArc.Add(p0.Lerp(p1, t).Lerp(p1.Lerp(p2, t), t));
				}
				DrawPolyline(backArc.ToArray(), outlineColor, strokeWidth);

				if (hasBubble)
				{
					Vector2 bubbleCenter = new(mainRightX + bubbleRadius, midY);
					DrawCircle(bubbleCenter, bubbleRadius, fillColor);
					DrawArc(bubbleCenter, bubbleRadius, 0, Mathf.Tau, 24, outlineColor, strokeWidth);
					DrawLine(new Vector2(bubbleCenter.X + bubbleRadius, midY), new Vector2(w, midY), outlineColor, strokeWidth);
				}
				break;
			}

			case "CLOCK":
			{
				// Saat Jeneratörü / Osilatör (Kutu içinde kare dalga)
				DrawRect(bounds, fillColor, filled: true);
				DrawRect(bounds, outlineColor, filled: false, width: strokeWidth);

				float sqH = h * 0.25f;
				float sqW = w * 0.18f;
				Vector2 pA = new(midY - sqW * 1.5f, midY + sqH * 0.5f);
				Vector2 pB = new(pA.X, midY - sqH * 0.5f);
				Vector2 pC = new(pB.X + sqW, pB.Y);
				Vector2 pD = new(pC.X, pA.Y);
				Vector2 pE = new(pD.X + sqW, pD.Y);
				Vector2 pF = new(pE.X, pB.Y);

				DrawPolyline(new[] { pA, pB, pC, pD, pE, pF }, outlineColor, strokeWidth + 0.5f);
				break;
			}

			default:
				DrawRect(bounds, fillColor, filled: true);
				DrawRect(bounds, outlineColor, filled: false, width: strokeWidth);
				DrawLabel(gateType);
				break;
		}
	}

	private void DrawLabel(string text, Color? textColor = null)
	{
		var font = ThemeDB.FallbackFont;
		int fontSize = 16;
		Vector2 textSize = font.GetStringSize(text, HorizontalAlignment.Center, -1, fontSize);
		Vector2 textPos = new Vector2((BodyPixelSize.X - textSize.X) / 2f, (BodyPixelSize.Y + textSize.Y * 0.7f) / 2f);
		DrawString(font, textPos, text, HorizontalAlignment.Center, -1, fontSize, textColor ?? Colors.White);
	}

	public PinView? GetTappedPin(Vector2 worldPos, float threshold = 18f)
	{
		PinView? closest = null;
		float minDst = threshold;

		foreach (var pin in InputPinViews)
		{
			float dst = pin.GlobalPosition.DistanceTo(worldPos);
			if (dst <= minDst)
			{
				minDst = dst;
				closest = pin;
			}
		}
		foreach (var pin in OutputPinViews)
		{
			float dst = pin.GlobalPosition.DistanceTo(worldPos);
			if (dst <= minDst)
			{
				minDst = dst;
				closest = pin;
			}
		}
		return closest;
	}

	public bool IsInsideBody(Vector2 worldPos)
	{
		Rect2 bodyRect = new(GlobalPosition + new Vector2(4, 0), new Vector2(BodyPixelSize.X - 8, BodyPixelSize.Y));
		return bodyRect.HasPoint(worldPos);
	}

	public Rect2 GetGlobalRect()
	{
		return new Rect2(GlobalPosition, BodyPixelSize);
	}

	public void OnBodyClicked()
	{
		if (Component is SwitchComponent sw)
		{
			sw.Toggle();
		}
	}

	public void SnapToGrid()
	{
		float x = Mathf.Round(Position.X / GridSize) * GridSize;
		float y = Mathf.Round(Position.Y / GridSize) * GridSize;
		Position = new Vector2(x, y);
	}
}
