using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Microsoft.UI.Xaml.Documents;

using RemoteLogViewer.Composition.Stores.Settings;
using RemoteLogViewer.Stores.Settings;

namespace RemoteLogViewer.Services.Viewer;

[AddTransient]
public class HighlightService {
	private readonly SettingsStoreModel _settingsStoreModel;
	private readonly ConcurrentDictionary<(string pattern, bool ignoreCase), Regex> _regexCache = [];
	public HighlightService(SettingsStoreModel settingsStoreModel) {
		this._settingsStoreModel = settingsStoreModel;
	}

	private Regex? GetCachedRegex(string pattern, bool ignoreCase) {
		var key = (pattern, ignoreCase);
		if (this._regexCache.TryGetValue(key, out var rx)) {
			return rx;
		}
		try {
			rx = new Regex(pattern, (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) | RegexOptions.Compiled | RegexOptions.Multiline);
			this._regexCache[key] = rx;
			return rx;
		} catch {
			return null;
		}
	}

	public IList<HighlightSpan> ComputeHighlightSpans(string content) {
		// ハイライト適用箇所の抽出
		var segments = this.CreateHighlightSegment(content);
		if (segments.Count == 0) {
			return [];
		}

		// 抽出されたハイライト適用箇所から、スタイル種別ごとに位置被りを取り除いたスタイルを作成
		var styleTypes = this.CreateStyles(segments, content);

		// スタイル種別ごとに作成されたスタイルをマージして戻り値作成
		var result = this.CreateMergedStyle(styleTypes);

		return result;
	}

	/// <summary>
	/// ハイライト適用箇所の抽出
	/// </summary>
	/// <param name="content">ハイライト対象文字列</param>
	/// <returns>ハイライト適用箇所リスト</returns>
	private List<HighlightSegment> CreateHighlightSegment(string content) {
		if (string.IsNullOrEmpty(content)) {
			return [];
		}

		// ハイライト条件適用箇所の抽出
		var segments = new List<HighlightSegment>();
		foreach (var rule in this._settingsStoreModel.SettingsModel.HighlightSettings.Rules) {
			foreach (var condition in rule.Conditions) {
				var pattern = condition.Pattern.Value;
				if (string.IsNullOrWhiteSpace(pattern)) {
					continue;
				}
				if (condition.PatternType.Value == HighlightPatternType.Regex) {
					var regex = this.GetCachedRegex(pattern, condition.IgnoreCase.Value);
					if (regex == null) {
						continue;
					}
					foreach (Match m in regex.Matches(content)) {
						if (!m.Success || m.Length == 0) {
							continue;
						}
						segments.Add(new HighlightSegment(m.Index, m.Index + m.Length - 1, new TextStyle {
							ForeColor = condition.ForeColor.Value,
							BackColor = condition.BackColor.Value
						}, condition.HighlightOnlyMatch.Value));
					}
				} else if (condition.PatternType.Value == HighlightPatternType.Exact) {
					var comparison = condition.IgnoreCase.Value ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
					var searchStart = 0;
					while (searchStart < content.Length) {
						var idx = content.IndexOf(pattern, searchStart, comparison);
						if (idx < 0) {
							break;
						}
						segments.Add(new HighlightSegment(idx, idx + pattern.Length - 1, new TextStyle {
							ForeColor = condition.ForeColor.Value,
							BackColor = condition.BackColor.Value
						}, condition.HighlightOnlyMatch.Value));
						searchStart = idx + pattern.Length;
					}
				}
			}
		}

		return segments;
	}

