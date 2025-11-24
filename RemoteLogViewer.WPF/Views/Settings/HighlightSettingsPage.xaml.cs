using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using RemoteLogViewer.Core.ViewModels.Settings.Highlight;

namespace RemoteLogViewer.WPF.Views.Settings;

public sealed partial class HighlightSettingsPage {
	public HighlightSettingsPageViewModel? ViewModel {
		get;
		private set;
	}

	public HighlightSettingsPage() {
		this.InitializeComponent();
		this.DataContextChanged += (_, _2) => {
			if (this.DataContext is HighlightSettingsPageViewModel vm) {
				this.ViewModel = vm;
			}
		};
	}

	private void ForeColorButton_Click(object sender, System.Windows.RoutedEventArgs e) {
		var popup = this.ForeColorPopup;
		popup.IsOpen = true;
	}

	private void BackColorButton_Click(object sender, System.Windows.RoutedEventArgs e) {
		var popup = this.BackColorPopup;
		popup.IsOpen = true;

	}
}
