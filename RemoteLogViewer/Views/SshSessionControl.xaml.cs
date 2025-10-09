using Microsoft.UI.Xaml.Controls;

using RemoteLogViewer.ViewModels;

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
}