	/// <summary>
	/// スタイル種別ごとの位置被りを取り除いたスタイル作成
	/// </summary>
	/// <param name="segments">ハイライト適用箇所リスト</param>
	/// <param name="content">ハイライト適用対象文字列</param>
	/// <returns>位置被りのないスタイル種別ごとのスタイル</returns>
	private SingleStyle[] CreateStyles(IList<HighlightSegment> segments, string content) {
		// 優先度順に重複排除しつつ戻り値作成
		var lineStyles = segments.Where(x => !x.OnlyMatch).Select(x => {
			var start = Math.Max(0, content.LastIndexOf('\n', x.Start) + 1);
			var nextLf = content.IndexOf('\n', x.End);
			var end = nextLf == -1 ? content.Length : nextLf;
			return new HighlightSegment(start, end, x.Style, x.OnlyMatch);
		});

		var pointStyles = segments.Where(x => x.OnlyMatch).OrderBy(s => s.Start).ThenByDescending(s => s.End);
		var styleTypes = new SingleStyle[] {
			new(){
				IsTargetPredicate = x => x.Style.ForeColor is not null,
				SetAction = (to,from) =>to.ForeColor = from.ForeColor
			},
			new(){
				IsTargetPredicate = x => x.Style.BackColor is not null,
				SetAction = (to,from) =>to.BackColor = from.BackColor
			}
		};

		foreach (var st in styleTypes) {
			var cursor = -1;
			foreach (var style in pointStyles.Where(st.IsTargetPredicate)) {
				if (style.Start > cursor) {
					// 前の行~今回ポイントスタイル適用部分までの行
					var previousLineStyles = lineStyles.Where(st.IsTargetPredicate).Where(ls => ls.Start < style.Start && ls.End > cursor);
					foreach (var previousLineStyle in previousLineStyles) {
						var start = Math.Max(cursor + 1, previousLineStyle.Start);
						var length = Math.Min(previousLineStyle.End, style.Start - 1) - start + 1;
						var rs = new RangeStyle() {
							Range = new TextRange(start, length),
						};
						st.SetAction(rs.Style, previousLineStyle.Style);
						st.RangeStyles.Add(rs);
					}
				}

				// ポイントスタイル適用部分
				var pStart = Math.Max(cursor + 1, style.Start);
				var pLength = style.End - pStart + 1;
				var prs = new RangeStyle() {
					Range = new TextRange(pStart, pLength),
				};
				st.SetAction(prs.Style, style.Style);
				st.RangeStyles.Add(prs);
				cursor = style.End;
			}

			// 残り行
			foreach (var previousLineStyle in lineStyles.Where(st.IsTargetPredicate).Where(ls => ls.End > cursor)) {
				var start = Math.Max(cursor + 1, previousLineStyle.Start);
				var length = previousLineStyle.End - start + 1;
				var rs = new RangeStyle() {
					Range = new TextRange(start, length),
				};
				st.SetAction(rs.Style, previousLineStyle.Style);
				st.RangeStyles.Add(rs);
			}
		}

		return styleTypes;
	}

	/// <summary>
	/// スタイル種別ごとに作成されたスタイルをマージ
	/// </summary>
	/// <param name="styleTypes">位置被りのないスタイル種別ごとのスタイル</param>
	/// <returns>位置の被りのない複数のスタイル種別を含むスタイル</returns>
	private IList<HighlightSpan> CreateMergedStyle(SingleStyle[] styleTypes) {
		var starts = styleTypes.SelectMany(s => s.RangeStyles).Select(x => x.Range).OrderBy(x => x.StartIndex).Select(x => x.StartIndex);
		var ends = styleTypes.SelectMany(s => s.RangeStyles).Select(x => x.Range).OrderBy(x => x.StartIndex + x.Length - 1).Select(x => x.StartIndex + x.Length - 1);
		var startIndexes = starts.Concat(ends.Select(x => x + 1))
			.Distinct().OrderBy(x => x).ToArray();

		var ranges = startIndexes
			.Zip(startIndexes.Skip(1), (start, nextStart) => new TextRange(start, nextStart - start))
			.Select(range => {
				var rangeStyles = styleTypes.Select(x => (styleType: x, rangeStyle: x.GetRangeStyle(range.StartIndex))).Where(x => x.rangeStyle != null).ToArray();
				if (rangeStyles.Length == 0) {
					// スタイル無し
					return null;
				}
				var hs = new HighlightSpan {
					Ranges = [range],
				};
				foreach (var (styleType, rangeStyle) in rangeStyles) {
					styleType.SetAction(hs.Style, rangeStyle!.Style);
				}
				return hs;
			}).Where(x => x != null)
			.Select(x => x!);

		return [.. ranges];
	}

	private record HighlightSegment(int Start, int End, TextStyle Style, bool OnlyMatch);

	private class SingleStyle() {
		public IList<RangeStyle> RangeStyles {
			get;
		} = [];

		/// <summary>
		/// 対象判定
		/// </summary>
		public required Func<HighlightSegment, bool> IsTargetPredicate {
			get;
			init;
		}

		/// <summary>
		/// スタイル設定アクション
		/// </summary>
		public required Action<TextStyle, TextStyle> SetAction {
			get;
			init;
		}

		public RangeStyle? GetRangeStyle(int index) {
			return this.RangeStyles.FirstOrDefault(x => x.Range.StartIndex <= index && index <= x.Range.StartIndex + x.Range.Length - 1);
		}

	}

	private class RangeStyle {
		public required TextRange Range {
			get;
			init;
		}
		public TextStyle Style {
			get;
		} = new();
	}
}
