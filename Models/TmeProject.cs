using System.Collections.ObjectModel;

namespace FSG_PushbackCreator.Models;

public sealed class TmeProject
{
	/// <summary>Root folder inside the TME zip. File name rules come later.</summary>
	public string PackageFolderName { get; set; } = "pca_scenery_pushback";

	public ObservableCollection<AirportEntry> Airports { get; } = [];

	public AirportEntry? FindAirport(Guid id) =>
		Airports.FirstOrDefault(a => a.Id == id);

	public static TmeProject CreateNew() => new();
}
