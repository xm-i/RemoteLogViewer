<template>
	<div class="text-file-viewer">
		<div class="main-area">
			<div ref="logArea" class="log-area log-container" :class="{'log-wrap-lines': wrapLines}">
				<div v-for="line in logs"
					:key="line.lineNumber"
					ref="row"
					:data-line-number="line.lineNumber"
					class="log-line">
					<span class="line-number" @click="onLineNumberClick(line)">{{ line.lineNumber }}</span>
					<span class="line-content" v-html="line.content"></span>
				</div>
			</div>
			<div class="scroll-area" ref="scrollArea" @scroll="onVirtualScroll" v-show="!isDisconnected">
				<div class="scroll-virtual-content"></div>
			</div>
		</div>
		<div class="tab-area">
			<TabArea :pageKey="pageKey"
				ref="tabArea"
				@grep-line-clicked="grepLineClicked"
				:isDisconnected="isDisconnected" />
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, nextTick, onMounted, watch } from 'vue';
import TabArea from './TabArea.vue';
import type { TextLine } from '@/types';
import { RequestWebMessage } from '@/types/outgoingMessages';

interface Props {
	pageKey: string
	isDisconnected?: boolean,
  wrapLines: boolean
}

const props = withDefaults(defineProps<Props>(), {
	isDisconnected: false,
  wrapLines: false
});

// 追加読み込み行数
const prefetchLines = 200;
// 追加読み込みのしきい値行数
const prefetchThreshold = 50;
// ログ保持上限行数
const maxLogLines = 1000;

// テンプレート参照
const logArea = ref<HTMLElement>();
const scrollArea = ref<HTMLElement>();
const tabArea = ref<InstanceType<typeof TabArea>>();
const row = ref<HTMLElement[]>([]);

// データ
// ログファイル状態
const totalLines = ref(0);
// 読み込みログ情報
const logs = ref<TextLine[]>([]);
const maxLineNumber = ref(0);
const minLineNumber = ref(0);
// 読み込み中リクエスト管理
const currentRequestId = ref(0);
const loadingRequests = ref<RequestWebMessage[]>([]);
// ログ表示状況
const startLine = ref(0);
// 仮想スクロール
const virtualScrollTimeout = ref<NodeJS.Timeout | null>(null);
const logScrollObserver = ref<IntersectionObserver | null>(null);
const visibleLines = ref<number[]>([]);
const virtualScrollTop = ref(0);
// 行指定スクロール
const jumpStartLine = ref<number | null>(null);

// DEBUGログ
const log = (createTextFunc: () => string) => {
	console.log(createTextFunc());
};

// ログ送信リクエスト
const requestLogs = (start: number, end: number) => {
	start = Math.max(start, 1);
	if (start >= end) {
		return;
	}

	const request = {
		pageKey: props.pageKey,
		type: 'Request',
		requestId: ++currentRequestId.value,
		start: start,
		end: end
	} as const;

	loadingRequests.value.push(request);

	log(() => `requestLogs id: ${request.requestId}, start: ${request.start}, end: ${request.end}, requesting: ${loadingRequests.value.map(x => x.requestId)}`);
	if (window.chrome?.webview) {
		window.chrome.webview.postMessage(request);
	}
};

