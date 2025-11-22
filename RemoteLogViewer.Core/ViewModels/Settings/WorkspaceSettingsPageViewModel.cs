using System.IO;
using Microsoft.Extensions.Logging;
using RemoteLogViewer.Core.Services;

namespace RemoteLogViewer.Core.ViewModels.Settings;

/// <summary>
/// ワークスペース選択ページ用 ViewModel です。パス選択と確定操作を提供します。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class WorkspaceSettingsPageViewModel: SettingsPageViewModel<WorkspaceSettingsPageViewModel> {
	private readonly WorkspaceService _workspaceService;
	/// <summary>選択中のワークスペースパス。</summary>
	public BindableReactiveProperty<string> SelectedPath { get; } = new(string.Empty);
	/// <summary>次回確認省略フラグ。</summary>
	public BindableReactiveProperty<bool> SkipPersist { get; } = new(false);
	/// <summary>エラーメッセージ。</summary>
	public BindableReactiveProperty<string?> ErrorMessage { get; } = new(null);
	/// <summary>確定コマンド。</summary>
	public ReactiveCommand ConfirmCommand { get; } = new();
	/// <summary>確定成功イベント。</summary>
	public event Action? Confirmed;

	/// <summary>コンストラクタ。</summary>
	public WorkspaceSettingsPageViewModel(WorkspaceService workspaceService, ILogger<WorkspaceSettingsPageViewModel> logger): base("Workspace", logger) {
		this.SelectedPath.Value = workspaceService.WorkspacePath ?? string.Empty;
		this.SkipPersist.Value = workspaceService.IsPersist;
		this._workspaceService = workspaceService;
		this.ConfirmCommand.Subscribe(_ => this.OnConfirm());
	}

	/// <summary>確定処理。</summary>
	private void OnConfirm() {
		var path = this.SelectedPath.Value.Trim();
		if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) {
			this.ErrorMessage.Value = "有効なフォルダを選択してください。";
			return;
		}
		this.ErrorMessage.Value = null;
		this._workspaceService.SetWorkspace(path, this.SkipPersist.Value);
		this.Confirmed?.Invoke();
	}
}
