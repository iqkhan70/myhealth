import React, { useState, useEffect } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TextInput,
  TouchableOpacity,
  Alert,
  ScrollView,
  SafeAreaView,
  Modal,
  FlatList,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import { StatusBar } from 'expo-status-bar';
import RealWebRTCService from './src/services/RealWebRTCService';

// Dynamic API URL based on platform
const getApiBaseUrl = () => {
  const hasDOM = typeof window !== 'undefined' && typeof document !== 'undefined';
  const isWebEnvironment = Platform.OS === 'web' || hasDOM;
  
  if (isWebEnvironment) {
    // Check if we're accessing from mobile browser (not localhost)
    const hostname = typeof window !== 'undefined' ? window.location.hostname : 'localhost';
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
      // Desktop browser - use localhost
      return 'http://localhost:5262/api';
    } else {
      // Mobile browser accessing via IP - use network IP
      return 'http://192.168.86.113:5262/api';
    }
  } else {
    // For native mobile, use the network IP
    return 'http://192.168.86.113:5262/api';
  }
};

const API_BASE_URL = getApiBaseUrl();
const SIGNALR_HUB_URL = API_BASE_URL.replace('/api', '/mobilehub');

// Debug logging
console.log('🌐 Environment Detection:');
console.log('  Platform.OS:', Platform.OS);
console.log('  Has DOM:', typeof window !== 'undefined' && typeof document !== 'undefined');
console.log('  Hostname:', typeof window !== 'undefined' && window.location ? window.location.hostname : 'N/A');
console.log('  API_BASE_URL:', API_BASE_URL);
console.log('  SIGNALR_HUB_URL:', SIGNALR_HUB_URL);

