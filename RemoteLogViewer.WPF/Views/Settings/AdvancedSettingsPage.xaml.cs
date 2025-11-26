using RemoteLogViewer.Core.ViewModels.Settings;

namespace RemoteLogViewer.WPF.Views.Settings;

public sealed partial class AdvancedSettingsPage {
	public AdvancedSettingsPageViewModel? ViewModel {
		get;
		private set;
	}

	public AdvancedSettingsPage() {
		this.InitializeComponent();
		this.DataContextChanged += (_, _2) => {
			if (this.DataContext is AdvancedSettingsPageViewModel vm) {
				this.ViewModel = vm;
			}
		};
	}
}
