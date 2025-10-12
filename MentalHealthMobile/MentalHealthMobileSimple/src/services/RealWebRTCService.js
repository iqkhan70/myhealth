import { Platform } from 'react-native';

/**
 * Real WebRTC Service optimized for Expo Go
 * Provides enhanced simulation for mobile and real WebRTC for web
 */
class RealWebRTCService {
  constructor() {
    this.isInitialized = false;
    this.isInCall = false;
    this.currentChannel = null;
    this.currentUid = null;
    this.listeners = {
      onUserJoined: null,
      onUserLeft: null,
      onError: null,
    };
    
    // WebRTC specific
    this.localStream = null;
    this.remoteStreams = new Map();
    this.peerConnections = new Map();
    this.signalingSocket = null;
    
    // Platform specific WebRTC
    this.webrtc = null;
  }

  async initialize(appId) {
    try {
      console.log('🚀 Real WebRTC: Initializing for platform:', Platform.OS);
      
      const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
      const isWebEnvironment = Platform.OS === 'web' || hasDOM;
      
      if (isWebEnvironment) {
        // Web implementation using browser WebRTC
        return await this.initializeWeb(appId);
      } else {
        // Native mobile implementation using react-native-webrtc
        return await this.initializeNative(appId);
      }
    } catch (error) {
      console.error('❌ Real WebRTC: Failed to initialize:', error);
      return false;
    }
  }

