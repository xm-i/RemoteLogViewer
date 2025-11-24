using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using RemoteLogViewer.Core.ViewModels;
using RemoteLogViewer.WPF.Views.Info;
using RemoteLogViewer.WPF.Views.Settings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RemoteLogViewer.WPF.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public sealed partial class MainWindow : Window {
	private readonly IServiceProvider _services;
	public MainWindow(MainWindowViewModel mainWindowViewModel, IServiceProvider services) {
		this._services = services;
		this.InitializeComponent();
		this.ViewModel = mainWindowViewModel;
		this.DataContext = this.ViewModel;
		this.ViewModel.Notifications.Subscribe(notification => {
			var dialog = new ContentDialogWindow {
				MessageTitle = "Message",
				PrimaryButtonText = "OK",
				PrimaryButtonCommand = new ReactiveCommand(_ => { }),
				Message = notification.Message,
				Severity = notification.Severity
			};
			dialog.ShowDialog();
		});
		this.ViewModel.NotificationWithActions.Subscribe(notification => {
			var dialog = new ContentDialogWindow {
				MessageTitle = "Message",
				PrimaryButtonText = notification.PrimaryActionText,
				PrimaryButtonCommand = new ReactiveCommand(_ => notification.PrimaryAction()),
				SecondaryButtonText = notification.SecondaryActionText,
				SecondaryButtonCommand = new ReactiveCommand(_ => notification.SecondaryAction()),
				Message = notification.Message,
				Severity = notification.Severity
			};
			dialog.ShowDialog();
		});
	}

	public MainWindowViewModel ViewModel {
		get;
	}

	private void TabCloseButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel.SelectedTab.Value is null) {
			return;
		}
		this.ViewModel.CloseTabCommand.Execute(this.ViewModel.SelectedTab.Value);
	}

	private void OpenSettings_Click(object sender, RoutedEventArgs e) {
		var window = this._services.GetRequiredService<SettingsWindow>();
		window.ShowDialog();
	}

	private void InfoButton_Click(object sender, RoutedEventArgs e) {
		var window = this._services.GetRequiredService<InfoWindow>();
		window.ShowDialog();
	}
}
