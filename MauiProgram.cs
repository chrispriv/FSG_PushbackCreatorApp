using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace FSG_PushbackCreator;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		HybridWebViewHandler.Mapper.AppendToMapping("PcaLeafletGestures", (handler, view) =>
		{
#if ANDROID
			var web = handler.PlatformView;
			web.Settings.SetSupportZoom(false);
			web.Settings.BuiltInZoomControls = false;
			web.Settings.DisplayZoomControls = false;
			web.Settings.UseWideViewPort = true;
			web.Settings.LoadWithOverviewMode = true;
			web.OverScrollMode = Android.Views.OverScrollMode.Never;
			web.VerticalScrollBarEnabled = false;
			web.HorizontalScrollBarEnabled = false;
			web.NestedScrollingEnabled = false;
			web.SetOnGenericMotionListener(new PcaMapWheelListener(view));
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

#if ANDROID
	sealed class PcaMapWheelListener : Java.Lang.Object, Android.Views.View.IOnGenericMotionListener
	{
		readonly WeakReference<IHybridWebView> _map;
		long _lastZoomMs;

		public PcaMapWheelListener(IHybridWebView map) => _map = new WeakReference<IHybridWebView>(map);

		public bool OnGenericMotion(Android.Views.View? v, Android.Views.MotionEvent? e)
		{
			if (e is null || e.Action != Android.Views.MotionEventActions.Scroll)
				return false;

			var dy = e.GetAxisValue(Android.Views.Axis.Vscroll);
			if (Math.Abs(dy) < 0.01f)
				dy = e.GetAxisValue(Android.Views.Axis.Hscroll);
			if (Math.Abs(dy) < 0.01f)
				return false;

			var now = Java.Lang.JavaSystem.CurrentTimeMillis();
			if (now - _lastZoomMs >= 80 && _map.TryGetTarget(out var map))
			{
				_lastZoomMs = now;
				var step = dy > 0 ? -1 : 1;
				MainThread.BeginInvokeOnMainThread(() =>
					_ = map.EvaluateJavaScriptAsync($"window.pcaZoomBy&&window.pcaZoomBy({step})"));
			}

			return true;
		}
	}
#endif
}
