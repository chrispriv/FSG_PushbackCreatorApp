namespace FSG_PushbackCreator;

public partial class App : Application
{
	const double TabletMaxWidth = 1366;
	const double TabletMaxHeight = 1024;
	const double TabletDefaultWidth = 1194;
	const double TabletDefaultHeight = 834;

	public App()
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Dark;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell())
		{
			Title = "Pushback Creator App",
			Width = TabletDefaultWidth,
			Height = TabletDefaultHeight,
			MinimumWidth = 768,
			MinimumHeight = 600,
			MaximumWidth = TabletMaxWidth,
			MaximumHeight = TabletMaxHeight
		};

#if WINDOWS
		window.HandlerChanged += OnWindowsWindowHandlerChanged;
#endif
		return window;
	}

#if WINDOWS
	static void OnWindowsWindowHandlerChanged(object? sender, EventArgs e)
	{
		if (sender is not Window window || window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window native)
			return;

		var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(native);
		var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
		var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
		if (appWindow?.TitleBar is null)
			return;

		if (native.Content is Microsoft.UI.Xaml.FrameworkElement root)
			root.RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Dark;

		var chrome = global::Windows.UI.Color.FromArgb(255, 0x1A, 0x1A, 0x1A);
		var text = global::Windows.UI.Color.FromArgb(255, 0xEE, 0xEE, 0xEE);
		var hover = global::Windows.UI.Color.FromArgb(255, 0x33, 0x33, 0x33);
		var bar = appWindow.TitleBar;
		bar.BackgroundColor = chrome;
		bar.ForegroundColor = text;
		bar.InactiveBackgroundColor = chrome;
		bar.InactiveForegroundColor = text;
		bar.ButtonBackgroundColor = chrome;
		bar.ButtonForegroundColor = text;
		bar.ButtonInactiveBackgroundColor = chrome;
		bar.ButtonInactiveForegroundColor = text;
		bar.ButtonHoverBackgroundColor = hover;
		bar.ButtonHoverForegroundColor = text;
		bar.ButtonPressedBackgroundColor = hover;
		bar.ButtonPressedForegroundColor = text;
	}
#endif
}
