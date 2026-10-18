using Godot;
using System;
using System.Collections.Generic;

namespace Golic.Presentation;

public partial class ComponentPalette : CanvasLayer
{
	public static event Action<string>? SpawnRequested;
	public static event Action<string, Vector2>? SpawnAtPositionRequested;
	public static event Action? ClearAllRequested;
	public static event Action<bool>? SimulationToggled;
	public static event Action? CenterCameraRequested;
	public static event Action<bool>? SelectModeToggled;

	// Sahneden gelen referanslar (% Unique Node Name)
	private PanelContainer _leftPanel = null!;
	private Button _leftToggleBtn = null!;
	private HBoxContainer _categoryHBox = null!;
	private GridContainer _itemsContainer = null!;
	private bool _leftOpen = false;

	private PanelContainer _rightPanel = null!;
	private Button _rightToggleBtn = null!;
	private Button _selectModeBtn = null!;
	private Button _pauseBtn = null!;
	private Button _centerBtn = null!;
	private Button _clearBtn = null!;
	private bool _rightOpen = false;

	// Sürükle-Bırak (Drag & Drop) Hayalet Önizleme
	private bool _isDraggingItem = false;
	private string? _draggingType = null;
	private PanelContainer? _ghostPreview = null;
	private MiniSymbolPreview? _ghostSymbol = null;

	private readonly Dictionary<string, string[]> _categories = new()
	{
		{ "I/O", new[] { "SWITCH", "LED" } },
		{ "LOGIC", new[] { "AND", "OR", "NOT", "XOR", "NAND", "NOR" } }
	};

	private bool _isSimPaused = false;
	private bool _isSelectModeActive = false;

	public override void _Ready()
	{
		_leftPanel = GetNode<PanelContainer>("LeftPanel");
		_leftToggleBtn = GetNode<Button>("%LeftToggleBtn");
		_categoryHBox = GetNode<HBoxContainer>("%CategoryHBox");
		_itemsContainer = GetNode<GridContainer>("%ItemsContainer");

		_rightPanel = GetNode<PanelContainer>("RightPanel");
		_rightToggleBtn = GetNode<Button>("%RightToggleBtn");
		_selectModeBtn = GetNode<Button>("%SelectModeBtn");
		_pauseBtn = GetNode<Button>("%PauseBtn");
		_centerBtn = GetNode<Button>("%CenterBtn");
		_clearBtn = GetNode<Button>("%ClearBtn");

		// Sürükleme önizleme kutusu (Objenin birebir görünümü, yarı şeffaf)
		_ghostPreview = new PanelContainer
		{
			CustomMinimumSize = new Vector2(80, 80),
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Modulate = new Color(1f, 1f, 1f, 0.75f),
			ZIndex = 200
		};
		AddChild(_ghostPreview);

		SetupCategories();

		_leftToggleBtn.Pressed += ToggleLeftPanel;
		_rightToggleBtn.Pressed += ToggleRightPanel;

		_selectModeBtn.Pressed += OnSelectModePressed;
		_pauseBtn.Pressed += OnPausePressed;
		_centerBtn.Pressed += () => CenterCameraRequested?.Invoke();
		_clearBtn.Pressed += () => ClearAllRequested?.Invoke();

		UpdateLeftPanelPos();
		UpdateRightPanelPos();

		ShowCategory("I/O");
	}

