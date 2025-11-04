using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>TextViewer設定。</summary>
[AddSingleton]
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

public class TextViewerSettingsModelForJson {
	public int? MaxPreviewOneLineCharacters {
		get; set;
	}

	public int? MaxPreviewCharacters {
		get; set;
	}

	public int? GrepMaxResults {
		get; set;
	}

	public static TextViewerSettingsModel CreateModel(TextViewerSettingsModelForJson json, IServiceProvider service) {
		var model = service.GetRequiredService<TextViewerSettingsModel>();
		if (json.MaxPreviewOneLineCharacters is { } maxPreviewOneLineCharacters) {
			model.MaxPreviewOneLineCharacters.Value = maxPreviewOneLineCharacters;
		}
		if (json.MaxPreviewCharacters is { } maxPreviewCharacters) {
			model.MaxPreviewCharacters.Value = maxPreviewCharacters;
		}
		if (json.GrepMaxResults is { } grepMaxResults) {
			model.GrepMaxResults.Value = grepMaxResults;
		}
		return model;
	}
	public static TextViewerSettingsModelForJson CreateJson(TextViewerSettingsModel model) {
		return new TextViewerSettingsModelForJson {
			MaxPreviewOneLineCharacters = model.MaxPreviewOneLineCharacters.Value,
			MaxPreviewCharacters = model.MaxPreviewCharacters.Value,
			GrepMaxResults = model.GrepMaxResults.Value
		};
	}
}
