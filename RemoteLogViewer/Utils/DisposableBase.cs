using System;

namespace RemoteLogViewer.Utils;

/// <summary>
/// リソース破棄を共通化する基底クラスです。R3 の <c>CompositeDisposable</c> を利用します。
/// </summary>
public abstract class DisposableBase : IDisposable {
	/// <summary>購読などをまとめて破棄するためのコンテナです。</summary>
	public CompositeDisposable CompositeDisposable { get; } = new();
	/// <summary>リソースを破棄します。</summary>
	public void Dispose() {
		this.CompositeDisposable.Dispose();
		this.OnDisposed();
		GC.SuppressFinalize(this);
	}
	/// <summary>派生クラス追加破棄処理。</summary>
	protected virtual void OnDisposed() { }
}

/// <summary>Model 基底クラスです。</summary>
public abstract class ModelBase : DisposableBase { }
/// <summary>ViewModel 基底クラスです。</summary>
public abstract class ViewModelBase : DisposableBase { }