// ログ追加
const addLogsFromRequest = (requestId: number, newLogs: TextLine[]) => {
	if (!loadingRequests.value.find(x => x.requestId === requestId)) {
		// リクエスト中でなければ、無視する。
		log(() => `addLogs skipped [${requestId}]`);
		return;
	}

	// リクエスト削除
	loadingRequests.value = loadingRequests.value.filter(x => x.requestId !== requestId);

	log(() => `addLogs start [${requestId}] ${newLogs[0]?.lineNumber}, ${newLogs.length}`);
	if (newLogs.length === 0) {
		return;
	}

	let isScrollUp = false;
	if (newLogs[newLogs.length - 1].lineNumber + 1 === minLineNumber.value) {
		isScrollUp = true;
		logs.value.unshift(...newLogs);
		if (logs.value.length > maxLogLines) {
			const removeCount = logs.value.length - maxLogLines;
			logs.value.splice(maxLogLines, removeCount);
		}
	} else if (newLogs[0].lineNumber - 1 === maxLineNumber.value) {
		logs.value.push(...newLogs);
		if (logs.value.length > maxLogLines) {
			const removeCount = logs.value.length - maxLogLines;
			logs.value.splice(0, removeCount);
		}
	} else {
		logs.value = newLogs;
	}

	if (jumpStartLine.value !== null) {
		// ジャンプ先指定あり
		const startLine = jumpStartLine.value;
		jumpStartLine.value = null;

		nextTick(() => {
			const target = row.value.find(x => Number(x.dataset.lineNumber) === startLine);
			if (!target) {
				return;
			}
			logArea.value!.scrollTop = target.offsetTop;
		});
	} else if (isScrollUp){
		// 上スクロールで且つジャンプ先指定なしの場合、スクロールはvisibleLinesの1行目に合わせる。
		const startLine = visibleLines.value[0];
		nextTick(() => {
			const target = row.value.find(x => Number(x.dataset.lineNumber) === startLine);
			if (!target) {
				return;
			}
			logArea.value!.scrollTop = target.offsetTop;
		});
	}
	log(() => 'addLogs end');
};

// リセット
const reset = () => {
	logs.value = [];
	loadingRequests.value = [];
	visibleLines.value = [];
};

// ログスクロール監視設定
const setupObserverForLogScroll = () => {
	// 既存の observer があれば解除
	if (logScrollObserver.value) {
		logScrollObserver.value.disconnect();
		log(() => 'logScrollObserver disconnected');
	}

	logScrollObserver.value = new IntersectionObserver((entries) => {
		for (const entry of entries) {
			const line = Number((entry.target as HTMLElement).dataset.lineNumber);
			if (entry.isIntersecting) {
				// 表示されたら追加
				if (!visibleLines.value.includes(line)) {
					visibleLines.value.push(line);
				}
			} else {
				// 非表示になったら削除
				const index = visibleLines.value.indexOf(line);
				if (index !== -1) {
					visibleLines.value.splice(index, 1);
				}
			}
		}
		visibleLines.value.sort((a, b) => a - b);

		// 切断状態では何もしない。
		if (props.isDisconnected) {
			return;
		}

		// 上方向の事前読み込み処理
		if (visibleLines.value[0] < minLineNumber.value + prefetchThreshold &&
				!loadingRequests.value.find(x => x.end === minLineNumber.value - 1)) {
			log(() => `request by intersect ${visibleLines.value[0]}`);
			requestLogs(minLineNumber.value - prefetchLines, minLineNumber.value - 1);
		}

		// 下方向の事前読み込み処理
		if (visibleLines.value[visibleLines.value.length - 1] > maxLineNumber.value - prefetchThreshold &&
				!loadingRequests.value.find(x => x.start === maxLineNumber.value + 1)) {
			log(() => `request by intersect ${visibleLines.value[visibleLines.value.length - 1]}`);
			requestLogs(maxLineNumber.value + 1, maxLineNumber.value + prefetchLines);
		}

		// 仮想スクロール位置同期
		startLine.value = visibleLines.value[0] || 1;

		log(() => `logScrollObserver intersecting / new startLine: ${startLine.value}`);
		const scrollRatio = (startLine.value - 1) / (totalLines.value - visibleLines.value.length);

		if (scrollArea.value) {
			virtualScrollTop.value = Math.floor(scrollRatio * (scrollArea.value.scrollHeight - scrollArea.value.clientHeight));
			scrollArea.value.scrollTop = virtualScrollTop.value;
		}
	}, {
		root: logArea.value
	});

	nextTick(() => {
		const rowRefs = row.value || [];
		for (const el of rowRefs) {
			logScrollObserver.value?.observe(el);
		}
	});
};

