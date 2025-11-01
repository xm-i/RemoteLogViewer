using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using RemoteLogViewer.ViewModels;
using RemoteLogViewer.Views.SshSession;
using RemoteLogViewer.ViewModels.Ssh;

namespace RemoteLogViewer.Views;

public sealed partial class LogViewerControl : UserControl {
	public LogViewerViewModel ViewModel {
		get {
			return field ?? throw new InvalidOperationException();
		}
		set {
			value.CurrentPageViewModel.Subscribe(vm => this.Navigate(vm));
			this.Navigate(value.CurrentPageViewModel.Value);
			field = value;
		}
	}

	public LogViewerControl() {
		this.InitializeComponent();
	}

	private void Navigate(IBaseSshPageViewModel? vm) {
		if (vm is SshServerSelectorViewModel selector) {
			this.SshFrame.Navigate(typeof(SshServerSelectorPage), selector);
		} else if (vm is SshBrowserViewModel browser) {
			this.SshFrame.Navigate(typeof(SshBrowserPage), browser);
		}
	}
}
