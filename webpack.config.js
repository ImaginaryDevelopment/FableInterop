var path = require("path");
var webpack = require("webpack");

function resolve(filePath) {
  return path.join(__dirname, filePath)
}

var babelOptions = {
  presets: [["es2015", {"modules": false}]],
  plugins: ["transform-runtime"]
}

module.exports = {
  devtool: "source-map",
  entry: resolve('./FableInterop.fsproj'),
  output: {
    filename: 'bundle.js',
    path: resolve('./public'),
  },
  devServer: {
    contentBase: resolve('./public'),
    port: 8080
 },
  module: {
    rules: [
      {
        test: /\.fs(x|proj)?$/,
        use: {
          loader: "fable-loader",
          options: { babel: babelOptions }
        }
      },
      {
        test: /\.jsx?$/,
        exclude: /node_modules[\\\/](?!fable-)/,
        use: {
          loader: 'babel-loader',
          options: babelOptions
        },
      }
    ]
  },
  externals:{
    "react": "React",
    "react-dom":"ReactDOM"
  }
};