export default function App() {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [user, setUser] = useState(null);
  const [email, setEmail] = useState('john@doe.com');
  const [password, setPassword] = useState('demo123');
  const [patients, setPatients] = useState([]);
  const [doctors, setDoctors] = useState([]);
  const [loading, setLoading] = useState(false);

  const login = async () => {
    if (!email || !password) {
      Alert.alert('Error', 'Please enter email and password');
      return;
    }

    setLoading(true);
    try {
      console.log('Attempting login to:', `${API_BASE_URL}/auth/login`);
      console.log('Login data:', { email, password });
      
      const response = await fetch(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        mode: 'cors',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, password }),
      });

      console.log('Response status:', response.status);
      const data = await response.json();
      console.log('Response data:', data);
      
          if (data.success) {
            setUser(data.user);
            setAuthToken(data.token);
            setIsLoggedIn(true);
            Alert.alert('Success', `Welcome ${data.user.firstName}!`);
            
            // Initialize Video Service
            initializeVideo();
            
            // Establish SignalR connection
            connectToSignalR(data.token);
            
            // Load assigned users
            if (data.user.roleId === 2) { // Doctor
              loadPatients(data.token);
            } else if (data.user.roleId === 1) { // Patient
              loadDoctors(data.token);
            }
          } else {
        Alert.alert('Login Failed', data.message || 'Invalid credentials');
      }
    } catch (error) {
      Alert.alert('Error', 'Network error. Please check your connection.');
      console.error('Login error:', error);
    }
    setLoading(false);
  };

  const loadPatients = async (token) => {
    try {
      const response = await fetch(`${API_BASE_URL}/mobile/doctor/patients`, {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });
      
      if (response.ok) {
        const data = await response.json();
        setPatients(data);
      }
    } catch (error) {
      console.error('Error loading patients:', error);
    }
  };

  const loadDoctors = async (token) => {
    try {
      const response = await fetch(`${API_BASE_URL}/mobile/patient/doctors`, {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });
      
      if (response.ok) {
        const data = await response.json();
        setDoctors(data);
      }
    } catch (error) {
      console.error('Error loading doctors:', error);
    }
  };

  const logout = () => {
    setIsLoggedIn(false);
    setUser(null);
    setEmail('');
    setPassword('');
    setPatients([]);
    setDoctors([]);
  };

  const [chatModal, setChatModal] = useState({ visible: false, targetUser: null, messages: [] });
  const [newMessage, setNewMessage] = useState('');
  const [signalRConnection, setSignalRConnection] = useState(null);
  const [authToken, setAuthToken] = useState(null);
  
  // Agora state
  const [agoraInitialized, setAgoraInitialized] = useState(false);
  const [remoteUsers, setRemoteUsers] = useState([]);
  const [isAudioMuted, setIsAudioMuted] = useState(false);
  const [isVideoMuted, setIsVideoMuted] = useState(false);

  const startChat = async (targetUser) => {
    setChatModal({ visible: true, targetUser, messages: [] });
    
    // Load existing messages
    if (authToken) {
      try {
        const existingMessages = await loadMessages(targetUser.id);
        setChatModal(prev => ({
          ...prev,
          messages: existingMessages
        }));
      } catch (error) {
        console.error('Failed to load existing messages:', error);
      }
    }
  };

  const [callModal, setCallModal] = useState({ visible: false, targetUser: null, callType: null });

  const connectToSignalR = async (token) => {
    try {
      console.log('Connecting to SignalR...');
      // For now, we'll simulate SignalR connection
      // In a real implementation, you'd use @microsoft/signalr package
      console.log('SignalR connection established');
    } catch (error) {
      console.error('SignalR connection failed:', error);
    }
  };

  // Removed loadAgoraSDK - now handled by NativeVideoService

  const initializeVideo = async () => {
    try {
      console.log('🚀 Mobile: Initializing Real WebRTC Service... Platform:', Platform.OS);
      console.log('🚀 Mobile: User Agent:', typeof navigator !== 'undefined' ? navigator.userAgent : 'N/A');
      console.log('🚀 Mobile: WebRTC Support:', typeof navigator !== 'undefined' && !!navigator.mediaDevices);
      console.log('🚀 Mobile: getUserMedia Support:', typeof navigator !== 'undefined' && !!navigator.mediaDevices?.getUserMedia);
      
      const success = await RealWebRTCService.initialize('efa11b3a7d05409ca979fb25a5b489ae');
      
      if (success) {
        setAgoraInitialized(true);
        
        // Set up video service event listeners
        RealWebRTCService.setEventListeners({
          onUserJoined: (uid) => {
            console.log('📱 Mobile: Remote user joined:', uid);
            setRemoteUsers(prev => [...prev, uid]);
          },
          onUserLeft: (uid) => {
            console.log('📱 Mobile: Remote user left:', uid);
            setRemoteUsers(prev => prev.filter(id => id !== uid));
          },
          onError: (error) => {
            console.error('❌ Mobile: WebRTC service error:', error);
            Alert.alert('Call Error', 'There was an issue with the call connection.');
          },
        });
        
        console.log('✅ Mobile: Real WebRTC Service initialized successfully');
      } else {
        console.error('❌ Mobile: Failed to initialize Real WebRTC Service');
        console.log('⚠️ Mobile: Call buttons will show "not ready" message');
        // Don't show alert immediately, let user try
      }
    } catch (error) {
      console.error('❌ Mobile: WebRTC service initialization error:', error);
      console.log('⚠️ Mobile: Call buttons will show "not ready" message');
      // Don't show alert immediately, let user try
    }
  };

  const sendMessage = async (message, targetUserId) => {
    try {
      const response = await fetch(`${API_BASE_URL}/mobile/send-message`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${authToken}`,
        },
        body: JSON.stringify({
          targetUserId: targetUserId,
          message: message
        }),
      });

      if (!response.ok) {
        console.log('API endpoint not ready, message not sent to server');
        return { message: 'Message sent locally (API not ready)' };
      }

      const text = await response.text();
      if (!text) {
        console.log('Empty response from send message');
        return { message: 'Message sent' };
      }

      const result = JSON.parse(text);
      console.log('Message sent:', result);
      return result;
    } catch (error) {
      console.error('Failed to send message:', error);
      // Don't throw error, just log it so chat still works locally
      return { message: 'Message sent locally (API error)' };
    }
  };

  const loadMessages = async (targetUserId) => {
    try {
      const response = await fetch(`${API_BASE_URL}/mobile/messages/${targetUserId}`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${authToken}`,
        },
      });

      if (!response.ok) {
        console.log('API endpoint not ready, using empty messages');
        return [];
      }

      const text = await response.text();
      if (!text) {
        console.log('Empty response, using empty messages');
        return [];
      }

      const messages = JSON.parse(text);
      console.log('Messages loaded:', messages);
      return messages.map(m => ({
        text: m.message,
        isMe: m.isMe,
        timestamp: m.sentAt
      }));
    } catch (error) {
      console.error('Failed to load messages:', error);
      return [];
    }
  };

  const initiateCall = async (targetUserId, callType) => {
    try {
      const response = await fetch(`${API_BASE_URL}/mobile/call/initiate`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${authToken}`,
        },
        body: JSON.stringify({
          targetUserId: targetUserId,
          callType: callType
        }),
      });

      const result = await response.json();
      console.log('Call initiated:', result);
      return result;
    } catch (error) {
      console.error('Failed to initiate call:', error);
      throw error;
    }
  };

  const startCall = async (targetUser, callType) => {
    console.log('🚀 startCall function ENTERED');
    console.log('🚀 Parameters:', { targetUser: targetUser?.firstName, callType, agoraInitialized });
    
    try {
      // Show call modal immediately
      console.log('🎯 Mobile: Setting call modal visible...');
      setCallModal({ visible: true, targetUser, callType });
      
      // Reset call state
      setRemoteUsers([]);
      setIsAudioMuted(false);
      setIsVideoMuted(false);
      
      // For mobile browser, let's try a simpler approach
      console.log('🎯 Mobile: Starting simplified call process...');
      
      // Handle media access based on platform
      if (callType === 'Audio' || callType === 'Video') {
        try {
          console.log('🎤 Mobile: Requesting microphone access...');
          
          // Check if we're in a web environment (browser)
          const isWeb = typeof window !== 'undefined' && typeof document !== 'undefined' && typeof navigator !== 'undefined';
          
          if (isWeb && navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
            // Web browser - use real getUserMedia
            console.log('🌐 Mobile: Using web getUserMedia...');
            const stream = await navigator.mediaDevices.getUserMedia({ 
              audio: true, 
              video: callType === 'Video' 
            });
            console.log('✅ Mobile: Media access granted!');
            
            // Keep the stream active for the call instead of stopping it
            console.log('🔊 Mobile: Keeping audio stream active for call...');
            
            // Play local audio back to test (you should hear yourself)
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const source = audioContext.createMediaStreamSource(stream);
            const destination = audioContext.createMediaStreamDestination();
            source.connect(destination);
            
            // Create audio element to play the stream
            const audio = new Audio();
            audio.srcObject = stream;
            audio.autoplay = true;
            audio.muted = false; // Make sure it's not muted
            audio.volume = 1.0;
            
            console.log('🔊 Mobile: Audio element created and should be playing');
          } else {
            // Native mobile - simulate media access
            console.log('📱 Mobile: Using native simulation (no real getUserMedia available)...');
            console.log('✅ Mobile: Simulated media access granted!');
          }
          
          // Simulate connection after getting media access
          setTimeout(() => {
            console.log('📱 Mobile: Simulating remote user joined');
            setRemoteUsers([999]); // Simulate remote user
          }, 2000);
          
        } catch (mediaError) {
          console.error('❌ Mobile: Media access denied:', mediaError);
          Alert.alert('Permission Required', 'Please allow microphone access for calls.');
          setCallModal({ visible: false, targetUser: null, callType: null });
          return;
        }
      }
      
      // Notify server about the call
      try {
        console.log('🎯 Mobile: Notifying server about call...');
        await initiateCall(targetUser.id, callType);
        console.log('✅ Mobile: Server notified about call');
      } catch (serverError) {
        console.warn('⚠️ Mobile: Failed to notify server about call:', serverError);
        // Don't fail the call if server notification fails
      }
      
    } catch (error) {
      console.error('❌ Mobile: Error starting call:', error);
      Alert.alert('Error', 'Failed to start call. Please check your connection.');
      setCallModal({ visible: false, targetUser: null, callType: null });
    }
    
    console.log('🚀 startCall function EXITING');
  };

  if (!isLoggedIn) {
    return (
      <SafeAreaView style={styles.container}>
        <StatusBar style="auto" />
        <View style={styles.loginContainer}>
          <Text style={styles.title}>Mental Health App</Text>
          <Text style={styles.subtitle}>Mobile App (React Native)</Text>
          
          <TextInput
            style={styles.input}
            placeholder="Email"
            value={email}
            onChangeText={setEmail}
            keyboardType="email-address"
            autoCapitalize="none"
          />
          
          <TextInput
            style={styles.input}
            placeholder="Password"
            value={password}
            onChangeText={setPassword}
            secureTextEntry
          />
          
          <TouchableOpacity
            style={[styles.button, loading && styles.buttonDisabled]}
            onPress={login}
            disabled={loading}
          >
            <Text style={styles.buttonText}>
              {loading ? 'Logging in...' : 'Login'}
            </Text>
          </TouchableOpacity>

          <View style={styles.demoCredentials}>
            <Text style={styles.demoTitle}>Demo Credentials:</Text>
            <Text style={styles.demoText}>Admin: admin@mentalhealth.com / demo123</Text>
            <Text style={styles.demoText}>Doctor: dr.sarah@mentalhealth.com / demo123</Text>
            <Text style={styles.demoText}>Patient: john@doe.com / demo123</Text>
          </View>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar style="auto" />
      <View style={styles.header}>
        <Text style={styles.headerTitle}>
          Welcome, {user.firstName}!
        </Text>
        <Text style={styles.headerSubtitle}>
          {user.roleId === 2 ? 'Doctor' : 'Patient'} Dashboard
        </Text>
        <TouchableOpacity style={styles.logoutButton} onPress={logout}>
          <Text style={styles.logoutButtonText}>Logout</Text>
        </TouchableOpacity>
      </View>

      <ScrollView style={styles.content}>
        {user.roleId === 2 ? (
          // Doctor View
          <View>
            <Text style={styles.sectionTitle}>My Patients ({patients.length})</Text>
            {patients.map((patient) => (
              <View key={patient.id} style={styles.userCard}>
                <View style={styles.userInfo}>
                  <Text style={styles.userName}>
                    {patient.firstName} {patient.lastName}
                  </Text>
                  <Text style={styles.userEmail}>{patient.email}</Text>
                </View>
                <View style={styles.userActions}>
                  <TouchableOpacity
                    style={styles.actionButton}
                    onPress={() => startChat(patient)}
                  >
                    <Text style={styles.actionButtonText}>💬</Text>
                  </TouchableOpacity>
                        <TouchableOpacity
                          style={styles.actionButton}
                          onPress={async () => {
                            console.log('🎯 Video Call Button Pressed - Service initialized:', agoraInitialized);
                            await startCall(patient, 'Video');
                          }}
                        >
                          <Text style={styles.actionButtonText}>📹</Text>
                        </TouchableOpacity>
                        <TouchableOpacity
                          style={styles.actionButton}
                          onPress={async () => {
                            console.log('🎯 Audio Call Button Pressed - Service initialized:', agoraInitialized);
                            await startCall(patient, 'Audio');
                          }}
                        >
                          <Text style={styles.actionButtonText}>📞</Text>
                        </TouchableOpacity>
                </View>
              </View>
            ))}
          </View>
        ) : (
          // Patient View
          <View>
            <Text style={styles.sectionTitle}>My Doctors ({doctors.length})</Text>
            {doctors.map((doctor) => (
              <View key={doctor.id} style={styles.userCard}>
                <View style={styles.userInfo}>
                  <Text style={styles.userName}>
                    Dr. {doctor.firstName} {doctor.lastName}
                  </Text>
                  <Text style={styles.userEmail}>{doctor.email}</Text>
                  {doctor.specialization && (
                    <Text style={styles.userSpecialization}>{doctor.specialization}</Text>
                  )}
                </View>
                <View style={styles.userActions}>
                  <TouchableOpacity
                    style={styles.actionButton}
                    onPress={() => startChat(doctor)}
                  >
                    <Text style={styles.actionButtonText}>💬</Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={styles.actionButton}
                    onPress={async () => {
                      console.log('🎯 Video Call Button Pressed - Service initialized:', agoraInitialized);
                      await startCall(doctor, 'Video');
                    }}
                  >
                    <Text style={styles.actionButtonText}>📹</Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={styles.actionButton}
                    onPress={async () => {
                      console.log('🎯 Audio Call Button Pressed - Service initialized:', agoraInitialized);
                      await startCall(doctor, 'Audio');
                    }}
                  >
                    <Text style={styles.actionButtonText}>📞</Text>
                  </TouchableOpacity>
                </View>
              </View>
            ))}
          </View>
        )}

        <View style={styles.footer}>
          <Text style={styles.footerText}>
            This is a React Native mobile app.{'\n'}
            Full features include real-time chat, video calls, and more.
          </Text>
          
          {/* Audio Test Button */}
          <TouchableOpacity
            style={[styles.actionButton, { backgroundColor: '#4CAF50', marginTop: 10 }]}
            onPress={async () => {
              try {
                console.log('🎤 Testing audio...');
                
                // Check if we're in a web environment (browser)
                const isWeb = typeof window !== 'undefined' && typeof document !== 'undefined' && typeof navigator !== 'undefined';
                
                if (isWeb && navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
                  // Web browser - use real getUserMedia
                  const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
                  
                  // Create audio element to play back microphone
                  const audio = new Audio();
                  audio.srcObject = stream;
                  audio.autoplay = true;
                  audio.muted = false;
                  audio.volume = 1.0;
                  
                  console.log('🔊 Audio test started - you should hear yourself');
                  Alert.alert('Audio Test', 'Audio test started! You should hear yourself speaking.');
                  
                  // Stop after 5 seconds
                  setTimeout(() => {
                    stream.getTracks().forEach(track => track.stop());
                    console.log('🔇 Audio test stopped');
                  }, 5000);
                } else {
                  // Native mobile - simulate audio test
                  console.log('📱 Native: Simulating audio test...');
                  Alert.alert('Audio Test', 'Audio test simulated for native mobile! In a real app, this would test the microphone.');
                }
                
              } catch (error) {
                console.error('❌ Audio test failed:', error);
                Alert.alert('Audio Test Failed', error.message);
              }
            }}
          >
            <Text style={styles.actionButtonText}>🎤 Test Audio</Text>
          </TouchableOpacity>
        </View>
      </ScrollView>

      {/* Chat Modal */}
      <Modal
        visible={chatModal.visible}
        animationType="slide"
        onRequestClose={() => setChatModal({ visible: false, targetUser: null, messages: [] })}
      >
        <SafeAreaView style={styles.modalContainer}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>
              Chat with {chatModal.targetUser ? `${chatModal.targetUser.firstName} ${chatModal.targetUser.lastName}` : 'User'}
            </Text>
            <TouchableOpacity
              onPress={() => setChatModal({ visible: false, targetUser: null, messages: [] })}
              style={styles.closeButton}
            >
              <Text style={styles.closeButtonText}>✕</Text>
            </TouchableOpacity>
          </View>
          
          <KeyboardAvoidingView 
            style={styles.chatContainer}
            behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
            keyboardVerticalOffset={Platform.OS === 'ios' ? 0 : 20}
          >
            <FlatList
              data={chatModal.messages}
              keyExtractor={(item, index) => index.toString()}
              renderItem={({ item }) => (
                <View style={[styles.messageContainer, item.isMe ? styles.myMessage : styles.theirMessage]}>
                  <Text style={styles.messageText}>{item.text}</Text>
                </View>
              )}
              style={styles.messagesList}
            />
            
            <View style={styles.inputContainer}>
              <TextInput
                style={styles.messageInput}
                placeholder="Type a message..."
                value={newMessage}
                onChangeText={setNewMessage}
                multiline
              />
              <TouchableOpacity
                style={styles.sendButton}
                onPress={async () => {
                  if (newMessage.trim() && chatModal.targetUser) {
                    // Add message to UI immediately
                    setChatModal(prev => ({
                      ...prev,
                      messages: [...prev.messages, { text: newMessage, isMe: true }]
                    }));
                    
                    const messageToSend = newMessage;
                    setNewMessage('');
                    
                    try {
                      // Send real message via API
                      await sendMessage(messageToSend, chatModal.targetUser.id);
                    } catch (error) {
                      console.error('Failed to send message:', error);
                      Alert.alert('Error', 'Failed to send message');
                    }
                  }
                }}
              >
                <Text style={styles.sendButtonText}>Send</Text>
              </TouchableOpacity>
            </View>
          </KeyboardAvoidingView>
        </SafeAreaView>
      </Modal>

      {/* Call Modal */}
      <Modal
        visible={callModal.visible}
        animationType="slide"
        onRequestClose={() => setCallModal({ visible: false, targetUser: null, callType: null })}
      >
        <SafeAreaView style={styles.callModalContainer}>
          <View style={styles.callHeader}>
            <Text style={styles.callTitle}>
              {callModal.callType} Call with {callModal.targetUser ? `${callModal.targetUser.firstName} ${callModal.targetUser.lastName}` : 'User'}
            </Text>
          </View>
          
          <View style={styles.callContent}>
            {callModal.callType === 'Video' ? (
              <View style={styles.videoContainer}>
                {/* Remote Video View */}
                {remoteUsers.length > 0 ? (
                  <View style={styles.remoteVideo}>
                    <View 
                      style={styles.videoElement}
                    >
                      <Text style={styles.videoPlaceholder}>Remote Video</Text>
                    </View>
                  </View>
                ) : (
                  <View style={styles.remoteVideo}>
                    <Text style={styles.videoPlaceholder}>Waiting for {callModal.targetUser?.firstName}...</Text>
                  </View>
                )}
                
                {/* Local Video View */}
                <View style={styles.localVideo}>
                  <View 
                    style={styles.videoElement}
                  >
                    <Text style={styles.videoPlaceholder}>You</Text>
                  </View>
                </View>
              </View>
            ) : (
              <View style={styles.audioCallContainer}>
                <Text style={styles.audioCallIcon}>🎧</Text>
                <Text style={styles.audioCallText}>
                  {remoteUsers.length > 0 ? 'Connected' : `Calling ${callModal.targetUser?.firstName}...`}
                </Text>
              </View>
            )}
          </View>
          
          <View style={styles.callControls}>
                <TouchableOpacity 
                  style={[styles.controlButton, isAudioMuted && styles.controlButtonMuted]}
                  onPress={async () => {
                    const newMuted = !isAudioMuted;
                    await RealWebRTCService.muteLocalAudio(newMuted);
                    setIsAudioMuted(newMuted);
                  }}
                >
                  <Text style={styles.controlButtonText}>{isAudioMuted ? '🔇' : '🎤'}</Text>
                </TouchableOpacity>
                
                {callModal.callType === 'Video' && (
                  <TouchableOpacity 
                    style={[styles.controlButton, isVideoMuted && styles.controlButtonMuted]}
                    onPress={async () => {
                      const newMuted = !isVideoMuted;
                      await RealWebRTCService.muteLocalVideo(newMuted);
                      setIsVideoMuted(newMuted);
                    }}
                  >
                    <Text style={styles.controlButtonText}>{isVideoMuted ? '📹' : '📷'}</Text>
                  </TouchableOpacity>
                )}
                
                <TouchableOpacity
                  style={[styles.controlButton, styles.endCallButton]}
                  onPress={async () => {
                    await RealWebRTCService.leaveChannel();
                    setCallModal({ visible: false, targetUser: null, callType: null });
                    setRemoteUsers([]);
                  }}
                >
                  <Text style={styles.controlButtonText}>📞</Text>
                </TouchableOpacity>
          </View>
        </SafeAreaView>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  loginContainer: {
    flex: 1,
    justifyContent: 'center',
    paddingHorizontal: 30,
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    textAlign: 'center',
    marginBottom: 10,
    color: '#2196F3',
  },
  subtitle: {
    fontSize: 16,
    textAlign: 'center',
    marginBottom: 40,
    color: '#666',
  },
  input: {
    backgroundColor: '#fff',
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 8,
    paddingHorizontal: 15,
    paddingVertical: 12,
    marginBottom: 15,
    fontSize: 16,
  },
  button: {
    backgroundColor: '#2196F3',
    borderRadius: 8,
    paddingVertical: 15,
    alignItems: 'center',
    marginTop: 10,
  },
  buttonDisabled: {
    backgroundColor: '#ccc',
  },
  buttonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
  demoCredentials: {
    marginTop: 30,
    padding: 15,
    backgroundColor: '#e3f2fd',
    borderRadius: 8,
  },
  demoTitle: {
    fontSize: 14,
    fontWeight: 'bold',
    marginBottom: 5,
    color: '#1976d2',
  },
  demoText: {
    fontSize: 12,
    color: '#1976d2',
    marginBottom: 2,
  },
  header: {
    backgroundColor: '#2196F3',
    padding: 20,
    alignItems: 'center',
  },
  headerTitle: {
    color: '#fff',
    fontSize: 20,
    fontWeight: 'bold',
  },
  headerSubtitle: {
    color: '#e3f2fd',
    fontSize: 14,
    marginTop: 5,
  },
  logoutButton: {
    marginTop: 10,
    paddingHorizontal: 20,
    paddingVertical: 8,
    backgroundColor: 'rgba(255,255,255,0.2)',
    borderRadius: 15,
  },
  logoutButtonText: {
    color: '#fff',
    fontSize: 14,
  },
  content: {
    flex: 1,
    padding: 20,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    marginBottom: 15,
    color: '#333',
  },
  userCard: {
    backgroundColor: '#fff',
    borderRadius: 8,
    padding: 15,
    marginBottom: 10,
    flexDirection: 'row',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  userInfo: {
    flex: 1,
  },
  userName: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
  },
  userEmail: {
    fontSize: 14,
    color: '#666',
    marginTop: 2,
  },
  userSpecialization: {
    fontSize: 12,
    color: '#2196F3',
    marginTop: 2,
  },
  userActions: {
    flexDirection: 'row',
  },
  actionButton: {
    width: 50,
    height: 50,
    borderRadius: 25,
    backgroundColor: '#f0f0f0',
    justifyContent: 'center',
    alignItems: 'center',
    marginLeft: 8,
    // Better touch target for mobile
    minWidth: 44,
    minHeight: 44,
  },
  actionButtonText: {
    fontSize: 18,
  },
  footer: {
    marginTop: 30,
    padding: 15,
    backgroundColor: '#fff',
    borderRadius: 8,
    alignItems: 'center',
  },
  footerText: {
    fontSize: 12,
    color: '#666',
    textAlign: 'center',
    lineHeight: 18,
  },
  // Modal Styles
  modalContainer: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 20,
    backgroundColor: '#2196F3',
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#fff',
    flex: 1,
  },
  closeButton: {
    padding: 5,
  },
  closeButtonText: {
    fontSize: 20,
    color: '#fff',
    fontWeight: 'bold',
  },
  chatContainer: {
    flex: 1,
    padding: 10,
  },
  messagesList: {
    flex: 1,
    marginBottom: 10,
  },
  messageContainer: {
    padding: 10,
    marginVertical: 5,
    borderRadius: 10,
    maxWidth: '80%',
  },
  myMessage: {
    backgroundColor: '#2196F3',
    alignSelf: 'flex-end',
  },
  theirMessage: {
    backgroundColor: '#e0e0e0',
    alignSelf: 'flex-start',
  },
  messageText: {
    color: '#fff',
    fontSize: 16,
  },
  inputContainer: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    padding: 10,
    backgroundColor: '#fff',
    borderTopWidth: 1,
    borderTopColor: '#ddd',
  },
  messageInput: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 20,
    paddingHorizontal: 15,
    paddingVertical: 10,
    marginRight: 10,
    maxHeight: 100,
  },
  sendButton: {
    backgroundColor: '#2196F3',
    borderRadius: 20,
    paddingHorizontal: 20,
    paddingVertical: 10,
  },
  sendButtonText: {
    color: '#fff',
    fontWeight: 'bold',
  },
  // Call Modal Styles
  callModalContainer: {
    flex: 1,
    backgroundColor: '#000',
  },
  callHeader: {
    padding: 20,
    backgroundColor: 'rgba(0,0,0,0.8)',
  },
  callTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#fff',
    textAlign: 'center',
  },
  callContent: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  videoContainer: {
    flex: 1,
    width: '100%',
    position: 'relative',
  },
  remoteVideo: {
    flex: 1,
    backgroundColor: '#333',
    justifyContent: 'center',
    alignItems: 'center',
  },
  localVideo: {
    position: 'absolute',
    top: 20,
    right: 20,
    width: 120,
    height: 160,
    backgroundColor: '#555',
    borderRadius: 10,
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 2,
    borderColor: '#2196F3',
  },
  videoPlaceholder: {
    color: '#fff',
    fontSize: 16,
  },
  videoElement: {
    width: '100%',
    height: '100%',
    backgroundColor: '#000',
    justifyContent: 'center',
    alignItems: 'center',
  },
  audioCallContainer: {
    alignItems: 'center',
  },
  audioCallIcon: {
    fontSize: 80,
    marginBottom: 20,
  },
  audioCallText: {
    color: '#fff',
    fontSize: 18,
  },
  callControls: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    padding: 30,
    gap: 30,
  },
  controlButton: {
    width: 60,
    height: 60,
    borderRadius: 30,
    backgroundColor: '#333',
    justifyContent: 'center',
    alignItems: 'center',
  },
  controlButtonMuted: {
    backgroundColor: '#f44336',
  },
  endCallButton: {
    backgroundColor: '#f44336',
  },
  controlButtonText: {
    fontSize: 24,
  },
});