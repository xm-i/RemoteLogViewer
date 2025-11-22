using System.Windows.Controls;

using RemoteLogViewer.Core.ViewModels.Settings;

namespace RemoteLogViewer.WPF.Views.Settings;

public sealed partial class AdvancedSettingsPage : Page {
	public Core.ViewModels.Settings.AdvancedSettingsPageViewModel? ViewModel {
		get;
		private set;
	}

	public AdvancedSettingsPage() {
		this.InitializeComponent();
	}

	/// <summary>
	/// ナビゲート時に ViewModel を受け取ります。
	/// </summary>
	protected override void OnNavigatedTo(NavigationEventArgs e) {
		if (e.Parameter is AdvancedSettingsPageViewModel vm) {
			this.ViewModel = vm;
		} else {
			throw new InvalidOperationException("ViewModel is not passed.");
		}
		base.OnNavigatedTo(e);
	}
}
