using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using RemoteLogViewer.Composition.Stores.Settings;
using RemoteLogViewer.Core.Stores.Settings;

namespace RemoteLogViewer.Core.Services.Viewer;

[Inject(InjectServiceLifetime.Transient)]
public class HighlightService {
	private readonly SettingsStoreModel _settingsStoreModel;
	private readonly ConcurrentDictionary<(string pattern, bool ignoreCase), Regex> _regexCache = [];
	private (string ClassName, HighlightConditionModel Condition)[] _ruleWithClassName = [];
	public HighlightService(SettingsStoreModel settingsStoreModel) {
		this._settingsStoreModel = settingsStoreModel;
	}

	public string CreateCss() {
		var conditions = this._settingsStoreModel.SettingsModel.HighlightSettings.Rules.SelectMany(x => x.Conditions);
		this._ruleWithClassName = conditions.Select((x, i) => ($"c{i}", x)).ToArray();

		return string.Join("", this._ruleWithClassName.Select(x => {
			var sb = new StringBuilder();
			sb.Append($".{x.ClassName}{{");
			if (x.Condition.ForeColor.Value is { } fore) {
				sb.Append($$"""color:rgba({{fore.R}},{{fore.G}},{{fore.B}},{{fore.A}});""");
			}
			if (x.Condition.BackColor.Value is { } back) {
				sb.Append($$"""background:rgba({{back.R}},{{back.G}},{{back.B}},{{back.A}});""");
			}
			sb.Append("}");
			return sb.ToString();
		}));
	}

	public string CreateStyledLine(string content) {
		// 行全体スタイルの決定
		string? lineClass = null;
		foreach (var lineCondition in this._ruleWithClassName.Where(x => !x.Condition.HighlightOnlyMatch.Value)) {
			var condition = lineCondition.Condition;
			if (condition.PatternType.Value == HighlightPatternType.Regex) {
				var regex = this.GetCachedRegex(condition.Pattern.Value, condition.IgnoreCase.Value);
				if (regex == null) {
					continue;
				}
				if (regex.IsMatch(content)) {
					lineClass = lineCondition.ClassName;
					break;
				}
			} else if (condition.PatternType.Value == HighlightPatternType.Exact) {
				var comparison = condition.IgnoreCase.Value ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

				if (content.IndexOf(condition.Pattern.Value, comparison) >= 0) {
					lineClass = lineCondition.ClassName;
					break;
				}
			}
		}

		// ポイントスタイルの決定
		List<PointStyle> wordStyles = [];

		foreach (var (wordCondition, priority) in this._ruleWithClassName.Where(x => x.Condition.HighlightOnlyMatch.Value).Select((x, i) => (x, i))) {
			var condition = wordCondition.Condition;
			if (condition.PatternType.Value == HighlightPatternType.Regex) {
				var regex = this.GetCachedRegex(condition.Pattern.Value, condition.IgnoreCase.Value);
				foreach (Match m in regex.Matches(content)) {
					if (!m.Success || m.Length == 0) {
						continue;
					}
					wordStyles.Add(new(wordCondition.ClassName, m.Index, m.Index + m.Length - 1, priority));
				}
			} else if (condition.PatternType.Value == HighlightPatternType.Exact) {
				var comparison = condition.IgnoreCase.Value ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
				var searchStart = 0;
				while (searchStart < content.Length) {
					var idx = content.IndexOf(condition.Pattern.Value, searchStart, comparison);
					if (idx < 0) {
						break;
					}
					wordStyles.Add(new(wordCondition.ClassName, idx, idx + condition.Pattern.Value.Length - 1, priority));
					searchStart = idx + condition.Pattern.Value.Length;
				}
			}
		}

		List<PointStyle> mergedWordStyles = [];
		foreach (var wsg in wordStyles.GroupBy(x => x.ClassName)) {
			List<PointStyle> merged = [];
			foreach (var ws in wsg) {
				var target = merged.FirstOrDefault(x => (x.Start >= ws.Start && x.Start <= ws.End + 1) || (x.End + 1 >= ws.Start && x.End <= ws.End));
				if (target != null) {
					target.Start = Math.Min(target.Start, ws.Start);
					target.End = Math.Max(target.End, ws.End);
				} else {
					merged.Add(ws);
				}
			}
			mergedWordStyles.AddRange(merged);
		}

		var sb = new StringBuilder();

		if (lineClass != null) {
			sb.Append($@"<span class=""{lineClass}"">");
		}

		var styledIndex = -1;
		List<PointStyle> applying = [];
		// スタイルの付け替えが発生する可能性のあるindexを列挙
		foreach (var index in mergedWordStyles.SelectMany(x => new int[] { x.Start, x.End + 1 }).OrderBy(x => x).Distinct()) {
			sb.Append(Escape(content[(styledIndex + 1)..index]));
			styledIndex = index - 1;

			var starts = mergedWordStyles.Where(x => x.Start == index).OrderByDescending(x => x.Priority).ToArray();
			var ends = mergedWordStyles.Where(x => x.End + 1 == index).OrderByDescending(x => x.Priority).ToArray();

			// 今回終了
			foreach (var end in ends) {
				sb.Append("</span>");
				applying.Remove(end);
			}

			List<PointStyle> requiredOpenStyles = [];
			// 今回適用開始/終了するスタイルよりも優先度が高いものは一旦閉じる (Priority数値が低いほど優先度が高い)
			foreach (var ap in applying.Where(x => (starts.Length>0 && x.Priority < starts[0].Priority) || (ends.Length > 0 && x.Priority < ends[0].Priority))) {
				sb.Append("</span>");
				requiredOpenStyles.Add(ap);
			}

			// 今回開始
			for (var si = 0; si < starts.Length; si++) {
				// 今回のスタイルを適用
				applying.Add(starts[si]);
				requiredOpenStyles.Add(starts[si]);
			}

			// 開始タグ追加
			foreach (var ap in requiredOpenStyles.OrderBy(x => x.Priority)) {
				sb.Append($@"<span class=""{ap.ClassName}"">");
			}
		}
		sb.Append(Escape(content[(styledIndex + 1)..]));

		if (lineClass != null) {
			sb.Append("</span>");
		}

		return sb.ToString();
	}


	private static string Escape(string s) {
		return WebUtility.HtmlEncode(s);
	}

	private Regex GetCachedRegex(string pattern, bool ignoreCase) {
		var key = (pattern, ignoreCase);
		if (this._regexCache.TryGetValue(key, out var rx)) {
			return rx;
		}
		rx = new Regex(pattern, (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) | RegexOptions.Compiled | RegexOptions.Multiline);
		this._regexCache[key] = rx;
		return rx;
	}
	private class PointStyle {
		public PointStyle(string className, int start, int end, int priority) {
			this.ClassName = className;
			this.Start = start;
			this.End = end;
			this.Priority = priority;
		}

		public string ClassName {
			get;
			set;
		}

		public int Start {
			get;
			set;
		}

		public int End {
			get;
			set;
		}
		public int Priority {
			get;
			set;
		}
	}
}
