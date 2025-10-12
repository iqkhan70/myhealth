// Web-compatible Agora service that works in both mobile web and desktop web
class WebAgoraService {
  constructor() {
    this.client = null;
    this.localAudioTrack = null;
    this.localVideoTrack = null;
    this.remoteUsers = new Map();
    this.isInitialized = false;
    this.isInCall = false;
    this.currentChannel = null;
    this.currentUid = null;
    this.listeners = {
      onUserJoined: null,
      onUserLeft: null,
      onError: null,
    };
  }

  async initialize(appId) {
    try {
      console.log('🚀 Mobile Web: Initializing Agora with App ID:', appId);
      console.log('🚀 Mobile Web: Window available:', typeof window !== 'undefined');
      console.log('🚀 Mobile Web: AgoraRTC available:', typeof window !== 'undefined' && !!window.AgoraRTC);
      
      // Check if AgoraRTC is available (loaded from CDN)
      if (typeof window !== 'undefined' && window.AgoraRTC) {
        // Create Agora client
        this.client = window.AgoraRTC.createClient({ 
          mode: "rtc", 
          codec: "vp8" 
        });

        // Set up event handlers
        this.client.on("user-published", async (user, mediaType) => {
          console.log('📱 Mobile Web: User published:', user.uid, mediaType);
          try {
            await this.client.subscribe(user, mediaType);
            
            if (mediaType === "video") {
              this.remoteUsers.set(user.uid, { videoTrack: user.videoTrack });
              // Auto-play remote video
              setTimeout(() => {
                this.playRemoteVideo(user.uid, 'remoteVideo');
              }, 500);
              
              if (this.listeners.onUserJoined) {
                this.listeners.onUserJoined(user.uid);
              }
            } else if (mediaType === "audio") {
              user.audioTrack.play();
              console.log('🔊 Mobile Web: Playing remote audio from user:', user.uid);
            }
          } catch (error) {
            console.error('❌ Error handling user published:', error);
          }
        });

        this.client.on("user-unpublished", (user, mediaType) => {
          console.log('📱 Mobile Web: User unpublished:', user.uid, mediaType);
          if (mediaType === "video") {
            this.remoteUsers.delete(user.uid);
            if (this.listeners.onUserLeft) {
              this.listeners.onUserLeft(user.uid);
            }
          }
        });

        this.client.on("connection-state-change", (curState, revState) => {
          console.log('🔗 Mobile Web: Connection state changed:', curState);
        });

        this.client.on("exception", (evt) => {
          console.error('❌ Mobile Web: Agora exception:', evt);
        });

        this.isInitialized = true;
        console.log('✅ Mobile Web: Agora initialized successfully');
        return true;
      } else {
        console.error('❌ Mobile Web: AgoraRTC SDK not loaded');
        console.error('❌ Mobile Web: Window type:', typeof window);
        console.error('❌ Mobile Web: AgoraRTC type:', typeof window !== 'undefined' ? typeof window.AgoraRTC : 'window undefined');
        return false;
      }
    } catch (error) {
      console.error('❌ Mobile Web: Failed to initialize Agora:', error);
      return false;
    }
  }

  async joinChannel(channelName, token, uid = 0) {
    try {
      if (!this.isInitialized || !this.client) {
        throw new Error('Agora not initialized');
      }

      console.log('🎯 Mobile Web: Joining channel:', channelName, 'with UID:', uid);
      
      // Join channel
      await this.client.join('efa11b3a7d05409ca979fb25a5b489ae', channelName, token, uid);
      
      this.isInCall = true;
      this.currentChannel = channelName;
      this.currentUid = uid;
      
      console.log('✅ Mobile Web: Joined channel successfully');
      return true;
    } catch (error) {
      console.error('❌ Mobile Web: Failed to join channel:.....other4', error);
      return false;
    }
  }

  async leaveChannel() {
    try {
      if (this.client && this.isInCall) {
        console.log('👋 Mobile Web: Leaving channel');
        await this.client.leave();
        
        // Clean up tracks
        if (this.localAudioTrack) {
          this.localAudioTrack.close();
          this.localAudioTrack = null;
        }
        if (this.localVideoTrack) {
          this.localVideoTrack.close();
          this.localVideoTrack = null;
        }
        
        this.isInCall = false;
        this.currentChannel = null;
        this.currentUid = null;
        this.remoteUsers.clear();
      }
    } catch (error) {
      console.error('❌ Mobile Web: Failed to leave channel:', error);
    }
  }