	private void SetupCategories()
	{
		foreach (Node child in _categoryHBox.GetChildren())
		{
			child.QueueFree();
		}

		foreach (var cat in _categories.Keys)
		{
			var catBtn = new Button
			{
				Text = cat,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			string currentCat = cat;
			catBtn.Pressed += () => ShowCategory(currentCat);
			_categoryHBox.AddChild(catBtn);
		}
	}

	private void ShowCategory(string categoryName)
	{
		foreach (Node child in _itemsContainer.GetChildren())
		{
			child.QueueFree();
		}

		if (_categories.TryGetValue(categoryName, out var items))
		{
			foreach (var item in items)
			{
				var card = CreateComponentCard(item);
				_itemsContainer.AddChild(card);
			}
		}
	}

	/// <summary>
	/// Dikdörtgen bileşen kartı (Üstte 1:1 şematik resim, altta isim)
	/// </summary>
	private Control CreateComponentCard(string compType)
	{
		var btn = new Button
		{
			CustomMinimumSize = new Vector2(86, 104),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};

		var vbox = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		vbox.AddThemeConstantOverride("separation", 6);
		btn.AddChild(vbox);

		// 1. 1:1 Orantılı Şematik Resim Alanı
		var preview = new MiniSymbolPreview(compType)
		{
			CustomMinimumSize = new Vector2(56, 56),
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		vbox.AddChild(preview);

		// 2. Alt Kısım: İsim etiketi
		var nameLabel = new Label
		{
			Text = compType,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		nameLabel.AddThemeFontSizeOverride("font_size", 12);
		nameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.9f, 0.95f));
		vbox.AddChild(nameLabel);

		// Butona basıldığında sürükleme başlasın
		string currentType = compType;
		btn.ButtonDown += () => StartDragging(currentType);

		return btn;
	}

	private void StartDragging(string compType)
	{
		_isDraggingItem = true;
		_draggingType = compType;

		if (_ghostPreview != null)
		{
			// Varsa önceki sembolü temizle
			foreach (Node c in _ghostPreview.GetChildren()) c.QueueFree();

			// Hayaletin içine seçilen objenin birebir şematik sembolünü yerleştir
			_ghostSymbol = new MiniSymbolPreview(compType)
			{
				CustomMinimumSize = new Vector2(96, 96),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			_ghostPreview.AddChild(_ghostSymbol);

			_ghostPreview.Visible = true;
			_ghostPreview.Position = GetViewport().GetMousePosition() - new Vector2(48, 48);
		}
	}

	/// <summary>
	/// _Input en üst düzey giriş katmanıdır, butonlar sürüklemeyi yutamaz!
	/// </summary>
	public override void _Input(InputEvent @event)
	{
		if (!_isDraggingItem) return;

		if (@event is InputEventMouseMotion mm)
		{
			if (_ghostPreview != null)
			{
				_ghostPreview.Position = mm.Position - new Vector2(48, 48);
			}
		}
		else if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
		{
			// Sol tık bırakıldı!
			_isDraggingItem = false;
			if (_ghostPreview != null) _ghostPreview.Visible = false;

			// Eğer sol panelin dışındaki boş alana bırakıldıysa spawn et
			Rect2 leftRect = _leftPanel.GetGlobalRect();
			if (!leftRect.HasPoint(mb.Position) && _draggingType != null)
			{
				var camera = GetViewport().GetCamera2D();
				Vector2 worldPos = mb.Position;
				if (camera != null)
				{
					// Farenin merkezine göre kapının sol üst köşesini hesapla
					worldPos = camera.GetGlobalMousePosition() - new Vector2(48, 48);
				}
				SpawnAtPositionRequested?.Invoke(_draggingType, worldPos);
			}

			_draggingType = null;
		}
	}

	private void ToggleLeftPanel()
	{
		_leftOpen = !_leftOpen;
		UpdateLeftPanelPos();
	}

	private void UpdateLeftPanelPos()
	{
		_leftPanel.Visible = _leftOpen;
		_leftToggleBtn.Text = _leftOpen ? "◀" : "▶";
		float xPos = _leftOpen ? 212 : 8;
		_leftToggleBtn.Position = new Vector2(xPos, 24);
	}

	private void ToggleRightPanel()
	{
		_rightOpen = !_rightOpen;
		UpdateRightPanelPos();
	}

	private void UpdateRightPanelPos()
	{
		_rightPanel.Visible = _rightOpen;
		_rightToggleBtn.Text = _rightOpen ? "▶" : "◀";

		var viewportWidth = GetViewport().GetVisibleRect().Size.X;
		float xPos = _rightOpen ? viewportWidth - 186 : viewportWidth - 38;
		_rightToggleBtn.Position = new Vector2(xPos, 24);
	}

	private void OnSelectModePressed()
	{
		_isSelectModeActive = !_isSelectModeActive;
		_selectModeBtn.Text = _isSelectModeActive ? "✓ SELECT ON" : "⬚ SELECT";
		SelectModeToggled?.Invoke(_isSelectModeActive);
	}

	private void OnPausePressed()
	{
		_isSimPaused = !_isSimPaused;
		_pauseBtn.Text = _isSimPaused ? "▶ PLAY" : "⏸ PAUSE";
		SimulationToggled?.Invoke(!_isSimPaused);
	}
}

/// <summary>
/// Menüdeki kartların ortasında kapının gerçek evrensel şematik sembolünü çizen kontrol
/// </summary>
public partial class MiniSymbolPreview : Control
{
	private readonly string _compType;
	private readonly Color _lineColor = Colors.White; // Referans resmindeki gibi net beyaz hatlar
	private readonly Color _fillColor = new(0.18f, 0.22f, 0.28f);

	public MiniSymbolPreview(string compType)
	{
		_compType = compType;
	}

	public override void _Draw()
	{
		float w = Size.X;
		float h = Size.Y;
		float midX = w * 0.5f;
		float midY = h * 0.5f;
		float stroke = 2.0f;

		// Sağdaki GateView gibi tam 1:1 kare en-boy oranı (42x42 alan içine tam orantılı)
		float span = 20f; 
		float topY = midY - span;
		float botY = midY + span;
		float lx = midX - span;
		float rx = midX + span;

		switch (_compType)
		{
			case "LED":
			{
				Vector2 c = new(midX, midY);
				float r = span;
				DrawCircle(c, r, _fillColor);
				DrawArc(c, r, 0, Mathf.Tau, 32, _lineColor, stroke);

				float o = r * 0.7071f;
				DrawLine(new Vector2(c.X - o, c.Y - o), new Vector2(c.X + o, c.Y + o), _lineColor, stroke);
				DrawLine(new Vector2(c.X - o, c.Y + o), new Vector2(c.X + o, c.Y - o), _lineColor, stroke);
				break;
			}

			case "SWITCH":
			{
				float leftTerm = lx + 4f;
				float rightTerm = rx - 4f;
				DrawCircle(new Vector2(leftTerm, midY), 3.5f, _lineColor);
				DrawCircle(new Vector2(rightTerm, midY), 3.5f, _lineColor);

				// Açık kontak kolu
				Vector2 armEnd = new(leftTerm + (rightTerm - leftTerm) * 0.95f, midY - span * 0.75f);
				DrawLine(new Vector2(leftTerm, midY), armEnd, _lineColor, stroke + 0.5f);
				break;
			}

			case "NOT":
			{
				float bodyRx = rx - 8f;
				Vector2[] tri = { new(lx, topY), new(lx, botY), new(bodyRx, midY) };
				DrawColoredPolygon(tri, _fillColor);
				DrawPolyline(new[] { tri[0], tri[1], tri[2], tri[0] }, _lineColor, stroke);

				// Invert Bubble
				DrawCircle(new Vector2(bodyRx + 4f, midY), 4f, _fillColor);
				DrawArc(new Vector2(bodyRx + 4f, midY), 4f, 0, Mathf.Tau, 20, _lineColor, stroke);
				break;
			}

			case "AND":
			case "NAND":
			{
				bool hasBubble = _compType == "NAND";
				float bodyRx = hasBubble ? rx - 8f : rx;
				float arcX = lx + (bodyRx - lx) * 0.45f;

				var pts = new List<Vector2> { new(lx, topY), new(arcX, topY) };
				for (int i = 0; i <= 16; i++)
				{
					float angle = -Mathf.Pi / 2f + (Mathf.Pi * i / 16f);
					pts.Add(new Vector2(arcX + Mathf.Cos(angle) * (bodyRx - arcX), midY + Mathf.Sin(angle) * span));
				}
				pts.Add(new Vector2(arcX, botY));
				pts.Add(new Vector2(lx, botY));

				DrawColoredPolygon(pts.ToArray(), _fillColor);
				pts.Add(pts[0]);
				DrawPolyline(pts.ToArray(), _lineColor, stroke);

				if (hasBubble)
				{
					DrawCircle(new Vector2(bodyRx + 4f, midY), 4f, _fillColor);
					DrawArc(new Vector2(bodyRx + 4f, midY), 4f, 0, Mathf.Tau, 20, _lineColor, stroke);
				}
				break;
			}

			case "OR":
			case "NOR":
			case "XOR":
			{
				bool hasBubble = _compType == "NOR";
				float bodyRx = hasBubble ? rx - 8f : rx;
				float orLx = _compType == "XOR" ? lx + 4f : lx;

				var pts = new List<Vector2>();
				for (int i = 0; i <= 16; i++)
				{
					float t = i / 16f;
					Vector2 p0 = new(orLx, topY);
					Vector2 p1 = new(bodyRx * 0.7f, topY + span * 0.2f);
					Vector2 p2 = new(bodyRx, midY);
					pts.Add(p0.Lerp(p1, t).Lerp(p1.Lerp(p2, t), t));
				}
				for (int i = 0; i <= 16; i++)
				{
					float t = i / 16f;
					Vector2 p0 = new(bodyRx, midY);
					Vector2 p1 = new(bodyRx * 0.7f, botY - span * 0.2f);
					Vector2 p2 = new(orLx, botY);
					pts.Add(p0.Lerp(p1, t).Lerp(p1.Lerp(p2, t), t));
				}
				for (int i = 0; i <= 16; i++)
				{
					float t = i / 16f;
					Vector2 p0 = new(orLx, botY);
					Vector2 p1 = new(orLx + (bodyRx - orLx) * 0.35f, midY);
					Vector2 p2 = new(orLx, topY);
					pts.Add(p0.Lerp(p1, t).Lerp(p1.Lerp(p2, t), t));
				}

				DrawColoredPolygon(pts.ToArray(), _fillColor);
				pts.Add(pts[0]);
				DrawPolyline(pts.ToArray(), _lineColor, stroke);

				if (_compType == "XOR")
				{
					var backPts = new List<Vector2>();
					for (int i = 0; i <= 16; i++)
					{
						float t = i / 16f;
						Vector2 p0 = new(lx, topY);
						Vector2 p1 = new(lx + (bodyRx - orLx) * 0.35f, midY);
						Vector2 p2 = new(lx, botY);
						backPts.Add(p0.Lerp(p1, t).Lerp(p1.Lerp(p2, t), t));
					}
					DrawPolyline(backPts.ToArray(), _lineColor, stroke);
				}

				if (hasBubble)
				{
					DrawCircle(new Vector2(bodyRx + 4f, midY), 4f, _fillColor);
					DrawArc(new Vector2(bodyRx + 4f, midY), 4f, 0, Mathf.Tau, 20, _lineColor, stroke);
				}
				break;
			}

			default:
				DrawRect(new Rect2(lx, topY, span * 2f, span * 2f), _lineColor, false, stroke);
				break;
		}
	}
}
