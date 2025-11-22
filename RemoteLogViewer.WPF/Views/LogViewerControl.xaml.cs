using System.Windows.Controls;

using RemoteLogViewer.Core.ViewModels;
using RemoteLogViewer.Core.ViewModels.Ssh;
using RemoteLogViewer.WPF.Views.Ssh;

namespace RemoteLogViewer.WPF.Views;

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
