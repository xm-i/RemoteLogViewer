using System.Collections.Generic;

using RemoteLogViewer.ViewModels.Settings.Highlight;

namespace RemoteLogViewer.ViewModels.Settings;

[AddSingleton]
public class SettingsWindowViewModel : ViewModelBase {
	public ObservableList<string> Categories {
		get;
	} = [];

	public BindableReactiveProperty<SettingsPageViewModel> SelectedSettingsPage {
		get;
	} = new();

	public List<SettingsPageViewModel> Pages {
		get;
	} = [];

	public HighlightSettingsPageViewModel HighlightSettings {
		get;
	}

	public SettingsWindowViewModel(HighlightSettingsPageViewModel highlightSettings) {
		this.HighlightSettings = highlightSettings;

		this.Pages.AddRange([highlightSettings]);
		this.SelectedSettingsPage.Value = this.Pages[0];
	}
}
