using Microsoft.Extensions.Logging;

namespace RemoteLogViewer.Core.Utils;

/// <summary>
/// リソース破棄を共通化する基底クラスです。R3 の <c>CompositeDisposable</c> を利用します。
/// </summary>
public abstract class DisposableBase : IDisposable {
	/// <summary>購読などをまとめて破棄するためのコンテナです。</summary>
	public CompositeDisposable CompositeDisposable { get; } = new();
	/// <summary>リソースを破棄します。</summary>
	public virtual void Dispose() {
		this.CompositeDisposable.Dispose();
		this.OnDisposed();
		GC.SuppressFinalize(this);
	}
	/// <summary>派生クラス追加破棄処理。</summary>
	protected virtual void OnDisposed() {
	}
}

/// <summary>Model 基底クラスです。</summary>
public abstract class ModelBase<T> : DisposableBase where T : ModelBase<T> {
	private readonly ILogger<T> _logger;
	public ModelBase(ILogger<T> logger) {
		this._logger = logger;
		logger.LogTrace("{Model} created.", this.GetType().Name);
	}

	protected override void OnDisposed() {
		this._logger.LogTrace("{Model} disposed.", this.GetType().Name);
		base.OnDisposed();
	}
}
/// <summary>ViewModel 基底クラスです。</summary>
public abstract class ViewModelBase<T> : DisposableBase where T : ViewModelBase<T> {
	private readonly ILogger<T> _logger;
	public ViewModelBase(ILogger<T> logger) {
		this._logger = logger;
		logger.LogTrace("{ViewModel} created.", this.GetType().Name);
	}

	protected override void OnDisposed() {
		this._logger.LogTrace("{ViewModel} disposed.", this.GetType().Name);
		base.OnDisposed();
	}
}
