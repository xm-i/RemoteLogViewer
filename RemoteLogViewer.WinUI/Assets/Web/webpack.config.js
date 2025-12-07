const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const { VueLoaderPlugin } = require('vue-loader');
const webpack = require('webpack');

module.exports = (env, argv) => {
	const isProduction = argv.mode === 'production';
	const isDevelopment = argv.mode === 'development';

	return {
		entry: './src/main.ts',
		output: {
			filename: 'index.js',
			path: path.resolve(__dirname, 'dist'),
			clean: true,
			sourceMapFilename: '[file].map'
		},
		optimization: {
			minimize: isProduction,
		},
		resolve: {
			extensions: ['.ts', '.js', '.vue'],
			alias: {
				'@': path.resolve(__dirname, 'src'),
				'vue': isProduction ? 'vue/dist/vue.esm-bundler.js' : 'vue/dist/vue.esm-bundler.js'
			},
		},
		module: {
			rules: [
				{
					test: /\.vue$/,
					loader: 'vue-loader',
				},
				{
					test: /\.ts$/,
					loader: 'ts-loader',
					options: {
						appendTsSuffixTo: [/\.vue$/],
						compilerOptions: {
							// リリース版では型定義ファイルを生成しない
							declaration: isDevelopment,
							declarationMap: isDevelopment,
							sourceMap: isDevelopment,
						}
					},
					exclude: /node_modules/,
				},
				{
					test: /\.css$/,
					use: ['style-loader', 'css-loader'],
				},
			],
		},
		plugins: [
			new VueLoaderPlugin(),
			new HtmlWebpackPlugin({
				template: './src/index.html',
				filename: 'index.html',
				minify: isProduction ? {
					removeComments: true,
					collapseWhitespace: true,
					removeRedundantAttributes: true,
				} : false
			}),
			new webpack.DefinePlugin({
				__VUE_OPTIONS_API__: JSON.stringify(true),
				__VUE_PROD_DEVTOOLS__: JSON.stringify(!isProduction),
				__VUE_PROD_HYDRATION_MISMATCH_DETAILS__: JSON.stringify(!isProduction),
				'process.env.NODE_ENV': JSON.stringify(isProduction ? 'production' : 'development')
			})
		],
		devtool: isProduction ? false : 'source-map',
	};
};
