const TabArea = {
	components: { GrepTab, LineViewTab },
	template: `
<div id="tab-container">
	<ul id="tab-headers">
	  <li v-on:click="change('Grep')" v-bind:class="{'active': isActive === 'Grep'}">Grep</li>
	  <li v-on:click="change('LineView')" v-bind:class="{'active': isActive === 'LineView'}">Line View</li>
	</ul>

	<div id="tab-contents">
		<grep-tab v-show="isActive === 'Grep'"></grep-tab>
		<line-view-tab v-show="isActive === 'LineView'"></line-view-tab>
	</div>
</div>
	`,
	data() {
		return {
			isActive: 'Grep'
		};
	},
	methods: {
		change: function (target) {
			this.isActive = target;
		}
	}
};