// 仮想スクロール
const onVirtualScroll = (e: Event) => {
	const target = e.target as HTMLElement;
	if (target.scrollTop === virtualScrollTop.value) {
		return;
	}

	log(() => 'onVirtualScroll start');
	if (virtualScrollTimeout.value) {
		clearTimeout(virtualScrollTimeout.value);
	}

	virtualScrollTimeout.value = setTimeout(() => {
		const scrollRatio = target.scrollTop / (target.scrollHeight - target.clientHeight);
		if (scrollRatio === 1) {
			jumpLine(totalLines.value);
		} else {
			const startLineNumber = Math.floor(scrollRatio * (totalLines.value - visibleLines.value.length)) + 1;
			jumpLine(startLineNumber);
		}
	}, 100);
};

// 行ジャンプ
const jumpLine = (startLineNumber: number) => {
	const targetLine = logs.value.find(x => x.lineNumber === startLineNumber);
	if (targetLine) {
		const target = row.value.find(x => Number(x.dataset.lineNumber) === startLineNumber);
		if (target && logArea.value) {
			logArea.value.scrollTop = target.offsetTop;
		}
	} else {
		reset();
		jumpStartLine.value = startLineNumber;
		requestLogs(startLineNumber - prefetchLines, startLineNumber + prefetchLines);
	}
};

// 行番号クリック
const onLineNumberClick = (line: TextLine) => {
	tabArea.value?.setLine(line);
};

// Grepライン クリック
const grepLineClicked = (lineNumber: number) => {
	jumpLine(lineNumber);
};

// ウォッチャー
watch(logs, () => {
	minLineNumber.value = logs.value[0]?.lineNumber ?? 0;
	maxLineNumber.value = logs.value[logs.value.length - 1]?.lineNumber ?? 0;
	setupObserverForLogScroll();
}, { deep: true });

onMounted(() => {
	// テスト用
	if (!window.chrome?.webview) {
		loadingRequests.value.push({ pageKey: props.pageKey, type: 'Request',requestId: 1, start: 1, end: 100 });
		addLogsFromRequest(1, [...Array(100)].map((_, i) => ({
			lineNumber: i + 1,
			content: 'I have a dream that one day on the red hills of Georgia, the sons of former slaves and the sons of former slave owners will be able to sit down together at the table of brotherhood.'
		})));
		totalLines.value = logs.value.length;
		return;
	}

	// C# → JS 通信
	window.chrome.webview.addEventListener('message', e => {
		const message = e.data;
		if (message.pageKey !== props.pageKey) {
			return;
		}
		switch (message.type) {
			case 'Loaded':
				addLogsFromRequest(message.data.requestId, message.data.content);
				break;
			case 'TotalLinesUpdated':
				totalLines.value = message.data;
				break;
			case 'FileChanged':
				reset();
				requestLogs(1, prefetchLines * 2);
				break;
      case 'ReloadRequested':
        reset();
				jumpLine(startLine.value);
				break;
		}
	});
});
</script>

<style scoped>
  .text-file-viewer {
    flex-shrink: 1;
    flex-grow: 1;
    overflow: hidden;

    .main-area {
      width: 100%;
      flex-direction: row;
      display: flex;
      position: relative;
      /* 左：ログ表示 */
      .log-area {
        white-space: pre;
        border-collapse: collapse;
        flex: 1;
        overflow: scroll;
        width: 100%;
      }
      /* 右：スクロールバー */
      .scroll-area {
        position: absolute;
        right: 0;
        width: 20px;
        height: 100%;
        border-radius: 5px;
        cursor: pointer;
        overflow: auto;

        .scroll-virtual-content {
          height: 10000px;
        }
      }
    }
  }
  .log-wrap-lines .line-content {
    white-space: pre-wrap;
    min-width: 0;
  }
</style>
