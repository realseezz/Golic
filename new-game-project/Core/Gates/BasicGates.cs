namespace Golic.Core.Gates;

public class AndGate : CircuitComponent
{
	public Pin InA { get; }
	public Pin InB { get; }
	public Pin Out { get; }

	public AndGate(string id) : base(id)
	{
		InA = new Pin($"{id}_InA", PinType.Input, this);
		InB = new Pin($"{id}_InB", PinType.Input, this);
		Out = new Pin($"{id}_Out", PinType.Output, this);

		Inputs.Add(InA);
		Inputs.Add(InB);
		Outputs.Add(Out);
	}

	public override void Evaluate()
	{
		Out.State = (InA.State == LogicState.High && InB.State == LogicState.High)
			? LogicState.High
			: LogicState.Low;
	}
}

public class OrGate : CircuitComponent
{
	public Pin InA { get; }
	public Pin InB { get; }
	public Pin Out { get; }

	public OrGate(string id) : base(id)
	{
		InA = new Pin($"{id}_InA", PinType.Input, this);
		InB = new Pin($"{id}_InB", PinType.Input, this);
		Out = new Pin($"{id}_Out", PinType.Output, this);

		Inputs.Add(InA);
		Inputs.Add(InB);
		Outputs.Add(Out);
	}

	public override void Evaluate()
	{
		Out.State = (InA.State == LogicState.High || InB.State == LogicState.High)
			? LogicState.High
			: LogicState.Low;
	}
}

public class NotGate : CircuitComponent
{
	public Pin In { get; }
	public Pin Out { get; }

	public NotGate(string id) : base(id)
	{
		In = new Pin($"{id}_In", PinType.Input, this);
		Out = new Pin($"{id}_Out", PinType.Output, this);

		Inputs.Add(In);
		Outputs.Add(Out);
	}

	public override void Evaluate()
	{
		Out.State = (In.State == LogicState.High)
			? LogicState.Low
			: LogicState.High;
	}
}

public class XorGate : CircuitComponent
{
	public Pin InA { get; }
	public Pin InB { get; }
	public Pin Out { get; }

	public XorGate(string id) : base(id)
	{
		InA = new Pin($"{id}_InA", PinType.Input, this);
		InB = new Pin($"{id}_InB", PinType.Input, this);
		Out = new Pin($"{id}_Out", PinType.Output, this);

		Inputs.Add(InA);
		Inputs.Add(InB);
		Outputs.Add(Out);
	}

	public override void Evaluate()
	{
		Out.State = (InA.State != InB.State && (InA.State == LogicState.High || InB.State == LogicState.High))
			? LogicState.High
			: LogicState.Low;
	}
}

public class NandGate : CircuitComponent
{
	public Pin InA { get; }
	public Pin InB { get; }
	public Pin Out { get; }

	public NandGate(string id) : base(id)
	{
		InA = new Pin($"{id}_InA", PinType.Input, this);
		InB = new Pin($"{id}_InB", PinType.Input, this);
		Out = new Pin($"{id}_Out", PinType.Output, this);

		Inputs.Add(InA);
		Inputs.Add(InB);
		Outputs.Add(Out);
	}

	public override void Evaluate()
	{
		Out.State = !(InA.State == LogicState.High && InB.State == LogicState.High)
			? LogicState.High
			: LogicState.Low;
	}
}

public class NorGate : CircuitComponent
{
	public Pin InA { get; }
	public Pin InB { get; }
	public Pin Out { get; }

	public NorGate(string id) : base(id)
	{
		InA = new Pin($"{id}_InA", PinType.Input, this);
		InB = new Pin($"{id}_InB", PinType.Input, this);
		Out = new Pin($"{id}_Out", PinType.Output, this);

		Inputs.Add(InA);
		Inputs.Add(InB);
		Outputs.Add(Out);
	}

	public override void Evaluate()
	{
		Out.State = !(InA.State == LogicState.High || InB.State == LogicState.High)
			? LogicState.High
			: LogicState.Low;
	}
}

public class SwitchComponent : CircuitComponent
{
	public Pin Out { get; }
	public bool IsOn { get; set; } = false;

	public SwitchComponent(string id) : base(id)
	{
		Out = new Pin($"{id}_Out", PinType.Output, this);
		Outputs.Add(Out);
	}

	public void Toggle()
	{
		IsOn = !IsOn;
	}

	public override void Evaluate()
	{
		Out.State = IsOn ? LogicState.High : LogicState.Low;
	}
}

public class LedComponent : CircuitComponent
{
	public Pin In { get; }

	public LedComponent(string id) : base(id)
	{
		In = new Pin($"{id}_In", PinType.Input, this);
		Inputs.Add(In);
	}

	public bool IsLit => In.State == LogicState.High;

	public override void Evaluate()
	{
		// LED sadece girişi okuyan bir uç birimdir
	}
}
