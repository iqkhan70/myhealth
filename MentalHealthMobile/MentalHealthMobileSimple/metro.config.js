const { getDefaultConfig } = require('expo/metro-config');

const config = getDefaultConfig(__dirname);

// Completely disable file watching to prevent EMFILE errors
config.watchFolders = [];
config.resolver.platforms = ['ios', 'android', 'native', 'web'];

// Minimize workers and disable watching
config.maxWorkers = 1;
config.resetCache = true;

// Disable all file watching
config.server = {
  ...config.server,
  enableVisualizer: false,
};

// Disable watchman completely
config.watchman = false;

// Minimal resolver configuration
config.resolver = {
  ...config.resolver,
  blacklistRE: /node_modules\/.*\/node_modules\/.*/,
  platforms: ['ios', 'android', 'native', 'web'],
  assetExts: [...config.resolver.assetExts, 'bin'],
};

// Disable transformer caching to reduce file operations
config.transformer = {
  ...config.transformer,
  enableBabelRCLookup: false,
  enableBabelRuntime: false,
};

module.exports = config;