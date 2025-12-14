<template>
	<div class="grep-tab">
		<div class="progress-bar-container">
			<div class="progress-bar"
					 :style="{ width: progress + '%' }"></div>
		</div>

		<div class="grep-toolbar">
			<div>
				Search Word:<br>
				<input type="text"
							 v-model="keyword"
							 placeholder="word"
							 class="grep-keyword" />
				<button @click="startGrepFirst" :disabled="isGrepRunning || clientOperating || isDisconnected">Search</button>
				<button @click="cancelGrep" :disabled="!isGrepRunning || clientOperating || isDisconnected">Cancel</button>
			</div>

			<div>
				Load Next Lines:<br>
				<input type="number"
							 v-model="grepNextStartLine"
							 placeholder="100"
							 class="grep-next-start-line"
							 min="1" />

				<button @click="startGrepNext" :disabled="isGrepRunning || clientOperating || isDisconnected">Next</button>
			</div>

			<div>
				Use Regex:<br>
				<input type="checkbox" v-model="useRegex" :disabled="isGrepRunning || isDisconnected" />
			</div>

			<div>
				Ignore Case:<br>
				<input type="checkbox" v-model="ignoreCase" :disabled="isGrepRunning || isDisconnected" />
			</div>

			<div class="grep-results-count">
				results: {{ logs.length }}
			</div>
		</div>

		<div class="grep-results log-container">
			<div v-for="line in logs"
					 :key="line.lineNumber"
					 ref="row"
					 :data-line-number="line.lineNumber"
					 class="log-line">
				<span class="line-number" @click="lineClick(line.lineNumber)">{{ line.lineNumber }}</span>
				<span class="line-content" v-html="line.content" tabindex="-1"></span>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
	import { ref, watch, onMounted } from 'vue';
	import type { TextLine } from '@/types';

	interface Props {
		pageKey: string
		isDisconnected?: boolean
	}

	const props = withDefaults(defineProps<Props>(), {
		isDisconnected: false
	});

	const emit = defineEmits<{
		'line-clicked': [lineNumber: number]
	}>();

	const keyword = ref('');
	const progress = ref(0);
	const isGrepRunning = ref(false);
	const grepNextStartLine = ref(1);
	const logs = ref<TextLine[]>([]);
	const clientOperating = ref(false);
	const requestId = ref(0);
	const useRegex = ref(false);
	const ignoreCase = ref(false);

	const addResult = (data: TextLine[]) => {
		logs.value.push(...data);
	};

	const reset = () => {
		logs.value = [];
		progress.value = 0;
	};

	const startGrepNext = () => {
		if (!Number.isInteger(grepNextStartLine.value)) {
			alert('Start Line must be an integer.');
			return;
		}
		startGrep(grepNextStartLine.value);
	};

	const startGrepFirst = () => {
		startGrep(1);
	};

	const startGrep = (startLine: number) => {
		progress.value = 0;
		clientOperating.value = true;
		if (window.chrome?.webview) {
			window.chrome.webview.postMessage({
				pageKey: props.pageKey,
				type: 'StartGrep',
				requestId: ++requestId.value,
				keyword: keyword.value,
				startLine: startLine,
				ignoreCase: ignoreCase.value,
				useRegex: useRegex.value
			});
		}
	};

	const cancelGrep = () => {
		progress.value = 0;
		clientOperating.value = true;
		if (window.chrome?.webview) {
			window.chrome.webview.postMessage({
				pageKey: props.pageKey,
				type: 'CancelGrep',
				requestId: ++requestId.value
			});
		}
	};

	const lineClick = (lineNumber: number) => {
		emit('line-clicked', lineNumber);
	};

	watch(clientOperating, () => {
		if (!clientOperating.value) {
			return;
		}
		setTimeout(() => {
			clientOperating.value = false;
		}, 100);
	});

	onMounted(() => {
		if (!window.chrome?.webview) {
			return;
		}

		// C# → JS
		window.chrome.webview.addEventListener('message', e => {
			const message = e.data;
			if (message.pageKey !== props.pageKey) {
				return;
			}
			switch (message.type) {
				case 'GrepResultAdded':
					addResult(message.data);
					break;
				case 'GrepResultReset':
					reset();
					break;
				case 'GrepProgressUpdated':
					progress.value = message.data * 100;
					break;
				case 'GrepStartLineUpdated':
					grepNextStartLine.value = message.data;
					break;
				case 'IsGrepRunningUpdated':
					isGrepRunning.value = message.data;
					clientOperating.value = false;
					break;
			}
		});
	});
</script>

<style scoped>
	.grep-tab {
		height: 100%;
		display: flex;
		flex-direction: column;

		.grep-toolbar {
			display: inline-flex;
			flex-direction: row;
			gap: 8px;
			padding: 8px;

			.grep-start-line {
				width: 60px;
			}

			.grep-results-count {
				display: inline-flex;
				align-items: center;
			}
		}

		.grep-results {
			flex-grow: 1;
			overflow: scroll;
		}
	}
</style>
