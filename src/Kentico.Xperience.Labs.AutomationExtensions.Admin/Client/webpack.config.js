const webpackMerge = require("webpack-merge");
const path = require("path");
const fs = require("fs");
const spawn = require("child_process").spawn;

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

  if (argv.mode === "production") {
    return buildConfig(baseConfig, opts, argv);
  }

  const { cert, key } = getDotnetCertPaths();

  if (fs.existsSync(cert) && fs.existsSync(key)) {
    return buildConfig(baseConfig, opts, argv, { cert, key });
  }

  return new Promise((resolve, reject) => {
    spawn(
      "dotnet",
      [
        "dev-certs",
        "https",
        "--export-path",
        cert,
        "--format",
        "Pem",
        "--no-password",
      ],
      { stdio: "inherit" },
    )
      .on("error", reject)
      .on("exit", (code) => {
        if (code !== 0) {
          reject(new Error(`dotnet dev-certs failed with exit code ${code}.`));
          return;
        }

        resolve(buildConfig(baseConfig, opts, argv, { cert, key }));
      });
  });
};

function buildConfig(baseConfig, opts, argv, certificate) {
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
  };

  // Required when running the admin client module in 'Proxy' mode.
  // Must match CMSAdminClientModuleSettings in examples/DancingGoat/appsettings.Development.json
  if (certificate) {
    projectConfig.devServer = {
      port: 3009,
      server: {
        type: "https",
        options: certificate,
      },
    };
  }

  return webpackMerge.merge(projectConfig, baseConfig(opts, argv));
}

function getDotnetCertPaths() {
  const baseFolder =
    process.env.APPDATA !== undefined && process.env.APPDATA !== ""
      ? path.join(process.env.APPDATA, "ASP.NET", "https")
      : path.join(process.env.HOME, ".aspnet", "https");

  fs.mkdirSync(baseFolder, { recursive: true });

  const certificateName = process.env.npm_package_name;
  const cert = path.join(baseFolder, `${certificateName}.pem`);
  const key = path.join(baseFolder, `${certificateName}.key`);

  return { cert, key };
}
