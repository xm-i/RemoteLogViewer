using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

using RemoteLogViewer.Services;
using RemoteLogViewer.Views;
using System.IO;
using WinRT.Interop;
using Serilog;
using Microsoft.Extensions.Logging;

namespace RemoteLogViewer;

public partial class App : Application {
	/// <summary>
	///     メインウィンドウインスタンスを取得します。
	/// </summary>
	public static Window MainWindow {
		get {
			return field ??= Ioc.Default.GetRequiredService<MainWindow>();
		}
	}

	/// <summary>
	///     ILoggerFactory for DI外クラスでのログ使用。
	/// </summary>
	public static ILoggerFactory LoggerFactory {
		get {
			return field ??= Ioc.Default.GetRequiredService<ILoggerFactory>();
		}
	}

	/// <summary>
	///     <see cref="App"/> の新しいインスタンスを初期化します。
	/// </summary>
	public App() {
		this.InitializeComponent();
	}

	/// <summary>
	///     アプリ起動時に呼び出されます。
	/// </summary>
	/// <param name="args">起動引数。</param>
	protected override void OnLaunched(LaunchActivatedEventArgs args) {
		Build();
		var logger = LoggerFactory.CreateLogger<App>();
		WinUI3ProviderInitializer.SetDefaultObservableSystem(ex => logger.LogWarning(ex, "Exception"));
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		var workspaceService = Ioc.Default.GetRequiredService<WorkspaceService>();
		if (string.IsNullOrWhiteSpace(workspaceService.WorkspacePath)) {
			this.ShowWorkspaceSelectionWindow(workspaceService);
		} else {
			MainWindow.Activate();
			SetWindowIcon();
		}
	}

	/// <summary>ワークスペース選択ウィンドウ表示。</summary>
	private void ShowWorkspaceSelectionWindow(WorkspaceService workspaceService) {
		var wsWindow = Ioc.Default.GetRequiredService<WorkspaceSelectionWindow>();
		wsWindow.AppWindow?.Resize(new(600, 250));
		wsWindow.Activate();
		wsWindow.WorkspaceSelected += () => {
			MainWindow.Activate();
			SetWindowIcon();
		};
	}

	// Win32 API constants & P/Invoke
	private const int WM_SETICON = 0x80;
	private static readonly IntPtr ICON_SMALL = 0;
	private static readonly IntPtr ICON_BIG = 1;
	private const uint IMAGE_ICON = 1;
	private const uint LR_LOADFROMFILE = 0x0010;
	private const uint LR_DEFAULTSIZE = 0x0040;

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

	[DllImport("user32.dll")]
	private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

	/// <summary>
	/// ウィンドウのアイコンを設定 (タイトルバー / タスクバー / Alt+Tab)。
	/// </summary>
	private static void SetWindowIcon() {
		try {
			var hwnd = WindowNative.GetWindowHandle(MainWindow);
			var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon256x256.ico");
			if (!File.Exists(iconPath)) {
				return;
			}
			// 大アイコン (既定サイズ) と小アイコン (16x16) を設定
			var hBig = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
			var hSmall = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
			if (hBig != IntPtr.Zero) {
				SendMessage(hwnd, WM_SETICON, ICON_BIG, hBig);
			}
			if (hSmall != IntPtr.Zero) {
				SendMessage(hwnd, WM_SETICON, ICON_SMALL, hSmall);
			}
		} catch (Exception ex) {
			var logger = LoggerFactory.CreateLogger<App>();
			logger.LogWarning(ex, "Failed to set window icon");
		}
	}

	/// <summary>DI コンテナ構築。</summary>
	private static void Build() {
		// Serilog設定
		Log.Logger = new LoggerConfiguration()
#if DEBUG_UNPACKAGED
			.MinimumLevel.Verbose()
			.WriteTo.Debug()
#else
			.MinimumLevel.Information()
#endif
			.WriteTo.File(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RemoteLogViewer", "log", ".log"),rollingInterval:RollingInterval.Month)
			.CreateLogger();

		var serviceCollection = new ServiceCollection();
		serviceCollection.AddLogging(loggingBuilder =>
		{
			loggingBuilder.AddSerilog(dispose: true);
		});

		var targetTypes = Assembly
			.GetExecutingAssembly()
			.GetTypes()
			.Where(x =>
				x.GetCustomAttributes<AddTransientAttribute>(inherit: true).Any());

		foreach (var targetType in targetTypes) {
			var attribute = targetType.GetCustomAttribute<AddTransientAttribute>();
			serviceCollection.AddTransient(attribute?.ServiceType ?? targetType, targetType);
		}

		var singletonTargetTypes = Assembly
			.GetExecutingAssembly()
			.GetTypes()
			.Where(x =>
				x.GetCustomAttributes<AddSingletonAttribute>(inherit: true).Any());

		foreach (var singletonTargetType in singletonTargetTypes) {
			serviceCollection.AddSingleton(singletonTargetType);
		}

		var scopedTargetTypes = Assembly
			.GetExecutingAssembly()
			.GetTypes()
			.Where(x =>
				x.GetCustomAttributes<AddScopedAttribute>(inherit: true).Any());

		foreach (var scopedTargetType in scopedTargetTypes) {
			serviceCollection.AddScoped(scopedTargetType);
		}

		Ioc.Default.ConfigureServices(
			serviceCollection.BuildServiceProvider()
		);
	}
}
