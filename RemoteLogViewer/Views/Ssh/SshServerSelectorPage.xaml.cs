using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using RemoteLogViewer.Services.Ssh;
using RemoteLogViewer.ViewModels.Ssh;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace RemoteLogViewer.Views.SshSession;

/// <summary>
/// SSH サーバー選択ページです。接続設定の選択および編集を行います。
/// </summary>
public sealed partial class SshServerSelectorPage : Page {
	/// <summary>
	/// ビューモデルを取得します。
	/// </summary>
	public SshServerSelectorViewModel? ViewModel {
		get;
		private set;
	}

	/// <summary>
	/// インスタンスを初期化します。
	/// </summary>
	public SshServerSelectorPage() {
		this.InitializeComponent();
	}

	/// <summary>
	/// ナビゲート時に ViewModel を受け取ります。
	/// </summary>
	protected override void OnNavigatedTo(NavigationEventArgs e) {
		if (e.Parameter is SshServerSelectorViewModel vm) {
			this.ViewModel = vm;
		} else {
			throw new InvalidOperationException("ViewModel is not passed.");
		}
		base.OnNavigatedTo(e);
	}

	/// <summary>
	/// 保存済み接続一覧ダブルタップ時の処理です。
	/// </summary>
	private void SavedConnections_DoubleTapped(object sender, DoubleTappedRoutedEventArgs _) {
		if(this.ViewModel == null) {
			return;
		}
		if (sender is ListBox lb && lb.SelectedItem is SshConnectionInfoViewModel info) {
			this.ViewModel.SelectSshConnectionInfoCommand.Execute(info);
		}
	}

	/// <summary>
	/// 保存済み接続一覧の選択変更時の処理です (シングルクリック対応)。
	/// </summary>
	private void SavedConnections_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (sender is ListBox lb && lb.SelectedItem is SshConnectionInfoViewModel info) {
			this.ViewModel.SelectSshConnectionInfoCommand.Execute(info);
		}
	}

	/// <summary>
	/// 秘密鍵ファイル選択ボタン押下時の処理です。
	/// </summary>
	private async void BrowsePrivateKeyButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (this.ViewModel.SelectedSshConnectionInfo.Value is not { } vm) {
			return;
		}
		try {
			var picker = new FileOpenPicker {
				ViewMode = PickerViewMode.List,
				SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
			};
			var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
			InitializeWithWindow.Initialize(picker, hwnd);
			string[] exts = [".pem", ".ppk", ".key", ".pub", ".crt", ".der", ".rsa"];
			foreach (var ext in exts) {
				picker.FileTypeFilter.Add(ext);
			}
			var file = await picker.PickSingleFileAsync();
			if (file != null) {
				vm.PrivateKeyPath.Value = file.Path;
			}
		} catch (Exception ex) {
			Debug.WriteLine($"[BrowsePrivateKey] Error: {ex}");
		}
	}
}
