using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Extensions.Logging;
using Microsoft.Win32;

using RemoteLogViewer.Core.ViewModels.Ssh;

namespace RemoteLogViewer.WPF.Views.Ssh;

/// <summary>
/// SSH サーバー選択ページです。接続設定の選択および編集を行います。
/// </summary>
public sealed partial class SshServerSelectorPage {

	private ILogger<SshServerSelectorPage> logger {
		get {
			return field ??= App.LoggerFactory.CreateLogger<SshServerSelectorPage>();
		}
	}
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
		this.DataContextChanged += (_, _2) => {
			if (this.DataContext is SshServerSelectorViewModel vm) {
				this.ViewModel = vm;
			}
		};
	}

	private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
		if (this.ViewModel?.SelectedSshConnectionInfo.Value is { } vm) {
			vm.Password.Value = this.PasswordBox.Password;
		}
	}

	private void PrivateKeyPassphraseBox_PasswordChanged(object sender, RoutedEventArgs e) {
		if (this.ViewModel?.SelectedSshConnectionInfo.Value is { } vm) {
			vm.PrivateKeyPassphrase.Value = this.PrivateKeyPassphraseBox.Password;
		}
	}

	/// <summary>
	/// 保存済み接続一覧ダブルタップ時の処理です。
	/// </summary>
	private void SavedConnections_MouseDoubleClick(object sender, MouseButtonEventArgs _) {
		if (this.ViewModel == null) {
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
	private void BrowsePrivateKeyButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel?.SelectedSshConnectionInfo.Value is not { } vm) {
			return;
		}
		try {
			var dialog = new OpenFileDialog {
				Title = "Select Private Key",
				Filter = "Key Files|*.pem;*.ppk;*.key;*.pub;*.crt;*.der;*.rsa|All Files|*.*",
				Multiselect = false
			};
			if (dialog.ShowDialog() == true) {
				vm.PrivateKeyPath.Value = dialog.FileName;
			}
		} catch (Exception ex) {
			this.logger.LogError(ex, "Exception selecting private key file");
		}
	}
}
