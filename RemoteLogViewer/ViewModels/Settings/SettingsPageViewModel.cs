namespace RemoteLogViewer.ViewModels.Settings;

public abstract class SettingsPageViewModel : ViewModelBase {
	public string PageName {
		get;
	}

	public SettingsPageViewModel(string pageName) {
		this.PageName = pageName;
	}
}