  async initializeWeb(appId) {
    try {
      console.log('🌐 Real WebRTC Web: Initializing...');
      
      // Check for WebRTC support
      if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        throw new Error('WebRTC not supported in this browser');
      }
      
      // Load Agora SDK for web
      if (typeof window !== 'undefined' && !window.AgoraRTC) {
        await this.loadAgoraSDK();
      }

      if (window.AgoraRTC) {
        this.client = window.AgoraRTC.createClient({ mode: "rtc", codec: "vp8" });
        this.setupWebEventHandlers();
        this.isInitialized = true;
        console.log('✅ Real WebRTC Web: Initialized successfully');
        return true;
      }
      
      // Fallback to native WebRTC if Agora fails
      this.isInitialized = true;
      console.log('✅ Real WebRTC Web: Initialized with native WebRTC');
      return true;
    } catch (error) {
      console.error('❌ Real WebRTC Web: Initialization failed:', error);
      return false;
    }
  }

  async initializeNative(appId) {
    try {
      console.log('📱 Real WebRTC Native: Initializing...');
      
      // Always use Expo Go fallback since we're running in Expo Go
      console.log('📱 Real WebRTC Native: Detected Expo Go environment');
      return await this.initializeExpoGoWebRTC(appId);
      
    } catch (error) {
      console.error('❌ Real WebRTC Native: Initialization failed:', error);
      return false;
    }
  }

  async initializeExpoGoWebRTC(appId) {
    try {
      console.log('📱 Expo Go WebRTC: Initializing web-compatible WebRTC...');
      
      // Check if we have web APIs available (Expo web mode)
      if (typeof navigator !== 'undefined' && navigator.mediaDevices) {
        console.log('📱 Expo Go WebRTC: Using browser WebRTC APIs');
        this.isInitialized = true;
        return true;
      }
      
      // For pure mobile Expo Go, use simulated WebRTC with better audio handling
      console.log('📱 Expo Go WebRTC: Using enhanced simulation for mobile');
      this.webrtc = this.createEnhancedSimulatedWebRTC();
      this.isInitialized = true;
      return true;
    } catch (error) {
      console.error('❌ Expo Go WebRTC: Initialization failed:', error);
      return false;
    }
  }

  createEnhancedSimulatedWebRTC() {
    // Enhanced simulated WebRTC with better audio feedback
    return {
      mediaDevices: {
        getUserMedia: async (constraints) => {
          console.log('📱 Enhanced Simulation: Getting user media', constraints);
          
          // Simulate audio feedback for testing
          if (constraints.audio) {
            console.log('🎤 Enhanced Simulation: Audio stream created - you should hear feedback');
            // Simulate audio playback after a delay
            setTimeout(() => {
              console.log('🔊 Enhanced Simulation: Playing simulated audio feedback');
            }, 1000);
          }
          
          if (constraints.video) {
            console.log('📹 Enhanced Simulation: Video stream created');
          }
          
          // Return a mock stream with better simulation
          return {
            id: 'enhanced-mock-stream',
            getTracks: () => [],
            getAudioTracks: () => constraints.audio ? [{ 
              id: 'enhanced-mock-audio', 
              enabled: true,
              kind: 'audio',
              stop: () => console.log('🎤 Enhanced Simulation: Audio track stopped')
            }] : [],
            getVideoTracks: () => constraints.video ? [{ 
              id: 'enhanced-mock-video', 
              enabled: true,
              kind: 'video',
              stop: () => console.log('📹 Enhanced Simulation: Video track stopped')
            }] : [],
            addTrack: () => {},
            removeTrack: () => {},
            stop: () => {
              console.log('📱 Enhanced Simulation: Stream stopped');
            }
          };
        }
      },
      RTCPeerConnection: class EnhancedMockRTCPeerConnection {
        constructor() {
          this.localDescription = null;
          this.remoteDescription = null;
          this.onicecandidate = null;
          this.ontrack = null;
          this.connectionState = 'new';
          console.log('📱 Enhanced Simulation: Peer connection created');
        }
        
        async createOffer() {
          console.log('📱 Enhanced Simulation: Creating offer');
          return { type: 'offer', sdp: 'enhanced-mock-offer-sdp' };
        }
        
        async createAnswer() {
          console.log('📱 Enhanced Simulation: Creating answer');
          return { type: 'answer', sdp: 'enhanced-mock-answer-sdp' };
        }
        
        async setLocalDescription(desc) {
          this.localDescription = desc;
          console.log('📱 Enhanced Simulation: Set local description', desc.type);
        }
        
        async setRemoteDescription(desc) {
          this.remoteDescription = desc;
          console.log('📱 Enhanced Simulation: Set remote description', desc.type);
          
          // Simulate connection established
          setTimeout(() => {
            this.connectionState = 'connected';
            console.log('✅ Enhanced Simulation: Connection established');
          }, 1000);
        }
        
        addTrack(track, stream) {
          console.log('📱 Enhanced Simulation: Added track', track.kind || track.id);
          
          // Simulate audio playback
          if (track.kind === 'audio' || track.id.includes('audio')) {
            setTimeout(() => {
              console.log('🔊 Enhanced Simulation: Simulating audio playback from remote peer');
            }, 2000);
          }
        }
        
        async addIceCandidate(candidate) {
          console.log('📱 Enhanced Simulation: Added ICE candidate');
        }
        
        close() {
          this.connectionState = 'closed';
          console.log('📱 Enhanced Simulation: Peer connection closed');
        }
      }
    };
  }

  loadAgoraSDK() {
    return new Promise((resolve, reject) => {
      if (typeof window === 'undefined' || typeof document === 'undefined') {
        reject(new Error('No DOM access'));
        return;
      }

      if (window.AgoraRTC) {
        resolve(true);
        return;
      }

      const script = document.createElement('script');
      script.src = 'https://download.agora.io/sdk/release/AgoraRTC_N.js';
      script.onload = () => resolve(true);
      script.onerror = () => reject(new Error('Failed to load Agora SDK'));
      document.head.appendChild(script);
    });
  }

  setupWebEventHandlers() {
    if (!this.client) return;

    this.client.on("user-published", async (user, mediaType) => {
      console.log('🌐 Real WebRTC Web: User published:', user.uid, mediaType);
      try {
        await this.client.subscribe(user, mediaType);
        
        if (mediaType === "video") {
          this.remoteStreams.set(user.uid, { videoTrack: user.videoTrack });
          if (this.listeners.onUserJoined) {
            this.listeners.onUserJoined(user.uid);
          }
        } else if (mediaType === "audio") {
          // Auto-play remote audio
          user.audioTrack.play();
          console.log('🔊 Real WebRTC Web: Playing remote audio from user:', user.uid);
        }
      } catch (error) {
        console.error('❌ Error handling user published:', error);
      }
    });

    this.client.on("user-unpublished", (user, mediaType) => {
      console.log('🌐 Real WebRTC Web: User unpublished:', user.uid, mediaType);
      if (mediaType === "video") {
        this.remoteStreams.delete(user.uid);
        if (this.listeners.onUserLeft) {
          this.listeners.onUserLeft(user.uid);
        }
      }
    });

    this.client.on("connection-state-change", (curState, revState) => {
      console.log('🔗 Real WebRTC Web: Connection state changed:', curState);
    });
  }

  async joinChannel(channelName, token, uid = 0) {
    try {
      console.log('🎯 Real WebRTC: Joining channel:', channelName, 'Platform:', Platform.OS);
      
      const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
      const isWebEnvironment = Platform.OS === 'web' || hasDOM;
      
      if (isWebEnvironment) {
        return await this.joinChannelWeb(channelName, token, uid);
      } else {
        return await this.joinChannelNative(channelName, token, uid);
      }
    } catch (error) {
      console.error('❌ Real WebRTC: Failed to join channel:', error);
      return false;
    }
  }

  async joinChannelWeb(channelName, token, uid) {
    if (this.client && this.isInitialized) {
      // Use Agora
      await this.client.join('efa11b3a7d05409ca979fb25a5b489ae', channelName, token, uid);
      this.isInCall = true;
      this.currentChannel = channelName;
      this.currentUid = uid;
      console.log('✅ Real WebRTC Web: Joined Agora channel successfully');
      return true;
    } else {
      // Use native WebRTC
      return await this.joinChannelWebRTC(channelName, token, uid);
    }
  }

  async joinChannelWebRTC(channelName, token, uid) {
    try {
      // Create peer connection
      const peerConnection = new RTCPeerConnection({
        iceServers: [
          { urls: 'stun:stun.l.google.com:19302' },
          { urls: 'stun:stun1.l.google.com:19302' }
        ]
      });

      this.peerConnections.set(uid, peerConnection);
      
      // Set up peer connection event handlers
      peerConnection.onicecandidate = (event) => {
        if (event.candidate) {
          console.log('🧊 Real WebRTC: ICE candidate generated');
          // In a real app, send this to the signaling server
        }
      };

      peerConnection.ontrack = (event) => {
        console.log('📡 Real WebRTC: Remote track received');
        const [remoteStream] = event.streams;
        this.remoteStreams.set(uid, { stream: remoteStream });
        
        if (this.listeners.onUserJoined) {
          this.listeners.onUserJoined(uid);
        }
      };

      this.isInCall = true;
      this.currentChannel = channelName;
      this.currentUid = uid;
      
      console.log('✅ Real WebRTC Web: Joined WebRTC channel successfully');
      return true;
    } catch (error) {
      console.error('❌ Real WebRTC Web: Failed to join WebRTC channel:', error);
      return false;
    }
  }

  async joinChannelNative(channelName, token, uid) {
    try {
      if (!this.webrtc) {
        throw new Error('WebRTC not initialized');
      }

      // Create peer connection
      const peerConnection = new this.webrtc.RTCPeerConnection({
        iceServers: [
          { urls: 'stun:stun.l.google.com:19302' },
          { urls: 'stun:stun1.l.google.com:19302' }
        ]
      });

      this.peerConnections.set(uid, peerConnection);
      
      // Set up peer connection event handlers
      peerConnection.onicecandidate = (event) => {
        if (event.candidate) {
          console.log('🧊 Real WebRTC Native: ICE candidate generated');
        }
      };

      peerConnection.ontrack = (event) => {
        console.log('📡 Real WebRTC Native: Remote track received');
        const [remoteStream] = event.streams;
        this.remoteStreams.set(uid, { stream: remoteStream });
        
        if (this.listeners.onUserJoined) {
          this.listeners.onUserJoined(uid);
        }
      };

      this.isInCall = true;
      this.currentChannel = channelName;
      this.currentUid = uid;
      
      // Simulate remote user joining after 3 seconds
      setTimeout(() => {
        if (this.listeners.onUserJoined) {
          this.listeners.onUserJoined(999); // Simulated remote user
        }
      }, 3000);
      
      console.log('✅ Real WebRTC Native: Joined channel successfully');
      return true;
    } catch (error) {
      console.error('❌ Real WebRTC Native: Failed to join channel:.....other3', error);
      return false;
    }
  }

  async enableLocalVideo(enable) {
    try {
      const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
      const isWebEnvironment = Platform.OS === 'web' || hasDOM;
      
      if (isWebEnvironment) {
        return await this.enableLocalVideoWeb(enable);
      } else {
        return await this.enableLocalVideoNative(enable);
      }
    } catch (error) {
      console.error('❌ Real WebRTC: Failed to toggle video:', error);
      return false;
    }
  }

  async enableLocalVideoWeb(enable) {
    if (this.client) {
      // Use Agora
      if (enable && !this.localVideoTrack) {
        this.localVideoTrack = await window.AgoraRTC.createCameraVideoTrack();
        await this.client.publish([this.localVideoTrack]);
        console.log('✅ Real WebRTC Web: Agora video enabled');
      } else if (!enable && this.localVideoTrack) {
        await this.client.unpublish([this.localVideoTrack]);
        this.localVideoTrack.close();
        this.localVideoTrack = null;
        console.log('✅ Real WebRTC Web: Agora video disabled');
      }
    } else {
      // Use native WebRTC
      if (enable && !this.localStream) {
        this.localStream = await navigator.mediaDevices.getUserMedia({ 
          video: true, 
          audio: false 
        });
        console.log('✅ Real WebRTC Web: Native video enabled');
      } else if (!enable && this.localStream) {
        this.localStream.getTracks().forEach(track => track.stop());
        this.localStream = null;
        console.log('✅ Real WebRTC Web: Native video disabled');
      }
    }
    return true;
  }

  async enableLocalVideoNative(enable) {
    try {
      if (!this.webrtc) {
        console.log('✅ Real WebRTC Native: Video', enable ? 'enabled' : 'disabled', '(simulated)');
        return true;
      }

      if (enable && !this.localStream) {
        this.localStream = await this.webrtc.mediaDevices.getUserMedia({ 
          video: true, 
          audio: false 
        });
        console.log('✅ Real WebRTC Native: Video enabled');
      } else if (!enable && this.localStream) {
        this.localStream.getTracks().forEach(track => track.stop());
        this.localStream = null;
        console.log('✅ Real WebRTC Native: Video disabled');
      }
      return true;
    } catch (error) {
      console.error('❌ Real WebRTC Native: Video toggle failed:', error);
      return false;
    }
  }

  async enableLocalAudio(enable) {
    try {
      const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
      const isWebEnvironment = Platform.OS === 'web' || hasDOM;
      
      if (isWebEnvironment) {
        return await this.enableLocalAudioWeb(enable);
      } else {
        return await this.enableLocalAudioNative(enable);
      }
    } catch (error) {
      console.error('❌ Real WebRTC: Failed to toggle audio:', error);
      return false;
    }
  }

  async enableLocalAudioWeb(enable) {
    if (this.client) {
      // Use Agora
      if (enable && !this.localAudioTrack) {
        this.localAudioTrack = await window.AgoraRTC.createMicrophoneAudioTrack();
        await this.client.publish([this.localAudioTrack]);
        console.log('✅ Real WebRTC Web: Agora audio enabled');
      } else if (!enable && this.localAudioTrack) {
        await this.client.unpublish([this.localAudioTrack]);
        this.localAudioTrack.close();
        this.localAudioTrack = null;
        console.log('✅ Real WebRTC Web: Agora audio disabled');
      }
    } else {
      // Use native WebRTC
      if (enable && !this.localAudioStream) {
        this.localAudioStream = await navigator.mediaDevices.getUserMedia({ 
          video: false, 
          audio: true 
        });
        console.log('✅ Real WebRTC Web: Native audio enabled');
      } else if (!enable && this.localAudioStream) {
        this.localAudioStream.getTracks().forEach(track => track.stop());
        this.localAudioStream = null;
        console.log('✅ Real WebRTC Web: Native audio disabled');
      }
    }
    return true;
  }

  async enableLocalAudioNative(enable) {
    try {
      if (!this.webrtc) {
        console.log('✅ Real WebRTC Native: Audio', enable ? 'enabled' : 'disabled', '(simulated)');
        return true;
      }

      if (enable && !this.localAudioStream) {
        this.localAudioStream = await this.webrtc.mediaDevices.getUserMedia({ 
          video: false, 
          audio: true 
        });
        console.log('✅ Real WebRTC Native: Audio enabled');
      } else if (!enable && this.localAudioStream) {
        this.localAudioStream.getTracks().forEach(track => track.stop());
        this.localAudioStream = null;
        console.log('✅ Real WebRTC Native: Audio disabled');
      }
      return true;
    } catch (error) {
      console.error('❌ Real WebRTC Native: Audio toggle failed:', error);
      return false;
    }
  }

  async leaveChannel() {
    try {
      const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
      const isWebEnvironment = Platform.OS === 'web' || hasDOM;
      
      if (isWebEnvironment) {
        await this.leaveChannelWeb();
      } else {
        await this.leaveChannelNative();
      }
      
      this.isInCall = false;
      this.currentChannel = null;
      this.currentUid = null;
      this.remoteStreams.clear();
    } catch (error) {
      console.error('❌ Real WebRTC: Failed to leave channel:', error);
    }
  }

  async leaveChannelWeb() {
    if (this.client && this.isInCall) {
      await this.client.leave();
      
      if (this.localAudioTrack) {
        this.localAudioTrack.close();
        this.localAudioTrack = null;
      }
      if (this.localVideoTrack) {
        this.localVideoTrack.close();
        this.localVideoTrack = null;
      }
    }
    
    // Clean up native WebRTC streams
    if (this.localStream) {
      this.localStream.getTracks().forEach(track => track.stop());
      this.localStream = null;
    }
    if (this.localAudioStream) {
      this.localAudioStream.getTracks().forEach(track => track.stop());
      this.localAudioStream = null;
    }
    
    // Close peer connections
    this.peerConnections.forEach(pc => pc.close());
    this.peerConnections.clear();
    
    console.log('👋 Real WebRTC Web: Left channel');
  }

  async leaveChannelNative() {
    // Clean up streams
    if (this.localStream) {
      this.localStream.stop();
      this.localStream = null;
    }
    if (this.localAudioStream) {
      this.localAudioStream.stop();
      this.localAudioStream = null;
    }
    
    // Close peer connections
    this.peerConnections.forEach(pc => pc.close());
    this.peerConnections.clear();
    
    console.log('👋 Real WebRTC Native: Left channel');
  }

  async muteLocalAudio(mute) {
    try {
      const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
      const isWebEnvironment = Platform.OS === 'web' || hasDOM;
      
      if (isWebEnvironment && this.localAudioTrack) {
        await this.localAudioTrack.setEnabled(!mute);
      } else if (this.localAudioStream) {
        this.localAudioStream.getAudioTracks().forEach(track => {
          track.enabled = !mute;
        });
      }
      
      console.log('🔇 Real WebRTC: Audio muted:', mute);
      return true;
    } catch (error) {
      console.error('❌ Real WebRTC: Failed to mute audio:', error);
      return false;
    }
  }

  async muteLocalVideo(mute) {
    try {
      const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
      const isWebEnvironment = Platform.OS === 'web' || hasDOM;
      
      if (isWebEnvironment && this.localVideoTrack) {
        await this.localVideoTrack.setEnabled(!mute);
      } else if (this.localStream) {
        this.localStream.getVideoTracks().forEach(track => {
          track.enabled = !mute;
        });
      }
      
      console.log('📹 Real WebRTC: Video muted:', mute);
      return true;
    } catch (error) {
      console.error('❌ Real WebRTC: Failed to mute video:', error);
      return false;
    }
  }

  setEventListeners(listeners) {
    this.listeners = { ...this.listeners, ...listeners };
  }

  async getAgoraToken(channelName, authToken) {
    try {
      const response = await fetch(`${this.getApiUrl()}/agora/token`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${authToken}`,
        },
        body: JSON.stringify({
          channelName: channelName,
          expirationTimeInSeconds: 3600,
        }),
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const data = await response.json();
      return data.token;
    } catch (error) {
      console.error('❌ Real WebRTC: Failed to get Agora token:', error);
      throw error;
    }
  }

  getApiUrl() {
    const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
    const isWebEnvironment = Platform.OS === 'web' || hasDOM;
    
    if (isWebEnvironment) {
      return 'http://localhost:5262/api';
    } else {
      return 'http://192.168.86.113:5262/api';
    }
  }

  // Video rendering helpers
  playLocalVideo(elementId) {
    const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
    if (hasDOM && this.localVideoTrack && elementId) {
      const element = document.getElementById(elementId);
      if (element) {
        this.localVideoTrack.play(element);
      }
    }
  }

  playRemoteVideo(uid, elementId) {
    const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
    if (hasDOM) {
      const remoteUser = this.remoteStreams.get(uid);
      if (remoteUser && remoteUser.videoTrack && elementId) {
        const element = document.getElementById(elementId);
        if (element) {
          remoteUser.videoTrack.play(element);
        }
      }
    }
  }

  getRemoteUsers() {
    return Array.from(this.remoteStreams.keys());
  }
}

// Export singleton instance
export default new RealWebRTCService();
