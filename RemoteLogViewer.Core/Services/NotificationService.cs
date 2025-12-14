namespace RemoteLogViewer.Core.Services;

/// <summary>
/// ユーザー通知 (エラー含む) を提供するサービスです。Publish された通知を購読できます。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class NotificationService {
	private readonly Subject<NotificationInfo> _notificationsSubject = new();
	/// <summary>通知ストリーム。</summary>
	public Observable<NotificationInfo> Notifications {
		get {
			field ??= this._notificationsSubject.AsObservable();
			return field;
		}
	}

	public readonly Subject<NotificationInfoWithAction> _notificationWithActionsSubject = new();
	/// <summary>
	/// アクション付き通知ストリーム。
	/// </summary>
	public Observable<NotificationInfoWithAction> NotificationWithActions {
		get {
			field ??= this._notificationWithActionsSubject.AsObservable();
			return field;
		}
	}

	/// <summary>
	/// 通知を送出します。
	/// </summary>
	/// <param name="source">発生元。</param>
	/// <param name="message">表示メッセージ。</param>
	/// <param name="severity">重大度。</param>
	/// <param name="ex">例外 (任意)。</param>
	public void Publish(string source, string message, NotificationSeverity severity, Exception? ex = null) {
		this._notificationsSubject.OnNext(new NotificationInfo(DateTimeOffset.UtcNow, source, message, severity, ex));
	}

	/// <summary>
	/// アクション付き通知を送出します。
	/// </summary>
	/// <param name="source">発生元。</param>
	/// <param name="message">表示メッセージ。</param>
	/// <param name="severity">重大度。</param>
	/// <param name="ex">例外 (任意)。</param>
	public void Publish(string source, string message, NotificationSeverity severity, string primaryActionText, Action primaryAction, string secondaryActionText, Action secondaryAction, Exception? ex = null) {
		this._notificationWithActionsSubject.OnNext(new NotificationInfoWithAction(DateTimeOffset.UtcNow, source, message, severity, primaryActionText, primaryAction, secondaryActionText, secondaryAction, ex));
	}
}

/// <summary>通知重大度。</summary>
public enum NotificationSeverity {
	Info, Warning, Error, Critical
}
/// <summary>通知情報。</summary>
public readonly record struct NotificationInfo(DateTimeOffset OccurredAt, string Source, string Message, NotificationSeverity Severity, Exception? Exception);

/// <summary>アクション付き通知情報。</summary>
public readonly record struct NotificationInfoWithAction(DateTimeOffset OccurredAt, string Source, string Message, NotificationSeverity Severity, string PrimaryActionText, Action PrimaryAction, string SecondaryActionText, Action SecondaryAction, Exception? Exception);