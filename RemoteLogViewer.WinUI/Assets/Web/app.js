// 追加読み込み行数
const prefetchLines = 200;
// 追加読み込みのしきい値行数
const prefetchThreshold = 50;
// ログ保持上限行数
const maxLogLines = 1000;

const App = {
	components: { TabArea },
	data() {
		return {
			// ログファイル情報
			totalLines: 0,
			// 読み込みログ情報
			logs: [],
			maxLineNumber: 0,
			minLineNumber: 0,
			// ログ表示状況
			startLine: 0,
			// 追加読み込み用 IntersectionObserver
			nextObserver: null,
			previousObserver: null,
			// 仮想スクロール
			virtualScrollTimeout: null,
			logScrollObserver: null,
			visibleLines: [],
			virtualScrollTop: 0,
			// 行指定スクロール
			jumpStartLine: null,
			// 行スタイル
			lineStyle: ""
		}
	},
	watch: {
		logs: {
			handler() {
				// 最小・最大行番号を更新
				this.minLineNumber = this.logs[0]?.LineNumber ?? 0;
				this.maxLineNumber = this.logs[this.logs.length - 1]?.LineNumber ?? 0;
				// 新しい行が追加されたら observer を再セット
				this.setupObserverForNextLoad();
				this.setupObserverForPreviousLoad();
				this.setupObserverForLogScroll();
			},
			deep: true
		}
	},
	methods: {
		// DEBUGログ
		log(createTextFunc) {
			console.log(createTextFunc());
		},
		// 次のチャンクロード
		loadNextChunk() {
			this.log(() => "loadNextChunk start");
			this.requestLogs(this.maxLineNumber + 1, this.maxLineNumber + prefetchLines);
			this.log(() => "loadNextChunk end");
		},
		// 前のチャンクロード
		loadPreviousChunk() {
			this.log(() => "loadPreviousChunk start");
			this.requestLogs(this.minLineNumber - prefetchLines, this.minLineNumber - 1);
			this.log(() => "loadPreviousChunk end");
		},
		// ログ送信をリクエスト
		requestLogs(start, end) {
			start = Math.max(start, 1);
			if (start >= end) {
				return;
			}
			this.log(() => `requestLogs start: ${start}, end: ${end}`);
			window.chrome.webview.postMessage({
				Type: "Request",
				Start: start,
				End: end
			});
		},
		reset() {
			this.logs.splice(0);
			this.visibleLines.splice(0);
		},
		// ログ受信時処理
		addLogs(newLogs) {
			this.log(() => `addLogs start ${newLogs[0]?.LineNumber}, ${newLogs.length}`);
			if (newLogs.length === 0) {
				return;
			}
			if (newLogs[newLogs.length - 1].LineNumber + 1 === this.minLineNumber) {
				this.logs.unshift(...newLogs);
				if (this.logs.length > maxLogLines) {
					const removeCount = this.logs.length - maxLogLines;
					this.logs.splice(maxLogLines, removeCount);
				}
			} else if (newLogs[0].LineNumber - 1 === this.maxLineNumber) {
				this.logs.push(...newLogs);
				if (this.logs.length > maxLogLines) {
					const removeCount = this.logs.length - maxLogLines;
					this.logs.splice(0, removeCount);
				}
			} else {
				this.logs = newLogs;
			}

			if (this.jumpStartLine !== null) {
				const startLine = this.jumpStartLine;
				this.jumpStartLine = null;
				this.$nextTick(() => {
					const target = this.$refs.row.find(x => Number(x.dataset.lineNumber) === startLine);
					if (!target) {
						return;
					}
					this.$refs.logArea.scrollTop = target.offsetTop;
				});
			}
			this.log(() => `addLogs end`);
		},
		// 次のチャンクを読み込むための監視処理
		setupObserverForNextLoad() {
			// 既存の observer があれば解除
			if (this.nextObserver) {
				this.nextObserver.disconnect();
				this.log(() => `nextObserver disconnected`);
			}

			// IntersectionObserver 作成
			this.nextObserver = new IntersectionObserver(entries => {
				if (entries.find(x => x.isIntersecting)) {
					this.nextObserver.disconnect();
					this.log(() => `nextObserver intersecting ${entries[0].target.dataset.lineNumber}`);
					this.loadNextChunk();
				}
			}, {
				root: this.$refs.logArea
			});

			this.$nextTick(() => {
				const rowRefs = this.$refs.row || [];

				const sentinelLine = this.maxLineNumber - prefetchThreshold;
				const sentinelEl = rowRefs.find(
					x => Number(x.dataset.lineNumber) === sentinelLine
				);
				if (sentinelEl) {
					this.log(() => `nextObserver observe ${sentinelEl.dataset.lineNumber}`);
					this.nextObserver.observe(sentinelEl);
				}

				const lastEl = rowRefs.find(
					x => Number(x.dataset.lineNumber) === this.maxLineNumber
				);
				if (lastEl) {
					this.log(() => `nextObserver observe ${lastEl.dataset.lineNumber}`);
					this.nextObserver.observe(lastEl);
				}
			});
		},
		// 前のチャンクを読み込むための監視処理
		setupObserverForPreviousLoad() {
			// 既存の observer があれば解除
			if (this.previousObserver) {
				this.previousObserver.disconnect();
				this.log(() => `previousObserver disconnected`);
			}

			// IntersectionObserver 作成
			this.previousObserver = new IntersectionObserver(entries => {
				if (entries.find(x => x.isIntersecting)) {
					this.previousObserver.disconnect();
					this.log(() => `previousObserver intersecting ${entries[0].target.dataset.lineNumber}`);
					this.loadPreviousChunk();
				}
			}, {
				root: this.$refs.logArea
			});

			this.$nextTick(() => {
				const rowRefs = this.$refs.row || [];

				const sentinelLine = this.minLineNumber + prefetchThreshold;
				const sentinelEl = rowRefs.find(
					x => Number(x.dataset.lineNumber) === sentinelLine
				);
				if (sentinelEl) {
					this.log(() => `previousObserver observe ${sentinelEl.dataset.lineNumber}`);
					this.previousObserver.observe(sentinelEl);
				}

				const firstEl = rowRefs.find(
					x => Number(x.dataset.lineNumber) === this.minLineNumber
				);
				if (firstEl) {
					this.log(() => `previousObserver observe ${firstEl.dataset.lineNumber}`);
					this.previousObserver.observe(firstEl);
				}
			});
		},
		// 仮想スクロール位置同期のためのログスクロール監視
		setupObserverForLogScroll() {
			// 既存の observer があれば解除
			if (this.logScrollObserver) {
				this.logScrollObserver.disconnect();
				this.log(() => `logScrollObserver disconnected`);
			}

			// IntersectionObserver 作成
			this.logScrollObserver = new IntersectionObserver(entries => {
				for (const entry of entries) {
					const line = Number(entry.target.dataset.lineNumber);
					if (entry.isIntersecting) {
						// 表示されたら追加
						if (!this.visibleLines.includes(line)) {
							this.visibleLines.push(line);
						}
					} else {
						// 非表示になったら削除
						const index = this.visibleLines.indexOf(line);
						if (index !== -1) {
							this.visibleLines.splice(index, 1);
						}
					}
				}
				this.visibleLines.sort((a, b) => a - b);

				this.startLine = this.visibleLines[0] || 1;

				this.log(() => `logScrollObserver intersecting / new startLine: ${this.startLine}`);
				const scrollRatio = (this.startLine - 1) / (this.totalLines - this.visibleLines.length);

				this.virtualScrollTop = Math.floor(scrollRatio * (this.$refs.scrollArea.scrollHeight - this.$refs.scrollArea.clientHeight));
				this.$refs.scrollArea.scrollTop = this.virtualScrollTop;
			}, {
				root: this.$refs.logArea
			});
			this.$nextTick(() => {
				const rowRefs = this.$refs.row || [];
				for (const el of rowRefs) {
					this.logScrollObserver.observe(el);
				}
			});
		},
		// 仮想スクロールバースクロール
		onVirtualScroll(e) {
			if (e.target.scrollTop === this.virtualScrollTop) {
				// ログスクロール監視で設定された値と同じ場合はイベント発生させない。
				return;
			}
			this.log(() => `onVirtualScroll start`);
			if (this.virtualScrollTimeout) {
				clearTimeout(this.virtualScrollTimeout);
			}
			this.virtualScrollTimeout = setTimeout(() => {
				const scrollArea = e.target;
				const scrollRatio = scrollArea.scrollTop / (scrollArea.scrollHeight - scrollArea.clientHeight);
				if (scrollRatio === 1) {
					// スクロール割合が100％の場合は、最終行へジャンプ
					this.jumpLine(this.totalLines);
				} else {
					// 表示開始行を計算
					const startLine = Math.floor(scrollRatio * (this.totalLines - this.visibleLines.length)) + 1;
					// ジャンプ
					this.jumpLine(startLine);
				}
			}, 100);
		},
		jumpLine(startLineNumber) {
			if (this.logs.find(x => x.LineNumber == startLineNumber)) {
				const target = this.$refs.row.find(x => Number(x.dataset.lineNumber) === startLineNumber);
				if (!target) {
					return;
				}
				this.$refs.logArea.scrollTop = target.offsetTop;
			} else {
				this.reset();
				this.jumpStartLine = startLineNumber;
				this.requestLogs(startLineNumber - prefetchLines, startLineNumber + prefetchLines);
			}
		},
		onLineNumberClick(line) {
			this.$refs.tabArea.setLine(line);
		},
		grepLineClicked(lineNumber) {
			this.jumpLine(lineNumber);
		}
	},
	mounted() {
		// テスト用
		if (!window.chrome.webview) {
			this.addLogs([...Array(100)].map((_, i) => i).map(i => {
				return {
					LineNumber: i + 1, Content: "I have a dream that one day on the red hills of Georgia, the sons of former slaves and the sons of former slave owners will be able to sit down together at the table of brotherhood." };
			}));
			this.totalLines = this.logs.length;
			return;
		}

		// C# → JS 通信
		window.chrome.webview.addEventListener("message", e => {
			const message = e.data;
			switch (message.type) {
				case "Loaded":
					this.addLogs(message.data);
					break;
				case "TotalLinesUpdated":
					this.totalLines = message.data;
					break;
				case "FileChanged":
					this.reset();
					this.requestLogs(1, prefetchLines * 2);
					break;
				case "ReloadRequested":
					this.jumpLine(this.startLine);
					break;
				case "LineStyleChanged":
					let styleTag = document.getElementById("dynamic-style");
					styleTag.textContent = message.data;
			}
		});
	}
}