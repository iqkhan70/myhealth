import React, { useState, useEffect, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Alert,
  Dimensions,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
// Note: RTCView not available in Expo managed workflow
import WebRTCService from '../services/WebRTCService';
import { useRealtime } from '../context/RealtimeContext';

export default function SimpleVideoCallScreen({ route, navigation }) {
  const { patient, doctor, callType, isInitiator } = route.params || {};
  const targetUser = patient || doctor;
  const { isConnected } = useRealtime();
  
  const [localStream, setLocalStream] = useState(null);
  const [remoteStream, setRemoteStream] = useState(null);
  const [isMuted, setIsMuted] = useState(false);
  const [isVideoEnabled, setIsVideoEnabled] = useState(true);
  const [callStatus, setCallStatus] = useState('Connecting...');
  const [isCallActive, setIsCallActive] = useState(false);

  useEffect(() => {
    initializeCall();
    
    return () => {
      WebRTCService.endCall();
    };
  }, []);

  const initializeCall = async () => {
    try {
      // Initialize WebRTC service
        WebRTCService.initialize();
      
      // Set up callbacks
      WebRTCService.setCallbacks({
        onLocalStream: (stream) => {
          setLocalStream(stream);
          setCallStatus('Call Connected');
          setIsCallActive(true);
        },
        onRemoteStream: (stream) => {
          setRemoteStream(stream);
        },
        onCallEnded: () => {
          navigation.goBack();
        },
        onError: (error) => {
          console.error('WebRTC Error:', error);
          Alert.alert('Call Error', error.message);
          navigation.goBack();
        }
      });

      // Start the call
      const success = await WebRTCService.startCall(
        targetUser.id, 
        isInitiator, 
        callType || 'video'
      );

      if (!success) {
        Alert.alert('Call Failed', 'Unable to start the call');
        navigation.goBack();
      }
    } catch (error) {
      console.error('Error initializing call:', error);
      Alert.alert('Call Error', 'Failed to initialize call');
      navigation.goBack();
    }
  };

  const handleEndCall = () => {
      WebRTCService.endCall();
      navigation.goBack();
  };

  const toggleMute = () => {
    WebRTCService.toggleMicrophone();
    setIsMuted(!isMuted);
  };

  const toggleVideo = () => {
    WebRTCService.toggleCamera();
    setIsVideoEnabled(!isVideoEnabled);
  };

  const switchCamera = () => {
    WebRTCService.switchCamera();
  };

  return (
    <View style={styles.container}>
      {/* Remote Video - Demo Mode */}
      <View style={styles.remoteVideo}>
        {remoteStream ? (
          <View style={styles.videoPlaceholder}>
            <Ionicons name="videocam" size={64} color="#4CAF50" />
            <Text style={styles.videoText}>Remote Video Stream</Text>
            <Text style={styles.demoText}>Demo Mode - WebRTC not available in Expo</Text>
          </View>
        ) : (
          <View style={styles.avatarContainer}>
            <Text style={styles.avatarText}>
              {targetUser?.firstName?.[0]}{targetUser?.lastName?.[0]}
            </Text>
          </View>
        )}
        
        <Text style={styles.callerName}>
          {targetUser ? `${targetUser.firstName} ${targetUser.lastName}` : 'Unknown User'}
        </Text>
        <Text style={styles.callStatus}>{callStatus}</Text>
      </View>

      {/* Local Video - Demo Mode */}
      {localStream && isVideoEnabled && (
        <View style={styles.localVideo}>
          <View style={styles.localVideoPlaceholder}>
            <Ionicons name="person" size={32} color="#2196F3" />
            <Text style={styles.localVideoText}>You</Text>
          </View>
        </View>
      )}

      {/* Controls */}
      <View style={styles.controls}>
        <TouchableOpacity
          style={[styles.controlButton, isMuted ? styles.controlButtonActive : styles.controlButtonInactive]}
          onPress={toggleMute}
        >
          <Ionicons name={isMuted ? "mic-off" : "mic"} size={24} color="#fff" />
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.controlButton, isVideoEnabled ? styles.controlButtonInactive : styles.controlButtonActive]}
          onPress={toggleVideo}
        >
          <Ionicons name={isVideoEnabled ? "videocam" : "videocam-off"} size={24} color="#fff" />
        </TouchableOpacity>

        {isVideoEnabled && (
          <TouchableOpacity
            style={[styles.controlButton, styles.controlButtonInactive]}
            onPress={switchCamera}
          >
            <Ionicons name="camera-reverse" size={24} color="#fff" />
          </TouchableOpacity>
        )}

        <TouchableOpacity
          style={[styles.controlButton, styles.endCallButton]}
          onPress={handleEndCall}
        >
          <Ionicons name="call" size={24} color="#fff" />
        </TouchableOpacity>
      </View>
    </View>
  );
}

const { width, height } = Dimensions.get('window');

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#000',
  },
  remoteVideo: {
    flex: 1,
    backgroundColor: '#000',
    justifyContent: 'center',
    alignItems: 'center',
  },
  localVideo: {
    position: 'absolute',
    top: 50,
    right: 20,
    width: 120,
    height: 160,
    borderRadius: 10,
    overflow: 'hidden',
    borderWidth: 2,
    borderColor: '#fff',
  },
  avatarContainer: {
    width: 120,
    height: 120,
    borderRadius: 60,
    backgroundColor: '#2196F3',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 20,
  },
  avatarText: {
    color: '#fff',
    fontSize: 48,
    fontWeight: 'bold',
  },
  callerName: {
    color: '#fff',
    fontSize: 24,
    fontWeight: 'bold',
    marginBottom: 8,
    textAlign: 'center',
  },
  callStatus: {
    color: '#4CAF50',
    fontSize: 18,
    marginBottom: 20,
  },
  controls: {
    position: 'absolute',
    bottom: 50,
    left: 0,
    right: 0,
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 20,
  },
  controlButton: {
    width: 60,
    height: 60,
    borderRadius: 30,
    justifyContent: 'center',
    alignItems: 'center',
    marginHorizontal: 10,
  },
  controlButtonInactive: {
    backgroundColor: 'rgba(255, 255, 255, 0.3)',
  },
  controlButtonActive: {
    backgroundColor: 'rgba(255, 255, 255, 0.8)',
  },
  endCallButton: {
    backgroundColor: '#f44336',
  },
  videoPlaceholder: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#000',
  },
  videoText: {
    color: '#4CAF50',
    fontSize: 18,
    fontWeight: 'bold',
    marginTop: 10,
  },
  demoText: {
    color: '#ccc',
    fontSize: 12,
    marginTop: 5,
    textAlign: 'center',
  },
  localVideoPlaceholder: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#333',
  },
  localVideoText: {
    color: '#2196F3',
    fontSize: 12,
    fontWeight: 'bold',
    marginTop: 5,
  },
});
