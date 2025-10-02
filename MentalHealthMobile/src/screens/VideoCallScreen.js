import React, { useState, useEffect, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Alert,
  Dimensions,
  StatusBar,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
// WebRTC functionality temporarily disabled for testing
// import { RTCView, RTCPeerConnection, RTCSessionDescription, RTCIceCandidate, mediaDevices } from 'react-native-webrtc';
import { useAuth } from '../context/AuthContext';
import { useSocket } from '../context/SocketContext';

const { width, height } = Dimensions.get('window');

export default function VideoCallScreen({ route, navigation }) {
  const [localStream, setLocalStream] = useState(null);
  const [remoteStream, setRemoteStream] = useState(null);
  const [isConnected, setIsConnected] = useState(false);
  const [isMuted, setIsMuted] = useState(false);
  const [isVideoEnabled, setIsVideoEnabled] = useState(true);
  const [callDuration, setCallDuration] = useState(0);
  const [callStatus, setCallStatus] = useState('Connecting...');
  
  const { user } = useAuth();
  const { socket, sendWebRTCOffer, sendWebRTCAnswer, sendICECandidate, endCall } = useSocket();
  const { patient, doctor, callType, isInitiator } = route.params || {};
  
  const peerConnection = useRef(null);
  const callTimer = useRef(null);
  const targetUser = patient || doctor;

  useEffect(() => {
    initializeCall();
    
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

  const initializeCall = async () => {
    try {
      // Get user media
      const stream = await mediaDevices.getUserMedia({
        video: true,
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
        console.log('Remote stream added');
        setRemoteStream(event.stream);
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

  const toggleVideo = () => {
    if (localStream) {
      localStream.getVideoTracks().forEach(track => {
        track.enabled = !isVideoEnabled;
      });
      setIsVideoEnabled(!isVideoEnabled);
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
      <StatusBar barStyle="light-content" backgroundColor="#000" />
      
      {/* Remote Video */}
      <View style={styles.remoteVideoContainer}>
        {remoteStream ? (
          <RTCView
            style={styles.remoteVideo}
            streamURL={remoteStream.toURL()}
            objectFit="cover"
          />
        ) : (
          <View style={styles.noVideoContainer}>
            <View style={styles.avatarContainer}>
              <Text style={styles.avatarText}>
                {targetUser?.firstName?.[0]}{targetUser?.lastName?.[0]}
              </Text>
            </View>
            <Text style={styles.noVideoText}>
              {targetUser ? `${targetUser.firstName} ${targetUser.lastName}` : 'Unknown User'}
            </Text>
            <Text style={styles.statusText}>{callStatus}</Text>
          </View>
        )}
      </View>

      {/* Local Video */}
      <View style={styles.localVideoContainer}>
        {localStream && isVideoEnabled ? (
          <RTCView
            style={styles.localVideo}
            streamURL={localStream.toURL()}
            objectFit="cover"
            mirror={true}
          />
        ) : (
          <View style={styles.localVideoPlaceholder}>
            <Ionicons name="videocam-off" size={24} color="#fff" />
          </View>
        )}
      </View>

      {/* Call Info */}
      <View style={styles.callInfo}>
        <Text style={styles.callerName}>
          {targetUser ? `${targetUser.firstName} ${targetUser.lastName}` : 'Unknown User'}
        </Text>
        {isConnected && (
          <Text style={styles.callDuration}>{formatDuration(callDuration)}</Text>
        )}
        {!isConnected && (
          <Text style={styles.callStatus}>{callStatus}</Text>
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
            size={24} 
            color={isMuted ? "#f44336" : "#fff"} 
          />
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.controlButton, !isVideoEnabled && styles.controlButtonActive]}
          onPress={toggleVideo}
        >
          <Ionicons 
            name={isVideoEnabled ? "videocam" : "videocam-off"} 
            size={24} 
            color={!isVideoEnabled ? "#f44336" : "#fff"} 
          />
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.controlButton, styles.endCallButton]}
          onPress={endCallHandler}
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
  },
  remoteVideoContainer: {
    flex: 1,
    backgroundColor: '#1a1a1a',
  },
  remoteVideo: {
    flex: 1,
  },
  noVideoContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
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
  noVideoText: {
    color: '#fff',
    fontSize: 24,
    fontWeight: 'bold',
    marginBottom: 8,
  },
  statusText: {
    color: '#ccc',
    fontSize: 16,
  },
  localVideoContainer: {
    position: 'absolute',
    top: 60,
    right: 20,
    width: 120,
    height: 160,
    borderRadius: 12,
    overflow: 'hidden',
    backgroundColor: '#333',
  },
  localVideo: {
    flex: 1,
  },
  localVideoPlaceholder: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#333',
  },
  callInfo: {
    position: 'absolute',
    top: 80,
    left: 20,
    right: 160,
  },
  callerName: {
    color: '#fff',
    fontSize: 20,
    fontWeight: 'bold',
    marginBottom: 4,
  },
  callDuration: {
    color: '#4CAF50',
    fontSize: 16,
  },
  callStatus: {
    color: '#ccc',
    fontSize: 16,
  },
  controls: {
    position: 'absolute',
    bottom: 50,
    left: 0,
    right: 0,
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
  },
  controlButton: {
    width: 60,
    height: 60,
    borderRadius: 30,
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
    justifyContent: 'center',
    alignItems: 'center',
    marginHorizontal: 15,
  },
  controlButtonActive: {
    backgroundColor: 'rgba(244, 67, 54, 0.8)',
  },
  endCallButton: {
    backgroundColor: '#f44336',
  },
});
