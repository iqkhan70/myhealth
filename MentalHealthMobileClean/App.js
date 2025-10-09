import React, { useState, useEffect } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TextInput,
  TouchableOpacity,
  Alert,
  ScrollView,
  Platform,
  KeyboardAvoidingView,
  Modal,
  SafeAreaView,
} from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { StatusBar } from 'expo-status-bar';
import SignalRService from './src/services/SignalRService';
import WebRTCService from './src/services/WebRTCService';

// Environment detection and API configuration
const getApiBaseUrl = () => {
  const isWeb = Platform.OS === 'web' && typeof window !== 'undefined';
  
  if (isWeb) {
    // For web (browser), always use localhost
    return 'http://localhost:5262/api';
  } else {
    // For native mobile, use network IP
    // Note: Update this IP if your network changes
    return 'http://192.168.86.28:5262/api';
  }
};

const API_BASE_URL = getApiBaseUrl();
const SIGNALR_HUB_URL = API_BASE_URL.replace('/api', '/mobilehub');

console.log('🌐 Environment Detection:');
console.log('  Platform.OS:', Platform.OS);
console.log('  Has DOM:', typeof window !== 'undefined' && typeof document !== 'undefined');
console.log('  Hostname:', typeof window !== 'undefined' && window.location ? window.location.hostname : 'N/A');
console.log('  API_BASE_URL:', API_BASE_URL);
console.log('  SIGNALR_HUB_URL:', SIGNALR_HUB_URL);

