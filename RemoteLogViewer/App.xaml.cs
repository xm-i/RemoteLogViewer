using System.Diagnostics;
using System.Reflection;

using CommunityToolkit.Mvvm.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

using RemoteLogViewer.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
		WinUI3ProviderInitializer.SetDefaultObservableSystem(ex => Trace.WriteLine(ex.ToString()));
		MainWindow.Activate();
	}

	private static void Build() {
		var serviceCollection = new ServiceCollection();
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
