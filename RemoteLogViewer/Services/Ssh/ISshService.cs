using System.Collections.Generic;
using System.Threading;

using RemoteLogViewer.Models.Ssh.FileViewer;

using RemoteLogViewer.Models.Ssh.FileViewer.ByteOffsetMap;

namespace RemoteLogViewer.Services.Ssh;

/// <summary>
/// SSH操作インターフェイス。
/// </summary>
public interface ISshService : IDisposable {
	public Observable<Exception> DisconnectedWithExceptionNotification {
		get;
	}

	/// <summary>
	///     パスワード / 鍵認証で接続します。password と privateKeyPath の両方が指定された場合は複数メソッドで試行します。
	/// </summary>
	/// <param name="host">ホスト。</param>
	/// <param name="port">ポート。</param>
	/// <param name="user">ユーザー名。</param>
	/// <param name="password">パスワード (任意)。</param>
	/// <param name="privateKeyPath">秘密鍵パス (任意)。</param>
	/// <param name="privateKeyPassphrase">秘密鍵パスフレーズ (任意)。</param>
	/// <param name="encoding">文字エンコード(CSharpのEncoding.GetEncoding()で取得可能な名称</param>
	public void Connect(string host, int port, string user, string? password, string? privateKeyPath, string? privateKeyPassphrase, string encoding);

	/// <summary>
	///     接続を切断します。
	/// </summary>
	public void Disconnect(Exception? cause = null);

	/// <summary>
	///     コマンドを実行し結果文字列を返します。
	/// </summary>
	/// <param name="command">コマンド。</param>
	/// <returns>標準出力。</returns>
	public string Run(string command);

	/// <summary>
	/// コマンドを非同期実行し、結果文字列を返します。
	/// </summary>
	/// <param name="command">コマンド。</param>
	/// <param name="ct">キャンセルトークン</param>
	/// <returns>標準出力</returns>
	public IAsyncEnumerable<string> RunAsync(string command, CancellationToken ct);

	/// <summary>
	/// SSH サーバー上のディレクトリを一覧表示します。
	/// </summary>
	/// <param name="path">対象ディレクトリのパス。</param>
	/// <returns>ディレクトリエントリ一覧。</returns>
	public FileSystemObject[] ListDirectory(string path);

	/// <summary>
	///     リモート環境で利用可能な iconv のエンコーディング一覧を取得します。
	/// </summary>
	/// <returns>エンコーディング名配列。</returns>
	public string[] ListIconvEncodings();

	/// <summary>
	///     指定した開始行から終了行までの行を取得します。
	/// </summary>
	/// <param name="remoteFilePath">対象ファイルのパス。</param>
	/// <param name="startLine">開始行 (1 始まり)。</param>
	/// <param name="endLine">終了行 (1 始まり)。</param>
	/// <param name="fileEncoding">ソースエンコーディング。</param>
	/// <returns>取得行。</returns>
	public IAsyncEnumerable<TextLine> GetLinesAsync(string remoteFilePath, long startLine, long endLine, string? fileEncoding, ByteOffset byteOffset, CancellationToken ct);

	/// <summary>
	///     grep 検索 を行います。
	/// </summary>
	/// <param name="remoteFilePath">対象ファイル。</param>
	/// <param name="pattern">パターン。</param>
	/// <param name="ignoreCase">大文字小文字無視。</param>
	/// <param name="fileEncoding">ファイルエンコーディング。</param>
	/// <param name="maxResults">取得件数上限。</param>
	/// <param name="byteOffset">検索開始に利用するバイトオフセット。</param>
	/// <param name="startLine">検索開始時点の行番号</param>
	/// <param name="ct">キャンセルトークン。</param>
	/// <returns>一致行。</returns>
	public IAsyncEnumerable<TextLine> GrepAsync(string remoteFilePath, string pattern, bool ignoreCase, string? fileEncoding, int maxResults, ByteOffset byteOffset, long startLine, CancellationToken ct);

	/// <summary>
	///     大規模ファイル向け: 行番号からおおよそのバイトオフセットを取得するためのインデックスを一定間隔で作成します。
	///     返されるByteOffset は <paramref name="remoteFilePath"/> 先頭からのオフセット (改行含む) です。
	///     最終行後(EOF) の行番号 + 1 と最終オフセットも出力されます。
	/// </summary>
	/// <param name="remoteFilePath">対象ファイル 。</param>
	/// <param name="interval">インデックス間隔行数。1 以上。</param>
	/// <param name="ct">キャンセルトークン</param>
	/// <returns>インデックス列挙。</returns>
	public IAsyncEnumerable<ByteOffset> CreateByteOffsetMap(string remoteFilePath, int interval, CancellationToken ct);

	/// <summary>
	///既存のバイトオフセット (<paramref name="startOffset"/>)から指定行番号 (<paramref name="targetLine"/>)まで追加で読み込まれるバイト数を AWKで算出し、新しい <see cref="ByteOffset"/> を返します。
	/// startOffset.LineNumber 行までの内容が既に取得済みである前提で、tail -c +{startOffset.Bytes+1}で残りを読み進めます。
	/// </summary>
	/// <param name="remoteFilePath">対象ファイルパス。</param>
	/// <param name="startOffset">開始バイトオフセット。</param>
	/// <param name="targetLine">新規に算出したい行番号 (startOffset.LineNumber以上)。</param>
	/// <param name="ct">キャンセルトークン。</param>
	/// <returns>対象行までの累積バイトオフセット。</returns>
	public Task<ByteOffset> CreateByteOffsetUntilLineAsync(string remoteFilePath, ByteOffset startOffset, long targetLine, CancellationToken ct);

	/// <summary>
	/// ファイル末尾の新規追記行を取得します。起点バイトオフセット以降の内容をtail -fで追跡し、既存最終行番号以前の行はスキップします。
	/// </summary>
	/// <param name="remoteFilePath">対象ファイル。</param>
	/// <param name="fileEncoding">ファイルエンコーディング。</param>
	/// <param name="startOffset">開始オフセット。</param>
	/// <param name="currentLastLine">現在取得済み最終行番号。</param>
	/// <param name="ct">キャンセルトークン。</param>
	public IAsyncEnumerable<TextLine> TailFollowAsync(string remoteFilePath, string? fileEncoding, ByteOffset startOffset, long currentLastLine, CancellationToken ct);

	/// <summary>
	/// ファイル末尾の新規追記行を取得し、行数のみを返却します。起点バイトオフセット以降の内容をtail -fで追跡し、既存最終行番号以前の行はスキップします。
	/// </summary>
	/// <param name="remoteFilePath">対象ファイル。</param>
	/// <param name="startOffset">開始オフセット。</param>
	/// <param name="currentLastLine">現在取得済み最終行番号。</param>
	/// <param name="ct">キャンセルトークン。</param>
	public IAsyncEnumerable<long> TailFollowAsyncOnlyLineNumber(string remoteFilePath, ByteOffset startOffset, long currentLastLine, CancellationToken ct);
}
