namespace FSG_PushbackCreator.Pages;

public interface IShellBackHandler
{
	bool AllowNavigateAway { get; }

	Task HandleShellBackAsync();
}
