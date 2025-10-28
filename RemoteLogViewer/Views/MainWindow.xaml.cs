using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using RemoteLogViewer.ViewModels;
using RemoteLogViewer.Views.Settings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
[AddSingleton]
public sealed partial class MainWindow : Window {
	private readonly IServiceProvider _services;
	public MainWindow(MainWindowViewModel mainWindowViewModel, IServiceProvider services) {
		this._services = services;
		this.InitializeComponent();
		this.AppWindow.SetIcon("Assets/icon256x256.ico");
		this.ViewModel = mainWindowViewModel;
		this.ViewModel.Notifications.SubscribeAwait(async (notification, ct) => {
			var dialog = new ContentDialog {
				XamlRoot = this.Content.XamlRoot,
				Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
				Title = "Message",
				PrimaryButtonText = "OK",
				DefaultButton = ContentDialogButton.Primary,
				Content = new ContentDialogContent(notification.Message, notification.Severity)
			};
			await dialog.ShowAsync();
		});
		this.ViewModel.NotificationWithActions.SubscribeAwait(async (notification, ct) => {
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
			await dialog.ShowAsync();
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
}
