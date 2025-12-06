const TabArea = {
	components: { GrepTab, LineViewTab },
	template: `
<div class="tab-container">
	<ul class="tab-headers" v-if="lineViewTabVisibility">
	  <li v-on:click="change('Grep')" v-bind:class="{'active': isActive === 'Grep'}">Grep</li>
	  <li v-on:click="change('LineView')" v-bind:class="{'active': isActive === 'LineView'}">Line View</li>
	</ul>

	<div class="tab-contents">
		<grep-tab :pageKey="pageKey" v-show="isActive === 'Grep'" @line-clicked="grepLineClicked" :isDisconnected="isDisconnected"></grep-tab>
		<line-view-tab ref="lineViewTab" v-show="isActive === 'LineView'"></line-view-tab>
	</div>
</div>
	`,
	props: {
		pageKey: null,
		isDisconnected: false
	},
	data() {
		return {
			isActive: 'Grep',
			lineViewTabVisibility: false
		};
	},
	methods: {
		change: function (target) {
			this.isActive = target;
		},
		setLine(line) {
			this.isActive = "LineView";
			this.lineViewTabVisibility = true;
			this.$refs.lineViewTab.setLine(line);
		},
		grepLineClicked(lineNumber) {
			this.$emit('grep-line-clicked', lineNumber);
		}
	}
};