using FSG_PushbackCreator.Models;

namespace FSG_PushbackCreator.Services;

public static class ProjectSession
{
	public static TmeProject Current { get; private set; } = new();

	public static bool IsOpen { get; private set; }

	public static bool HasUnexportedChanges { get; private set; }

	public static void StartNew()
	{
		Current = TmeProject.CreateNew();
		IsOpen = true;
		HasUnexportedChanges = false;
	}

	public static void MarkDirty() => HasUnexportedChanges = true;

	public static void MarkExported() => HasUnexportedChanges = false;

	public static void Open(TmeProject project)
	{
		Current = project;
		IsOpen = true;
		HasUnexportedChanges = false;
	}

	public static void Close()
	{
		Current = new TmeProject();
		IsOpen = false;
		HasUnexportedChanges = false;
	}
}
