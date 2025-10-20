using System.IO;

namespace RemoteLogViewer.ViewModels;

/// <summary>
/// ワークスペース選択ウィンドウ用 ViewModel です。パス選択と確定操作を提供します。
/// </summary>
[AddTransient]
public class WorkspaceSelectionWindowViewModel {
	/// <summary>選択中のワークスペースパス。</summary>
	public BindableReactiveProperty<string> SelectedPath { get; } = new(string.Empty);
	/// <summary>次回確認省略フラグ。</summary>
	public BindableReactiveProperty<bool> SkipPersist { get; } = new(false);
	/// <summary>エラーメッセージ。</summary>
	public BindableReactiveProperty<string?> ErrorMessage { get; } = new(null);
	/// <summary>確定コマンド。</summary>
	public ReactiveCommand ConfirmCommand { get; } = new();
	/// <summary>確定成功イベント。(path, skipPersist)</summary>
	public event Action<string, bool>? Confirmed;

	/// <summary>コンストラクタ。</summary>
	public WorkspaceSelectionWindowViewModel() {
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
		this.Confirmed?.Invoke(path, this.SkipPersist.Value);
	}
}
