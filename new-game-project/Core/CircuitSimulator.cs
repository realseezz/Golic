using System.Collections.Generic;

namespace Golic.Core;

public class CircuitSimulator
{
	public List<CircuitComponent> Components { get; } = new();
	public List<CircuitNet> Nets { get; } = new();

	public void RegisterComponent(CircuitComponent comp) => Components.Add(comp);
	public void UnregisterComponent(CircuitComponent comp) => Components.Remove(comp);

	public void RegisterNet(CircuitNet net) => Nets.Add(net);
	public void UnregisterNet(CircuitNet net) => Nets.Remove(net);

	/// <summary>
	/// Tek bir simülasyon adımı (Tick).
	/// </summary>
	public void Step()
	{
		// 1. Tüm bileşenlerin çıkışlarını hesapla
		foreach (var comp in Components)
		{
			comp.Evaluate();
		}

		// 2. Giriş pinlerini temizle (High sinyal gelirse net'ler güncelleyecek)
		foreach (var comp in Components)
		{
			foreach (var inPin in comp.Inputs)
			{
				inPin.State = LogicState.Low;
			}
		}

		// 3. Hatların (kabloların) durumunu çöz ve girişlere yay
		foreach (var net in Nets)
		{
			net.ResolveNetState();
		}
	}
}
