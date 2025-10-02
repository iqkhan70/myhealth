const { getDefaultConfig } = require('expo/metro-config');

const config = getDefaultConfig(__dirname);

// COMPLETELY DISABLE FILE WATCHING
config.watchFolders = [];
config.resolver.platforms = ['ios', 'android', 'native', 'web'];

// Disable all file watching
config.maxWorkers = 1;
config.watchman = false;

// Disable file watcher completely
config.watcher = {
  additionalExts: [],
  ignore: /.*/,
  watchman: false
};

// Block everything to prevent file watching
config.resolver.blacklistRE = /.*/;
config.resolver.blockList = [/.*/];

// Disable file system watching
config.fileMap = {
  watchFolders: [],
  platforms: ['ios', 'android', 'native', 'web']
};

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
