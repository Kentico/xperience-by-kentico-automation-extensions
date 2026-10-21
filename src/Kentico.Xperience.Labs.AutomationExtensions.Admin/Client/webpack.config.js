const webpackMerge = require("webpack-merge");

const baseWebpackConfig = require("@kentico/xperience-webpack-config");

module.exports = (opts, argv) => {
  const baseConfig = (webpackConfigEnv, webpackArgv) =>
    baseWebpackConfig({
      // Must match RegisterClientModule(org, project) on the backend
      // and AdminOrgName/ProjectName in the .csproj
      orgName: "kentico",
      projectName: "xperience-labs-automation-extensions-admin",
      webpackConfigEnv,
      argv: webpackArgv,
    });

  const projectConfig = {
    module: {
      rules: [
        {
          test: /\.(js|ts)x?$/,
          exclude: [/node_modules/],
          loader: "babel-loader",
        },
        {
          test: /\.css$/,
          use: ["style-loader", "css-loader"],
        },
      ],
    },
    output: {
      clean: true,
      /**
       * Separate chunks cause Xperience's admin to fail when loading the bundle
       * ("Expected 1 file for source path '/adminresources/.../entry.js'").
       * This setting forces the output to be a single chunk.
       * https://github.com/webpack/webpack/issues/12464#issuecomment-1911309972
       */
      chunkFormat: false,
    },
    // Required when running the admin client module in 'Proxy' mode (`npm start`, or `aspire run`).
    // Plain HTTP, like DancingGoat in Development, so no dev certificate is needed.
    // Must match CMSAdminClientModuleSettings in examples/DancingGoat/appsettings.Development.json
    devServer: {
      port: 3063,
      server: {
        type: "http",
      },
    },
  };

  return webpackMerge.merge(projectConfig, baseConfig(opts, argv));
};
