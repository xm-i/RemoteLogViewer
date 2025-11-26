using System.IO;
using System.Text;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RemoteLogViewer.Core.Services;
using RemoteLogViewer.WPF.Views;
using Serilog;

namespace RemoteLogViewer.WPF;

public partial class App : Application {

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

	protected override void OnStartup(StartupEventArgs e) {
		base.OnStartup(e);
		Build();
		var logger = LoggerFactory.CreateLogger<App>();
		WpfProviderInitializer.SetDefaultObservableSystem(ex => logger.LogWarning(ex, "Exception"));
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		this.MainWindow = Ioc.Default.GetRequiredService<MainWindow>();
		var workspaceService = Ioc.Default.GetRequiredService<WorkspaceService>();
		if (string.IsNullOrWhiteSpace(workspaceService.WorkspacePath)) {
			this.ShowWorkspaceSelectionWindow(workspaceService);
		}
		if (string.IsNullOrWhiteSpace(workspaceService.WorkspacePath)) {
			return;
		}
		this.MainWindow.ShowDialog();
	}

	/// <summary>ワークスペース選択ウィンドウ表示。</summary>
	private void ShowWorkspaceSelectionWindow(WorkspaceService workspaceService) {
		var wsWindow = Ioc.Default.GetRequiredService<WorkspaceSelectionWindow>();
		wsWindow.ShowDialog();
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

		serviceCollection.AddGeneratedServices();
		Core.DIRegistration.AddGeneratedServices(serviceCollection);
		Composition.DIRegistration.AddGeneratedServices(serviceCollection);

		Ioc.Default.ConfigureServices(
			serviceCollection.BuildServiceProvider()
		);
	}
}
