import {
  RtcEngine,
  RtcLocalView,
  RtcRemoteView,
  VideoRenderMode,
  ClientRole,
  ChannelProfile,
} from 'react-native-agora';

class AgoraService {
  constructor() {
    this.engine = null;
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
      console.log('🚀 Mobile: Initializing Agora with App ID:', appId);
      
      // Create RTC engine
      this.engine = await RtcEngine.create(appId);
      
      // Enable video
      await this.engine.enableVideo();
      
      // Set channel profile to communication
      await this.engine.setChannelProfile(ChannelProfile.Communication);
      
      // Set client role to broadcaster (for two-way communication)
      await this.engine.setClientRole(ClientRole.Broadcaster);
      
      // Set up event listeners
      this.engine.addListener('UserJoined', (uid, elapsed) => {
        console.log('📱 Mobile: User joined:', uid);
        if (this.listeners.onUserJoined) {
          this.listeners.onUserJoined(uid);
        }
      });
      
      this.engine.addListener('UserOffline', (uid, reason) => {
        console.log('📱 Mobile: User left:', uid, reason);
        if (this.listeners.onUserLeft) {
          this.listeners.onUserLeft(uid);
        }
      });
      
      this.engine.addListener('Error', (err) => {
        console.error('❌ Mobile: Agora error:', err);
        if (this.listeners.onError) {
          this.listeners.onError(err);
        }
      });
      
      this.engine.addListener('JoinChannelSuccess', (channel, uid, elapsed) => {
        console.log('✅ Mobile: Joined channel successfully:', channel, uid);
        this.isInCall = true;
        this.currentChannel = channel;
        this.currentUid = uid;
      });
      
      this.isInitialized = true;
      console.log('✅ Mobile: Agora initialized successfully');
      return true;
    } catch (error) {
      console.error('❌ Mobile: Failed to initialize Agora:', error);
      return false;
    }
  }

  async joinChannel(channelName, token, uid = 0) {
    try {
      if (!this.isInitialized || !this.engine) {
        throw new Error('Agora not initialized');
      }

      console.log('🎯 Mobile: Joining channel:', channelName, 'with UID:', uid);
      
      // Join channel
      await this.engine.joinChannel(token, channelName, null, uid);
      
      return true;
    } catch (error) {
      console.error('❌ Mobile: Failed to join channel:....other1', error);
      return false;
    }
  }

  async leaveChannel() {
    try {
      if (this.engine && this.isInCall) {
        console.log('👋 Mobile: Leaving channel');
        await this.engine.leaveChannel();
        this.isInCall = false;
        this.currentChannel = null;
        this.currentUid = null;
      }
    } catch (error) {
      console.error('❌ Mobile: Failed to leave channel:', error);
    }
  }

  async enableLocalVideo(enable) {
    try {
      if (this.engine) {
        if (enable) {
          await this.engine.enableLocalVideo(true);
        } else {
          await this.engine.enableLocalVideo(false);
        }
        return true;
      }
      return false;
    } catch (error) {
      console.error('❌ Mobile: Failed to toggle video:', error);
      return false;
    }
  }

  async enableLocalAudio(enable) {
    try {
      if (this.engine) {
        if (enable) {
          await this.engine.enableLocalAudio(true);
        } else {
          await this.engine.enableLocalAudio(false);
        }
        return true;
      }
      return false;
    } catch (error) {
      console.error('❌ Mobile: Failed to toggle audio:', error);
      return false;
    }
  }

  async muteLocalAudio(mute) {
    try {
      if (this.engine) {
        await this.engine.muteLocalAudioStream(mute);
        return true;
      }
      return false;
    } catch (error) {
      console.error('❌ Mobile: Failed to mute audio:', error);
      return false;
    }
  }

  async muteLocalVideo(mute) {
    try {
      if (this.engine) {
        await this.engine.muteLocalVideoStream(mute);
        return true;
      }
      return false;
    } catch (error) {
      console.error('❌ Mobile: Failed to mute video:', error);
      return false;
    }
  }

  async switchCamera() {
    try {
      if (this.engine) {
        await this.engine.switchCamera();
        return true;
      }
      return false;
    } catch (error) {
      console.error('❌ Mobile: Failed to switch camera:', error);
      return false;
    }
  }

  setEventListeners(listeners) {
    this.listeners = { ...this.listeners, ...listeners };
  }

  async destroy() {
    try {
      if (this.isInCall) {
        await this.leaveChannel();
      }
      if (this.engine) {
        await RtcEngine.destroy();
        this.engine = null;
        this.isInitialized = false;
      }
    } catch (error) {
      console.error('❌ Mobile: Failed to destroy Agora:', error);
    }
  }

  // Helper method to get Agora token from server
  async getAgoraToken(channelName, authToken) {
    try {
      console.log('🎯 Mobile: Getting Agora token for channel:', channelName);
      
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
      console.log('✅ Mobile: Got Agora token:', data.token?.substring(0, 20) + '...');
      
      return data.token;
    } catch (error) {
      console.error('❌ Mobile: Failed to get Agora token:', error);
      throw error;
    }
  }
}

// Export singleton instance
export default new AgoraService();

// Export components for video rendering
export { RtcLocalView, RtcRemoteView, VideoRenderMode };
