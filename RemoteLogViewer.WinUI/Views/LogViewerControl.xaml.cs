using Microsoft.UI.Xaml.Controls;

using RemoteLogViewer.Core.ViewModels;
using RemoteLogViewer.Core.ViewModels.Ssh;
using RemoteLogViewer.WinUI.Views.Ssh;

namespace RemoteLogViewer.WinUI.Views;

public sealed partial class LogViewerControl : UserControl {
	public LogViewerViewModel ViewModel {
		get {
			return field ?? throw new InvalidOperationException();
		}
		set {
			_ = value.CurrentPageViewModel.Subscribe(vm => this.Navigate(vm));
			this.Navigate(value.CurrentPageViewModel.Value);
			field = value;
		}
	}

	public LogViewerControl() {
		this.InitializeComponent();
	}

	private void Navigate(IBaseSshPageViewModel? vm) {
		if (vm is SshServerSelectorViewModel selector) {
			_ = this.SshFrame.Navigate(typeof(SshServerSelectorPage), selector);
		} else if (vm is SshBrowserViewModel browser) {
			_ = this.SshFrame.Navigate(typeof(SshBrowserPage), browser);
		}
	}
}