export default function App() {
  // State management
  const [user, setUser] = useState(null);
  const [email, setEmail] = useState('john@doe.com');
  const [password, setPassword] = useState('demo123');
  const [loading, setLoading] = useState(false);
  const [contacts, setContacts] = useState([]);
  const [messages, setMessages] = useState([]);
  const [newMessage, setNewMessage] = useState('');
  const [selectedContact, setSelectedContact] = useState(null);
  const [callModal, setCallModal] = useState({ visible: false, targetUser: null, callType: null });
  const [remoteUsers, setRemoteUsers] = useState([]);
  const [isAudioMuted, setIsAudioMuted] = useState(false);
  const [isVideoMuted, setIsVideoMuted] = useState(false);
  const [agoraInitialized, setAgoraInitialized] = useState(false);
  const [signalRConnected, setSignalRConnected] = useState(false);
  const [incomingCall, setIncomingCall] = useState(null);
  const [currentView, setCurrentView] = useState('contacts'); // 'contacts', 'chat'

  // Initialize app
  useEffect(() => {
    checkAuthStatus();
    initializeServices();
  }, []);

  const checkAuthStatus = async () => {
    try {
      const token = await AsyncStorage.getItem('userToken');
      const userData = await AsyncStorage.getItem('currentUser');
      if (token && userData) {
        const user = JSON.parse(userData);
        setUser(user);
        await loadContactsForUser(user, token);
      }
    } catch (error) {
      console.error('Error checking auth status:', error);
    }
  };

  const initializeServices = async () => {
    try {
      console.log('🚀 Mobile: Initializing services...');
      
      // Initialize WebRTC service
      const webRTCInitialized = await WebRTCService.initialize('demo-app-id');
      if (webRTCInitialized) {
        console.log('✅ Mobile: WebRTC service initialized');
        setAgoraInitialized(true);
        
        // Set up WebRTC event listeners
        WebRTCService.setEventListener('onUserJoined', (uid) => {
          console.log('📱 Mobile: Remote user joined:', uid);
          setRemoteUsers(prev => [...prev, uid]);
        });
        
        WebRTCService.setEventListener('onUserLeft', (uid) => {
          console.log('📱 Mobile: Remote user left:', uid);
          setRemoteUsers(prev => prev.filter(u => u !== uid));
        });
        
        WebRTCService.setEventListener('onConnectionStateChanged', (state) => {
          console.log('🔗 Mobile: WebRTC connection state:', state);
        });
      }
      
      console.log('✅ Mobile: Services initialized successfully');
    } catch (error) {
      console.error('❌ Mobile: Failed to initialize services:', error);
    }
  };

  // Authentication
  const login = async () => {
    setLoading(true);
    try {
      console.log('🔐 Attempting login to:', `${API_BASE_URL}/auth/login`);
      console.log('🔐 Login data:', { email, password });
      console.log('🔐 Platform:', Platform.OS);
      console.log('🔐 Is Web:', Platform.OS === 'web');

      const response = await fetch(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, password }),
      });

      console.log('Response status:', response.status);
      const data = await response.json();
      console.log('Response data:', data);

      if (response.ok && data.success) {
        await AsyncStorage.setItem('userToken', data.token);
        await AsyncStorage.setItem('currentUser', JSON.stringify(data.user));
        setUser(data.user);
        
        // Load contacts with the user data directly since state update is async
        await loadContactsForUser(data.user, data.token);
        
        // Initialize SignalR for real-time communication
        await initializeSignalR(data.token);
        
        Alert.alert('Success', 'Login successful!');
      } else {
        Alert.alert('Error', data.message || 'Login failed');
      }
    } catch (error) {
      console.error('❌ Login error:', error);
      
      let errorMessage = 'Network error. Please check your connection.';
      if (error.message.includes('Network request failed')) {
        errorMessage = `Cannot connect to server at ${API_BASE_URL}. Please check if:\n• Server is running\n• You're on the same network\n• IP address is correct`;
      } else if (error.message.includes('timeout')) {
        errorMessage = 'Connection timeout. Server may be slow or unreachable.';
      }
      
      Alert.alert('Connection Error', errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const logout = async () => {
    try {
      // Disconnect SignalR
      await SignalRService.disconnect();
      setSignalRConnected(false);
      
      // Clean up WebRTC
      await WebRTCService.destroy();
      
      // Clear storage and state
      await AsyncStorage.removeItem('userToken');
      await AsyncStorage.removeItem('currentUser');
      setUser(null);
      setContacts([]);
      setMessages([]);
      setSelectedContact(null);
      setCurrentView('contacts');
      setIncomingCall(null);
    } catch (error) {
      console.error('Logout error:', error);
    }
  };

  // Initialize SignalR for real-time communication
  const initializeSignalR = async (token) => {
    try {
      console.log('🔗 Mobile: Initializing SignalR...');
      
      const connected = await SignalRService.initialize(SIGNALR_HUB_URL, token);
      if (connected) {
        setSignalRConnected(true);
        console.log('✅ Mobile: SignalR connected successfully');
        
        // Set up SignalR event listeners
        SignalRService.setEventListener('onMessageReceived', (message) => {
          console.log('📨 Mobile: Message received:', message);
          setMessages(prev => [...prev, {
            id: Date.now().toString(),
            text: message.message,
            isMe: false,
            timestamp: new Date(),
            senderId: message.senderId,
            senderName: message.senderName || 'Unknown'
          }]);
        });
        
        SignalRService.setEventListener('onIncomingCall', (callData) => {
          console.log('📞 Mobile: Incoming call:', callData);
          setIncomingCall(callData);
        });
        
        SignalRService.setEventListener('onCallAccepted', (callData) => {
          console.log('✅ Mobile: Call accepted:', callData);
          // Handle call accepted
        });
        
        SignalRService.setEventListener('onCallRejected', (callData) => {
          console.log('❌ Mobile: Call rejected:', callData);
          setCallModal({ visible: false, targetUser: null, callType: null });
        });
        
        SignalRService.setEventListener('onCallEnded', (callData) => {
          console.log('👋 Mobile: Call ended:', callData);
          setCallModal({ visible: false, targetUser: null, callType: null });
          setRemoteUsers([]);
        });
        
        SignalRService.setEventListener('onConnectionStateChanged', (state) => {
          console.log('🔗 Mobile: SignalR connection state:', state);
          setSignalRConnected(state === 'Connected');
        });
      }
    } catch (error) {
      console.error('❌ Mobile: Failed to initialize SignalR:', error);
    }
  };

  // Load contacts for a specific user (used during login)
  const loadContactsForUser = async (userData, token) => {
    try {
      if (!userData || !token) {
        console.log('❌ No user data or token provided for loading contacts');
        return;
      }

      // Determine endpoint based on user role
      let endpoint;
      if (userData.roleId === 2 || userData.roleName === 'Doctor') {
        endpoint = `${API_BASE_URL}/mobile/doctor/patients`;
        console.log('🏥 Loading patients for doctor from:', endpoint);
      } else {
        endpoint = `${API_BASE_URL}/mobile/patient/doctors`;
        console.log('👤 Loading doctors for patient from:', endpoint);
      }

      const response = await fetch(endpoint, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      console.log('📋 Contacts response status:', response.status);
      
      if (response.ok) {
        const data = await response.json();
        console.log('📋 Contacts response data:', data);
        
        // The API returns the contacts directly as an array
        setContacts(Array.isArray(data) ? data : []);
      } else {
        const errorData = await response.json();
        console.error('❌ Error response:', errorData);
        setContacts([]);
      }
    } catch (error) {
      console.error('❌ Error loading contacts:', error);
      setContacts([]);
    }
  };

  // Load contacts based on user role (used for refresh)
  const loadContacts = async () => {
    try {
      const token = await AsyncStorage.getItem('userToken');
      if (!user || !token) {
        console.log('No user or token available for loading contacts');
        return;
      }

      await loadContactsForUser(user, token);
    } catch (error) {
      console.error('❌ Error loading contacts:', error);
      setContacts([]);
    }
  };

  // Chat functionality
  const sendMessage = async () => {
    if (!newMessage.trim() || !selectedContact) return;
    
    try {
      console.log('📤 Mobile: Sending message:', newMessage);
      
      // Add message to local state immediately
      const localMessage = {
        id: Date.now().toString(),
        text: newMessage.trim(),
        isMe: true,
        timestamp: new Date(),
        senderId: user?.id,
        senderName: `${user?.firstName} ${user?.lastName}`
      };
      setMessages(prev => [...prev, localMessage]);
      
      // Send via SignalR if connected
      if (signalRConnected) {
        await SignalRService.sendMessage(selectedContact.id, newMessage.trim());
      }
      
      // Also send via REST API as backup
      const response = await fetch(`${API_BASE_URL}/mobile/send-message`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${await AsyncStorage.getItem('userToken')}`,
        },
        body: JSON.stringify({
          targetUserId: selectedContact.id,
          message: newMessage.trim()
        }),
      });
      
      if (response.ok) {
        console.log('✅ Mobile: Message sent via API');
      }
      
      setNewMessage('');
    } catch (error) {
      console.error('❌ Mobile: Failed to send message:', error);
    }
  };

  const openChat = (contact) => {
    setSelectedContact(contact);
    setCurrentView('chat');
    // Load chat history for this contact
    loadChatHistory(contact.id);
  };

  const loadChatHistory = async (targetUserId) => {
    try {
      const token = await AsyncStorage.getItem('userToken');
      const response = await fetch(`${API_BASE_URL}/mobile/messages/${targetUserId}`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (response.ok) {
        const chatMessages = await response.json();
        const formattedMessages = chatMessages.map(m => ({
          id: m.id || Date.now().toString(),
          text: m.message,
          isMe: m.isMe,
          timestamp: new Date(m.sentAt),
          senderId: m.senderId,
          senderName: m.senderName || 'Unknown'
        }));
        setMessages(formattedMessages);
        console.log('📋 Mobile: Chat history loaded:', formattedMessages.length, 'messages');
      }
    } catch (error) {
      console.error('❌ Mobile: Failed to load chat history:', error);
      setMessages([]);
    }
  };

  // Call functionality
  const initiateCall = async (targetUserId, callType) => {
    try {
      const token = await AsyncStorage.getItem('userToken');
      const response = await fetch(`${API_BASE_URL}/mobile/call/initiate`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          targetUserId: targetUserId,
          callType: callType,
        }),
      });

      const result = await response.json();
      console.log('Call initiated:', result);
      return result;
    } catch (error) {
      console.error('Error initiating call:', error);
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
      
      // Notify server about the call first
      try {
        console.log('🎯 Mobile: Notifying server about call...');
        await initiateCall(targetUser.id, callType);
        console.log('✅ Mobile: Server notified about call');
      } catch (serverError) {
        console.warn('⚠️ Mobile: Failed to notify server about call:', serverError);
      }
      
      // Join WebRTC channel
      if (agoraInitialized) {
        try {
          console.log('🎯 Mobile: Joining WebRTC channel...');
          const channelName = `call_${targetUser.id}_${user?.id}`;
          const uid = user?.id || Math.floor(Math.random() * 100000);
          const isVideoCall = callType === 'Video';
          
          const success = await WebRTCService.joinChannel(channelName, 'demo-token', uid, isVideoCall);
          if (success) {
            console.log('✅ Mobile: Successfully joined WebRTC channel');
          } else {
            console.warn('⚠️ Mobile: Failed to join WebRTC channel, using simulation');
            // Simulate remote user joining for demo
            setTimeout(() => {
              console.log('📱 Mobile: Simulating remote user joined');
              setRemoteUsers([999]);
            }, 2000);
          }
        } catch (webRTCError) {
          console.error('❌ Mobile: WebRTC error:', webRTCError);
          // Fallback to simulation
          setTimeout(() => {
            console.log('📱 Mobile: Simulating remote user joined (fallback)');
            setRemoteUsers([999]);
          }, 2000);
        }
      } else {
        // Fallback simulation if WebRTC not initialized
        console.log('🎤 Mobile: WebRTC not initialized, using simulation...');
        setTimeout(() => {
          console.log('📱 Mobile: Simulating remote user joined');
          setRemoteUsers([999]);
        }, 2000);
      }
      
    } catch (error) {
      console.error('❌ Mobile: Error starting call:', error);
      Alert.alert('Error', 'Failed to start call. Please check your connection.');
      setCallModal({ visible: false, targetUser: null, callType: null });
    }
    
    console.log('🚀 startCall function EXITING');
  };

  const endCall = async () => {
    console.log('👋 Ending call');
    
    try {
      // Leave WebRTC channel
      await WebRTCService.leaveChannel();
      console.log('✅ Mobile: Left WebRTC channel');
    } catch (error) {
      console.error('❌ Mobile: Error leaving WebRTC channel:', error);
    }
    
    // Reset UI state
    setCallModal({ visible: false, targetUser: null, callType: null });
    setRemoteUsers([]);
    setIsAudioMuted(false);
    setIsVideoMuted(false);
  };

  const toggleMute = async () => {
    const newMuteState = !isAudioMuted;
    setIsAudioMuted(newMuteState);
    
    try {
      await WebRTCService.muteLocalAudio(newMuteState);
      console.log('🔇 Audio muted:', newMuteState);
    } catch (error) {
      console.error('❌ Mobile: Error toggling audio mute:', error);
    }
  };

  const toggleVideo = async () => {
    const newMuteState = !isVideoMuted;
    setIsVideoMuted(newMuteState);
    
    try {
      await WebRTCService.muteLocalVideo(newMuteState);
      console.log('📹 Video muted:', newMuteState);
    } catch (error) {
      console.error('❌ Mobile: Error toggling video mute:', error);
    }
  };


  // Render functions
  const renderLogin = () => (
    <SafeAreaView style={styles.container}>
      <View style={styles.loginContainer}>
        <Text style={styles.title}>Mental Health App</Text>
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
      </View>
    </SafeAreaView>
  );

  // const renderContacts = () => (
  //   <SafeAreaView style={styles.container}>
  //     <View style={styles.header}>
  //       <Text style={styles.headerTitle}>Contacts</Text>
  //       <TouchableOpacity style={styles.logoutButton} onPress={logout}>
  //         <Text style={styles.logoutButtonText}>Logout</Text>
  //       </TouchableOpacity>
  //     </View>
  //     <ScrollView style={styles.contactsList}>
  //       {contacts.map((contact) => (
  //         <View key={contact.id} style={styles.contactItem}>
  //           <TouchableOpacity
  //             style={styles.contactInfo}
  //             onPress={() => setSelectedContact(contact)}
  //           >
  //             <Text style={styles.contactName}>
  //               {contact.firstName} {contact.lastName}
  //             </Text>
  //             <Text style={styles.contactRole}>{contact.roleName}</Text>
  //           </TouchableOpacity>
  //           <View style={styles.contactActions}>
  //             <TouchableOpacity
  //               style={[styles.actionButton, styles.audioButton]}
  //               onPress={() => {
  //                 console.log('🎯 Audio Call Button Pressed - Service initialized:', agoraInitialized);
  //                 startCall(contact, 'Audio');
  //               }}
  //             >
  //               <Text style={styles.actionButtonText}>📞</Text>
  //             </TouchableOpacity>
  //             <TouchableOpacity
  //               style={[styles.actionButton, styles.videoButton]}
  //               onPress={() => {
  //                 console.log('🎯 Video Call Button Pressed - Service initialized:', agoraInitialized);
  //                 startCall(contact, 'Video');
  //               }}
  //             >
  //               <Text style={styles.actionButtonText}>📹</Text>
  //             </TouchableOpacity>
  //             <TouchableOpacity
  //               style={[styles.actionButton, styles.chatButton]}
  //               onPress={() => setSelectedContact(contact)}
  //             >
  //               <Text style={styles.actionButtonText}>💬</Text>
  //             </TouchableOpacity>
  //           </View>
  //         </View>
  //       ))}
  //     </ScrollView>
  //   </SafeAreaView>
  // );

  // const renderChat = () => (
  //   <KeyboardAvoidingView 
  //     style={styles.container} 
  //     behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
  //   >
  //     <SafeAreaView style={styles.container}>
  //       <View style={styles.header}>
  //         <TouchableOpacity onPress={() => setSelectedContact(null)}>
  //           <Text style={styles.backButton}>← Back</Text>
  //         </TouchableOpacity>
  //         <Text style={styles.headerTitle}>
  //           {selectedContact?.firstName} {selectedContact?.lastName}
  //         </Text>
  //       </View>
  //       <ScrollView style={styles.messagesList}>
  //         {messages
  //           .filter(msg => 
  //             (msg.senderId === user.id && msg.recipientId === selectedContact.id) ||
  //             (msg.senderId === selectedContact.id && msg.recipientId === user.id)
  //           )
  //           .map((message) => (
  //             <View
  //               key={message.id}
  //               style={[
  //                 styles.messageItem,
  //                 message.senderId === user.id ? styles.sentMessage : styles.receivedMessage,
  //               ]}
  //             >
  //               <Text style={styles.messageText}>{message.message}</Text>
  //               <Text style={styles.messageTime}>
  //                 {new Date(message.timestamp).toLocaleTimeString()}
  //               </Text>
  //             </View>
  //           ))}
  //       </ScrollView>
  //       <View style={styles.messageInput}>
  //         <TextInput
  //           style={styles.messageTextInput}
  //           placeholder="Type a message..."
  //           value={newMessage}
  //           onChangeText={setNewMessage}
  //           multiline
  //         />
  //         <TouchableOpacity style={styles.sendButton} onPress={sendMessage}>
  //           <Text style={styles.sendButtonText}>Send</Text>
  //         </TouchableOpacity>
  //       </View>
  //     </SafeAreaView>
  //   </KeyboardAvoidingView>
  // );

  const renderCallModal = () => (
    <Modal visible={callModal.visible} animationType="slide">
      <SafeAreaView style={styles.callContainer}>
        <View style={styles.callHeader}>
          <Text style={styles.callTitle}>
            {callModal.callType} Call with {callModal.targetUser?.firstName}
          </Text>
          <Text style={styles.callStatus}>
            {remoteUsers.length > 0 ? 'Connected' : 'Connecting...'}
          </Text>
        </View>

        <View style={styles.callContent}>
          {callModal.callType === 'Video' ? (
            <View style={styles.videoContainer}>
              <View style={styles.remoteVideo}>
                {remoteUsers.length > 0 ? (
                  <Text style={styles.videoPlaceholder}>📹 Remote Video</Text>
                ) : (
                  <Text style={styles.videoPlaceholder}>Waiting for remote user...</Text>
                )}
              </View>
              <View style={styles.localVideo}>
                <Text style={styles.videoPlaceholder}>📱 Your Video</Text>
              </View>
            </View>
          ) : (
            <View style={styles.audioContainer}>
              <Text style={styles.audioIndicator}>🎵 Audio Call Active</Text>
              {remoteUsers.length > 0 && (
                <Text style={styles.connectedIndicator}>✅ Connected</Text>
              )}
            </View>
          )}
        </View>

        <View style={styles.callControls}>
          <TouchableOpacity
            style={[styles.controlButton, isAudioMuted && styles.controlButtonActive]}
            onPress={toggleMute}
          >
            <Text style={styles.controlButtonText}>
              {isAudioMuted ? '🔇' : '🎤'}
            </Text>
          </TouchableOpacity>

          {callModal.callType === 'Video' && (
            <TouchableOpacity
              style={[styles.controlButton, isVideoMuted && styles.controlButtonActive]}
              onPress={toggleVideo}
            >
              <Text style={styles.controlButtonText}>
                {isVideoMuted ? '📹' : '📷'}
              </Text>
            </TouchableOpacity>
          )}

          <TouchableOpacity style={styles.endCallButton} onPress={endCall}>
            <Text style={styles.endCallButtonText}>📞 End Call</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    </Modal>
  );

  // Render contacts list
  const renderContacts = () => (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>
          Welcome, {user?.firstName}! ({user?.roleName})
        </Text>
        <View style={styles.headerActions}>
          <Text style={[styles.connectionStatus, { color: signalRConnected ? '#4CAF50' : '#F44336' }]}>
            {signalRConnected ? '🟢 Connected' : '🔴 Disconnected'}
          </Text>
          <TouchableOpacity style={styles.logoutButton} onPress={logout}>
            <Text style={styles.logoutButtonText}>Logout</Text>
          </TouchableOpacity>
        </View>
      </View>

      <View style={styles.contactsHeader}>
        <Text style={styles.contactsTitle}>
          {user?.roleId === 2 ? 'Your Patients' : 'Your Doctors'}
        </Text>
        <Text style={styles.contactsCount}>({contacts.length})</Text>
      </View>

      <ScrollView style={styles.contactsList}>
        {contacts.map((contact) => (
          <TouchableOpacity
            key={contact.id}
            style={styles.contactItem}
            onPress={() => openChat(contact)}
          >
            <View style={styles.contactInfo}>
              <Text style={styles.contactName}>
                {contact.firstName} {contact.lastName}
              </Text>
              <Text style={styles.contactRole}>
                {contact.specialization || contact.roleName || 'User'}
              </Text>
              {contact.mobilePhone && (
                <Text style={styles.contactPhone}>{contact.mobilePhone}</Text>
              )}
            </View>
            <View style={styles.contactActions}>
              <TouchableOpacity
                style={styles.actionButton}
                onPress={() => {
                  console.log('🎯 Audio Call Button Pressed - Service initialized:', agoraInitialized);
                  startCall(contact, 'Audio');
                }}
              >
                <Text style={styles.actionButtonText}>📞</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={styles.actionButton}
                onPress={() => {
                  console.log('🎯 Video Call Button Pressed - Service initialized:', agoraInitialized);
                  startCall(contact, 'Video');
                }}
              >
                <Text style={styles.actionButtonText}>📹</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={styles.actionButton}
                onPress={() => openChat(contact)}
              >
                <Text style={styles.actionButtonText}>💬</Text>
              </TouchableOpacity>
            </View>
          </TouchableOpacity>
        ))}
      </ScrollView>
    </SafeAreaView>
  );

  // Render chat interface
  const renderChat = () => (
    <SafeAreaView style={styles.container}>
      <View style={styles.chatHeader}>
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => {
            setSelectedContact(null);
            setCurrentView('contacts');
            setMessages([]);
          }}
        >
          <Text style={styles.backButtonText}>← Back</Text>
        </TouchableOpacity>
        <Text style={styles.chatTitle}>
          {selectedContact?.firstName} {selectedContact?.lastName}
        </Text>
        <View style={styles.chatActions}>
          <TouchableOpacity
            style={styles.chatActionButton}
            onPress={() => startCall(selectedContact, 'Audio')}
          >
            <Text style={styles.chatActionText}>📞</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={styles.chatActionButton}
            onPress={() => startCall(selectedContact, 'Video')}
          >
            <Text style={styles.chatActionText}>📹</Text>
          </TouchableOpacity>
        </View>
      </View>

      <KeyboardAvoidingView
        style={styles.chatContainer}
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        keyboardVerticalOffset={Platform.OS === 'ios' ? 90 : 0}
      >
        <ScrollView
          style={styles.messagesContainer}
          contentContainerStyle={styles.messagesContent}
        >
          {messages.map((message) => (
            <View
              key={message.id}
              style={[
                styles.messageItem,
                message.isMe ? styles.myMessage : styles.otherMessage,
              ]}
            >
              <Text style={styles.messageText}>{message.text}</Text>
              <Text style={styles.messageTime}>
                {message.timestamp?.toLocaleTimeString() || 'Now'}
              </Text>
            </View>
          ))}
        </ScrollView>

        <View style={styles.messageInput}>
          <TextInput
            style={styles.textInput}
            value={newMessage}
            onChangeText={setNewMessage}
            placeholder="Type a message..."
            multiline
            maxLength={500}
          />
          <TouchableOpacity
            style={[styles.sendButton, !newMessage.trim() && styles.sendButtonDisabled]}
            onPress={sendMessage}
            disabled={!newMessage.trim()}
          >
            <Text style={styles.sendButtonText}>Send</Text>
          </TouchableOpacity>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );

  // Main render
  if (!user) {
    return renderLogin();
  }

  return (
    <View style={styles.container}>
      <StatusBar style="auto" />
      {selectedContact ? renderChat() : renderContacts()}
      {renderCallModal()}
    </View>
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
    padding: 20,
    backgroundColor: '#fff',
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    textAlign: 'center',
    marginBottom: 30,
    color: '#333',
  },
  input: {
    borderWidth: 1,
    borderColor: '#ddd',
    padding: 15,
    marginBottom: 15,
    borderRadius: 8,
    fontSize: 16,
  },
  button: {
    backgroundColor: '#007bff',
    padding: 15,
    borderRadius: 8,
    alignItems: 'center',
  },
  buttonDisabled: {
    backgroundColor: '#ccc',
  },
  buttonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 15,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#eee',
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
  },
  logoutButton: {
    padding: 8,
  },
  logoutButtonText: {
    color: '#007bff',
    fontSize: 16,
  },
  backButton: {
    color: '#007bff',
    fontSize: 16,
  },
  contactsList: {
    flex: 1,
  },
  contactItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 15,
    backgroundColor: '#fff',
    marginVertical: 1,
  },
  contactInfo: {
    flex: 1,
  },
  contactName: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
  },
  contactRole: {
    fontSize: 14,
    color: '#666',
  },
  contactActions: {
    flexDirection: 'row',
    gap: 10,
  },
  actionButton: {
    width: 50,
    height: 50,
    borderRadius: 25,
    justifyContent: 'center',
    alignItems: 'center',
    minWidth: 50,
    minHeight: 50,
  },
  audioButton: {
    backgroundColor: '#28a745',
  },
  videoButton: {
    backgroundColor: '#007bff',
  },
  chatButton: {
    backgroundColor: '#ffc107',
  },
  actionButtonText: {
    fontSize: 20,
  },
  messagesList: {
    flex: 1,
    padding: 10,
  },
  messageItem: {
    padding: 10,
    marginVertical: 5,
    borderRadius: 8,
    maxWidth: '80%',
  },
  sentMessage: {
    backgroundColor: '#007bff',
    alignSelf: 'flex-end',
  },
  receivedMessage: {
    backgroundColor: '#e9ecef',
    alignSelf: 'flex-start',
  },
  messageText: {
    fontSize: 16,
    color: '#333',
  },
  messageTime: {
    fontSize: 12,
    color: '#666',
    marginTop: 5,
  },
  messageInput: {
    flexDirection: 'row',
    padding: 10,
    backgroundColor: '#fff',
    borderTopWidth: 1,
    borderTopColor: '#eee',
  },
  messageTextInput: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#ddd',
    padding: 10,
    borderRadius: 20,
    marginRight: 10,
    maxHeight: 100,
  },
  sendButton: {
    backgroundColor: '#007bff',
    paddingHorizontal: 20,
    paddingVertical: 10,
    borderRadius: 20,
    justifyContent: 'center',
  },
  sendButtonText: {
    color: '#fff',
    fontWeight: 'bold',
  },
  callContainer: {
    flex: 1,
    backgroundColor: '#1a1a1a',
  },
  callHeader: {
    padding: 20,
    alignItems: 'center',
  },
  callTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#fff',
    marginBottom: 5,
  },
  callStatus: {
    fontSize: 16,
    color: '#ccc',
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
    justifyContent: 'center',
    alignItems: 'center',
    borderRadius: 8,
  },
  videoPlaceholder: {
    color: '#fff',
    fontSize: 16,
  },
  audioContainer: {
    alignItems: 'center',
  },
  audioIndicator: {
    fontSize: 24,
    color: '#fff',
    marginBottom: 20,
  },
  connectedIndicator: {
    fontSize: 18,
    color: '#28a745',
  },
  callControls: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    padding: 30,
    gap: 20,
  },
  controlButton: {
    width: 60,
    height: 60,
    borderRadius: 30,
    backgroundColor: '#333',
    justifyContent: 'center',
    alignItems: 'center',
  },
  controlButtonActive: {
    backgroundColor: '#dc3545',
  },
  controlButtonText: {
    fontSize: 24,
  },
  endCallButton: {
    backgroundColor: '#dc3545',
    paddingHorizontal: 20,
    paddingVertical: 15,
    borderRadius: 25,
  },
  endCallButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
  // Enhanced contacts and chat styles
  headerActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
  },
  connectionStatus: {
    fontSize: 12,
    fontWeight: 'bold',
  },
  logoutButton: {
    backgroundColor: '#dc3545',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 5,
  },
  logoutButtonText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: 'bold',
  },
  contactsHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 15,
    paddingVertical: 10,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#eee',
  },
  contactsTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
  },
  contactsCount: {
    fontSize: 16,
    color: '#666',
    marginLeft: 5,
  },
  contactInfo: {
    flex: 1,
  },
  contactName: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 2,
  },
  contactRole: {
    fontSize: 14,
    color: '#666',
    marginBottom: 2,
  },
  contactPhone: {
    fontSize: 12,
    color: '#999',
  },
  contactActions: {
    flexDirection: 'row',
    gap: 8,
  },
  // Chat styles
  chatHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: 15,
    backgroundColor: '#007bff',
    borderBottomWidth: 1,
    borderBottomColor: '#0056b3',
  },
  backButton: {
    paddingHorizontal: 10,
    paddingVertical: 5,
  },
  backButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
  chatTitle: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
    flex: 1,
    textAlign: 'center',
  },
  chatActions: {
    flexDirection: 'row',
    gap: 10,
  },
  chatActionButton: {
    padding: 8,
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
    borderRadius: 20,
  },
  chatActionText: {
    fontSize: 18,
  },
  chatContainer: {
    flex: 1,
  },
  messagesContainer: {
    flex: 1,
    padding: 10,
  },
  messagesContent: {
    paddingBottom: 10,
  },
  messageItem: {
    marginVertical: 5,
    padding: 12,
    borderRadius: 15,
    maxWidth: '80%',
  },
  myMessage: {
    alignSelf: 'flex-end',
    backgroundColor: '#007bff',
  },
  otherMessage: {
    alignSelf: 'flex-start',
    backgroundColor: '#e9ecef',
  },
  messageText: {
    fontSize: 16,
    color: '#fff',
  },
  messageTime: {
    fontSize: 12,
    color: 'rgba(255, 255, 255, 0.8)',
    marginTop: 4,
  },
  messageInput: {
    flexDirection: 'row',
    padding: 10,
    backgroundColor: '#fff',
    borderTopWidth: 1,
    borderTopColor: '#eee',
    alignItems: 'flex-end',
  },
  textInput: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 20,
    paddingHorizontal: 15,
    paddingVertical: 10,
    marginRight: 10,
    maxHeight: 100,
    fontSize: 16,
  },
  sendButton: {
    backgroundColor: '#007bff',
    paddingHorizontal: 20,
    paddingVertical: 12,
    borderRadius: 20,
  },
  sendButtonDisabled: {
    backgroundColor: '#ccc',
  },
  sendButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
});