namespace FSG_PushbackCreator.Models;

public enum AirportKind
{
	/// <summary>Extra pushback for an existing FSG airport. TME code is ICAO + "00".</summary>
	DummyPushback,

	/// <summary>Standalone heliport. TME code is the 4-character field (may be unofficial, e.g. CH99).</summary>
	Heliport
}
