const GrepTab = {
	template: `
<div id="grep-tab">
	<div id="grep-progress-bar-container">
		<div
			id="grep-progress-bar"
			:style="{ width: progress + '%' }"
		></div>
	</div>

	<div id="grep-toolbar">

		<span>
			Search Word:<br>
			<input
				type="text"
				v-model="keyword"
				placeholder="word"
				id="grep-keyword"
			/>
		</span>

		<button @click="startGrepFirst" :disabled="isGrepRunning || clientOperationg">Search</button>

		<span>
			Next Start Line:<br>
			<input
				type="number"
				v-model="grepNextStartLine"
				placeholder="100"
				id="grep-next-start-line"
				min="1"
			/>
		</span>

		<button @click="startGrepNext" :disabled="isGrepRunning || clientOperationg">Next</button>

		<button @click="cancelGrep" :disabled="!isGrepRunning || clientOperationg">Cancel</button>

		<span class="grep-results-count">
			results: {{ logs.length }}
		</span>
	</div>

	<div id="grep-results" class="log-container">
		<div
			v-for="line in logs"
			:key="line.LineNumber"
			ref="row"
			:data-line-number="line.LineNumber"
			class="log-line">
			<span class="line-number" @click="lineClick(line.LineNumber)">{{ line.LineNumber }}</span>
			<span class="line-content" v-html="line.Content"></span>
		</div>
	</div>

</div>
  `,
	data() {
		return {
			keyword: "",
			progress: 0,
			isGrepRunning: false,
			grepNextStartLine: 1,
			logs: [],
			clientOperationg: false
		};
	},
	watch: {
		clientOperationg: {
			handler() {
				if (!this.clientOperationg) {
					return;
				}
				setTimeout(() => {
					this.clientOperationg = false;
				}, 100);
			}
		}
	},
	methods: {
		addResult(data) {
			this.logs.push(...data);
		},
		reset() {
			this.logs.splice(0);
			this.progress = 0;
		},
		startGrepNext() {
			if (!Number.isInteger(this.grepNextStartLine)) {
				alert("Start Line must be an integer.");
				return;
			}
			this.startGrep(this.grepNextStartLine);
		},
		startGrepFirst() {
			this.startGrep(1);
		},
		startGrep(startLine) {
			this.progress = 0;
			this.clientOperationg = true;
			window.chrome.webview.postMessage({
				Type: "StartGrep",
				Keyword: this.keyword,
				StartLine: startLine
			});
		},
		cancelGrep() {
			this.progress = 0;
			this.clientOperationg = true;
			window.chrome.webview.postMessage({ Type: "CancelGrep" });
		},
		lineClick(lineNumber) {
			this.$emit("line-clicked", lineNumber);
		}
	},
	mounted() {
		// C# → JS
		window.chrome.webview.addEventListener("message", e => {
			const message = e.data;
			switch (message.type) {
				case "GrepResultAdded":
					this.addResult(message.data);
					break;
				case "GrepResultReset":
					this.reset();
					break;
				case "GrepProgressUpdated":
					this.progress = message.data;
					break;
				case "GrepStartLineUpdated":
					this.grepNextStartLine = message.data;
					break;
				case "IsGrepRunningUpdated":
					this.isGrepRunning = message.data;
					this.clientOperationg = false;
					break;
			}
		});
	}
};
