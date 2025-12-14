using Microsoft.Extensions.Logging;

using RemoteLogViewer.Core.Utils;

namespace RemoteLogViewer.Core.ViewModels.Ssh;

public interface IBaseSshPageViewModel {
}

/// <summary>
/// SSH 関連ページ ViewModel の共通基底クラスです。
/// </summary>
public abstract class BaseSshPageViewModel<T> : ViewModelBase<T>, IBaseSshPageViewModel where T : BaseSshPageViewModel<T> {
	protected BaseSshPageViewModel(ILogger<T> logger) : base(logger) {
	}
}
