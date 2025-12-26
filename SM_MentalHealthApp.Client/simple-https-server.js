const express = require('express');
const https = require('https');
const fs = require('fs');
const path = require('path');
const { createProxyMiddleware } = require('http-proxy-middleware');

const app = express();
app.use(express.json());

// ⚙️ Config
const HTTPS_PORT = 5443;
const BACKEND_URL = 'https://localhost:5263';

// 🔹 Serve Blazor static build output
const blazorPath = path.join(__dirname, 'bin/Debug/net9.0/wwwroot');
app.use(express.static(blazorPath, {
  setHeaders: (res, filePath) => {
    if (filePath.endsWith('.js')) res.setHeader('Content-Type', 'application/javascript');
    else if (filePath.endsWith('.css')) res.setHeader('Content-Type', 'text/css');
    else if (filePath.endsWith('.wasm')) res.setHeader('Content-Type', 'application/wasm');
  }
}));

// 🔹 Also serve any extra static assets from /wwwroot
app.use(express.static(path.join(__dirname, 'wwwroot')));

// 🔹 Optional lightweight “call ring” page
app.get('/mobile-call', (req, res) => {
  const callerName = req.query.caller || 'Mobile User';
  const channelName = req.query.channel || 'call_3_1';
  res.send(`
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<title>Incoming Call</title>
<style>
body { font-family: Arial; text-align:center; padding:50px; }
.call { background:#4CAF50; color:white; padding:20px; border-radius:10px; }
.btn { padding:10px 20px; margin:10px; border:none; border-radius:5px; cursor:pointer; }
.accept { background:#4CAF50; color:white; }
.decline { background:#f44336; color:white; }
</style>
</head>
<body>
<div class="call">
  <h2>📞 Incoming Call</h2>
  <p>From: <b>${callerName}</b></p>
  <p>Channel: ${channelName}</p>
</div>
<div>
  <button class="btn accept" onclick="acceptCall()">Accept</button>
  <button class="btn decline" onclick="window.close()">Decline</button>
</div>
<script>
function acceptCall(){
  // Redirect into your Blazor app route that handles Agora join
  window.location.href = '/audio-call/3';
}
// Auto-accept after 3s for testing
setTimeout(acceptCall, 3000);
</script>
</body>
</html>
  `);
});

// 🔹 Proxy backend API calls (.NET server on port 5262)
app.use('/api', createProxyMiddleware({
  target: BACKEND_URL,
  changeOrigin: true,
  logLevel: 'debug'
}));

// 🔹 SPA fallback – always serve Blazor index.html
app.use((req, res) => {
  if (req.path.startsWith('/api/') ||
      (req.path.includes('.') && !req.path.endsWith('.html'))) {
    return res.status(404).send('Not found');
  }
  res.sendFile(path.join(blazorPath, 'index.html'));
});

// 🔐 HTTPS certs (self-signed ok for dev)
const options = {
  key: fs.readFileSync(path.join(__dirname, 'webapp-key.pem')),
  cert: fs.readFileSync(path.join(__dirname, 'webapp-cert.pem'))
};

https.createServer(options, app).listen(HTTPS_PORT, '0.0.0.0', () => {
  console.log(`🚀 HTTPS Blazor host running at https://localhost:${HTTPS_PORT}`);
  console.log(`📱 From mobile on same Wi-Fi: https://192.168.86.113:${HTTPS_PORT}`);
  console.log('⚠️  Accept the self-signed certificate if prompted.');
});
