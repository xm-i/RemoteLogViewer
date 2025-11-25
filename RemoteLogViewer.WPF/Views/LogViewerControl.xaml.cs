using System.Windows.Controls;

using RemoteLogViewer.Core.ViewModels;

namespace RemoteLogViewer.WPF.Views;

public sealed partial class LogViewerControl : UserControl {
	public LogViewerViewModel? ViewModel {
		get;
		private set;
	}

	public LogViewerControl() {
		this.InitializeComponent();
		this.DataContextChanged += (_, _2) => {
			if (this.DataContext is LogViewerViewModel vm) {
				this.ViewModel = vm;
			}
		};
	}
}
