using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

using RemoteLogViewer.Services;
using RemoteLogViewer.Views;
using System.IO;
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
		}
	}

	/// <summary>ワークスペース選択ウィンドウ表示。</summary>
	private void ShowWorkspaceSelectionWindow(WorkspaceService workspaceService) {
		var wsWindow = Ioc.Default.GetRequiredService<WorkspaceSelectionWindow>();
		wsWindow.AppWindow?.Resize(new(600, 250));
		wsWindow.Activate();
		wsWindow.WorkspaceSelected += () => {
			MainWindow.Activate();
		};
	}

	/// <summary>DI コンテナ構築。</summary>
	private static void Build() {
		// Serilog設定
		string[] logFileds = [
			"{Timestamp:HH:mm:ss.fff}",
			"{Level:u4}",
			"{ThreadId:00}",
			"{Message:j}",
			"{SourceContext}",
			"{NewLine}{Exception}"
		];

		Log.Logger = new LoggerConfiguration()
			.Enrich.WithThreadId()
#if DEBUG_UNPACKAGED
			.MinimumLevel.Verbose()
#else
			.MinimumLevel.Information()
#endif
			.WriteTo.Debug(outputTemplate: string.Join("｜", logFileds))
			.WriteTo.File(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RemoteLogViewer", "log", ".log"), rollingInterval: RollingInterval.Month, outputTemplate: string.Join("\t", logFileds))
			.CreateLogger();

		var serviceCollection = new ServiceCollection();
		serviceCollection.AddLogging(loggingBuilder => {
			loggingBuilder.AddSerilog(dispose: true);
		});

		DIRegistration.AddGeneratedServices(serviceCollection);
		Composition.DIRegistration.AddGeneratedServices(serviceCollection);

		Ioc.Default.ConfigureServices(
			serviceCollection.BuildServiceProvider()
		);
	}
}
