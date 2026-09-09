namespace FSG_PushbackCreator;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("airports", typeof(Pages.AirportListPage));
		Routing.RegisterRoute("airport", typeof(Pages.AirportEditPage));
		Routing.RegisterRoute("parking", typeof(Pages.ParkingEditPage));
		Navigating += OnNavigating;
#if WINDOWS
		Navigated += (_, _) =>
		{
			if (CurrentPage is not null)
				SetNavBarIsVisible(CurrentPage, false);
		};
#endif
	}

	void OnNavigating(object? sender, ShellNavigatingEventArgs e)
	{
		if (e.Source is not ShellNavigationSource.Pop and not ShellNavigationSource.PopToRoot)
			return;
		if (CurrentPage is not Pages.IShellBackHandler handler || handler.AllowNavigateAway)
			return;

		e.Cancel();
		Dispatcher.Dispatch(async () => await handler.HandleShellBackAsync());
	}

	void OnExitClicked(object? sender, EventArgs e) => Application.Current?.Quit();
}
