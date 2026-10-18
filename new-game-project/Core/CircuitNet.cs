using System.Collections.Generic;
using System.Linq;

namespace Golic.Core;

public class CircuitNet
{
	public string Id { get; }
	public LogicState State { get; private set; } = LogicState.Low;
	public List<Pin> Pins { get; } = new();

	public CircuitNet(string id)
	{
		Id = id;
	}

	public void AddPin(Pin pin)
	{
		if (!Pins.Contains(pin))
		{
			Pins.Add(pin);
			pin.AddNet(this);
		}
	}

	public void RemovePin(Pin pin)
	{
		if (Pins.Remove(pin))
		{
			pin.RemoveNet(this);
		}
	}

	/// <summary>
	/// Bu hattaki çıkış pininden değeri okur, giriş pinlerine dağıtır.
	/// </summary>
	public void ResolveNetState()
	{
		// Bu net'in driver'ı (output pin) High ise net High
		LogicState resolved = LogicState.Low;
		foreach (var pin in Pins)
		{
			if (pin.Type == PinType.Output && pin.State == LogicState.High)
			{
				resolved = LogicState.High;
				break;
			}
		}

		State = resolved;

		// Bu net'e bağlı giriş pinlerine değeri ilet (Eğer net High ise pini High yap)
		foreach (var pin in Pins)
		{
			if (pin.Type == PinType.Input && State == LogicState.High)
			{
				pin.State = LogicState.High;
			}
		}
	}
}
