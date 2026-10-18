using System.Collections.Generic;

namespace Golic.Core;

public enum PinType
{
	Input,
	Output
}

public class Pin
{
	public string Id { get; }
	public PinType Type { get; }
	public LogicState State { get; set; } = LogicState.Low;
	public CircuitComponent Owner { get; }

	// Her kablo ayrı bir net oluşturduğu için pin birden fazla net'e bağlı olabilir.
	// (Fan-out: bir çıkış → birden fazla net / Fan-in: bir giriş → birden fazla net)
	private readonly List<CircuitNet> _connectedNets = new();
	public IReadOnlyList<CircuitNet> ConnectedNets => _connectedNets;

	// Geriye dönük uyumluluk için — ilk net'i döndürür
	public CircuitNet? ConnectedNet => _connectedNets.Count > 0 ? _connectedNets[0] : null;

	public void AddNet(CircuitNet net)
	{
		if (!_connectedNets.Contains(net))
			_connectedNets.Add(net);
	}

	public void RemoveNet(CircuitNet net)
	{
		_connectedNets.Remove(net);
	}

	public Pin(string id, PinType type, CircuitComponent owner)
	{
		Id = id;
		Type = type;
		Owner = owner;
	}
}
