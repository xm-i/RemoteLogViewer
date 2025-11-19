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
	/// 1行に表示する最大文字数
	/// </summary>
	public ReactiveProperty<int> MaxPreviewOneLineCharacters {
		get;
	} = new(1000);

	/// <summary>
	/// 全体で表示する最大文字数
	/// </summary>
	public ReactiveProperty<int> MaxPreviewCharacters {
		get;
	} = new(30000);

	/// <summary>
	/// Grep の最大件数
	/// </summary>
	public ReactiveProperty<int> GrepMaxResults {
		get;
	} = new(1000);
}
