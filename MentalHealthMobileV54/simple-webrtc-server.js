const express = require('express');
const app = express();
const PORT = 3002;

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
    <title>Simple WebRTC Audio Test</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; text-align: center; }
        #status { font-size: 1.2em; margin-bottom: 20px; }
        .button { padding: 10px 20px; margin: 5px; cursor: pointer; background: #007bff; color: white; border: none; border-radius: 5px; }
        .button:hover { background: #0056b3; }
        #log { text-align: left; background: #f8f9fa; padding: 10px; margin: 20px 0; border-radius: 5px; max-height: 300px; overflow-y: auto; }
    </style>
</head>
<body>
    <h1>Simple WebRTC Audio Test</h1>
    <div id="status">Ready to test audio</div>
    <button class="button" onclick="testMicrophone()">Test Microphone</button>
    <button class="button" onclick="testAudioPlayback()">Test Audio Playback</button>
    <button class="button" onclick="startWebRTCTest()">Start WebRTC Test</button>
    <div id="log"></div>

    <script>
        let localStream = null;
        let remoteStream = null;
        let peerConnection = null;

        function log(message) {
            const logDiv = document.getElementById('log');
            const timestamp = new Date().toLocaleTimeString();
            logDiv.innerHTML += \`[\${timestamp}] \${message}<br>\`;
            console.log(message);
        }

        async function testMicrophone() {
            try {
                log('🎤 Testing microphone access...');
                
                if (location.protocol !== 'https:' && location.hostname !== 'localhost' && location.hostname !== '127.0.0.1') {
                    log('⚠️ Microphone requires HTTPS or localhost. Current: ' + location.protocol + '//' + location.hostname);
                    log('💡 Try using Chrome instead of Safari, or access from your computer: http://localhost:3002/webrtc-test');
                    log('💡 Chrome is more permissive with microphone access on local networks');
                    return;
                }
                
                if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                    log('❌ getUserMedia not available. This might be a Safari security restriction.');
                    log('💡 Try using Chrome or Firefox, or enable microphone permissions in Safari settings.');
                    return;
                }
                
                localStream = await navigator.mediaDevices.getUserMedia({ 
                    audio: {
                        echoCancellation: true,
                        noiseSuppression: true,
                        autoGainControl: true
                    }
                });
                log('✅ Microphone access granted');
                log('📊 Audio tracks: ' + localStream.getAudioTracks().length);
                
                // Test if we can record audio
                const audioContext = new AudioContext();
                const source = audioContext.createMediaStreamSource(localStream);
                const analyser = audioContext.createAnalyser();
                source.connect(analyser);
                
                log('✅ Audio context created successfully');
                
            } catch (error) {
                log('❌ Microphone error: ' + error.message);
                log('💡 This is likely due to Safari security restrictions. Try Chrome or enable permissions.');
            }
        }

        async function testAudioPlayback() {
            try {
                log('🔊 Testing audio playback...');
                const audioContext = new AudioContext();
                const oscillator = audioContext.createOscillator();
                const gainNode = audioContext.createGain();
                
                oscillator.connect(gainNode);
                gainNode.connect(audioContext.destination);
                
                oscillator.frequency.setValueAtTime(440, audioContext.currentTime);
                gainNode.gain.setValueAtTime(0.1, audioContext.currentTime);
                
                oscillator.start();
                log('✅ Playing test tone (440Hz)...');
                
                setTimeout(() => {
                    oscillator.stop();
                    log('✅ Test tone stopped');
                }, 2000);
                
            } catch (error) {
                log('❌ Audio playback error: ' + error.message);
            }
        }

        async function startWebRTCTest() {
            try {
                log('🎯 Starting WebRTC audio test...');
                
                if (!localStream) {
                    log('❌ Please test microphone first');
                    return;
                }
                
                // Create a simple peer connection for testing
                const configuration = {
                    iceServers: [
                        { urls: 'stun:stun.l.google.com:19302' },
                        { urls: 'stun:stun1.l.google.com:19302' }
                    ]
                };
                
                peerConnection = new RTCPeerConnection(configuration);
                
                // Add local stream
                localStream.getTracks().forEach(track => {
                    peerConnection.addTrack(track, localStream);
                });
                
                // Handle remote stream
                peerConnection.ontrack = (event) => {
                    log('✅ Remote audio track received');
                    remoteStream = event.streams[0];
                    const audio = document.createElement('audio');
                    audio.srcObject = remoteStream;
                    audio.autoplay = true;
                    document.body.appendChild(audio);
                };
                
                // Handle ICE candidates
                peerConnection.onicecandidate = (event) => {
                    if (event.candidate) {
                        log('✅ ICE candidate generated');
                    }
                };
                
                // Create offer
                const offer = await peerConnection.createOffer();
                await peerConnection.setLocalDescription(offer);
                
                log('✅ WebRTC offer created successfully');
                log('🎉 WebRTC audio system is working!');
                
            } catch (error) {
                log('❌ WebRTC error: ' + error.message);
            }
        }

        // Auto-start when page loads
        window.addEventListener('load', () => {
            log('🚀 Page loaded, ready for testing');
            log('💡 This test bypasses Agora and uses native WebRTC');
        });
    </script>
</body>
</html>
  `;
  res.send(html);
});

app.listen(PORT, '0.0.0.0', () => {
  console.log(`🚀 Simple WebRTC server running on http://localhost:${PORT}`);
  console.log(`📱 Mobile can access: http://192.168.86.113:${PORT}/webrtc-test`);
  console.log(`🌐 Server bound to all interfaces (0.0.0.0:${PORT})`);
});
