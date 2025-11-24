using System.Windows;

using RemoteLogViewer.Core.ViewModels.Settings;

namespace RemoteLogViewer.WPF.Views.Settings;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class SettingsWindow : Window {
	public SettingsWindow(SettingsWindowViewModel vm) {
		this.ViewModel = vm;
		this.DataContext = vm;
		this.InitializeComponent();
	}

	public SettingsWindowViewModel ViewModel {
		get;
	}

	private void Button_Click(object sender, RoutedEventArgs e) {
		this.ViewModel.SaveCommand.Execute(Unit.Default);
		this.Close();
	}
}
