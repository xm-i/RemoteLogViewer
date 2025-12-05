// 追加読み込み行数
const prefetchLines = 200;
// 追加読み込みのしきい値行数
const prefetchThreshold = 50;
// ログ保持上限行数
const maxLogLines = 1000;

const TextFileViewer = {
	components: { TabArea },
	template: `
		<div class="main-area">
			<div ref="logArea" class="log-area log-container" @scroll="onLogAreaScroll">
				<div v-for="line in logs"
						:key="line.LineNumber"
						ref="row"
						:data-line-number="line.LineNumber"
						class="log-line">
					<span class="line-number" @click="onLineNumberClick(line)">{{ line.LineNumber }}</span>
					<span class="line-content" v-html="line.Content"></span>
				</div>
			</div>
			<div class="scroll-area" ref="scrollArea" @scroll="onVirtualScroll">
				<div class="scroll-virtual-content"></div>
			</div>
		</div>
		<div class="tab-area">
			<tab-area :pageKey="pageKey" ref="tabArea" @grep-line-clicked="grepLineClicked"></tab-area>
		</div>`,
	props: {
		pageKey: null
	},
	data() {
		return {
			// ログファイル情報
			totalLines: 0,
			// 読み込みログ情報
			logs: [],
			maxLineNumber: 0,
			minLineNumber: 0,
			// 読み込み中リクエスト管理
			currentRequestId: 0,
			loadingRequests: [],
			// ログ表示状況
			startLine: 0,
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
				// 読み込み完了
				this.loadingStartLineNumber = null;
				this.loadingEndLineNumber = null;
				// 最小・最大行番号を更新
				this.minLineNumber = this.logs[0]?.LineNumber ?? 0;
				this.maxLineNumber = this.logs[this.logs.length - 1]?.LineNumber ?? 0;
				// 新しい行が追加されたら observer を再セット
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
			this.loadingStartLineNumber = start;
			this.loadingEndLineNumber = end;

			const request = {
				PageKey: this.pageKey,
				Type: "Request",
				RequestId: ++this.currentRequestId,
				Start: start,
				End: end
			}

			this.loadingRequests.push(request);

			this.log(() => `requestLogs id: ${request.RequestId}, start: ${request.Start}, end: ${request.End}, requesting: ${this.loadingRequests.map(x => x.RequestId)}`);
			window.chrome.webview.postMessage(request);
		},
		reset() {
			this.loadingRequests.splice(0);
			this.logs.splice(0);
			this.visibleLines.splice(0);
		},
		// ログ受信時処理
		addLogsFromRequest(requestId, newLogs) {
			if (!this.loadingRequests.find(x => x.RequestId === requestId)) {
				// リクエスト中でなければ、無視する。
				this.log(() => `addLogs skipped [${requestId}]`);
				return;
			}
			this.loadingRequests = this.loadingRequests.filter(x => x.RequestId !== requestId);

			this.log(() => `addLogs start [${requestId}] ${newLogs[0]?.LineNumber}, ${newLogs.length}`);
			if (newLogs.length === 0) {
				return;
			}

			let isScrollUp = false;
			if (newLogs[newLogs.length - 1].LineNumber + 1 === this.minLineNumber) {
				isScrollUp = true;
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
				// ジャンプ先指定あり
				const startLine = this.jumpStartLine;
				this.jumpStartLine = null;

				this.$nextTick(() => {
					const target = this.$refs.row.find(x => Number(x.dataset.lineNumber) === startLine);
					if (!target) {
						return;
					}
					this.$refs.logArea.scrollTop = target.offsetTop;
				});
			} else if (isScrollUp){
				// 上スクロールで且つジャンプ先指定なしの場合、スクロールはvisibleLinesの1行目に合わせる。
				const startLine = this.visibleLines[0];
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
		// ログスクロール監視
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

				// 上方向の事前読み込み処理
				if (this.visibleLines[0] < this.minLineNumber + prefetchThreshold && !this.loadingRequests.find(x => x.End === this.minLineNumber - 1)) {
					this.log(() => `request by intersect ${this.visibleLines[0]}`);
					this.requestLogs(this.minLineNumber - prefetchLines, this.minLineNumber - 1);
				}

				// 下方向の事前読み込み処理
				if (this.visibleLines[this.visibleLines.length - 1] > this.maxLineNumber - prefetchThreshold && !this.loadingRequests.find(x => x.Start === this.maxLineNumber + 1)) {
					this.log(() => `request by intersect ${this.visibleLines[this.visibleLines.length - 1]}`);
					this.requestLogs(this.maxLineNumber + 1, this.maxLineNumber + prefetchLines);
				}
				// 仮想スクロール位置同期
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
			this.loadingRequests.push({ RequestId: 1, Start: 1, End: 100 });
			this.addLogsFromRequest(1, [...Array(100)].map((_, i) => i).map(i => {
				return {
					LineNumber: i + 1, Content: "I have a dream that one day on the red hills of Georgia, the sons of former slaves and the sons of former slave owners will be able to sit down together at the table of brotherhood." };
			}));
			this.totalLines = this.logs.length;
			return;
		}

		// C# → JS 通信
		window.chrome.webview.addEventListener("message", e => {
			const message = e.data;
			if (message.pageKey !== this.pageKey) {
				return;
			}
			switch (message.type) {
				case "Loaded":
					this.addLogsFromRequest(message.data.RequestId, message.data.Content);
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