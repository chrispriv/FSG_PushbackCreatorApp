using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace FSG_PushbackCreator;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		HybridWebViewHandler.Mapper.AppendToMapping("PcaLeafletGestures", (handler, _) =>
		{
#if ANDROID
			var web = handler.PlatformView;
			web.Settings.SetSupportZoom(false);
			web.Settings.BuiltInZoomControls = false;
			web.Settings.DisplayZoomControls = false;
			web.Settings.UseWideViewPort = true;
			web.Settings.LoadWithOverviewMode = true;
#endif
#if WINDOWS
			if (handler.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 webView2)
			{
				webView2.CoreWebView2Initialized += (_, _) =>
				{
					if (webView2.CoreWebView2 is null)
						return;
					webView2.CoreWebView2.Settings.IsPinchZoomEnabled = false;
					webView2.CoreWebView2.Settings.IsZoomControlEnabled = false;
				};
			}
#endif
		});

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
