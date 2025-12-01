using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using RemoteLogViewer.Core.ViewModels;
using RemoteLogViewer.WinUI.Views.Info;
using RemoteLogViewer.WinUI.Views.Settings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RemoteLogViewer.WinUI.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public sealed partial class MainWindow : Window {
	private readonly IServiceProvider _services;
	public MainWindow(MainWindowViewModel mainWindowViewModel, IServiceProvider services) {
		this._services = services;
		this.InitializeComponent();
		this.ExtendsContentIntoTitleBar = true;
		this.AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Standard;
		this.SetTitleBar(this.titleBar);
		this.ViewModel = mainWindowViewModel;
		_ = this.ViewModel.Notifications.SubscribeAwait(async (notification, ct) => {
			var dialog = new ContentDialog {
				XamlRoot = this.Content.XamlRoot,
				Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
				Title = "Message",
				PrimaryButtonText = "OK",
				DefaultButton = ContentDialogButton.Primary,
				Content = new ContentDialogContent(notification.Message, notification.Severity)
			};
			_ = await dialog.ShowAsync();
		});
		_ = this.ViewModel.NotificationWithActions.SubscribeAwait(async (notification, ct) => {
			var dialog = new ContentDialog {
				XamlRoot = this.Content.XamlRoot,
				Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
				Title = "Message",
				PrimaryButtonText = notification.PrimaryActionText,
				PrimaryButtonCommand = new ReactiveCommand(_ => notification.PrimaryAction()),
				SecondaryButtonText = notification.SecondaryActionText,
				SecondaryButtonCommand = new ReactiveCommand(_ => notification.SecondaryAction()),
				DefaultButton = ContentDialogButton.Primary,
				Content = new ContentDialogContent(notification.Message, notification.Severity)
			};
			_ = await dialog.ShowAsync();
		});
	}

	public MainWindowViewModel ViewModel {
		get;
	}

	private void TabView_TabCloseRequested(object sender, TabViewTabCloseRequestedEventArgs e) {
		if (e.Item is LogViewerViewModel vm) {
			this.ViewModel.CloseTabCommand.Execute(vm);
		}
	}

	private void OpenSettings_Click(object sender, RoutedEventArgs e) {
		var window = this._services.GetRequiredService<SettingsWindow>();
		window.Activate();
	}

	private void InfoButton_Click(object sender, RoutedEventArgs e) {
		var window = this._services.GetRequiredService<InfoWindow>();
		window.Activate();
	}
}
