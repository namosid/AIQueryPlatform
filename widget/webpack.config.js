const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');

module.exports = (env, argv) => {
  const isDevelopment = argv.mode === 'development';

  return {
    entry: {
      'ai-widget': './src/loader.ts',
      'widget-app': './src/index.tsx',
      // Modal will be lazy-loaded via dynamic import
    },
    output: {
      path: path.resolve(__dirname, 'dist'),
      filename: '[name].js',
      chunkFilename: '[name].chunk.js', // For dynamically imported chunks
      clean: true,
      publicPath: isDevelopment ? '/' : 'https://cdn.yourdomain.com/', // Update for production
    },
    resolve: {
      extensions: ['.ts', '.tsx', '.js', '.jsx'],
      alias: {
        '@': path.resolve(__dirname, 'src'),
      },
    },
    module: {
      rules: [
        {
          test: /\.tsx?$/,
          use: 'ts-loader',
          exclude: /node_modules/,
        },
        {
          test: /\.(widget|modal)\.css$/,
          type: 'asset/source', // Import as string for Shadow DOM injection
        },
        {
          test: /\.css$/,
          exclude: /\.(widget|modal)\.css$/,
          use: ['style-loader', 'css-loader'],
        },
      ],
    },
    plugins: [
      new HtmlWebpackPlugin({
        template: './public/widget.html',
        filename: 'index.html',
        inject: false,
      }),
    ],
    devServer: {
      static: [
        {
          directory: path.join(__dirname, 'dist'),
        },
        {
          directory: path.join(__dirname, 'public'),
          publicPath: '/',
        },
      ],
      compress: true,
      port: 3001,
      hot: true,
      open: true,
      headers: {
        'Access-Control-Allow-Origin': '*', // Allow CDN access
      },
    },
    optimization: {
      splitChunks: {
        cacheGroups: {
          vendor: {
            test: /[\\/]node_modules[\\/]/,
            name: 'vendors',
            priority: 10,
            chunks: (chunk) => {
              // Don't extract vendors from ai-widget (keep it standalone)
              return chunk.name !== 'ai-widget';
            },
          },
          modal: {
            test: /[\\/]modal[\\/]/,
            name: 'modal',
            priority: 5,
            chunks: 'async', // Only async chunks
          },
        },
      },
      runtimeChunk: {
        name: (entrypoint) => {
          // Don't create separate runtime for ai-widget (make it standalone)
          return entrypoint.name === 'ai-widget' ? false : 'runtime';
        },
      },
    },
    devtool: isDevelopment ? 'inline-source-map' : 'source-map',
  };
};
