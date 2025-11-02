using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RemoteLogViewer.Stores.Settings;
using RemoteLogViewer.ViewModels.Settings.Highlight;

namespace RemoteLogViewer.ViewModels.Settings;

[AddSingleton]
public class SettingsWindowViewModel : ViewModelBase<SettingsWindowViewModel> {
	public ObservableList<string> Categories {
		get;
	} = [];

	public BindableReactiveProperty<ISettingsPageViewModel> SelectedSettingsPage {
		get;
	} = new();

	public List<ISettingsPageViewModel> Pages {
		get;
	} = [];

	public HighlightSettingsPageViewModel HighlightSettings {
		get;
	}

	public WorkspaceSettingsPageViewModel WorkspaceSettings {
		get;
	}
	public TextViewerSettingsPageViewModel TextViewerSettings {
		get;
	}

	public ReactiveCommand SaveCommand {
		get;
	} = new();

	public SettingsWindowViewModel(SettingsStoreModel model, WorkspaceSettingsPageViewModel workspaceSettings, TextViewerSettingsPageViewModel textViewerSettings, ILogger<SettingsWindowViewModel> logger) : base(logger) {
		this.HighlightSettings = model.SettingsModel.HighlightSettings.ScopedService.GetRequiredService<HighlightSettingsPageViewModel>();
		this.WorkspaceSettings = workspaceSettings;
		this.TextViewerSettings = textViewerSettings;

		this.Pages.AddRange([this.HighlightSettings, this.WorkspaceSettings, this.TextViewerSettings]);
		this.SelectedSettingsPage.Value = this.Pages[0];

		this.SaveCommand.Subscribe(_ => {
			model.Save();
		});
	}
}
