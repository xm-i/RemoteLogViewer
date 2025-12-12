using System;

using R3;
using R3.JsonConfig.Attributes;

namespace RemoteLogViewer.Composition.Stores.Settings;

/// <summary>TextViewer設定。</summary>
[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDto]
public class TextViewerSettingsModel(IServiceProvider service) {
	public IServiceProvider ScopedService { get; } = service;
	/// <summary>
	/// 1度に追加読み込みする行数
	/// </summary>
	public ReactiveProperty<int> PrefetchLineCount {
		get;
	} = new(200);

	/// <summary>
	/// 追加読み込みの閾値行数(残りXX行になったら追加読み込みする。)
	/// </summary>
	public ReactiveProperty<int> PrefetchThresholdLines {
		get;
	} = new(50);

	/// <summary>
	/// 画面内に保持する最大行数
	/// </summary>
	public ReactiveProperty<int> MaxLogLineLimit {
		get;
	} = new(1000);

	/// <summary>
	/// Grep の最大件数
	/// </summary>
	public ReactiveProperty<int> GrepMaxResults {
		get;
	} = new(1000);
}
