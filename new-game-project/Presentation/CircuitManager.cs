using Godot;
using Golic.Core;
using Golic.Core.Gates;
using System.Collections.Generic;

namespace Golic.Presentation;

public partial class CircuitManager : Node2D
{
	public CircuitSimulator Simulator { get; } = new();

	[Export] public double TickInterval = 0.05;
	private double _tickTimer = 0.0;
	private bool _isSimulationActive = true;

	private readonly List<GateView> _gateViews = new();
	private readonly List<GateView> _selectedGates = new();
	private readonly List<WireView> _selectedWires = new();
	private readonly List<GateView> _pendingDeleteGates = new();
	private readonly List<WireView> _pendingDeleteWires = new();
	private WireView? _pendingDeleteWire = null;

	public WiringController Wiring { get; private set; } = null!;
	public ComponentPalette Palette { get; private set; } = null!;

	// Sürükleme
	private GateView? _leadDraggedGate = null;
	private Vector2 _gateDragOffset = Vector2.Zero;
	private Vector2 _dragStartPos = Vector2.Zero;
	private readonly Dictionary<GateView, Vector2> _multiDragStartPositions = new();

	// Box Selection
	private bool _isSelectModeActive = false;
	private bool _isBoxSelecting = false;
	private Vector2 _boxSelectStart = Vector2.Zero;
	private Vector2 _boxSelectEnd = Vector2.Zero;

	// Onay Penceresi (Confirmation Dialog)
	private ConfirmationDialog _confirmDialog = null!;

	private int _componentCounter = 1;

	public override void _Ready()
	{
		Wiring = new WiringController();
		AddChild(Wiring);

		var paletteScene = GD.Load<PackedScene>("res://Presentation/ComponentPalette.tscn");
		Palette = paletteScene.Instantiate<ComponentPalette>();
		AddChild(Palette);

		ComponentPalette.SpawnRequested += OnSpawnRequested;
		ComponentPalette.SpawnAtPositionRequested += (type, pos) => SpawnComponent(type, pos);
		ComponentPalette.ClearAllRequested += OnClearAllRequested;
		ComponentPalette.SimulationToggled += OnSimulationToggled;
		ComponentPalette.CenterCameraRequested += OnCenterCameraRequested;
		ComponentPalette.SelectModeToggled += (active) => _isSelectModeActive = active;

		CreateConfirmationDialog();

		SpawnComponent("SWITCH", new Vector2(-128, 0));
		SpawnComponent("LED", new Vector2(96, 0));
	}

	private void CreateConfirmationDialog()
	{
		_confirmDialog = new ConfirmationDialog
		{
			Title = "Silme Onayı",
			OkButtonText = "Evet, Sil",
			CancelButtonText = "İptal"
		};
		_confirmDialog.Confirmed += OnDeleteConfirmed;
		_confirmDialog.Canceled += OnDeleteCanceled;
		AddChild(_confirmDialog);
	}

	public override void _ExitTree()
	{
		ComponentPalette.SpawnRequested -= OnSpawnRequested;
		ComponentPalette.ClearAllRequested -= OnClearAllRequested;
		ComponentPalette.SimulationToggled -= OnSimulationToggled;
		ComponentPalette.CenterCameraRequested -= OnCenterCameraRequested;
	}

	private void OnSimulationToggled(bool active) => _isSimulationActive = active;

	private void OnCenterCameraRequested()
	{
		var camera = GetViewport().GetCamera2D();
		if (camera != null) camera.Position = Vector2.Zero;
	}

	private void OnClearAllRequested()
	{
		_pendingDeleteGates.Clear();
		_pendingDeleteGates.AddRange(_gateViews);
		foreach (var g in _pendingDeleteGates) g.IsDeletePending = true;

		_confirmDialog.DialogText = $"Tüm devreyi ({_pendingDeleteGates.Count} kapı) silmek istediğinize emin misiniz?";
		_confirmDialog.PopupCentered();
	}

	private void OnDeleteConfirmed()
	{
		foreach (var gate in _pendingDeleteGates)
		{
			DeleteGate(gate);
		}
		_pendingDeleteGates.Clear();

		foreach (var wire in _pendingDeleteWires)
		{
			Wiring.DeleteWire(wire);
		}
		_pendingDeleteWires.Clear();

		if (_pendingDeleteWire != null)
		{
			Wiring.DeleteWire(_pendingDeleteWire);
			_pendingDeleteWire = null;
		}

		ClearSelection();
	}

	private void OnDeleteCanceled()
	{
		foreach (var gate in _pendingDeleteGates)
		{
			gate.IsDeletePending = false;
		}
		_pendingDeleteGates.Clear();

		foreach (var wire in _pendingDeleteWires)
		{
			wire.IsDeletePending = false;
		}
		_pendingDeleteWires.Clear();

		if (_pendingDeleteWire != null)
		{
			_pendingDeleteWire.IsDeletePending = false;
			_pendingDeleteWire = null;
		}
	}

