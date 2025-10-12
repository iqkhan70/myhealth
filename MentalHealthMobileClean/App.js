// App.js
import React, { useState, useEffect } from 'react';
import {
  StyleSheet, Text, View, TextInput, TouchableOpacity, Alert, ScrollView,
  Platform, KeyboardAvoidingView, Modal, SafeAreaView
} from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { StatusBar } from 'expo-status-bar';
import SignalRService from './src/services/SignalRService';
// ✅ Use Agora now
import AgoraService from './src/services/AgoraService';
import { RtcLocalView, RtcRemoteView } from 'react-native-agora';

// ---------- ENV / URLS ----------
const getApiBaseUrl = () => {
  const isWeb = Platform.OS === 'web' && typeof window !== 'undefined';
  if (isWeb) return 'http://localhost:5262/api';
  return 'http://192.168.86.28:5262/api';
};
const API_BASE_URL = getApiBaseUrl();
const SIGNALR_HUB_URL = API_BASE_URL.replace('/api', '/mobilehub');

// 👉 Set your Agora App ID here (or pull from .env)
const AGORA_APP_ID = 'efa11b3a7d05409ca979fb25a5b489ae';

// ---------- APP ----------
export default function App() {
  const [user, setUser] = useState(null);
  const [email, setEmail] = useState('john@doe.com');
  const [password, setPassword] = useState('demo123');
  const [loading, setLoading] = useState(false);

  const [contacts, setContacts] = useState([]);
  const [messages, setMessages] = useState([]);
  const [newMessage, setNewMessage] = useState('');
  const [selectedContact, setSelectedContact] = useState(null);

  const [signalRConnected, setSignalRConnected] = useState(false);

  // 🔔 Call modal state
  const [callModal, setCallModal] = useState({ visible: false, targetUser: null, callType: null, channelName: null });
  const [remoteUsers, setRemoteUsers] = useState([]);
  const [isAudioMuted, setIsAudioMuted] = useState(false);
  const [isVideoMuted, setIsVideoMuted] = useState(false);
  const [agoraInitialized, setAgoraInitialized] = useState(false);

  // ---------- INIT ----------
  useEffect(() => {
    checkAuthStatus();
    initializeServices();
  }, []);

  const checkAuthStatus = async () => {
    try {
      const token = await AsyncStorage.getItem('userToken');
      const userData = await AsyncStorage.getItem('currentUser');
      if (token && userData) {
        const u = JSON.parse(userData);
        setUser(u);
        await loadContactsForUser(u, token);
      }
    } catch (e) {
      console.error('Error checking auth:', e);
    }
  };

  const initializeServices = async () => {
    try {
      console.log('🚀 Initializing Agora…');
      const ok = await AgoraService.initialize(AGORA_APP_ID);
      if (ok) {
        setAgoraInitialized(true);

        // Hook Agora events → UI
        AgoraService.setListener('onUserJoined', (uid) => {
          setRemoteUsers((prev) => (prev.includes(uid) ? prev : [...prev, uid]));
        });
        AgoraService.setListener('onUserLeft', (uid) => {
          setRemoteUsers((prev) => prev.filter((id) => id !== uid));
        });
        AgoraService.setListener('onConnectionStateChanged', (state) => {
          console.log('Agora state:', state);
        });
      }
    } catch (e) {
      console.error('❌ Failed to init services:', e);
    }
  };

  // ---------- AUTH ----------
  const login = async () => {
    setLoading(true);
    try {
      const resp = await fetch(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });
      const data = await resp.json();
      if (resp.ok && data.success) {
        await AsyncStorage.setItem('userToken', data.token);
        await AsyncStorage.setItem('currentUser', JSON.stringify(data.user));
        setUser(data.user);

        await loadContactsForUser(data.user, data.token);
        await initializeSignalR(data.token);

        Alert.alert('Success', 'Login successful!');
      } else {
        Alert.alert('Error', data.message || 'Login failed');
      }
    } catch (e) {
      console.error('Login error:', e);
      Alert.alert('Connection Error', `Cannot reach ${API_BASE_URL}`);
    } finally {
      setLoading(false);
    }
  };

  const logout = async () => {
    try {
      await SignalRService.disconnect();
      setSignalRConnected(false);
      await AgoraService.leaveChannel();
      await AgoraService.destroy();

      await AsyncStorage.removeItem('userToken');
      await AsyncStorage.removeItem('currentUser');
      setUser(null);
      setContacts([]);
      setMessages([]);
      setSelectedContact(null);
      setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
      setRemoteUsers([]);
    } catch (e) {
      console.error('Logout error:', e);
    }
  };

  // ---------- SIGNALR ----------
  const initializeSignalR = async (token) => {
    try {
      const connected = await SignalRService.initialize(SIGNALR_HUB_URL, token);
      if (connected) {
        setSignalRConnected(true);

        SignalRService.setEventListener('onMessageReceived', (message) => {
          setMessages((prev) => [...prev, {
            id: Date.now().toString(),
            text: message.message,
            isMe: false,
            timestamp: new Date(),
            senderId: message.senderId,
            senderName: message.senderName || 'Unknown'
          }]);
        });

        // Optional: wire these if your server emits them
        SignalRService.setEventListener('onIncomingCall', (callData) => {
          // You can auto-open modal or show a dialog
          console.log('Incoming call:', callData);
        });
      }
    } catch (e) {
      console.error('SignalR init error:', e);
    }
  };

  // ---------- CONTACTS ----------
  const loadContactsForUser = async (userData, token) => {
    try {
      let endpoint;
      if (userData.roleId === 2 || userData.roleName === 'Doctor') {
        endpoint = `${API_BASE_URL}/mobile/doctor/patients`;
      } else {
        endpoint = `${API_BASE_URL}/mobile/patient/doctors`;
      }
      const resp = await fetch(endpoint, { headers: { Authorization: `Bearer ${token}` } });
      if (resp.ok) {
        const arr = await resp.json();
        setContacts(Array.isArray(arr) ? arr : []);
      } else {
        setContacts([]);
      }
    } catch (e) {
      console.error('Contacts error:', e);
      setContacts([]);
    }
  };

  const loadContacts = async () => {
    try {
      const token = await AsyncStorage.getItem('userToken');
      if (!user || !token) return;
      await loadContactsForUser(user, token);
    } catch (e) {
      console.error('loadContacts error:', e);
      setContacts([]);
    }
  };

  // ---------- CHAT ----------
  const openChat = (contact) => {
    setSelectedContact(contact);
    setCurrentView('chat');
    loadChatHistory(contact.id);
  };

  const [currentView, setCurrentView] = useState('contacts');

  const loadChatHistory = async (targetUserId) => {
    try {
      const token = await AsyncStorage.getItem('userToken');
      const resp = await fetch(`${API_BASE_URL}/mobile/messages/${targetUserId}`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (resp.ok) {
        const arr = await resp.json();
        setMessages(
          (arr || []).map((m, i) => ({
            id: m.id || `${Date.now()}_${i}`,
            text: m.message,
            isMe: m.isMe,
            timestamp: new Date(m.sentAt),
            senderId: m.senderId,
            senderName: m.senderName || 'Unknown'
          }))
        );
      } else {
        setMessages([]);
      }
    } catch (e) {
      console.error('loadChatHistory error:', e);
      setMessages([]);
    }
  };

  const sendMessage = async () => {
    if (!newMessage.trim() || !selectedContact) return;
    const msg = {
      id: Date.now().toString(),
      text: newMessage.trim(),
      isMe: true,
      timestamp: new Date(),
      senderId: user?.id,
      senderName: `${user?.firstName} ${user?.lastName}`
    };
    setMessages((prev) => [...prev, msg]);

    try {
      if (signalRConnected) {
        await SignalRService.sendMessage(selectedContact.id, newMessage.trim());
      }
      const resp = await fetch(`${API_BASE_URL}/mobile/send-message`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${await AsyncStorage.getItem('userToken')}`
        },
        body: JSON.stringify({ targetUserId: selectedContact.id, message: newMessage.trim() })
      });
      // (optional) handle resp
    } catch (e) {
      console.error('sendMessage error:', e);
    } finally {
      setNewMessage('');
    }
  };

  // ---------- CALLING (AGORA) ----------
  const fetchAgoraToken = async (channelName, uid) => {
    try {
      if (!channelName || uid == null) {
        console.error('❌ Missing channelName or uid');
        return null;
      }

      const url = `${API_BASE_URL}/realtime/token?channel=${encodeURIComponent(channelName)}&uid=${uid}`;
      console.log(`🎯 Fetching Agora token from: ${url}`);

      const authToken = await AsyncStorage.getItem('userToken');

      const resp = await fetch(url, {
        method: 'GET',
        headers: {
          Accept: 'application/json',
          Authorization: `Bearer ${authToken || ''}`,
        },
      });

      if (!resp.ok) {
        const errText = await resp.text();
        console.error(`❌ Token request failed [${resp.status}]: ${errText}`);
        return null;
      }

      const data = await resp.json();
      if (!data?.token) {
        console.error('❌ Invalid token response:', data);
        return null;
      }

      console.log(`✅ Received Agora token for channel "${channelName}"`);
      return data.token;
    } catch (e) {
      console.warn('⚠️ Token fetch failed:', e.message);
      return null;
    }
  };


  const initiateCall = async (targetUserId, callType) => {
    try {
      const token = await AsyncStorage.getItem('userToken');
      const resp = await fetch(`${API_BASE_URL}/mobile/call/initiate`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({ targetUserId, callType })
      });
      return await resp.json();
    } catch (e) {
      console.error('initiateCall error:', e);
      throw e;
    }
  };

  const startCall = async (targetUser, callType) => {
      try {
        setCallModal({ visible: true, targetUser, callType, channelName: null });
        setRemoteUsers([]);
        setIsAudioMuted(false);
        setIsVideoMuted(false);

        // Notify server (optional)
        try {
          await initiateCall(targetUser.id, callType);
        } catch (e) {
          console.warn('Server call notify failed (continuing):', e?.message);
        }

        if (!agoraInitialized) {
          Alert.alert('Error', 'Agora not initialized');
          setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
          return;
        }

        // 🟢 define your channel & uid
        const channelName = `call_${targetUser.id}_${user?.id}`;
        const uid = user?.id || Math.floor(Math.random() * 100000);
        const withVideo = callType === 'Video';

        // 🟢 get token dynamically from backend (port 5262)
        const rtcToken = await fetchAgoraToken(channelName, uid);
        console.log('🎫 Agora Token:', rtcToken);

        if (!rtcToken) {
          Alert.alert('Error', 'Failed to fetch Agora token.');
          setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
          return;
        }

        console.log("📞 Joining Agora:", { channelName, uid, token: rtcToken });
        
        // 🟢 now join the channel
        const ok = await AgoraService.joinChannel({
          token: rtcToken,
          channelName,
          uid,
          withVideo,
        });

        //const ok = await AgoraService.joinChannel({ token: "DEV_702387eed4a52fcad5b0e9b041fff1ea79e1b7852bb769c1d9e4b1985654103", channelName: "test", uid: 0, withVideo });


        if (!ok) throw new Error('Join channel failed');

        setCallModal((prev) => ({ ...prev, channelName }));
      } catch (e) {
        console.error('startCall error:', e);
        Alert.alert('Error', e.message || 'Failed to start call.');
        setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
      }
    };

  const endCall = async () => {
    try {
      await AgoraService.leaveChannel();
    } catch (e) {
      console.error('leave error:', e);
    }
    setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
    setRemoteUsers([]);
    setIsAudioMuted(false);
    setIsVideoMuted(false);
  };

  const toggleMute = async () => {
    const next = !isAudioMuted;
    setIsAudioMuted(next);
    await AgoraService.muteLocalAudio(next);
  };

  const toggleVideo = async () => {
    const next = !isVideoMuted;
    setIsVideoMuted(next);
    await AgoraService.muteLocalVideo(next);
  };

  // ---------- RENDER ----------
  const renderLogin = () => (
    <SafeAreaView style={styles.container}>
      <View style={styles.loginContainer}>
        <Text style={styles.title}>Mental Health App</Text>
        <TextInput style={styles.input} placeholder="Email" value={email} onChangeText={setEmail} autoCapitalize="none" />
        <TextInput style={styles.input} placeholder="Password" value={password} onChangeText={setPassword} secureTextEntry />
        <TouchableOpacity style={[styles.button, loading && styles.buttonDisabled]} onPress={login} disabled={loading}>
          <Text style={styles.buttonText}>{loading ? 'Logging in…' : 'Login'}</Text>
        </TouchableOpacity>
      </View>
    </SafeAreaView>
  );

  const renderCallModal = () => (
    <Modal visible={callModal.visible} animationType="slide">
      <SafeAreaView style={styles.callContainer}>
        <View style={styles.callHeader}>
          <Text style={styles.callTitle}>
            {callModal.callType} Call with {callModal.targetUser?.firstName}
          </Text>
          <Text style={styles.callStatus}>
            {remoteUsers.length > 0 ? 'Connected' : 'Connecting…'}
          </Text>
        </View>

        <View style={styles.callContent}>
          {callModal.callType === 'Video' ? (
            <View style={styles.videoContainer}>
              {/* Remote */}
              <View style={styles.remoteVideo}>
                {remoteUsers.length > 0 && callModal.channelName ? (
                  <RtcRemoteView.SurfaceView
                    uid={remoteUsers[0]}
                    channelId={callModal.channelName}
                    style={{ width: '100%', height: '100%' }}
                  />
                ) : (
                  <Text style={styles.videoPlaceholder}>Waiting for remote…</Text>
                )}
              </View>
              {/* Local PiP */}
              {callModal.channelName ? (
                <View style={styles.localVideo}>
                  <RtcLocalView.SurfaceView
                    channelId={callModal.channelName}
                    style={{ width: '100%', height: '100%', borderRadius: 8 }}
                  />
                </View>
              ) : null}
            </View>
          ) : (
            <View style={styles.audioContainer}>
              <Text style={styles.audioIndicator}>🎵 Audio Call Active</Text>
              {remoteUsers.length > 0 && <Text style={styles.connectedIndicator}>✅ Connected</Text>}
            </View>
          )}
        </View>

        <View style={styles.callControls}>
          <TouchableOpacity
            style={[styles.controlButton, isAudioMuted && styles.controlButtonActive]}
            onPress={toggleMute}
          >
            <Text style={styles.controlButtonText}>{isAudioMuted ? '🔇' : '🎤'}</Text>
          </TouchableOpacity>

          {callModal.callType === 'Video' && (
            <TouchableOpacity
              style={[styles.controlButton, isVideoMuted && styles.controlButtonActive]}
              onPress={toggleVideo}
            >
              <Text style={styles.controlButtonText}>{isVideoMuted ? '📹' : '📷'}</Text>
            </TouchableOpacity>
          )}

          <TouchableOpacity style={styles.endCallButton} onPress={endCall}>
            <Text style={styles.endCallButtonText}>📞 End Call</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    </Modal>
  );

  const renderContacts = () => (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>Welcome, {user?.firstName}! ({user?.roleName})</Text>
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
        <Text style={styles.contactsTitle}>{user?.roleId === 2 ? 'Your Patients' : 'Your Doctors'}</Text>
        <Text style={styles.contactsCount}>({contacts.length})</Text>
      </View>

      <ScrollView style={styles.contactsList}>
        {contacts.map((contact) => (
          <TouchableOpacity key={contact.id} style={styles.contactItem} onPress={() => openChat(contact)}>
            <View style={styles.contactInfo}>
              <Text style={styles.contactName}>{contact.firstName} {contact.lastName}</Text>
              <Text style={styles.contactRole}>{contact.specialization || contact.roleName || 'User'}</Text>
              {contact.mobilePhone && <Text style={styles.contactPhone}>{contact.mobilePhone}</Text>}
            </View>
            <View style={styles.contactActions}>
              <TouchableOpacity style={styles.actionButton} onPress={() => startCall(contact, 'Audio')}>
                <Text style={styles.actionButtonText}>📞</Text>
              </TouchableOpacity>
              <TouchableOpacity style={styles.actionButton} onPress={() => startCall(contact, 'Video')}>
                <Text style={styles.actionButtonText}>📹</Text>
              </TouchableOpacity>
              <TouchableOpacity style={styles.actionButton} onPress={() => openChat(contact)}>
                <Text style={styles.actionButtonText}>💬</Text>
              </TouchableOpacity>
            </View>
          </TouchableOpacity>
        ))}
      </ScrollView>
    </SafeAreaView>
  );

  const renderChat = () => (
    <SafeAreaView style={styles.container}>
      <View style={styles.chatHeader}>
        <TouchableOpacity style={styles.backButton} onPress={() => {
          setSelectedContact(null);
          setCurrentView('contacts');
          setMessages([]);
        }}>
          <Text style={styles.backButtonText}>← Back</Text>
        </TouchableOpacity>
        <Text style={styles.chatTitle}>{selectedContact?.firstName} {selectedContact?.lastName}</Text>
        <View style={styles.chatActions}>
          <TouchableOpacity style={styles.chatActionButton} onPress={() => startCall(selectedContact, 'Audio')}>
            <Text style={styles.chatActionText}>📞</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.chatActionButton} onPress={() => startCall(selectedContact, 'Video')}>
            <Text style={styles.chatActionText}>📹</Text>
          </TouchableOpacity>
        </View>
      </View>

      <KeyboardAvoidingView style={styles.chatContainer} behavior={Platform.OS === 'ios' ? 'padding' : 'height'} keyboardVerticalOffset={Platform.OS === 'ios' ? 90 : 0}>
        <ScrollView style={styles.messagesContainer} contentContainerStyle={styles.messagesContent}>
          {messages.map((m) => (
            <View key={m.id} style={[styles.messageItem, m.isMe ? styles.myMessage : styles.otherMessage]}>
              <Text style={styles.messageText}>{m.text}</Text>
              <Text style={styles.messageTime}>{m.timestamp?.toLocaleTimeString() || 'Now'}</Text>
            </View>
          ))}
        </ScrollView>

        <View style={styles.messageInput}>
          <TextInput
            style={styles.textInput}
            value={newMessage}
            onChangeText={setNewMessage}
            placeholder="Type a message…"
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

  if (!user) return renderLogin();

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
