const LineViewTab = {
	template: `
<div id="line-view-tab">
	<div id="line-view-results" class="log-container">
		<div v-if="line !== null" class="log-line">
			<span class="line-number">{{ line.LineNumber }}</span>
			<span class="line-content wrap" v-html="line.Content"></span>
		</div>
	</div>
</div>
	`,
	data() {
		return {
			line: null
		};
	},
	methods: {
		setLine(line) {
			this.line = line;
		},
	}
};