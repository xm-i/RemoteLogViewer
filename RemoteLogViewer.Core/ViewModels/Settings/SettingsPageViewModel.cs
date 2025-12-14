using Microsoft.Extensions.Logging;

using RemoteLogViewer.Core.Utils;

namespace RemoteLogViewer.Core.ViewModels.Settings;

public interface ISettingsPageViewModel {
	public string PageName {
		get;
	}
}

public abstract class SettingsPageViewModel<T> : ViewModelBase<T>, ISettingsPageViewModel where T : SettingsPageViewModel<T> {
	public string PageName {
		get;
	}

	protected SettingsPageViewModel(string pageName, ILogger<T> logger) : base(logger) {
		this.PageName = pageName;
	}
}
