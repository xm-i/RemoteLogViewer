using Microsoft.Extensions.DependencyInjection;

using System.Collections.Generic;

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
}

public class TextViewerSettingsModelForJson {
	public required int MaxPreviewOneLineCharacters {
		get; set;
	}

	public required int MaxPreviewCharacters {
		get; set;
	}

	public static TextViewerSettingsModel CreateModel(TextViewerSettingsModelForJson json, IServiceProvider service) {
		var model = service.GetRequiredService<TextViewerSettingsModel>();
		model.MaxPreviewOneLineCharacters.Value = json.MaxPreviewOneLineCharacters;
		model.MaxPreviewCharacters.Value = json.MaxPreviewCharacters;
		return model;
	}
	public static TextViewerSettingsModelForJson CreateJson(TextViewerSettingsModel model) {
		return new TextViewerSettingsModelForJson {
			MaxPreviewOneLineCharacters = model.MaxPreviewOneLineCharacters.Value,
			MaxPreviewCharacters = model.MaxPreviewCharacters.Value
		};
	}
}
