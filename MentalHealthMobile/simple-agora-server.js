const express = require('express');
const app = express();
const PORT = 3002;

// Serve a simple page that shows connection status
app.get('/agora-simple', (req, res) => {
  const { appId, channel, token, uid } = req.query;
  
  const html = `
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Agora Call - Simple Mode</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 20px;
            background: #000;
            color: #fff;
            text-align: center;
        }
        .status {
            font-size: 24px;
            margin: 20px 0;
            padding: 20px;
            border: 2px solid #4CAF50;
            border-radius: 10px;
            background: #1a1a1a;
        }
        .info {
            font-size: 16px;
            margin: 10px 0;
            color: #ccc;
        }
        .button {
            background: #4CAF50;
            color: white;
            padding: 15px 30px;
            border: none;
            border-radius: 5px;
            font-size: 18px;
            cursor: pointer;
            margin: 10px;
        }
        .button:hover {
            background: #45a049;
        }
    </style>
</head>
<body>
    <div class="status" id="status">Initializing Agora Connection...</div>
    <div class="info">App ID: ${appId}</div>
    <div class="info">Channel: ${channel}</div>
    <div class="info">UID: ${uid}</div>
    <div class="info">Token: ${token || 'None (Testing Mode)'}</div>
    
    <button class="button" onclick="testConnection()">Test Connection</button>
    <button class="button" onclick="simulateCall()">Simulate Call</button>
    
    <div id="users" style="margin-top: 20px;"></div>
    
    <script>
        let isConnected = false;
        
        function testConnection() {
            document.getElementById('status').innerHTML = 'Testing Agora Gateway Connection...';
            
            // Try to load Agora SDK
            if (typeof AgoraRTC === 'undefined') {
                // Load Agora SDK dynamically
                const script = document.createElement('script');
                script.src = 'https://download.agora.io/sdk/release/AgoraRTC_N-4.19.0.js';
                script.onload = () => {
                    console.log('✅ Agora SDK loaded');
                    testAgoraConnection();
                };
                script.onerror = () => {
                    console.error('❌ Failed to load Agora SDK');
                    document.getElementById('status').innerHTML = 'Failed to load Agora SDK. Using simulation mode.';
                    simulateCall();
                };
                document.head.appendChild(script);
            } else {
                testAgoraConnection();
            }
        }
        
        async function testAgoraConnection() {
            try {
                console.log('🧪 Testing Agora connection...');
                const client = AgoraRTC.createClient({ 
                    mode: "rtc", 
                    codec: "vp8",
                    // Try different settings to avoid gateway issues
                    enableAudioVolumeIndicator: false,
                    enableAudioProcessing: false
                });
                
                // Set up event listeners first
                client.on("user-published", async (user, mediaType) => {
                    console.log('📱 User published:', user.uid, mediaType);
                    await client.subscribe(user, mediaType);
                    if (mediaType === "audio") {
                        user.audioTrack.play();
                        console.log('🔊 Playing remote audio');
                        addUser(user.uid, 'Remote User (Real)');
                    }
                });
                
                client.on("user-unpublished", (user, mediaType) => {
                    console.log('📱 User unpublished:', user.uid, mediaType);
                });
                
                client.on("user-left", (user) => {
                    console.log('📱 User left:', user.uid);
                });
                
                // Try to join with a longer timeout and different approach
                console.log('🔄 Attempting to join channel...');
                await client.join('${appId}', '${channel}', '${token || ''}', ${uid});
                
                document.getElementById('status').innerHTML = '✅ Connected to Agora! Creating audio...';
                isConnected = true;
                
                // Create and publish audio track
                try {
                    const audioTrack = await AgoraRTC.createMicrophoneAudioTrack({
                        encoderConfig: "music_standard",
                        echoCancellation: true,
                        noiseSuppression: true,
                        autoGainControl: true
                    });
                    
                    await client.publish([audioTrack]);
                    console.log('✅ Audio track published');
                    
                    document.getElementById('status').innerHTML = '✅ Connected to Agora! Audio active!';
                    addUser('${uid}', 'You (Real)');
                    
                } catch (audioError) {
                    console.error('❌ Audio track error:', audioError);
                    document.getElementById('status').innerHTML = '✅ Connected to Agora! (Audio failed)';
                    addUser('${uid}', 'You (Real - No Audio)');
                }
                
            } catch (error) {
                console.error('❌ Agora connection failed:', error);
                
                // Try a different approach - use a different region or settings
                if (error.code === 4096) {
                    document.getElementById('status').innerHTML = '❌ Gateway Error 4096 - Trying alternative approach...';
                    
                    // Try with different settings
                    try {
                        const client2 = AgoraRTC.createClient({ 
                            mode: "live", 
                            codec: "vp8"
                        });
                        
                        await client2.join('${appId}', '${channel}', '${token || ''}', ${uid});
                        document.getElementById('status').innerHTML = '✅ Connected with alternative settings!';
                        addUser('${uid}', 'You (Alternative)');
                        return;
                        
                    } catch (error2) {
                        console.error('❌ Alternative approach also failed:', error2);
                    }
                }
                
                document.getElementById('status').innerHTML = '❌ Agora Gateway Error: ' + error.message + '<br>Using simulation mode instead.';
                simulateCall();
            }
        }
        
        function simulateCall() {
            document.getElementById('status').innerHTML = '🎭 Simulation Mode - Gateway Bypassed';
            isConnected = true;
            
            // Add user to display
            addUser('${uid}', 'You (Simulation)');
            
            // Simulate remote user after 2 seconds
            setTimeout(() => {
                addUser(${uid + 1}, 'Remote User (Simulation)');
                document.getElementById('status').innerHTML = '🎭 Simulation Mode - Both users connected';
            }, 2000);
        }
        
        function addUser(uid, name) {
            const usersDiv = document.getElementById('users');
            const userDiv = document.createElement('div');
            userDiv.style.cssText = 'background: #333; padding: 10px; margin: 10px 0; border-radius: 5px;';
            userDiv.innerHTML = name + ' (UID: ' + uid + ')';
            usersDiv.appendChild(userDiv);
        }
        
        // Auto-start when page loads
        window.addEventListener('load', () => {
            setTimeout(testConnection, 1000);
        });
    </script>
</body>
</html>`;

  res.send(html);
});

app.listen(PORT, '0.0.0.0', () => {
  console.log(`🚀 Simple Agora server running on http://localhost:${PORT}`);
  console.log(`📱 Mobile can access: http://192.168.86.113:${PORT}/agora-simple`);
});
