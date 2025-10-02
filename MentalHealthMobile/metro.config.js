const { getDefaultConfig } = require('expo/metro-config');

const config = getDefaultConfig(__dirname);

// Moderate file watching reduction to prevent EMFILE errors
config.watchFolders = [];
config.resolver.platforms = ['ios', 'android', 'native', 'web'];

// Reduce workers but don't disable everything
config.maxWorkers = 1;
config.watchman = false;

// Block only unnecessary files, not everything
config.resolver.blacklistRE = /node_modules\/.*\/node_modules\/.*/;
config.resolver.blockList = [
  /node_modules\/.*\/node_modules\/.*/,
  /.*\/\.git\/.*/,
  /.*\/\.DS_Store$/,
  /.*\/Thumbs\.db$/,
];

// Basic server config
config.server = {
  ...config.server,
  port: 8081,
  enhanceMiddleware: (middleware) => {
    return (req, res, next) => {
      res.setHeader('Cache-Control', 'public, max-age=31536000');
      return middleware(req, res, next);
    };
  },
};

module.exports = config;
