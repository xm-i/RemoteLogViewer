using RemoteLogViewer.Core.ViewModels.Settings;

namespace RemoteLogViewer.WPF.Views.Settings;

public sealed partial class TextViewerSettingsPage {
	public TextViewerSettingsPageViewModel? ViewModel {
		get;
		private set;
	}

	public TextViewerSettingsPage() {
		this.InitializeComponent();
		this.DataContextChanged += (_, _2) => {
			if (this.DataContext is TextViewerSettingsPageViewModel vm) {
				this.ViewModel = vm;
			}
		};
	}
}
