const express = require('express');
const https = require('https');
const fs = require('fs');
const path = require('path');

const app = express();
const PORT = 3003;

// Serve static files
app.use(express.static(__dirname));

// Redirect root to WebRTC test
app.get('/', (req, res) => {
  res.redirect('/webrtc-test');
});

app.get('/webrtc-test', (req, res) => {
  const html = `
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Shared Agora Room - Mobile</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; text-align: center; }
        #status { font-size: 1.2em; margin-bottom: 20px; }
        .button { padding: 10px 20px; margin: 5px; cursor: pointer; background: #007bff; color: white; border: none; border-radius: 5px; }
        .button:hover { background: #0056b3; }
        .button:disabled { background: #ccc; cursor: not-allowed; }
        #log { text-align: left; background: #f8f9fa; padding: 10px; margin: 20px 0; border-radius: 5px; max-height: 300px; overflow-y: auto; }
        .connected { color: green; font-weight: bold; }
        .disconnected { color: red; font-weight: bold; }
    </style>
</head>
<body>
    <h1>📱 Mobile - Shared Agora Room</h1>
    <div id="status" class="disconnected">Disconnected</div>
    <button class="button" onclick="testMicrophone()">Test Microphone</button>
    <button class="button" onclick="joinRoom()" id="joinBtn">Join Shared Room</button>
    <button class="button" onclick="leaveRoom()" id="leaveBtn" disabled>Leave Room</button>
    <div id="log"></div>

    <script>
        let localStream = null;
        let peerConnection = null;
        let isConnected = false;
        let signalingChannel = null;

        function log(message) {
            const logDiv = document.getElementById('log');
            const timestamp = new Date().toLocaleTimeString();
            logDiv.innerHTML += \`[\${timestamp}] \${message}<br>\`;
            logDiv.scrollTop = logDiv.scrollHeight;
            console.log(message);
        }

        function updateStatus(status, className) {
            const statusDiv = document.getElementById('status');
            statusDiv.textContent = status;
            statusDiv.className = className;
        }

        async function testMicrophone() {
            try {
                log('🎤 Testing microphone access...');
                
                if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                    log('❌ getUserMedia not available. This might be a browser security restriction.');
                    return;
                }
                
                const stream = await navigator.mediaDevices.getUserMedia({ 
                    audio: {
                        echoCancellation: true,
                        noiseSuppression: true,
                        autoGainControl: true
                    }
                });
                log('✅ Microphone access granted');
                log('📊 Audio tracks: ' + stream.getAudioTracks().length);
                
                // Stop the test stream
                stream.getTracks().forEach(track => track.stop());
                
            } catch (error) {
                log('❌ Microphone error: ' + error.message);
            }
        }

        async function joinRoom() {
            try {
                log('🚀 Starting WebRTC connection...');
                updateStatus('Connecting...', 'disconnected');
                
                // Get local media stream
                localStream = await navigator.mediaDevices.getUserMedia({ 
                    audio: {
                        echoCancellation: true,
                        noiseSuppression: true,
                        autoGainControl: true
                    }
                });
                log('✅ Microphone access granted');
                
                // Create peer connection
                const configuration = {
                    iceServers: [
                        { urls: 'stun:stun.l.google.com:19302' },
                        { urls: 'stun:stun1.l.google.com:19302' },
                        { urls: 'stun:stun2.l.google.com:19302' }
                    ]
                };
                
                peerConnection = new RTCPeerConnection(configuration);
                
                // Add local stream
                localStream.getTracks().forEach(track => {
                    peerConnection.addTrack(track, localStream);
                    log('📡 Added local track: ' + track.kind);
                });
                
                // Handle remote stream
                peerConnection.ontrack = (event) => {
                    log('👤 Remote audio track received!');
                    const remoteStream = event.streams[0];
                    const audio = document.createElement('audio');
                    audio.srcObject = remoteStream;
                    audio.autoplay = true;
                    audio.controls = true;
                    audio.style.width = '100%';
                    audio.style.marginTop = '10px';
                    document.body.appendChild(audio);
                    updateStatus('Connected - Audio Active', 'connected');
                    log('🔊 Playing remote audio...');
                };
                
                // Handle ICE candidates
                peerConnection.onicecandidate = (event) => {
                    if (event.candidate) {
                        log('✅ ICE candidate generated');
                        // In a real app, you'd send this to the other peer via signaling server
                    }
                };
                
                // Handle connection state changes
                peerConnection.onconnectionstatechange = () => {
                    log('🔄 Connection state: ' + peerConnection.connectionState);
                    if (peerConnection.connectionState === 'connected') {
                        isConnected = true;
                        updateStatus('Connected - Broadcasting Audio', 'connected');
                    } else if (peerConnection.connectionState === 'disconnected') {
                        isConnected = false;
                        updateStatus('Disconnected', 'disconnected');
                    }
                };
                
                // Create offer
                const offer = await peerConnection.createOffer();
                await peerConnection.setLocalDescription(offer);
                log('✅ WebRTC offer created');
                
                // For testing, we'll create a simple signaling mechanism
                // In a real app, this would go through a signaling server
                log('💡 WebRTC is ready for connection!');
                log('📱 This is a peer-to-peer connection (no Agora needed)');
                updateStatus('Ready for Connection', 'connected');
                
                // Update button states
                document.getElementById('joinBtn').disabled = true;
                document.getElementById('leaveBtn').disabled = false;
                
            } catch (error) {
                log('❌ Failed to start connection: ' + error.message);
                updateStatus('Connection Failed', 'disconnected');
            }
        }

        async function leaveRoom() {
            try {
                log('🚪 Leaving room...');
                
                if (localStream) {
                    localStream.getTracks().forEach(track => track.stop());
                    localStream = null;
                }
                
                if (peerConnection) {
                    peerConnection.close();
                    peerConnection = null;
                }
                
                // Remove any audio elements
                const audioElements = document.querySelectorAll('audio');
                audioElements.forEach(audio => audio.remove());
                
                log('✅ Left room successfully');
                updateStatus('Disconnected', 'disconnected');
                isConnected = false;
                
                // Update button states
                document.getElementById('joinBtn').disabled = false;
                document.getElementById('leaveBtn').disabled = true;
                
            } catch (error) {
                log('❌ Error leaving room: ' + error.message);
            }
        }

        // Auto-start when page loads
        window.addEventListener('load', () => {
            log('🚀 Mobile WebRTC Room loaded');
            log('💡 Click "Join Shared Room" to start WebRTC connection');
            log('📱 This uses direct WebRTC (no Agora gateway issues)');
        });
    </script>
</body>
</html>
  `;
  res.send(html);
});

// Load SSL certificate
const options = {
  key: fs.readFileSync(path.join(__dirname, 'server-key.pem')),
  cert: fs.readFileSync(path.join(__dirname, 'server-cert.pem'))
};

https.createServer(options, app).listen(PORT, '0.0.0.0', () => {
  console.log(`🚀 HTTPS WebRTC server running on https://localhost:${PORT}`);
  console.log(`📱 Mobile can access: https://192.168.86.113:${PORT}/webrtc-test`);
  console.log(`🌐 Server bound to all interfaces (0.0.0.0:${PORT})`);
  console.log(`⚠️  You may need to accept the self-signed certificate in your browser`);
  console.log(`🔒 HTTPS enabled - microphone access should work!`);
});
