import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Alert,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import WebRTCService from '../services/WebRTCService';
import { useRealtime } from '../context/RealtimeContext';

export default function SimpleAudioCallScreen({ route, navigation }) {
  const { patient, doctor, callType, isInitiator } = route.params || {};
  const targetUser = patient || doctor;
  const { isConnected } = useRealtime();
  
  const [isMuted, setIsMuted] = useState(false);
  const [isSpeakerOn, setIsSpeakerOn] = useState(false);
  const [callStatus, setCallStatus] = useState('Connecting...');
  const [isCallActive, setIsCallActive] = useState(false);
  const [callDuration, setCallDuration] = useState(0);

  useEffect(() => {
    initializeCall();
    
    return () => {
      WebRTCService.endCall();
    };
  }, []);

  // Call duration timer
  useEffect(() => {
    let interval;
    if (isCallActive) {
      interval = setInterval(() => {
        setCallDuration(prev => prev + 1);
      }, 1000);
    }
    return () => clearInterval(interval);
  }, [isCallActive]);

  const initializeCall = async () => {
    try {
      // Initialize WebRTC service
        WebRTCService.initialize();
      
      // Set up callbacks
      WebRTCService.setCallbacks({
        onLocalStream: (stream) => {
          setCallStatus('Call Connected');
          setIsCallActive(true);
        },
        onRemoteStream: (stream) => {
          // Audio stream received
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

      // Start the call (audio only)
      const success = await WebRTCService.startCall(
        targetUser.id, 
        isInitiator, 
        'audio'
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

  const toggleSpeaker = () => {
    // Note: Speaker toggle would require additional native implementation
    setIsSpeakerOn(!isSpeakerOn);
  };

  const formatDuration = (seconds) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  return (
    <View style={styles.container}>
      {/* Call Info */}
      <View style={styles.callInfo}>
        <View style={styles.avatarContainer}>
          <Text style={styles.avatarText}>
            {targetUser?.firstName?.[0]}{targetUser?.lastName?.[0]}
          </Text>
        </View>
        
        <Text style={styles.callerName}>
          {targetUser ? `${targetUser.firstName} ${targetUser.lastName}` : 'Unknown User'}
        </Text>
        
        <Text style={styles.callStatus}>{callStatus}</Text>
        
        {isCallActive && (
          <Text style={styles.duration}>{formatDuration(callDuration)}</Text>
        )}
      </View>

      {/* Controls */}
      <View style={styles.controls}>
        <TouchableOpacity
          style={[styles.controlButton, isMuted ? styles.controlButtonActive : styles.controlButtonInactive]}
          onPress={toggleMute}
        >
          <Ionicons name={isMuted ? "mic-off" : "mic"} size={24} color="#fff" />
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.controlButton, isSpeakerOn ? styles.controlButtonActive : styles.controlButtonInactive]}
          onPress={toggleSpeaker}
        >
          <Ionicons name={isSpeakerOn ? "volume-high" : "volume-low"} size={24} color="#fff" />
        </TouchableOpacity>

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

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#000',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 50,
  },
  callInfo: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 40,
  },
  avatarContainer: {
    width: 150,
    height: 150,
    borderRadius: 75,
    backgroundColor: '#2196F3',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 30,
  },
  avatarText: {
    color: '#fff',
    fontSize: 60,
    fontWeight: 'bold',
  },
  callerName: {
    color: '#fff',
    fontSize: 28,
    fontWeight: 'bold',
    marginBottom: 10,
    textAlign: 'center',
  },
  callStatus: {
    color: '#4CAF50',
    fontSize: 18,
    marginBottom: 10,
  },
  duration: {
    color: '#fff',
    fontSize: 24,
    fontWeight: 'bold',
  },
  controls: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 20,
  },
  controlButton: {
    width: 70,
    height: 70,
    borderRadius: 35,
    justifyContent: 'center',
    alignItems: 'center',
    marginHorizontal: 15,
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
});