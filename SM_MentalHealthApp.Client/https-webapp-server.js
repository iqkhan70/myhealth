const express = require('express');
const https = require('https');
const fs = require('fs');
const path = require('path');
const { spawn } = require('child_process');

const app = express();
const HTTPS_PORT = 5443;

// Serve static files from the web app
app.use(express.static(path.join(__dirname, 'wwwroot')));

// Serve the Blazor app - catch all routes
app.get('/*', (req, res) => {
  res.sendFile(path.join(__dirname, 'wwwroot', 'index.html'));
});

// HTTPS server options
const options = {
  key: fs.readFileSync(path.join(__dirname, 'webapp-key.pem')),
  cert: fs.readFileSync(path.join(__dirname, 'webapp-cert.pem'))
};

https.createServer(options, app).listen(HTTPS_PORT, '0.0.0.0', () => {
  console.log(`🚀 HTTPS Web App running on https://localhost:${HTTPS_PORT}`);
  console.log(`📱 Mobile can access: https://192.168.86.113:${HTTPS_PORT}`);
  console.log(`🌐 Server bound to all interfaces (0.0.0.0:${HTTPS_PORT})`);
  console.log('⚠️  You may need to accept the self-signed certificate in your browser');
  console.log('🔒 HTTPS enabled - should resolve authentication issues!');
  console.log('');
  console.log('📋 Test URLs:');
  console.log(`   Computer: https://localhost:${HTTPS_PORT}`);
  console.log(`   Mobile:   https://192.168.86.113:${HTTPS_PORT}`);
  console.log('');
  console.log('🔑 Default credentials:');
  console.log('   Username: admin');
  console.log('   Password: admin123');
});
