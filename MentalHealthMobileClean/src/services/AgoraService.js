// src/services/AgoraService.js
import { 
  createAgoraRtcEngine, 
  ChannelProfileType, 
  ClientRoleType 
} from 'react-native-agora';
import { Platform, PermissionsAndroid } from 'react-native';

class AgoraService {
  engine = null;
  listeners = {};
  currentChannel = null;

  async initialize(appId) {
    try {
      if (this.engine) return true;

      if (Platform.OS === 'android') {
        await PermissionsAndroid.requestMultiple([
          PermissionsAndroid.PERMISSIONS.CAMERA,
          PermissionsAndroid.PERMISSIONS.RECORD_AUDIO,
        ]);
      }

      console.log('🚀 Initializing Agora Engine...');
      this.engine = createAgoraRtcEngine();

      this.engine.initialize({
        appId,
        channelProfile: ChannelProfileType.ChannelProfileCommunication,
      });

      await this.engine.enableVideo();

      // 🎧 Event listeners
      this.engine.registerEventHandler({
        onJoinChannelSuccess: (connection, elapsed) => {
          console.log('✅ Joined channel successfully:', connection);
          this.currentChannel = connection.channelId;
          this._emit('onJoinSuccess', connection);
        },
        onUserJoined: (connection, remoteUid, elapsed) => {
          console.log('👤 Remote user joined:', remoteUid, 'connection:', connection);
          this._emit('onUserJoined', remoteUid);
        },
        onUserPublished: (connection, remoteUid, mediaType) => {
          console.log('📹 Remote user published:', remoteUid, 'mediaType:', mediaType, 'connection:', connection);
          // When remote user publishes video/audio, add them to remote users
          // mediaType: 0 = audio, 1 = video
          if (mediaType === 0 || mediaType === 1) {
            console.log('📹 Emitting onUserJoined for published user:', remoteUid);
            this._emit('onUserJoined', remoteUid);
          }
        },
        onFirstRemoteVideoDecoded: (connection, remoteUid, width, height, elapsed) => {
          console.log('📹 First remote video decoded:', remoteUid, 'width:', width, 'height:', height);
          // When remote video is decoded, user is definitely there
          this._emit('onUserJoined', remoteUid);
        },
        onFirstRemoteAudioFrame: (connection, remoteUid, elapsed) => {
          console.log('🎵 First remote audio frame:', remoteUid);
          // When remote audio is received, user is definitely there
          this._emit('onUserJoined', remoteUid);
        },
        onUserUnpublished: (connection, remoteUid, mediaType) => {
          console.log('📹 Remote user unpublished:', remoteUid, 'mediaType:', mediaType);
        },
        onUserOffline: (connection, remoteUid, reason) => {
          // Reason codes: 0 = USER_OFFLINE_QUIT, 1 = USER_OFFLINE_DROPPED, 2 = USER_OFFLINE_BECOME_AUDIENCE
          console.log('👋 Remote user left:', remoteUid, 'reason:', reason, 'reasonText:', 
            reason === 0 ? 'QUIT' : reason === 1 ? 'DROPPED' : reason === 2 ? 'BECOME_AUDIENCE' : 'UNKNOWN');
          // Only emit if reason is not 0 (QUIT) - reason 0 might be a false positive
          // Actually, let's emit all reasons but add logging
          this._emit('onUserLeft', remoteUid);
        },
        onError: (err, msg) => {
          console.error('❌ Agora error:', err, msg);
        },
      });

      console.log('✅ Agora Engine initialized.');
      return true;
    } catch (err) {
      console.error('❌ Agora initialize error:', err);
      return false;
    }
  }

  setListener(event, callback) {
    this.listeners[event] = callback;
  }

  _emit(event, data) {
    if (this.listeners[event]) this.listeners[event](data);
  }

  // async joinChannel({ token, channelName, uid }) {
  //   if (!this.engine) throw new Error('Agora not initialized');
  //   this.engine.joinChannel(token || null, channelName, uid, {
  //     clientRoleType: ClientRoleType.ClientRoleBroadcaster,
  //   });
  // }

  // async joinChannel({ token, channelName, uid, withVideo }) {
  //   if (!this.engine) return false;
  //   try {
  //     await this.engine.enableAudio();
  //     if (withVideo) await this.engine.enableVideo();

  //     console.log("📞 Joining Agora:", { channelName, uid, token });
  //     const result = await this.engine.joinChannel(token, channelName, uid);

  //     await this.engine.muteLocalAudioStream(false);
  //     await this.engine.enableLocalAudio(true);

  //     await this.engine.setEnableSpeakerphone(true);


  //     console.log("✅ Joined channel successfully:", result);
  //     return true;
  //   } catch (e) {
  //     console.error("startCall error:", e);
  //     return false;
  //   }
  // }

  async joinChannel({ token, channelName, uid, withVideo }) {
    if (!this.engine) {
      console.warn("⚠️ Engine not initialized");
      return false;
    }

    try {
      console.log("📞 Joining Agora:", { channelName, uid, token });

      // ensure correct role + profile
      await this.engine.setChannelProfile(1); // 1 = communication
      await this.engine.setClientRole(1);     // 1 = broadcaster

      await this.engine.enableAudio();
      if (withVideo) {
        await this.engine.enableVideo();
        await this.engine.startPreview(); // 👈 critical for iOS local camera
      }

      // join the channel
      const result = await this.engine.joinChannel(token, channelName, uid, {
        clientRoleType: 1, // broadcaster
        channelProfile: 1, // communication
      });

      // ensure audio is routed properly
      await this.engine.muteLocalAudioStream(false);
      await this.engine.enableLocalAudio(true);
      await this.engine.setEnableSpeakerphone(true);

      console.log("✅ Joined channel successfully:", result);
      
      // Give a moment for remote users to be detected, then check
      setTimeout(() => {
        console.log("🔍 Checking for existing remote users in channel...");
        // Note: Agora SDK doesn't provide a direct way to list users,
        // but onUserJoined should fire for users already in channel
      }, 2000);
      
      return true;
    } catch (e) {
      console.error("❌ joinChannel error:", e);
      return false;
    }
  }



  async leaveChannel() {
    if (!this.engine) return;
    this.engine.leaveChannel();
    this.currentChannel = null;
  }

  async muteLocalAudio(muted) {
    if (this.engine) this.engine.muteLocalAudioStream(muted);
  }

  async muteLocalVideo(muted) {
    if (this.engine) this.engine.muteLocalVideoStream(muted);
  }

  async destroy() {
    if (this.engine) {
      this.engine.release();
      this.engine = null;
      this.listeners = {};
      this.currentChannel = null;
    }
  }
}

export default new AgoraService();

