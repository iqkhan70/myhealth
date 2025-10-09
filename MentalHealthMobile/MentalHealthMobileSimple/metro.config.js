const { getDefaultConfig } = require('expo/metro-config');

const config = getDefaultConfig(__dirname);

// Ultra-minimal config for web-only to avoid all file watcher issues
config.resolver.platforms = ['web'];
config.maxWorkers = 1;

module.exports = config;