	private void OnSpawnRequested(string type)
	{
		Vector2 spawnPos = GetGlobalMousePosition();
		SpawnComponent(type, spawnPos);
	}

	public void SpawnComponent(string type, Vector2 pos)
	{
		string id = $"{type}_{_componentCounter++}";
		CircuitComponent? comp = type switch
		{
			"SWITCH" => new SwitchComponent(id),
			"LED" => new LedComponent(id),
			"AND" => new AndGate(id),
			"OR" => new OrGate(id),
			"NOT" => new NotGate(id),
			"XOR" => new XorGate(id),
			"NAND" => new NandGate(id),
			"NOR" => new NorGate(id),
			_ => null
		};

		if (comp == null) return;

		Simulator.RegisterComponent(comp);

		var view = new GateView();
		view.Position = pos;
		AddChild(view);
		view.Setup(comp, type);
		_gateViews.Add(view);
	}

	public void DeleteGate(GateView gate)
	{
		Wiring.RemoveAllWiresConnectedTo(gate);
		Simulator.UnregisterComponent(gate.Component);

		_selectedGates.Remove(gate);
		_gateViews.Remove(gate);
		gate.QueueFree();
	}

	private void ClearSelection()
	{
		foreach (var g in _selectedGates) g.IsSelected = false;
		_selectedGates.Clear();

		foreach (var w in _selectedWires) w.IsSelected = false;
		_selectedWires.Clear();
	}

