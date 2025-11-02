using Microsoft.Extensions.Logging;

namespace RemoteLogViewer.ViewModels.Settings;

public interface ISettingsPageViewModel {
	public string PageName { get; }
}

public abstract class SettingsPageViewModel<T> : ViewModelBase<T>, ISettingsPageViewModel where T : SettingsPageViewModel<T> {
	public string PageName {
		get;
	}

	protected SettingsPageViewModel(string pageName, ILogger<T> logger) : base(logger) {
		this.PageName = pageName;
	}
}
