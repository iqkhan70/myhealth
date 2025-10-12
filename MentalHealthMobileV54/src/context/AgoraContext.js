import React, { createContext, useContext, useState, useEffect } from 'react';
import * as SecureStore from 'expo-secure-store';
import AgoraService from '../services/AgoraService';
import { authService } from '../services/authService';

const AgoraContext = createContext();

export const useAgora = () => {
  const context = useContext(AgoraContext);
  if (!context) {
    throw new Error('useAgora must be used within an AgoraProvider');
  }
  return context;
};

export const AgoraProvider = ({ children }) => {
  const [isInitialized, setIsInitialized] = useState(false);
  const [isInCall, setIsInCall] = useState(false);
  const [currentChannel, setCurrentChannel] = useState(null);
  const [currentUid, setCurrentUid] = useState(null);
  const [remoteUsers, setRemoteUsers] = useState([]);
  const [connectionState, setConnectionState] = useState('disconnected');
  const [error, setError] = useState(null);

  // Agora App ID - This should be configured
  const AGORA_APP_ID = 'efa11b3a7d05409ca979fb25a5b489ae'; // Replace with actual App ID

  useEffect(() => {
    initializeAgora();
    
    // Set up event handlers
    AgoraService.onUserJoined = handleUserJoined;
    AgoraService.onUserLeft = handleUserLeft;
    AgoraService.onConnectionStateChanged = handleConnectionStateChanged;
    AgoraService.onTokenWillExpire = handleTokenWillExpire;
    AgoraService.onError = handleError;

    return () => {
      // Cleanup
      AgoraService.destroy();
    };
  }, []);

  const initializeAgora = async () => {
    try {
      await AgoraService.initialize(AGORA_APP_ID);
      setIsInitialized(true);
      console.log('Agora context initialized');
    } catch (error) {
      console.error('Failed to initialize Agora context:', error);
      setError(error.message);
    }
  };

  const getAgoraToken = async (channelName) => {
    try {
      // Get token from secure storage
      const token = await SecureStore.getItemAsync('authToken');
      if (!token) {
        throw new Error('No authentication token found');
      }

      const response = await fetch('http://192.168.86.113:5262/api/agora/token', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          channelName: channelName,
          expirationTimeInSeconds: 3600
        })
      });

      if (!response.ok) {
        throw new Error(`Failed to get Agora token: ${response.statusText}`);
      }

      const data = await response.json();
      return data;
    } catch (error) {
      console.error('Failed to get Agora token:', error);
      throw error;
    }
  };

  const joinChannel = async (channelName, targetUserId) => {
    try {
      if (!isInitialized) {
        throw new Error('Agora not initialized');
      }

      if (isInCall) {
        console.log('Already in a call');
        return;
      }

      // Get token from server
      const tokenData = await getAgoraToken(channelName);
      
      // Generate a unique UID for this user
      const uid = Math.floor(Math.random() * 1000000);
      
      // Join channel
      await AgoraService.joinChannel(channelName, tokenData.token, uid);
      
      setIsInCall(true);
      setCurrentChannel(channelName);
      setCurrentUid(uid);
      setRemoteUsers([]);
      setConnectionState('connected');
      setError(null);
      
      console.log(`✅ Joined channel: ${channelName} with UID: ${uid}`);
      console.log('✅ Connection state set to: connected');
      
    } catch (error) {
      console.error('Failed to join channel:other6', error);
      setError(error.message);
      throw error;
    }
  };

  const leaveChannel = async () => {
    try {
      await AgoraService.leaveChannel();
      
      setIsInCall(false);
      setCurrentChannel(null);
      setCurrentUid(null);
      setRemoteUsers([]);
      setConnectionState('disconnected');
      
      console.log('✅ Left channel successfully');
      console.log('✅ Connection state set to: disconnected');
      
    } catch (error) {
      console.error('Failed to leave channel:', error);
      setError(error.message);
      throw error;
    }
  };

  const enableLocalVideo = async (enable = true) => {
    try {
      await AgoraService.enableLocalVideo(enable);
    } catch (error) {
      console.error('Failed to toggle local video:', error);
      setError(error.message);
    }
  };

  const enableLocalAudio = async (enable = true) => {
    try {
      await AgoraService.enableLocalAudio(enable);
    } catch (error) {
      console.error('Failed to toggle local audio:', error);
      setError(error.message);
    }
  };

  const switchCamera = async () => {
    try {
      await AgoraService.switchCamera();
    } catch (error) {
      console.error('Failed to switch camera:', error);
      setError(error.message);
    }
  };

  const muteLocalAudio = async (mute = true) => {
    try {
      await AgoraService.muteLocalAudio(mute);
    } catch (error) {
      console.error('Failed to mute/unmute audio:', error);
      setError(error.message);
    }
  };

  const muteLocalVideo = async (mute = true) => {
    try {
      await AgoraService.muteLocalVideo(mute);
    } catch (error) {
      console.error('Failed to mute/unmute video:', error);
      setError(error.message);
    }
  };

  // Event handlers
  const handleUserJoined = (uid) => {
    console.log(`User ${uid} joined`);
    setRemoteUsers(prev => [...prev, uid]);
  };

  const handleUserLeft = (uid) => {
    console.log(`User ${uid} left`);
    setRemoteUsers(prev => prev.filter(id => id !== uid));
  };

  const handleConnectionStateChanged = (state, reason) => {
    console.log(`Connection state: ${state}, reason: ${reason}`);
    setConnectionState(state === 3 ? 'connected' : 'disconnected'); // 3 = CONNECTION_STATE_CONNECTED
  };

  const handleTokenWillExpire = async () => {
    console.log('Token will expire, refreshing...');
    try {
      if (currentChannel) {
        const tokenData = await getAgoraToken(currentChannel);
        // Note: In a real implementation, you'd need to renew the token with Agora
        console.log('Token refreshed');
      }
    } catch (error) {
      console.error('Failed to refresh token:', error);
      setError(error.message);
    }
  };

  const handleError = (err, msg) => {
    console.error(`Agora error: ${err} - ${msg}`);
    setError(`${err}: ${msg}`);
  };

  const value = {
    // State
    isInitialized,
    isInCall,
    currentChannel,
    currentUid,
    remoteUsers,
    connectionState,
    error,
    
    // Actions
    joinChannel,
    leaveChannel,
    enableLocalVideo,
    enableLocalAudio,
    switchCamera,
    muteLocalAudio,
    muteLocalVideo,
    
    // Utils
    clearError: () => setError(null)
  };

  return (
    <AgoraContext.Provider value={value}>
      {children}
    </AgoraContext.Provider>
  );
};