  async enableLocalVideo(enable) {
    try {
      if (!this.client) return false;

      if (enable && !this.localVideoTrack) {
        // Create and publish video track
        this.localVideoTrack = await window.AgoraRTC.createCameraVideoTrack();
        await this.client.publish([this.localVideoTrack]);
        console.log('✅ Mobile Web: Video track published');
      } else if (!enable && this.localVideoTrack) {
        // Unpublish and close video track
        await this.client.unpublish([this.localVideoTrack]);
        this.localVideoTrack.close();
        this.localVideoTrack = null;
        console.log('✅ Mobile Web: Video track stopped');
      }
      return true;
    } catch (error) {
      console.error('❌ Mobile Web: Failed to toggle video:', error);
      return false;
    }
  }

  async enableLocalAudio(enable) {
    try {
      if (!this.client) return false;

      if (enable && !this.localAudioTrack) {
        // Create and publish audio track
        this.localAudioTrack = await window.AgoraRTC.createMicrophoneAudioTrack();
        await this.client.publish([this.localAudioTrack]);
        console.log('✅ Mobile Web: Audio track published');
      } else if (!enable && this.localAudioTrack) {
        // Unpublish and close audio track
        await this.client.unpublish([this.localAudioTrack]);
        this.localAudioTrack.close();
        this.localAudioTrack = null;
        console.log('✅ Mobile Web: Audio track stopped');
      }
      return true;
    } catch (error) {
      console.error('❌ Mobile Web: Failed to toggle audio:', error);
      return false;
    }
  }

  async muteLocalAudio(mute) {
    try {
      if (this.localAudioTrack) {
        await this.localAudioTrack.setEnabled(!mute);
        console.log('🔇 Mobile Web: Audio muted:', mute);
        return true;
      }
      return false;
    } catch (error) {
      console.error('❌ Mobile Web: Failed to mute audio:', error);
      return false;
    }
  }

  async muteLocalVideo(mute) {
    try {
      if (this.localVideoTrack) {
        await this.localVideoTrack.setEnabled(!mute);
        console.log('📹 Mobile Web: Video muted:', mute);
        return true;
      }
      return false;
    } catch (error) {
      console.error('❌ Mobile Web: Failed to mute video:', error);
      return false;
    }
  }

  async switchCamera() {
    try {
      if (this.localVideoTrack) {
        // Get available cameras
        const devices = await window.AgoraRTC.getCameras();
        if (devices.length > 1) {
          // Switch to next camera (simplified)
          console.log('📷 Mobile Web: Switching camera (feature limited in web)');
        }
        return true;
      }
      return false;
    } catch (error) {
      console.error('❌ Mobile Web: Failed to switch camera:', error);
      return false;
    }
  }

  setEventListeners(listeners) {
    this.listeners = { ...this.listeners, ...listeners };
  }

  async destroy() {
    try {
      await this.leaveChannel();
      this.client = null;
      this.isInitialized = false;
    } catch (error) {
      console.error('❌ Mobile Web: Failed to destroy Agora:', error);
    }
  }

  // Helper method to get Agora token from server
  async getAgoraToken(channelName, authToken) {
    try {
      console.log('🎯 Mobile Web: Getting Agora token for channel:', channelName);
      
      const response = await fetch('http://192.168.86.113:5262/api/agora/token', {
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
      console.log('✅ Mobile Web: Got Agora token:', data.token?.substring(0, 20) + '...');
      
      return data.token;
    } catch (error) {
      console.error('❌ Mobile Web: Failed to get Agora token:', error);
      throw error;
    }
  }

  // Helper methods for video rendering
  playLocalVideo(elementId) {
    if (this.localVideoTrack && elementId) {
      const element = document.getElementById(elementId);
      if (element) {
        this.localVideoTrack.play(element);
      }
    }
  }

  playRemoteVideo(uid, elementId) {
    const remoteUser = this.remoteUsers.get(uid);
    if (remoteUser && remoteUser.videoTrack && elementId) {
      const element = document.getElementById(elementId);
      if (element) {
        remoteUser.videoTrack.play(element);
      }
    }
  }

  getRemoteUsers() {
    return Array.from(this.remoteUsers.keys());
  }
}

// Export singleton instance
export default new WebAgoraService();
