using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using RemoteLogViewer.Stores.Settings;
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

	public ReactiveCommand SaveCommand {
		get;
	} = new();

	public SettingsWindowViewModel(SettingsStoreModel model) {
		this.HighlightSettings = model.SettingsModel.HighlightSettings.ScopedService.GetRequiredService<HighlightSettingsPageViewModel>();

		this.Pages.AddRange([this.HighlightSettings]);
		this.SelectedSettingsPage.Value = this.Pages[0];

		this.SaveCommand.Subscribe(_ => {
			model.Save();
		});
	}
}
