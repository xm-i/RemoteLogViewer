using Microsoft.Extensions.Logging;

namespace RemoteLogViewer.ViewModels.Info;

public interface IInfoPageViewModel {
	public string PageName {
		get;
	}
}

public abstract class InfoPageViewModel<T> : ViewModelBase<T>, IInfoPageViewModel where T : InfoPageViewModel<T> {
	public string PageName {
		get;
	}

	protected InfoPageViewModel(string pageName, ILogger<T> logger) : base(logger) {
		this.PageName = pageName;
	}
}
