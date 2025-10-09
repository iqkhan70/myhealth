const express = require('express');
const https = require('https');
const fs = require('fs');
const path = require('path');
const { createProxyMiddleware } = require('http-proxy-middleware');
const { createProxyServer } = require('http-proxy');

const app = express();

// Add JSON body parser middleware
app.use(express.json());
const HTTPS_PORT = 5443;
const BACKEND_URL = 'http://localhost:5262';

// Serve static files from the build output with proper MIME types
app.use(express.static(path.join(__dirname, 'bin/Debug/net9.0/wwwroot'), {
  setHeaders: (res, path) => {
    if (path.endsWith('.js')) {
      res.setHeader('Content-Type', 'application/javascript');
    } else if (path.endsWith('.css')) {
      res.setHeader('Content-Type', 'text/css');
    } else if (path.endsWith('.html')) {
      res.setHeader('Content-Type', 'text/html');
    } else if (path.endsWith('.wasm')) {
      res.setHeader('Content-Type', 'application/wasm');
    }
  }
}));

// Also serve from wwwroot for additional assets
app.use(express.static(path.join(__dirname, 'wwwroot')));

// HTTP-based signaling endpoints
const signalingRooms = new Map();

app.post('/api/signaling/join', (req, res) => {
  const { roomId, userId } = req.body;
  console.log('👤 User ' + userId + ' joining room ' + roomId);
  
  if (!signalingRooms.has(roomId)) {
    signalingRooms.set(roomId, {
      users: new Set(),
      offers: [],
      answers: [],
      iceCandidates: []
    });
  }
  
  const room = signalingRooms.get(roomId);
  room.users.add(userId);
  
  res.json({ success: true, roomId, userId });
});

app.get('/api/signaling/poll', (req, res) => {
  const { roomId, userId } = req.query;
  const room = signalingRooms.get(roomId);
  
  if (!room) {
    return res.json({ offers: [], answers: [], iceCandidates: [] });
  }
  
  // Return all signaling data except from this user
  const data = {
    offers: room.offers.filter(item => item.userId !== userId),
    answers: room.answers.filter(item => item.userId !== userId),
    iceCandidates: room.iceCandidates.filter(item => item.userId !== userId)
  };
  
  res.json(data);
});

app.post('/api/signaling/offer', (req, res) => {
  const { roomId, userId, offer } = req.body;
  const room = signalingRooms.get(roomId);
  
  if (room) {
    room.offers.push({ offer, userId, timestamp: Date.now() });
    console.log('📤 Offer from ' + userId + ' in room ' + roomId);
  }
  
  res.json({ success: true });
});

app.post('/api/signaling/answer', (req, res) => {
  const { roomId, userId, answer } = req.body;
  const room = signalingRooms.get(roomId);
  
  if (room) {
    room.answers.push({ answer, userId, timestamp: Date.now() });
    console.log('📤 Answer from ' + userId + ' in room ' + roomId);
  }
  
  res.json({ success: true });
});

app.post('/api/signaling/ice-candidate', (req, res) => {
  const { roomId, userId, candidate } = req.body;
  const room = signalingRooms.get(roomId);
  
  if (room) {
    room.iceCandidates.push({ candidate, userId, timestamp: Date.now() });
    console.log('📤 ICE candidate from ' + userId + ' in room ' + roomId);
  }
  
  res.json({ success: true });
});

// Call notification endpoint
app.get('/mobile-call', (req, res) => {
  const callerName = req.query.caller || 'Mobile User';
  const channelName = req.query.channel || 'mobile_call_' + Date.now();
  
  // Return a simple page that triggers the call notification
  const html = `
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>Incoming Call</title>
    <style>
        body { font-family: Arial, sans-serif; text-align: center; padding: 50px; }
        .call-notification { background: #4CAF50; color: white; padding: 20px; border-radius: 10px; margin: 20px; }
        .call-actions { margin: 20px; }
        .btn { padding: 10px 20px; margin: 10px; border: none; border-radius: 5px; cursor: pointer; }
        .accept { background: #4CAF50; color: white; }
        .decline { background: #f44336; color: white; }
    </style>
</head>
<body>
    <div class="call-notification">
        <h2>📞 Incoming Call</h2>
        <h3>From: ${callerName}</h3>
        <p>Channel: ${channelName}</p>
    </div>
    
    <div class="call-actions">
        <button class="btn accept" onclick="acceptCall()">Accept Call</button>
        <button class="btn decline" onclick="declineCall()">Decline Call</button>
    </div>
    
    <script>
        function acceptCall() {
            // Redirect to the shared room for actual calling
            window.location.href = '/shared-room?mobile_call=true&caller=${encodeURIComponent(callerName)}&channel=${channelName}';
        }
        
        function declineCall() {
            window.close();
        }
        
        // Auto-accept after 3 seconds for testing
        setTimeout(() => {
            console.log('Auto-accepting call for testing...');
            acceptCall();
        }, 3000);
    </script>
</body>
</html>
  `;
  res.send(html);
});

