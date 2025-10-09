using Renci.SshNet;

namespace RemoteLogViewer.Services;

/// <summary>
///     SSH 接続とコマンド実行を提供します。
/// </summary>
[AddTransient]
public class SshService : IDisposable {
	private SshClient? _client;

	/// <summary>
	///     パスワード認証で接続します。
	/// </summary>
	/// <param name="host">ホスト。</param>
	/// <param name="port">ポート。</param>
	/// <param name="user">ユーザー名。</param>
	/// <param name="password">パスワード。</param>
	public void Connect(string host, int port, string user, string password) {
		this.Disconnect();
		this._client = new SshClient(host, port, user, password);
		this._client.Connect();
	}

	/// <summary>
	///     接続を切断します。
	/// </summary>
	public void Disconnect() {
		if (this._client is { IsConnected: true }) {
			this._client.Disconnect();
		}
		this._client?.Dispose();
		this._client = null;
	}

	/// <summary>
	///     コマンドを実行し結果文字列を返します。
	/// </summary>
	/// <param name="command">コマンド。</param>
	/// <returns>標準出力。</returns>
	public string Run(string command) {
		if (this._client is not { IsConnected: true }) {
			throw new InvalidOperationException("SSH not connected.");
		}
		using var cmd = this._client.CreateCommand(command);
		return cmd.Execute();
	}

	/// <summary>
	///     リソースを解放します。
	/// </summary>
	public void Dispose() {
		this.Disconnect();
	}
}
