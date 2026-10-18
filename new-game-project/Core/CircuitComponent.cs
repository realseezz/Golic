using System.Collections.Generic;

namespace Golic.Core;

public abstract class CircuitComponent
{
	public string Id { get; }
	public List<Pin> Inputs { get; } = new();
	public List<Pin> Outputs { get; } = new();

	protected CircuitComponent(string id)
	{
		Id = id;
	}

	/// <summary>
	/// Kapının mantık fonksiyonunu hesaplar.
	/// </summary>
	public abstract void Evaluate();
}
