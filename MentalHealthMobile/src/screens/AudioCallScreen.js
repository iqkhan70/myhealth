import React, { useState, useEffect, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Alert,
  StatusBar,
  Animated,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
// WebRTC functionality temporarily disabled for testing
// import { RTCPeerConnection, RTCSessionDescription, RTCIceCandidate, mediaDevices } from 'react-native-webrtc';
import { useAuth } from '../context/AuthContext';
import { useSocket } from '../context/SocketContext';

export default function AudioCallScreen({ route, navigation }) {
  const [localStream, setLocalStream] = useState(null);
  const [isConnected, setIsConnected] = useState(false);
  const [isMuted, setIsMuted] = useState(false);
  const [callDuration, setCallDuration] = useState(0);
  const [callStatus, setCallStatus] = useState('Connecting...');
  
  const { user } = useAuth();
  const { socket, sendWebRTCOffer, sendWebRTCAnswer, sendICECandidate, endCall } = useSocket();
  const { patient, doctor, callType, isInitiator } = route.params || {};
  
  const peerConnection = useRef(null);
  const callTimer = useRef(null);
  const pulseAnim = useRef(new Animated.Value(1)).current;
  const targetUser = patient || doctor;

  useEffect(() => {
    initializeCall();
    startPulseAnimation();
    
    return () => {
      cleanup();
    };
  }, []);

  useEffect(() => {
    if (socket) {
      socket.on('webrtc-offer', handleRemoteOffer);
      socket.on('webrtc-answer', handleRemoteAnswer);
      socket.on('webrtc-ice-candidate', handleRemoteICECandidate);
      socket.on('call-ended', handleCallEnded);
      
      return () => {
        socket.off('webrtc-offer', handleRemoteOffer);
        socket.off('webrtc-answer', handleRemoteAnswer);
        socket.off('webrtc-ice-candidate', handleRemoteICECandidate);
        socket.off('call-ended', handleCallEnded);
      };
    }
  }, [socket]);

  const startPulseAnimation = () => {
    Animated.loop(
      Animated.sequence([
        Animated.timing(pulseAnim, {
          toValue: 1.1,
          duration: 1000,
          useNativeDriver: true,
        }),
        Animated.timing(pulseAnim, {
          toValue: 1,
          duration: 1000,
          useNativeDriver: true,
        }),
      ])
    ).start();
  };

  const initializeCall = async () => {
    try {
      // Get user media (audio only)
      const stream = await mediaDevices.getUserMedia({
        video: false,
        audio: true,
      });
      
      setLocalStream(stream);
      
      // Create peer connection
      const configuration = {
        iceServers: [
          { urls: 'stun:stun.l.google.com:19302' },
          { urls: 'stun:stun1.l.google.com:19302' },
        ],
      };
      
      peerConnection.current = new RTCPeerConnection(configuration);
      
      // Add local stream to peer connection
      stream.getTracks().forEach(track => {
        peerConnection.current.addTrack(track, stream);
      });
      
      // Handle remote stream
      peerConnection.current.onaddstream = (event) => {
        console.log('Remote audio stream added');
        setIsConnected(true);
        setCallStatus('Connected');
        startCallTimer();
      };
      
      // Handle ICE candidates
      peerConnection.current.onicecandidate = (event) => {
        if (event.candidate && targetUser) {
          sendICECandidate(targetUser.id, event.candidate);
        }
      };
      
      // Handle connection state changes
      peerConnection.current.onconnectionstatechange = () => {
        const state = peerConnection.current.connectionState;
        console.log('Connection state:', state);
        
        if (state === 'connected') {
          setIsConnected(true);
          setCallStatus('Connected');
        } else if (state === 'disconnected' || state === 'failed') {
          setCallStatus('Disconnected');
          handleCallEnded();
        }
      };
      
      // If initiator, create offer
      if (isInitiator) {
        createOffer();
      } else {
        setCallStatus('Waiting for connection...');
      }
      
    } catch (error) {
      console.error('Error initializing call:', error);
      Alert.alert('Error', 'Failed to initialize call');
      navigation.goBack();
    }
  };

  const createOffer = async () => {
    try {
      const offer = await peerConnection.current.createOffer();
      await peerConnection.current.setLocalDescription(offer);
      
      if (targetUser) {
        sendWebRTCOffer(targetUser.id, offer);
        setCallStatus('Calling...');
      }
    } catch (error) {
      console.error('Error creating offer:', error);
    }
  };

  const handleRemoteOffer = async (data) => {
    try {
      const { offer } = data;
      await peerConnection.current.setRemoteDescription(new RTCSessionDescription(offer));
      
      const answer = await peerConnection.current.createAnswer();
      await peerConnection.current.setLocalDescription(answer);
      
      if (targetUser) {
        sendWebRTCAnswer(targetUser.id, answer);
      }
    } catch (error) {
      console.error('Error handling remote offer:', error);
    }
  };

  const handleRemoteAnswer = async (data) => {
    try {
      const { answer } = data;
      await peerConnection.current.setRemoteDescription(new RTCSessionDescription(answer));
    } catch (error) {
      console.error('Error handling remote answer:', error);
    }
  };

  const handleRemoteICECandidate = async (data) => {
    try {
      const { candidate } = data;
      await peerConnection.current.addIceCandidate(new RTCIceCandidate(candidate));
    } catch (error) {
      console.error('Error handling remote ICE candidate:', error);
    }
  };

  const handleCallEnded = () => {
    cleanup();
    navigation.goBack();
  };

  const startCallTimer = () => {
    callTimer.current = setInterval(() => {
      setCallDuration(prev => prev + 1);
    }, 1000);
  };

  const formatDuration = (seconds) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  const toggleMute = () => {
    if (localStream) {
      localStream.getAudioTracks().forEach(track => {
        track.enabled = isMuted;
      });
      setIsMuted(!isMuted);
    }
  };

  const endCallHandler = () => {
    Alert.alert(
      'End Call',
      'Are you sure you want to end this call?',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'End Call',
          style: 'destructive',
          onPress: () => {
            if (targetUser) {
              endCall(targetUser.id);
            }
            cleanup();
            navigation.goBack();
          },
        },
      ]
    );
  };

  const cleanup = () => {
    if (callTimer.current) {
      clearInterval(callTimer.current);
    }
    
    if (localStream) {
      localStream.getTracks().forEach(track => track.stop());
    }
    
    if (peerConnection.current) {
      peerConnection.current.close();
    }
  };

  return (
    <View style={styles.container}>
      <StatusBar barStyle="light-content" backgroundColor="#1a1a1a" />
      
      {/* Background gradient effect */}
      <View style={styles.backgroundGradient} />
      
      {/* User Avatar */}
      <View style={styles.avatarSection}>
        <Animated.View 
          style={[
            styles.avatarContainer,
            { transform: [{ scale: pulseAnim }] }
          ]}
        >
          <Text style={styles.avatarText}>
            {targetUser?.firstName?.[0]}{targetUser?.lastName?.[0]}
          </Text>
        </Animated.View>
        
        <Text style={styles.callerName}>
          {targetUser ? `${targetUser.firstName} ${targetUser.lastName}` : 'Unknown User'}
        </Text>
        
        <Text style={styles.callerRole}>
          {user.roleId === 2 ? 'Patient' : 'Doctor'}
        </Text>
        
        {isConnected ? (
          <Text style={styles.callDuration}>{formatDuration(callDuration)}</Text>
        ) : (
          <Text style={styles.callStatus}>{callStatus}</Text>
        )}
      </View>

      {/* Audio Visualizer */}
      <View style={styles.audioVisualizerContainer}>
        {isConnected && (
          <View style={styles.audioVisualizer}>
            {[...Array(5)].map((_, index) => (
              <Animated.View
                key={index}
                style={[
                  styles.audioBar,
                  {
                    height: Math.random() * 40 + 10,
                    animationDelay: `${index * 0.1}s`,
                  }
                ]}
              />
            ))}
          </View>
        )}
      </View>

      {/* Controls */}
      <View style={styles.controls}>
        <TouchableOpacity
          style={[styles.controlButton, isMuted && styles.controlButtonActive]}
          onPress={toggleMute}
        >
          <Ionicons 
            name={isMuted ? "mic-off" : "mic"} 
            size={28} 
            color={isMuted ? "#f44336" : "#fff"} 
          />
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.controlButton, styles.endCallButton]}
          onPress={endCallHandler}
        >
          <Ionicons name="call" size={28} color="#fff" />
        </TouchableOpacity>

        <TouchableOpacity
          style={styles.controlButton}
          onPress={() => {
            // TODO: Add speaker toggle functionality
            Alert.alert('Speaker', 'Speaker toggle functionality coming soon');
          }}
        >
          <Ionicons name="volume-high" size={28} color="#fff" />
        </TouchableOpacity>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#1a1a1a',
  },
  backgroundGradient: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: '#1a1a1a',
  },
  avatarSection: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingTop: 100,
  },
  avatarContainer: {
    width: 150,
    height: 150,
    borderRadius: 75,
    backgroundColor: '#2196F3',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 30,
    shadowColor: '#2196F3',
    shadowOffset: {
      width: 0,
      height: 0,
    },
    shadowOpacity: 0.5,
    shadowRadius: 20,
    elevation: 10,
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
    marginBottom: 8,
    textAlign: 'center',
  },
  callerRole: {
    color: '#ccc',
    fontSize: 18,
    marginBottom: 20,
  },
  callDuration: {
    color: '#4CAF50',
    fontSize: 20,
    fontWeight: '600',
  },
  callStatus: {
    color: '#ccc',
    fontSize: 18,
  },
  audioVisualizerContainer: {
    height: 80,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 50,
  },
  audioVisualizer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
  },
  audioBar: {
    width: 4,
    backgroundColor: '#4CAF50',
    marginHorizontal: 2,
    borderRadius: 2,
  },
  controls: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    paddingBottom: 50,
  },
  controlButton: {
    width: 70,
    height: 70,
    borderRadius: 35,
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
    justifyContent: 'center',
    alignItems: 'center',
    marginHorizontal: 20,
  },
  controlButtonActive: {
    backgroundColor: 'rgba(244, 67, 54, 0.8)',
  },
  endCallButton: {
    backgroundColor: '#f44336',
    width: 80,
    height: 80,
    borderRadius: 40,
  },
});
