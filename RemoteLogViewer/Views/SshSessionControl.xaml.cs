using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using RemoteLogViewer.ViewModels.Ssh;

namespace RemoteLogViewer.Views;

public sealed partial class SshSessionControl : UserControl {
	public SshSessionViewModel ViewModel {
		get;
	}

	public SshSessionControl() {
		this.InitializeComponent();
		this.ViewModel = Ioc.Default.GetRequiredService<SshSessionViewModel>();
		this.DataContext = this.ViewModel;
	}

	private void SavedConnections_DoubleTapped(object sender, DoubleTappedRoutedEventArgs _) {
		if (sender is ListBox lb && lb.SelectedItem is SshConnectionInfoViewModel info) {
			this.ViewModel.SelectSshConnectionInfoCommand.Execute(info);
		}
	}
}