// Shared Agora room for testing
app.get('/shared-room', (req, res) => {
  const html = `
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Shared Agora Room - Web App</title>
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
    <h1>💻 Web App - Shared Agora Room</h1>
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
        let hasProcessedAnswer = false;
        let lastAnswerTime = 0;
        let answerCooldown = 500; // Reduced to 500ms for faster connection
        let connectionTimeout = null;
        // Get room ID from URL parameters or use default
        const urlParams = new URLSearchParams(window.location.search);
        let roomId = urlParams.get('channel') || 'shared_room_test';
        
        // Force consistent room ID for testing
        if (roomId.includes('test_call_')) {
            roomId = 'shared_room_test';
        }
        let userId = Math.floor(Math.random() * 100000);
        let signalingData = { offers: [], answers: [], iceCandidates: [] };
        let pollingInterval = null;

        function log(message) {
            const logDiv = document.getElementById('log');
            const timestamp = new Date().toLocaleTimeString();
            logDiv.innerHTML += '[' + timestamp + '] ' + message + '<br>';
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
                
                // Test audio playback
                const audio = document.createElement('audio');
                audio.srcObject = stream;
                audio.autoplay = true;
                audio.muted = false;
                audio.volume = 0.5;
                document.body.appendChild(audio);
                
                log('🔊 Test audio playing...');
                setTimeout(() => {
                    stream.getTracks().forEach(track => track.stop());
                    audio.remove();
                    log('✅ Microphone test completed');
                }, 2000);
                
            } catch (error) {
                log('❌ Microphone error: ' + error.message);
            }
        }

        async function joinRoom() {
            try {
                log('🚀 Starting WebRTC connection...');
                log('🔧 Debug: Room ID = ' + roomId + ', User ID = ' + userId);
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
                
                // Start HTTP-based signaling
                log('🔌 Starting HTTP-based signaling...');
                await startSignaling();
                
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
                    log('📊 Event streams: ' + event.streams.length);
                    log('📊 Event tracks: ' + event.track.kind);
                    
                    const remoteStream = event.streams[0];
                    if (!remoteStream) {
                        log('❌ No remote stream found in event');
                        return;
                    }
                    
                    log('📊 Remote stream tracks: ' + remoteStream.getTracks().length);
                    log('📊 Remote stream active: ' + remoteStream.active);
                    
                    // Remove any existing audio elements
                    const existingAudio = document.querySelector('audio');
                    if (existingAudio) {
                        existingAudio.remove();
                    }
                    
                    const audio = document.createElement('audio');
                    audio.srcObject = remoteStream;
                    audio.autoplay = true;
                    audio.controls = true;
                    audio.muted = false;
                    audio.volume = 1.0;
                    audio.style.width = '100%';
                    audio.style.marginTop = '10px';
                    audio.style.border = '2px solid #007bff';
                    audio.style.borderRadius = '5px';
                    audio.style.padding = '10px';
                    document.body.appendChild(audio);
                    
                    log('🔊 Audio element created and added to page');
                    
                    // Add event listeners for debugging
                    audio.addEventListener('loadstart', () => log('🔊 Audio load started'));
                    audio.addEventListener('loadeddata', () => log('🔊 Audio data loaded'));
                    audio.addEventListener('canplay', () => log('🔊 Audio can play'));
                    audio.addEventListener('playing', () => log('🔊 Audio is playing'));
                    audio.addEventListener('error', (e) => log('❌ Audio error: ' + e.message));
                    
                    // Force play the audio
                    audio.play().then(() => {
                        log('🔊 Remote audio playing successfully!');
                        updateStatus('Connected - Audio Active', 'connected');
                    }).catch(error => {
                        log('❌ Failed to play remote audio: ' + error.message);
                        updateStatus('Connected - Audio Error', 'disconnected');
                    });
                };
                
                // Handle ICE candidates
                peerConnection.onicecandidate = async (event) => {
                    if (event.candidate) {
                        log('✅ ICE candidate generated');
                        signalingData.iceCandidates.push({
                            candidate: event.candidate,
                            userId: userId,
                            timestamp: Date.now()
                        });
                        
                        // Send ICE candidate via HTTP
                        try {
                            await fetch('/api/signaling/ice-candidate', {
                                method: 'POST',
                                headers: { 'Content-Type': 'application/json' },
                                body: JSON.stringify({ roomId, userId, candidate: event.candidate })
                            });
                        } catch (error) {
                            log('❌ Failed to send ICE candidate: ' + error.message);
                        }
                    }
                };
                
                // Handle connection state changes
                peerConnection.onconnectionstatechange = () => {
                    log('🔄 Connection state: ' + peerConnection.connectionState);
                    log('🔄 ICE connection state: ' + peerConnection.iceConnectionState);
                    log('🔄 ICE gathering state: ' + peerConnection.iceGatheringState);
                    
                    if (peerConnection.connectionState === 'connected') {
                        isConnected = true;
                        updateStatus('Connected - Broadcasting Audio', 'connected');
                        log('✅ WebRTC connection established successfully!');
                        
                        // Clear connection timeout
                        if (connectionTimeout) {
                            clearTimeout(connectionTimeout);
                            connectionTimeout = null;
                        }
                        
                        // Check if we have remote tracks
                        const receivers = peerConnection.getReceivers();
                        log('📊 Number of receivers: ' + receivers.length);
                        receivers.forEach((receiver, index) => {
                            const track = receiver.track;
                            if (track) {
                                log('📊 Receiver ' + index + ': ' + track.kind + ' track, enabled: ' + track.enabled);
                            }
                        });
                    } else if (peerConnection.connectionState === 'disconnected') {
                        isConnected = false;
                        updateStatus('Disconnected', 'disconnected');
                        log('❌ WebRTC connection lost');
                    } else if (peerConnection.connectionState === 'failed') {
                        isConnected = false;
                        updateStatus('Connection Failed', 'disconnected');
                        log('❌ WebRTC connection failed');
                    } else if (peerConnection.connectionState === 'connecting') {
                        updateStatus('Connecting...', 'connecting');
                        log('🔄 WebRTC connecting...');
                    }
                };
                
                // Handle ICE connection state changes
                peerConnection.oniceconnectionstatechange = () => {
                    log('🧊 ICE connection state changed: ' + peerConnection.iceConnectionState);
                    if (peerConnection.iceConnectionState === 'connected' || peerConnection.iceConnectionState === 'completed') {
                        log('✅ ICE connection established!');
                        updateStatus('Connected - Broadcasting Audio', 'connected');
                    } else if (peerConnection.iceConnectionState === 'failed') {
                        log('❌ ICE connection failed');
                        updateStatus('Connection Failed', 'disconnected');
                    }
                };
                
                // Update button states
                document.getElementById('joinBtn').disabled = true;
                document.getElementById('leaveBtn').disabled = false;
                
                // Add a timeout to check connection status
                setTimeout(() => {
                    log('🔍 Connection check after 5 seconds:');
                    log('  - Connection state: ' + peerConnection.connectionState);
                    log('  - ICE connection state: ' + peerConnection.iceConnectionState);
                    log('  - ICE gathering state: ' + peerConnection.iceGatheringState);
                    log('  - Local description: ' + (peerConnection.localDescription ? 'Set' : 'Not set'));
                    log('  - Remote description: ' + (peerConnection.remoteDescription ? 'Set' : 'Not set'));
                    
                    if (peerConnection.iceConnectionState === 'connected' || peerConnection.iceConnectionState === 'completed') {
                        updateStatus('Connected - Broadcasting Audio', 'connected');
                    } else if (peerConnection.iceConnectionState === 'checking') {
                        updateStatus('Connecting...', 'disconnected');
                    } else if (peerConnection.iceConnectionState === 'failed') {
                        updateStatus('Connection Failed', 'disconnected');
                    }
                }, 5000);
                
            } catch (error) {
                log('❌ Failed to start connection: ' + error.message);
                updateStatus('Connection Failed', 'disconnected');
            }
        }
        
        async function startSignaling() {
            try {
                // Check if we're already connected
                if (isConnected) {
                    log('⚠️ Already connected, skipping signaling setup');
                    return;
                }
                
                // Register this device in the room
                const response = await fetch('/api/signaling/join', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ roomId, userId })
                });
                
                if (response.ok) {
                    log('✅ Joined signaling room: ' + roomId);
                    
                // Start polling for signaling data (balanced frequency)
                pollingInterval = setInterval(pollSignaling, 3000);
                
                // Set a connection timeout
                connectionTimeout = setTimeout(() => {
                    if (!isConnected) {
                        log('⏰ Connection timeout - no connection established after 15 seconds');
                        updateStatus('Connection Timeout', 'disconnected');
                    }
                }, 15000);
                    
                // Create initial offer only if we're the first user in the room
                // Use a simple heuristic: user with lower ID creates offer
                setTimeout(async () => {
                    try {
                        const response = await fetch('/api/signaling/poll?roomId=' + roomId + '&userId=' + userId);
                        if (response.ok) {
                            const data = await response.json();
                            const otherUsers = (data.offers || []).concat(data.answers || []).map(item => item.userId);
                            const isFirstUser = otherUsers.length === 0 || userId < Math.min(...otherUsers);
                            
                            if (isFirstUser) {
                                log('🎯 I am the first user, creating offer...');
                                createOffer();
                            } else {
                                log('⏳ Waiting for offer from other user...');
                            }
                        }
                    } catch (error) {
                        log('❌ Error checking room state: ' + error.message);
                        // Fallback: create offer anyway
                        createOffer();
                    }
                }, 3000); // Increased delay to 3 seconds to prevent race conditions
                } else {
                    log('❌ Failed to join signaling room');
                }
            } catch (error) {
                log('❌ Signaling error: ' + error.message);
            }
        }
        
        async function pollSignaling() {
            try {
                const response = await fetch('/api/signaling/poll?roomId=' + roomId + '&userId=' + userId);
                if (response.ok) {
                    const data = await response.json();
                    // Only log if there's actual signaling data to reduce noise
                    const hasData = (data.offers?.length || 0) > 0 || (data.answers?.length || 0) > 0 || (data.iceCandidates?.length || 0) > 0;
                    if (hasData) {
                        log('🔄 Polling... Found ' + (data.offers?.length || 0) + ' offers, ' + (data.answers?.length || 0) + ' answers, ' + (data.iceCandidates?.length || 0) + ' ICE candidates');
                    }
                    await processSignalingData(data);
                } else {
                    log('❌ Polling failed: ' + response.status);
                }
            } catch (error) {
                log('❌ Polling error: ' + error.message);
            }
        }
        
        async function processSignalingData(data) {
            // Process offers
            for (const offer of data.offers || []) {
                if (offer.userId !== userId) {
                    log('📥 Received offer from: ' + offer.userId);
                    await handleOffer(offer.offer);
                }
            }
            
            // Process answers
            for (const answer of data.answers || []) {
                if (answer.userId !== userId) {
                    log('📥 Received answer from: ' + answer.userId);
                    await handleAnswer(answer.answer);
                }
            }
            
            // Process ICE candidates
            for (const candidate of data.iceCandidates || []) {
                if (candidate.userId !== userId) {
                    log('📥 Received ICE candidate from: ' + candidate.userId);
                    await handleIceCandidate(candidate.candidate);
                }
            }
            
            // If we received any signaling data, log it
            if (data.offers && data.offers.length > 0) {
                log('📊 Found ' + data.offers.length + ' offers in room');
            }
            if (data.answers && data.answers.length > 0) {
                log('📊 Found ' + data.answers.length + ' answers in room');
            }
            if (data.iceCandidates && data.iceCandidates.length > 0) {
                log('📊 Found ' + data.iceCandidates.length + ' ICE candidates in room');
            }
        }
        
        async function createOffer() {
            try {
                // Only create offer if we don't already have a remote description
                if (peerConnection.remoteDescription) {
                    log('⚠️ Already have remote description, skipping offer creation');
                    return;
                }
                
                // Check if we already have a local description
                if (peerConnection.localDescription) {
                    log('⚠️ Already have local description, skipping offer creation');
                    return;
                }
                
                log('🔄 Creating WebRTC offer...');
                const offer = await peerConnection.createOffer();
                await peerConnection.setLocalDescription(offer);
                log('✅ WebRTC offer created');
                
                // Send offer via HTTP
                await fetch('/api/signaling/offer', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ roomId, userId, offer })
                });
                log('📤 Offer sent to signaling server');
            } catch (error) {
                log('❌ Failed to create offer: ' + error.message);
            }
        }
        
        async function handleOffer(offer) {
            try {
                // Only handle offer if we don't already have a remote description
                if (peerConnection.remoteDescription) {
                    log('⚠️ Already have remote description, skipping offer handling');
                    return;
                }
                
                // Check if we already have a local description
                if (peerConnection.localDescription) {
                    log('⚠️ Already have local description, skipping offer handling');
                    return;
                }
                
                // Rate limiting: don't create answers too frequently
                const now = Date.now();
                if (now - lastAnswerTime < answerCooldown) {
                    log('⚠️ Answer cooldown active, skipping offer handling');
                    return;
                }
                
                await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
                const answer = await peerConnection.createAnswer();
                await peerConnection.setLocalDescription(answer);
                log('✅ WebRTC answer created');
                
                lastAnswerTime = now;
                
                // Check connection state after creating answer
                setTimeout(() => {
                    log('🔍 After answer - Connection state: ' + peerConnection.connectionState);
                    log('🔍 After answer - ICE connection state: ' + peerConnection.iceConnectionState);
                }, 1000);
                
                // Store answer for other peers
                signalingData.answers.push({
                    answer: answer,
                    userId: userId,
                    timestamp: Date.now()
                });
                
                // Send answer via HTTP
                await fetch('/api/signaling/answer', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ roomId, userId, answer })
                });
            } catch (error) {
                log('❌ Failed to handle offer: ' + error.message);
            }
        }
        
        async function handleAnswer(answer) {
            try {
                // Only handle answer if we haven't already processed one
                if (hasProcessedAnswer) {
                    log('⚠️ Already processed answer, skipping duplicate');
                    return;
                }
                
                // Only handle answer if we don't already have a remote description
                if (peerConnection.remoteDescription) {
                    log('⚠️ Already have remote description, skipping answer handling');
                    return;
                }
                
                // Check if we have a local description (offer) before setting remote description
                if (!peerConnection.localDescription) {
                    log('⚠️ No local description (offer) found, skipping answer handling');
                    return;
                }
                
                hasProcessedAnswer = true;
                log('📥 Processing answer from remote peer...');
                await peerConnection.setRemoteDescription(new RTCSessionDescription(answer));
                log('✅ WebRTC answer processed - connection should be established');
                
                // Check connection state
                setTimeout(() => {
                    log('🔍 Connection state: ' + peerConnection.connectionState);
                    log('🔍 ICE connection state: ' + peerConnection.iceConnectionState);
                    log('🔍 ICE gathering state: ' + peerConnection.iceGatheringState);
                    
                    // Check if we have remote tracks
                    const receivers = peerConnection.getReceivers();
                    log('📊 Number of receivers: ' + receivers.length);
                    receivers.forEach((receiver, index) => {
                        log('  Receiver ' + index + ': ' + receiver.track?.kind + ' (enabled: ' + receiver.track?.enabled + ')');
                    });
                }, 1000);
                
            } catch (error) {
                log('❌ Failed to handle answer: ' + error.message);
            }
        }
        
        async function handleIceCandidate(candidate) {
            try {
                await peerConnection.addIceCandidate(candidate);
                log('✅ ICE candidate added');
            } catch (error) {
                log('❌ Failed to add ICE candidate: ' + error.message);
            }
        }

        async function leaveRoom() {
            try {
                log('🚪 Leaving room...');
                
                // Stop polling
                if (pollingInterval) {
                    clearInterval(pollingInterval);
                    pollingInterval = null;
                }
                
                // Clear connection timeout
                if (connectionTimeout) {
                    clearTimeout(connectionTimeout);
                    connectionTimeout = null;
                }
                
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
                hasProcessedAnswer = false;
                lastAnswerTime = 0;
                
                // Update button states
                document.getElementById('joinBtn').disabled = false;
                document.getElementById('leaveBtn').disabled = true;
                
            } catch (error) {
                log('❌ Error leaving room: ' + error.message);
            }
        }

        // Auto-start when page loads
        window.addEventListener('load', () => {
            log('🚀 Web App WebRTC Room loaded');
            log('💡 Click "Join Shared Room" to start WebRTC connection');
            log('💻 This uses direct WebRTC (no Agora gateway issues)');
            log('🔧 Debug: Room ID = ' + roomId + ', User ID = ' + userId);
            
            // Check if this is a call from mobile app
            const urlParams = new URLSearchParams(window.location.search);
            const isMobileCall = urlParams.get('mobile_call') === 'true';
            const callerName = urlParams.get('caller') || 'Mobile User';
            const channelName = urlParams.get('channel') || 'shared_room_test';
            
            if (isMobileCall) {
                log('📱 Incoming call from mobile app!');
                log('🔔 Auto-joining shared room for call...');
                log('👤 Caller: ' + callerName);
                log('📞 Channel: ' + channelName);
                log('🔧 Debug: Using room ID = ' + roomId);
                
                // Auto-join the room for the call
                setTimeout(() => {
                    log('🚀 Auto-starting WebRTC connection for call...');
                    joinRoom();
                }, 2000);
            }
        });
        
        // Cleanup on page unload
        window.addEventListener('beforeunload', () => {
            leaveRoom();
        });
    </script>
</body>
</html>
  `;
  res.send(html);
});

