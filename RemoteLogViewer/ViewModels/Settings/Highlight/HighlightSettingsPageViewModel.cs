using System.Linq;

using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.ViewModels.Settings.Highlight;

public enum HighlightPatternType {
	Regex,
	Exact
}

[AddSingleton]
public class HighlightSettingsPageViewModel : SettingsPageViewModel {
	private readonly ObservableList<HighlightSettingViewModel> _settings = [];
	/// <summary>
	/// UI バインド用の設定一覧 (コレクション変更通知対応ビュー)。
	/// </summary>
	public NotifyCollectionChangedSynchronizedViewList<HighlightSettingViewModel> Settings {
		get;
	}

	public BindableReactiveProperty<HighlightSettingViewModel?> SelectedSetting {
		get;
	} = new();

	public ReactiveCommand AddSettingCommand { get; } = new();
	public ReactiveCommand RemoveSettingCommand { get; } = new();

	public HighlightSettingsPageViewModel() : base("Highlight") {
		// View生成
		var view = this._settings.CreateView(x => x).AddTo(this.CompositeDisposable);
		this.Settings = view.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);

		this.AddSettingCommand.Subscribe(_ => {
			var scope = Ioc.Default.CreateScope();
			var setting = scope.ServiceProvider.GetRequiredService<HighlightSettingViewModel>();
			this._settings.Add(setting);
			this.SelectedSetting.Value = setting;
		}).AddTo(this.CompositeDisposable);

		this.RemoveSettingCommand.Subscribe(_ => {
			if (this.SelectedSetting.Value != null) {
				this._settings.Remove(this.SelectedSetting.Value);
				this.SelectedSetting.Value = this._settings.LastOrDefault();
			}
		}).AddTo(this.CompositeDisposable);
	}
}
