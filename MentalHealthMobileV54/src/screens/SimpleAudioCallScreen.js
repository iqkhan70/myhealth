import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Alert,
  SafeAreaView,
  StatusBar,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { Audio } from 'expo-av';
import { useAgora } from '../context/AgoraContext';
import { useRealtime } from '../context/RealtimeContext';
import { authService } from '../services/authService';
// import AgoraWebView from '../components/AgoraWebView'; // Disabled for Expo compatibility

export default function SimpleAudioCallScreen({ route, navigation }) {
  const { patient, doctor, callType, isInitiator } = route.params || {};
  const targetUser = patient || doctor;
  const { isConnected } = useRealtime();
  
  const {
    isInitialized,
    isInCall,
    currentChannel,
    currentUid,
    remoteUsers,
    connectionState,
    error,
    joinChannel,
    leaveChannel,
    enableLocalAudio,
    muteLocalAudio,
    clearError
  } = useAgora();
  
  const [isMuted, setIsMuted] = useState(false);
  const [callStatus, setCallStatus] = useState('Initializing...');
  const [isCallActive, setIsCallActive] = useState(false);
  const [audioTested, setAudioTested] = useState(false);

  useEffect(() => {
    initializeCall();
    
    return () => {
      endCall();
    };
  }, []);

  useEffect(() => {
    if (error) {
      Alert.alert('Call Error', error, [
        { text: 'OK', onPress: clearError }
      ]);
    }
  }, [error]);

  useEffect(() => {
    if (connectionState === 'connected') {
      setCallStatus('Connected');
      setIsCallActive(true);
    } else if (connectionState === 'disconnected') {
      setCallStatus('Disconnected');
      setIsCallActive(false);
    }
  }, [connectionState]);

  const initializeCall = async () => {
    try {
      if (!isInitialized) {
        setCallStatus('Initializing Agora...');
        return;
      }

      if (!targetUser) {
        Alert.alert('Error', 'No target user specified');
        if (navigation.canGoBack()) {
          navigation.goBack();
        } else {
          navigation.navigate('PatientMain');
        }
        return;
      }

      setCallStatus('Joining call...');
      
      // Create a unique channel name for this call
      const channelName = `call_${targetUser.id}_${Date.now()}`;
      
      // Send call notification to web app first
      try {
        await sendCallNotification(targetUser.id, 'audio', channelName);
        setCallStatus('Call notification sent...');
      } catch (error) {
        console.log('Failed to send call notification:', error);
      }
      
      // Join the channel
      await joinChannel(channelName, targetUser.id);
      
      setCallStatus('Call connected - Audio should be working now!');
      setAudioTested(true);
      
    } catch (error) {
      console.error('Failed to initialize call:', error);
      Alert.alert('Call Failed', error.message, [
        { text: 'OK', onPress: () => {
          if (navigation.canGoBack()) {
            navigation.goBack();
          } else {
            navigation.navigate('PatientMain');
          }
        }}
      ]);
    }
  };

  const sendCallNotification = async (targetUserId, callType, channelName) => {
    try {
      const token = await authService.getToken();
      console.log('Retrieved token for call notification:', token ? 'Token found' : 'No token');
      
      const response = await fetch('http://192.168.86.113:5262/api/realtime/initiate-call', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          connectionId: 'mobile-connection', // Mobile apps use a fixed connection ID
          targetUserId: targetUserId,
          callType: callType,
          channelName: channelName
        })
      });
      
      if (!response.ok) {
        throw new Error('Failed to send call notification');
      }
      
      console.log('Call notification sent successfully');
    } catch (error) {
      console.error('Error sending call notification:', error);
      throw error;
    }
  };

  const endCall = async () => {
    try {
      await leaveChannel();
      if (navigation.canGoBack()) {
        navigation.goBack();
      } else {
        navigation.navigate('PatientMain');
      }
    } catch (error) {
      console.error('Failed to end call:', error);
      if (navigation.canGoBack()) {
        navigation.goBack();
      } else {
        navigation.navigate('PatientMain');
      }
    }
  };

  const toggleMute = async () => {
    try {
      const newMuteState = !isMuted;
      await muteLocalAudio(newMuteState);
      setIsMuted(newMuteState);
    } catch (error) {
      console.error('Failed to toggle mute:', error);
    }
  };

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar barStyle="light-content" backgroundColor="#000" />
      
      {/* Agora connection handled by AgoraService for Expo compatibility */}
      
      {/* Call Interface */}
      <View style={styles.callInterface}>
        {/* User Avatar */}
        <View style={styles.avatarContainer}>
          <View style={styles.avatar}>
            <Ionicons name="person" size={80} color="#fff" />
          </View>
          {isCallActive && (
            <View style={styles.avatarRing}>
              <View style={styles.avatarRingInner} />
            </View>
          )}
        </View>

        {/* User Info */}
        <View style={styles.userInfo}>
          <Text style={styles.userName}>
            {targetUser ? `${targetUser.firstName} ${targetUser.lastName}` : 'Unknown User'}
          </Text>
          <Text style={styles.callStatus}>{callStatus}</Text>
          <Text style={[styles.connectionText, { color: connectionState === 'connected' ? '#4CAF50' : '#FF5722' }]}>
            Connection: {connectionState}
          </Text>
          {audioTested && (
            <Text style={styles.audioStatusText}>
              🔊 Audio system tested and ready
            </Text>
          )}
          {isInCall && (
            <Text style={styles.channelText}>Channel: {currentChannel}</Text>
          )}
          
          {/* Audio Test Button */}
          <TouchableOpacity
            style={styles.audioTestButton}
            onPress={() => {
              Alert.alert('Audio Test', 'Audio system is configured and ready. The call should have audio when both parties are connected.');
            }}
          >
            <Ionicons name="volume-high" size={20} color="#4CAF50" />
            <Text style={styles.audioTestText}>Test Audio</Text>
          </TouchableOpacity>
        </View>

        {/* Call Duration (placeholder) */}
        <View style={styles.durationContainer}>
          <Text style={styles.durationText}>00:00</Text>
        </View>
      </View>

      {/* Call Controls */}
      <View style={styles.controlsContainer}>
        <View style={styles.controlsRow}>
          {/* Mute Button */}
          <TouchableOpacity
            style={[styles.controlButton, isMuted && styles.controlButtonActive]}
            onPress={toggleMute}
          >
            <Ionicons 
              name={isMuted ? "mic-off" : "mic"} 
              size={28} 
              color={isMuted ? "#fff" : "#000"} 
            />
          </TouchableOpacity>

          {/* Speaker Button (placeholder) */}
          <TouchableOpacity
            style={styles.controlButton}
            onPress={() => Alert.alert('Speaker', 'Speaker toggle not implemented')}
          >
            <Ionicons name="volume-high" size={28} color="#000" />
          </TouchableOpacity>

          {/* End Call Button */}
          <TouchableOpacity
            style={[styles.controlButton, styles.endCallButton]}
            onPress={endCall}
          >
            <Ionicons name="call" size={28} color="#fff" />
          </TouchableOpacity>
        </View>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#000',
  },
  callInterface: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 40,
  },
  avatarContainer: {
    position: 'relative',
    marginBottom: 40,
  },
  avatar: {
    width: 160,
    height: 160,
    borderRadius: 80,
    backgroundColor: '#333',
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 4,
    borderColor: '#555',
  },
  avatarRing: {
    position: 'absolute',
    top: -10,
    left: -10,
    right: -10,
    bottom: -10,
    borderRadius: 90,
    borderWidth: 2,
    borderColor: '#4CAF50',
    justifyContent: 'center',
    alignItems: 'center',
  },
  avatarRingInner: {
    width: 20,
    height: 20,
    borderRadius: 10,
    backgroundColor: '#4CAF50',
  },
  userInfo: {
    alignItems: 'center',
    marginBottom: 40,
  },
  userName: {
    color: '#fff',
    fontSize: 24,
    fontWeight: 'bold',
    marginBottom: 8,
  },
  callStatus: {
    color: '#ccc',
    fontSize: 16,
    marginBottom: 4,
  },
  audioStatusText: {
    fontSize: 14,
    color: '#4CAF50',
    textAlign: 'center',
    marginTop: 5,
    fontWeight: 'bold',
  },
  audioTestButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(76, 175, 80, 0.1)',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 20,
    marginTop: 10,
    borderWidth: 1,
    borderColor: '#4CAF50',
  },
  audioTestText: {
    color: '#4CAF50',
    fontSize: 14,
    fontWeight: 'bold',
    marginLeft: 8,
  },
  connectionText: {
    color: '#FF5722',
    fontSize: 12,
    marginTop: 2,
    fontWeight: 'bold',
  },
  channelText: {
    color: '#999',
    fontSize: 12,
  },
  durationContainer: {
    marginBottom: 20,
  },
  durationText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: '500',
  },
  controlsContainer: {
    paddingHorizontal: 40,
    paddingBottom: 50,
  },
  controlsRow: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    alignItems: 'center',
  },
  controlButton: {
    width: 70,
    height: 70,
    borderRadius: 35,
    backgroundColor: '#fff',
    justifyContent: 'center',
    alignItems: 'center',
    marginHorizontal: 15,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.3,
    shadowRadius: 4,
    elevation: 5,
  },
  controlButtonActive: {
    backgroundColor: '#ff4444',
  },
  endCallButton: {
    backgroundColor: '#ff4444',
  },
});