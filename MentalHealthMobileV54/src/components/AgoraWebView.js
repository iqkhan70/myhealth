import React, { useRef, useEffect } from 'react';
import { WebView } from 'react-native-webview';

const AgoraWebView = ({ 
  appId, 
  channel, 
  token, 
  uid, 
  onUserJoined, 
  onUserLeft, 
  onError,
  isVisible = true 
}) => {
  const webViewRef = useRef(null);

  const agoraHTML = `
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Agora Call</title>
        <script src="https://download.agora.io/sdk/release/AgoraRTC_N-4.20.0.js"></script>
    </head>
    <body>
        <div id="status">Initializing...</div>
        <div id="users"></div>
        
        <script>
            let client = null;
            let localTracks = {
                audioTrack: null,
                videoTrack: null
            };
            let remoteUsers = {};
            let isJoined = false;

            async function initializeAgora() {
                try {
                    console.log('🎯 Mobile WebView: Initializing Agora...');
                    console.log('App ID:', '${appId}');
                    console.log('Channel:', '${channel}');
                    console.log('Token:', '${token}');
                    console.log('UID:', ${uid});

                    // Check if AgoraRTC is loaded
                    if (typeof AgoraRTC === 'undefined') {
                        throw new Error('AgoraRTC SDK not loaded');
                    }

                    // Create Agora client
                    client = AgoraRTC.createClient({ mode: "rtc", codec: "vp8" });
                    console.log('✅ Mobile WebView: Agora client created');

                    // Set up event listeners
                    client.on("user-published", async (user, mediaType) => {
                        console.log('📱 Mobile WebView: User published:', user.uid, mediaType);
                        await client.subscribe(user, mediaType);
                        
                        if (mediaType === "audio") {
                            const remoteAudioTrack = user.audioTrack;
                            remoteAudioTrack.play();
                            console.log('🔊 Mobile WebView: Playing remote audio');
                            
                            // Notify React Native
                            window.ReactNativeWebView.postMessage(JSON.stringify({
                                type: 'user_joined',
                                uid: user.uid
                            }));
                        }
                    });

                    client.on("user-unpublished", (user, mediaType) => {
                        console.log('📱 Mobile WebView: User unpublished:', user.uid, mediaType);
                    });

                    client.on("user-left", (user) => {
                        console.log('📱 Mobile WebView: User left:', user.uid);
                        delete remoteUsers[user.uid];
                        
                        // Notify React Native
                        window.ReactNativeWebView.postMessage(JSON.stringify({
                            type: 'user_left',
                            uid: user.uid
                        }));
                    });

                    // Join the channel
                    console.log('🚀 Mobile WebView: Joining channel...');
                    await client.join('${appId}', '${channel}', '${token}', ${uid});
                    isJoined = true;
                    console.log('✅ Mobile WebView: Successfully joined channel');

                    // Create and publish local audio track
                    console.log('🎤 Mobile WebView: Creating microphone audio track...');
                    localTracks.audioTrack = await AgoraRTC.createMicrophoneAudioTrack();
                    console.log('📡 Mobile WebView: Publishing audio track...');
                    await client.publish([localTracks.audioTrack]);
                    console.log('✅ Mobile WebView: Audio track published successfully!');

                    // Update status
                    document.getElementById('status').innerHTML = 'Connected to channel: ${channel}';
                    
                    // Notify React Native
                    window.ReactNativeWebView.postMessage(JSON.stringify({
                        type: 'connected',
                        channel: '${channel}',
                        uid: ${uid}
                    }));

                } catch (error) {
                    console.error('❌ Mobile WebView: Agora error:', error);
                    document.getElementById('status').innerHTML = 'Error: ' + error.message;
                    
                    // Notify React Native
                    window.ReactNativeWebView.postMessage(JSON.stringify({
                        type: 'error',
                        message: error.message
                    }));
                }
            }

            // Initialize when page loads
            window.addEventListener('load', initializeAgora);

            // Cleanup function
            window.cleanup = async () => {
                try {
                    if (localTracks.audioTrack) {
                        localTracks.audioTrack.close();
                        localTracks.audioTrack = null;
                    }
                    
                    if (client && isJoined) {
                        await client.leave();
                        isJoined = false;
                        console.log('📱 Mobile WebView: Left channel');
                    }
                } catch (error) {
                    console.error('❌ Mobile WebView: Cleanup error:', error);
                }
            };
        </script>
    </body>
    </html>
  `;

  const handleMessage = (event) => {
    try {
      const data = JSON.parse(event.nativeEvent.data);
      console.log('📱 Mobile WebView received message:', data);
      
      switch (data.type) {
        case 'connected':
          console.log('✅ Mobile WebView: Connected to Agora channel');
          break;
        case 'user_joined':
          console.log('👤 Mobile WebView: User joined:', data.uid);
          onUserJoined?.(data.uid);
          break;
        case 'user_left':
          console.log('👤 Mobile WebView: User left:', data.uid);
          onUserLeft?.(data.uid);
          break;
        case 'error':
          console.error('❌ Mobile WebView: Error:', data.message);
          onError?.(data.message);
          break;
      }
    } catch (error) {
      console.error('❌ Mobile WebView: Error parsing message:', error);
    }
  };

  useEffect(() => {
    return () => {
      // Cleanup when component unmounts
      if (webViewRef.current) {
        webViewRef.current.postMessage(JSON.stringify({ type: 'cleanup' }));
      }
    };
  }, []);

  if (!isVisible) {
    return null;
  }

  return (
    <WebView
      ref={webViewRef}
      source={{ html: agoraHTML }}
      onMessage={handleMessage}
      style={{ flex: 1 }}
      javaScriptEnabled={true}
      domStorageEnabled={true}
      startInLoadingState={true}
      mixedContentMode="compatibility"
      allowsInlineMediaPlayback={true}
      mediaPlaybackRequiresUserAction={false}
    />
  );
};

export default AgoraWebView;
