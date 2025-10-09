import { Platform, Alert } from 'react-native';

/**
 * Native Video Service that works on both mobile and web
 * Uses different implementations based on platform
 */
class NativeVideoService {
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
    this.localStream = null;
    this.remoteStreams = new Map();
  }

  async initialize(appId) {
    try {
      console.log('🚀 Native Video: Initializing for platform:', Platform.OS);
      
      const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
      const isWebEnvironment = Platform.OS === 'web' || hasDOM;
      
      if (isWebEnvironment) {
        // Web implementation using WebRTC
        return await this.initializeWeb(appId);
      } else {
        // Native mobile implementation using simulated WebRTC
        return await this.initializeNative(appId);
      }
    } catch (error) {
      console.error('❌ Native Video: Failed to initialize:', error);
      return false;
    }
  }

  async initializeWeb(appId) {
    try {
      // Load Agora SDK for web
      if (typeof window !== 'undefined' && !window.AgoraRTC) {
        await this.loadAgoraSDK();
      }

      if (window.AgoraRTC) {
        this.client = window.AgoraRTC.createClient({ mode: "rtc", codec: "vp8" });
        this.setupWebEventHandlers();
        this.isInitialized = true;
        console.log('✅ Native Video: Web initialized successfully');
        return true;
      }
      return false;
    } catch (error) {
      console.error('❌ Native Video: Web initialization failed:', error);
      return false;
    }
  }

  async initializeNative(appId) {
    try {
      // For native mobile, we'll use a simulated WebRTC implementation
      // In a real app, you'd use react-native-webrtc or similar
      console.log('✅ Native Video: Native mobile initialized (simulated)');
      this.isInitialized = true;
      return true;
    } catch (error) {
      console.error('❌ Native Video: Native initialization failed:', error);
      return false;
    }
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
      console.log('📱 Native Video Web: User published:', user.uid, mediaType);
      try {
        await this.client.subscribe(user, mediaType);
        
        if (mediaType === "video") {
          this.remoteStreams.set(user.uid, { videoTrack: user.videoTrack });
          if (this.listeners.onUserJoined) {
            this.listeners.onUserJoined(user.uid);
          }
        } else if (mediaType === "audio") {
          user.audioTrack.play();
        }
      } catch (error) {
        console.error('❌ Error handling user published:', error);
      }
    });

    this.client.on("user-unpublished", (user, mediaType) => {
      console.log('📱 Native Video Web: User unpublished:', user.uid, mediaType);
      if (mediaType === "video") {
        this.remoteStreams.delete(user.uid);
        if (this.listeners.onUserLeft) {
          this.listeners.onUserLeft(user.uid);
        }
      }
    });
  }

  async joinChannel(channelName, token, uid = 0) {
    try {
      console.log('🎯 Native Video: Joining channel:', channelName, 'Platform:', Platform.OS);
      
      const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
      const isWebEnvironment = Platform.OS === 'web' || hasDOM;
      
      if (isWebEnvironment) {
        return await this.joinChannelWeb(channelName, token, uid);
      } else {
        return await this.joinChannelNative(channelName, token, uid);
      }
    } catch (error) {
      console.error('❌ Native Video: Failed to join channel:', error);
      return false;
    }
  }

  async joinChannelWeb(channelName, token, uid) {
    if (!this.client || !this.isInitialized) {
      throw new Error('Not initialized');
    }

    await this.client.join('efa11b3a7d05409ca979fb25a5b489ae', channelName, token, uid);
    this.isInCall = true;
    this.currentChannel = channelName;
    this.currentUid = uid;
    
    console.log('✅ Native Video Web: Joined channel successfully');
    return true;
  }

  async joinChannelNative(channelName, token, uid) {
    // Simulate joining channel for native mobile
    this.isInCall = true;
    this.currentChannel = channelName;
    this.currentUid = uid;
    
    // Simulate a remote user joining after 2 seconds
    setTimeout(() => {
      if (this.listeners.onUserJoined) {
        this.listeners.onUserJoined(999); // Simulated remote user
      }
    }, 2000);
    
    console.log('✅ Native Video Native: Joined channel successfully (simulated)');
    return true;
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
      console.error('❌ Native Video: Failed to toggle video:', error);
      return false;
    }
  }

  async enableLocalVideoWeb(enable) {
    if (!this.client) return false;

    if (enable && !this.localVideoTrack) {
      this.localVideoTrack = await window.AgoraRTC.createCameraVideoTrack();
      await this.client.publish([this.localVideoTrack]);
      console.log('✅ Native Video Web: Video enabled');
    } else if (!enable && this.localVideoTrack) {
      await this.client.unpublish([this.localVideoTrack]);
      this.localVideoTrack.close();
      this.localVideoTrack = null;
      console.log('✅ Native Video Web: Video disabled');
    }
    return true;
  }

  async enableLocalVideoNative(enable) {
    // Simulate video enable/disable for native
    console.log('✅ Native Video Native: Video', enable ? 'enabled' : 'disabled', '(simulated)');
    return true;
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
      console.error('❌ Native Video: Failed to toggle audio:', error);
      return false;
    }
  }

  async enableLocalAudioWeb(enable) {
    if (!this.client) return false;

    if (enable && !this.localAudioTrack) {
      this.localAudioTrack = await window.AgoraRTC.createMicrophoneAudioTrack();
      await this.client.publish([this.localAudioTrack]);
      console.log('✅ Native Video Web: Audio enabled');
    } else if (!enable && this.localAudioTrack) {
      await this.client.unpublish([this.localAudioTrack]);
      this.localAudioTrack.close();
      this.localAudioTrack = null;
      console.log('✅ Native Video Web: Audio disabled');
    }
    return true;
  }

  async enableLocalAudioNative(enable) {
    // Simulate audio enable/disable for native
    console.log('✅ Native Video Native: Audio', enable ? 'enabled' : 'disabled', '(simulated)');
    return true;
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
      console.error('❌ Native Video: Failed to leave channel:', error);
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
    console.log('👋 Native Video Web: Left channel');
  }

  async leaveChannelNative() {
    // Simulate leaving channel for native
    console.log('👋 Native Video Native: Left channel (simulated)');
  }

  async muteLocalAudio(mute) {
    try {
      const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
      const isWebEnvironment = Platform.OS === 'web' || hasDOM;
      
      if (isWebEnvironment && this.localAudioTrack) {
        await this.localAudioTrack.setEnabled(!mute);
      }
      
      console.log('🔇 Native Video: Audio muted:', mute);
      return true;
    } catch (error) {
      console.error('❌ Native Video: Failed to mute audio:', error);
      return false;
    }
  }

  async muteLocalVideo(mute) {
    try {
      const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
      const isWebEnvironment = Platform.OS === 'web' || hasDOM;
      
      if (isWebEnvironment && this.localVideoTrack) {
        await this.localVideoTrack.setEnabled(!mute);
      }
      
      console.log('📹 Native Video: Video muted:', mute);
      return true;
    } catch (error) {
      console.error('❌ Native Video: Failed to mute video:', error);
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
      console.error('❌ Native Video: Failed to get Agora token:', error);
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
export default new NativeVideoService();