	public override void _Process(double delta)
	{
		if (_isSimulationActive)
		{
			_tickTimer += delta;
			if (_tickTimer >= TickInterval)
			{
				_tickTimer = 0.0;
				Simulator.Step();
			}
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_isBoxSelecting)
		{
			Vector2 topleft = new(Mathf.Min(_boxSelectStart.X, _boxSelectEnd.X), Mathf.Min(_boxSelectStart.Y, _boxSelectEnd.Y));
			Vector2 size = new(Mathf.Abs(_boxSelectEnd.X - _boxSelectStart.X), Mathf.Abs(_boxSelectEnd.Y - _boxSelectStart.Y));
			Rect2 rect = new(topleft, size);

			DrawRect(rect, new Color(0.2f, 0.8f, 0.4f, 0.2f), filled: true);
			DrawRect(rect, new Color(0.2f, 0.9f, 0.4f, 0.8f), filled: false, width: 1.5f);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// 1. SAĞ TIK: SİLME ONAYI (Kapı veya Kablo)
		if (@event is InputEventMouseButton rightMouse && rightMouse.Pressed && rightMouse.ButtonIndex == MouseButton.Right)
		{
			Vector2 worldPos = GetGlobalMousePosition();

			// A) Seçili bir grup (kapı ve/veya kablo) varken sağ tıklanırsa -> Grubu sil
			if (_selectedGates.Count > 0 || _selectedWires.Count > 0)
			{
				_pendingDeleteGates.Clear();
				_pendingDeleteGates.AddRange(_selectedGates);
				foreach (var g in _pendingDeleteGates) g.IsDeletePending = true;

				_pendingDeleteWires.Clear();
				_pendingDeleteWires.AddRange(_selectedWires);
				foreach (var w in _pendingDeleteWires) w.IsDeletePending = true;

				string summary = "";
				if (_pendingDeleteGates.Count > 0 && _pendingDeleteWires.Count > 0)
					summary = $"{_pendingDeleteGates.Count} kapı ve {_pendingDeleteWires.Count} kabloyu";
				else if (_pendingDeleteGates.Count > 0)
					summary = $"{_pendingDeleteGates.Count} kapıyı";
				else
					summary = $"{_pendingDeleteWires.Count} kabloyu";

				_confirmDialog.DialogText = $"Seçili {summary} silmek istiyor musunuz?";
				_confirmDialog.PopupCentered();
				GetViewport().SetInputAsHandled();
				return;
			}

			// B) Tek bir kapıya sağ tıklanırsa -> O kapıyı sor
			for (int i = _gateViews.Count - 1; i >= 0; i--)
			{
				var gate = _gateViews[i];
				if (gate.IsInsideBody(worldPos))
				{
					_pendingDeleteGates.Clear();
					_pendingDeleteGates.Add(gate);
					gate.IsDeletePending = true;

					_confirmDialog.DialogText = $"'{gate.DisplayName}' kapısını silmek istiyor musunuz?";
					_confirmDialog.PopupCentered();
					GetViewport().SetInputAsHandled();
					return;
				}
			}

			// C) Tek bir kabloya sağ tıklanırsa -> Kabloyu sor
			var clickedWire = Wiring.GetWireAt(worldPos, 20f);
			if (clickedWire != null)
			{
				_pendingDeleteWire = clickedWire;
				clickedWire.IsDeletePending = true;

				_confirmDialog.DialogText = "Bu kablo bağlantısını silmek istiyor musunuz?";
				_confirmDialog.PopupCentered();
				GetViewport().SetInputAsHandled();
				return;
			}

			Wiring.DeselectPin();
		}

		// 2. SOL TIK: ETKİLEŞİM VEYA KUTU SEÇİMİ
		else if (@event is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left)
		{
			Vector2 worldPos = GetGlobalMousePosition();

			if (mouse.Pressed)
			{
				// A) Kutu Seçim Modu
				if (_isSelectModeActive)
				{
					_isBoxSelecting = true;
					_boxSelectStart = worldPos;
					_boxSelectEnd = worldPos;
					GetViewport().SetInputAsHandled();
					return;
				}

				// C) PIN Tıklaması
				for (int i = _gateViews.Count - 1; i >= 0; i--)
				{
					var pin = _gateViews[i].GetTappedPin(worldPos, 20f);
					if (pin != null)
					{
						Wiring.HandlePinTap(pin);
						GetViewport().SetInputAsHandled();
						return;
					}
				}

				// D) Kapı Gövdesi Tıklaması
				for (int i = _gateViews.Count - 1; i >= 0; i--)
				{
					var gate = _gateViews[i];
					if (gate.IsInsideBody(worldPos))
					{
						_leadDraggedGate = gate;
						_dragStartPos = gate.GlobalPosition;
						_gateDragOffset = gate.GlobalPosition - worldPos;

						_multiDragStartPositions.Clear();
						if (_selectedGates.Contains(gate))
						{
							foreach (var g in _selectedGates)
							{
								_multiDragStartPositions[g] = g.GlobalPosition;
								g.MoveToFront();
							}
						}
						else
						{
							ClearSelection();
							_multiDragStartPositions[gate] = gate.GlobalPosition;
							gate.MoveToFront();
						}

						Wiring.DeselectPin();
						GetViewport().SetInputAsHandled();
						return;
					}
				}

				// E) Boş Alan
				ClearSelection();
				Wiring.DeselectPin();
			}
			else if (!mouse.Pressed)
			{
				// Kutu seçimi bitti
				if (_isBoxSelecting)
				{
					_isBoxSelecting = false;
					_boxSelectEnd = worldPos;

					Vector2 topleft = new(Mathf.Min(_boxSelectStart.X, _boxSelectEnd.X), Mathf.Min(_boxSelectStart.Y, _boxSelectEnd.Y));
					Vector2 size = new(Mathf.Abs(_boxSelectEnd.X - _boxSelectStart.X), Mathf.Abs(_boxSelectEnd.Y - _boxSelectStart.Y));
					Rect2 selectionRect = new(topleft, size);

					ClearSelection();

					// 1. Kapıları seç
					foreach (var gate in _gateViews)
					{
						if (selectionRect.Intersects(gate.GetGlobalRect()))
						{
							_selectedGates.Add(gate);
							gate.IsSelected = true;
						}
					}

					// 2. Kabloları seç (Uçları veya çizgisi kutuya temas eden kablolar)
					foreach (var wire in Wiring.Wires)
					{
						bool inside = selectionRect.HasPoint(wire.SourcePin.GlobalPosition) ||
									  selectionRect.HasPoint(wire.TargetPin.GlobalPosition);
						
						if (!inside)
						{
							int ptCount = wire.GetPointCount();
							for (int p = 0; p < ptCount; p++)
							{
								if (selectionRect.HasPoint(wire.GetPointPosition(p)))
								{
									inside = true;
									break;
								}
							}
						}

						if (inside)
						{
							_selectedWires.Add(wire);
							wire.IsSelected = true;
						}
					}

					GetViewport().SetInputAsHandled();
					return;
				}

				// Sürükleme bitti
				if (_leadDraggedGate != null)
				{
					// Tıklandı ama sürüklenmediyse -> Switch Aç/Kapa
					if (_leadDraggedGate.GlobalPosition.DistanceTo(_dragStartPos) < 4f)
					{
						_leadDraggedGate.OnBodyClicked();
					}

					foreach (var kv in _multiDragStartPositions)
					{
						kv.Key.SnapToGrid();
					}

					_leadDraggedGate = null;
					_multiDragStartPositions.Clear();
					GetViewport().SetInputAsHandled();
				}
			}
		}
		else if (@event is InputEventMouseMotion)
		{
			Vector2 worldPos = GetGlobalMousePosition();

			if (_isBoxSelecting)
			{
				_boxSelectEnd = worldPos;
			}
			else if (_leadDraggedGate != null)
			{
				Vector2 deltaMove = (worldPos + _gateDragOffset) - _dragStartPos;
				foreach (var kv in _multiDragStartPositions)
				{
					kv.Key.GlobalPosition = kv.Value + deltaMove;
				}
			}
		}
	}
}