// Manual proxy for API calls
app.use('/api', (req, res) => {
  console.log('🔄 Proxying API request:', req.method, req.url, '->', BACKEND_URL + req.url);
  
  const proxy = createProxyServer({
    target: BACKEND_URL,
    changeOrigin: true
  });
  
  proxy.on('proxyReq', (proxyReq, req, res) => {
    console.log('📤 Sending request to backend:', proxyReq.method, proxyReq.path);
  });
  
  proxy.on('proxyRes', (proxyRes, req, res) => {
    console.log('📥 Received response from backend:', proxyRes.statusCode);
  });
  
  proxy.on('error', (err, req, res) => {
    console.error('❌ Proxy error:', err.message);
    res.status(500).json({ error: 'Proxy error' });
  });
  
  proxy.web(req, res);
});

// Handle SPA routing - serve index.html for all non-API routes
app.use((req, res) => {
  // Don't serve index.html for API routes or static assets
  if (req.path.startsWith('/api/') || 
      (req.path.includes('.') && !req.path.endsWith('.html'))) {
    return res.status(404).send('Not found');
  }
  res.sendFile(path.join(__dirname, 'wwwroot', 'index.html'));
});

// HTTPS server options
const options = {
  key: fs.readFileSync(path.join(__dirname, 'webapp-key.pem')),
  cert: fs.readFileSync(path.join(__dirname, 'webapp-cert.pem'))
};

https.createServer(options, app).listen(HTTPS_PORT, '0.0.0.0', () => {
  console.log('🚀 HTTPS Web App running on https://localhost:' + HTTPS_PORT);
  console.log('📱 Mobile can access: https://192.168.86.113:' + HTTPS_PORT);
  console.log('🌐 Server bound to all interfaces (0.0.0.0:' + HTTPS_PORT + ')');
  console.log('⚠️  You may need to accept the self-signed certificate in your browser');
  console.log('🔒 HTTPS enabled - should resolve authentication issues!');
  console.log('');
  console.log('📋 Test URLs:');
  console.log('   Computer: https://localhost:' + HTTPS_PORT);
  console.log('   Mobile:   https://192.168.86.113:' + HTTPS_PORT);
  console.log('');
  console.log('🔑 Default credentials:');
  console.log('   Username: admin');
  console.log('   Password: admin123');
});
