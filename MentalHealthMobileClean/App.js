// App.js
import React, { useState, useEffect, useRef, useCallback } from 'react';
import {
  StyleSheet, Text, View, TextInput, TouchableOpacity, Alert, ScrollView,
  Platform, KeyboardAvoidingView, Modal
} from 'react-native';

import { SafeAreaView } from 'react-native-safe-area-context';

import AsyncStorage from '@react-native-async-storage/async-storage';
import { StatusBar } from 'expo-status-bar';
import SignalRService from './src/services/SignalRService';
// ✅ Use Agora now (conditionally imported for development builds only)
let AgoraService = null;
let RtcLocalView = null;
let RtcRemoteView = null;

// Try to import Agora - will fail in Expo Go, that's OK
// In Expo Go, react-native-agora requires native code which isn't available
try {
  AgoraService = require('./src/services/AgoraService').default;
  const agoraComponents = require('react-native-agora');
  RtcLocalView = agoraComponents.RtcLocalView;
  RtcRemoteView = agoraComponents.RtcRemoteView;
  console.log('✅ Agora components loaded successfully');
} catch (error) {
  console.warn('⚠️ Agora not available (running in Expo Go?):', error.message);
  console.warn('💡 Video/Audio calls will not work. To enable calls, create a development build:');
  console.warn('   npx expo run:ios  or  npx expo run:android');
  // Create mock components for Expo Go (with same structure as real components)
  const MockView = () => null;
  MockView.TextureView = () => null;
  MockView.SurfaceView = () => null;
  RtcLocalView = MockView;
  RtcRemoteView = MockView;
}

import DocumentUpload from './src/components/DocumentUpload';
import SmsComponent from './src/components/SmsComponent';

// Import app configuration
import AppConfig from './src/config/app.config';

// detect platform once
const isIOS = Platform.OS === 'ios';

// ---------- ENV / URLS ----------
const getApiBaseUrl = () => {
  const isWeb = Platform.OS === 'web' && typeof window !== 'undefined';
  // For web/localhost development, use HTTPS (server runs on HTTPS)
  if (isWeb) {
    const url = AppConfig.getWebApiBaseUrl();
    console.log('🌐 Using Web API URL:', url);
    return url;
  }
  // For mobile: use HTTPS with configured server IP
  const url = AppConfig.getMobileApiBaseUrl();
  console.log('📱 Using Mobile API URL:', url);
  console.log('📱 Config:', {
    SERVER_IP: AppConfig.SERVER_IP,
    SERVER_PORT: AppConfig.SERVER_PORT,
    USE_HTTPS: AppConfig.USE_HTTPS
  });
  return url;
};
const API_BASE_URL = getApiBaseUrl();
const SIGNALR_HUB_URL = API_BASE_URL.replace('/api', '/mobilehub');
console.log('✅ API Base URL initialized:', API_BASE_URL);
console.log('✅ SignalR Hub URL:', SIGNALR_HUB_URL);

// 👉 Set your Agora App ID here (or pull from .env)
const AGORA_APP_ID = 'b480142a879c4ed2ab7efb07d318abda';

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

  // 📄 Document upload state
  const [currentView, setCurrentView] = useState('login'); // 'login', 'main', 'documents', 'chat', 'contact-detail'
  const [availablePatients, setAvailablePatients] = useState([]);
  const [selectedContactDetail, setSelectedContactDetail] = useState(null);

  // 📱 SMS state
  const [smsModalVisible, setSmsModalVisible] = useState(false);

  // 🚨 Emergency state
  const [sendingEmergency, setSendingEmergency] = useState(false);
  const [deviceToken, setDeviceToken] = useState(null);
  const deviceRegisteredRef = useRef(false);
  const userInitializedRef = useRef(false);
  const contactsLoadedRef = useRef(false);
  const lastLoadedUserIdRef = useRef(null);

  // ---------- INIT ----------
  useEffect(() => {
    checkAuthStatus();
  }, []); // Only run once on mount

  useEffect(() => {
    if (user && user.id && !userInitializedRef.current) {
      userInitializedRef.current = true;
      initializeServices();
      registerDeviceForEmergency();
    }
  }, [user?.id]); // Only run when user ID changes, not on every user object change

  const checkAuthStatus = async () => {
    try {
      const token = await AsyncStorage.getItem('userToken');
      const userData = await AsyncStorage.getItem('currentUser');
      if (token && userData) {
        const u = JSON.parse(userData);
        setUser(u);
        // Only load contacts once
        if (!contactsLoadedRef.current) {
          contactsLoadedRef.current = true;
          await loadContactsForUser(u, token);
        }
      }
    } catch (e) {
      console.error('Error checking auth:', e);
    }
  };

  const initializeServices = async () => {
    try {
      // Only initialize Agora if it's available (not in Expo Go)
      if (AgoraService) {
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
      } else {
        console.warn('⚠️ Agora not available - running in Expo Go. Video/Audio calls will not work.');
        console.warn('💡 To enable calls, create a development build: npx expo run:ios or npx expo run:android');
      }
    } catch (e) {
      console.error('❌ Failed to init services:', e);
      console.warn('⚠️ Agora initialization failed - calls will not work');
    }
  };

  // ---------- AUTH ----------
  const login = async () => {
    setLoading(true);
    try {
      console.log('🔐 Attempting login to:', `${API_BASE_URL}/auth/login`);
      const resp = await fetch(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });
      console.log('📡 Login response status:', resp.status, resp.statusText);
      
      const data = await resp.json();
      if (resp.ok && data.success) {
        await AsyncStorage.setItem('userToken', data.token);
        await AsyncStorage.setItem('currentUser', JSON.stringify(data.user));
        
        // Reset flags for new login BEFORE setting user
        contactsLoadedRef.current = false;
        userInitializedRef.current = false;
        lastLoadedUserIdRef.current = null;
        loadingContactsRef.current = false;
        lastCallTimeRef.current = 0; // Reset to allow first call
        hasContactsRef.current = false; // Reset contacts flag
        
        setUser(data.user);
        
        // Load contacts after a small delay to ensure state is set
        // and to avoid race conditions with useEffect
        setTimeout(async () => {
          if (!loadingContactsRef.current && lastLoadedUserIdRef.current !== data.user.id) {
            await loadContactsForUser(data.user, data.token);
          }
        }, 300); // Slightly longer delay to ensure everything is ready
        
        await loadAvailablePatients(data.user, data.token);
        await initializeSignalR(data.token);

        setCurrentView('main');
        Alert.alert('Success', 'Login successful!');
      } else {
        Alert.alert('Error', data.message || 'Login failed');
      }
    } catch (e) {
      console.error('❌ Login error:', e);
      console.error('❌ Error details:', {
        message: e.message,
        name: e.name,
        stack: e.stack
      });
      
      // Provide helpful error message
      let errorMsg = `Cannot reach server at ${API_BASE_URL}`;
      if (e.message && e.message.includes('Network request failed')) {
        errorMsg += '\n\nPossible causes:\n';
        errorMsg += '• Server not running\n';
        errorMsg += '• SSL certificate not trusted (self-signed cert)\n';
        errorMsg += '• App needs to be rebuilt after certificate bypass config\n';
        errorMsg += '• Wrong IP address\n';
        errorMsg += '• Firewall blocking connection\n\n';
        errorMsg += '🔧 Solution: Rebuild the app to apply certificate bypass:\n';
        errorMsg += '   npx expo run:android  (or run:ios)\n\n';
        errorMsg += '⚠️  Note: Certificate bypass config requires a native rebuild.\n';
        errorMsg += '   Expo Go won\'t work - you need a development build.';
      }
      
      Alert.alert('Connection Error', errorMsg);
    } finally {
      setLoading(false);
    }
  };

  const loadAvailablePatients = async (currentUser, token) => {
    try {
      // For doctors, load their assigned patients
      if (currentUser.roleId === 2) { // Assuming 2 is Doctor role
        // TODO: Implement API call to get assigned patients
        // For now, use mock data
        setAvailablePatients([
          { id: 1, firstName: 'John', lastName: 'Doe', email: 'john.doe@example.com' },
          { id: 2, firstName: 'Jane', lastName: 'Smith', email: 'jane.smith@example.com' }
        ]);
      } else if (currentUser.roleId === 1) { // Patient role
        // For patients, they can only upload for themselves
        setAvailablePatients([currentUser]);
      }
    } catch (error) {
      console.error('Error loading available patients:', error);
    }
  };

  const logout = async () => {
    try {
      await SignalRService.disconnect();
      setSignalRConnected(false);
      // Clean up Agora if available
      if (AgoraService && agoraInitialized) {
        try {
          await AgoraService.leaveChannel();
          await AgoraService.destroy();
        } catch (e) {
          console.warn('Agora cleanup error:', e);
        }
      }

      await AsyncStorage.removeItem('userToken');
      await AsyncStorage.removeItem('currentUser');
      setUser(null);
      setContacts([]);
      setMessages([]);
      setSelectedContact(null);
      setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
      setCurrentView('login');
      setAvailablePatients([]);
      setAgoraInitialized(false);
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
  const loadingContactsRef = useRef(false);
  const lastCallTimeRef = useRef(0);
  const hasContactsRef = useRef(false);
  
  const loadContactsForUser = useCallback(async (userData, token) => {
    const callId = Math.random().toString(36).substring(7);
    const now = Date.now();
    
    console.log(`🔍 [${callId}] loadContactsForUser called:`, {
      userId: userData?.id,
      loadingContacts: loadingContactsRef.current,
      lastLoadedUserId: lastLoadedUserIdRef.current,
      timeSinceLastCall: now - lastCallTimeRef.current,
      timestamp: new Date().toISOString()
    });
    
    // Client-side rate limiting: prevent calls more than once per 2 seconds
    // But allow the first call (when lastCallTimeRef is 0)
    if (lastCallTimeRef.current > 0 && (now - lastCallTimeRef.current) < 2000) {
      console.log(`⏸️ [${callId}] Rate limited: Only ${now - lastCallTimeRef.current}ms since last call. Skipping...`);
      return;
    }
    
    // Prevent multiple simultaneous calls
    if (loadingContactsRef.current) {
      console.log(`⏸️ [${callId}] Contacts already loading, skipping...`);
      return;
    }
    
    // Prevent loading for the same user ID multiple times (unless it's been more than 10 seconds)
    // This allows refreshing contacts after a delay, but prevents spam
    if (lastLoadedUserIdRef.current === userData?.id && lastCallTimeRef.current > 0 && (now - lastCallTimeRef.current) < 10000) {
      console.log(`⏸️ [${callId}] Contacts already loaded for user ${userData.id} recently, skipping...`);
      return;
    }
    
    lastCallTimeRef.current = now;
    
    try {
      console.log(`✅ [${callId}] Starting to load contacts for user ${userData?.id}`);
      loadingContactsRef.current = true;
      lastLoadedUserIdRef.current = userData?.id; // Mark as loading for this user
      
      let endpoint;
      if (userData.roleId === 2 || userData.roleName === 'Doctor') {
        endpoint = `${API_BASE_URL}/mobile/doctor/patients`;
      } else {
        endpoint = `${API_BASE_URL}/mobile/patient/doctors`;
      }
      console.log(`📞 [${callId}] Loading contacts from:`, endpoint);
      console.log(`📞 [${callId}] Using token:`, token ? `${token.substring(0, 20)}...` : 'NO TOKEN');
      
      let resp;
      try {
        resp = await fetch(endpoint, { 
          headers: { 
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
          } 
        });
        console.log(`📞 [${callId}] Contacts response status:`, resp.status, resp.statusText);
      } catch (fetchError) {
        console.error(`📞 [${callId}] Fetch error details:`, {
          message: fetchError.message,
          name: fetchError.name,
          stack: fetchError.stack,
          endpoint: endpoint
        });
        
        // Check if it's an SSL/certificate error
        if (fetchError.message && (
          fetchError.message.includes('Network request failed') ||
          fetchError.message.includes('SSL') ||
          fetchError.message.includes('certificate')
        )) {
          console.error(`🔒 [${callId}] SSL/Certificate error detected!`);
          console.error(`💡 This usually means the certificate isn't trusted. Check:`);
          console.error(`   - iOS: App Transport Security settings`);
          console.error(`   - Android: network_security_config.xml`);
          console.error(`   - Make sure you rebuilt the app after native config changes`);
        }
        
        throw fetchError;
      }
      
      if (resp.ok) {
        const arr = await resp.json();
        const contactCount = Array.isArray(arr) ? arr.length : 0;
        console.log(`📞 [${callId}] Contacts loaded:`, contactCount);
        
        // Only update contacts if we actually got data
        // If we got an empty array, it's likely rate-limited - don't overwrite existing contacts
        if (contactCount > 0) {
          setContacts(arr);
          hasContactsRef.current = true;
          console.log(`✅ [${callId}] Contacts updated with ${contactCount} contacts`);
        } else {
          // Empty array - likely rate-limited
          // If we haven't loaded contacts yet, retry after rate limit period
          if (!hasContactsRef.current) {
            console.log(`⚠️ [${callId}] Got empty contacts on first load (likely rate-limited). Will retry in 6 seconds...`);
            // Reset refs to allow retry after server rate limit (5 seconds) + buffer
            lastLoadedUserIdRef.current = null;
            lastCallTimeRef.current = 0;
            setTimeout(async () => {
              console.log(`🔄 Retrying contacts load after rate limit...`);
              await loadContactsForUser(userData, token);
            }, 6000);
          } else {
            // We already have contacts, keep them (don't overwrite with empty)
            console.log(`⚠️ [${callId}] Got empty contacts (rate-limited). Keeping existing contacts.`);
          }
        }
        
        // Always mark as loaded to prevent infinite retry loops
        console.log(`✅ [${callId}] Request completed, lastLoadedUserIdRef set to:`, lastLoadedUserIdRef.current);
      } else {
        const errorText = await resp.text();
        console.error(`📞 [${callId}] Contacts API error:`, resp.status, errorText);
        setContacts([]);
        // Reset ref on error so it can retry
        lastLoadedUserIdRef.current = null;
        // Show user-friendly error
        if (resp.status === 401) {
          console.warn('⚠️ Authentication failed - token may be invalid');
        } else if (resp.status === 403) {
          console.warn('⚠️ Forbidden - user may not have permission');
        }
      }
    } catch (e) {
      console.error(`📞 [${callId}] Contacts fetch error:`, e);
      setContacts([]);
      // Reset ref on error so it can retry
      lastLoadedUserIdRef.current = null;
    } finally {
      loadingContactsRef.current = false;
      console.log(`🏁 [${callId}] loadContactsForUser finished, loadingContactsRef set to false`);
    }
  }, []); // No dependencies - function is stable

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

  // ---------- CONTACT DETAIL ----------
  const openContactDetail = (contact) => {
    setSelectedContactDetail(contact);
    setCurrentView('contact-detail');
  };

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

      // Use .NET Server for token generation (same as main API)
      const url = `${API_BASE_URL}/realtime/token?channel=${encodeURIComponent(channelName)}&uid=${uid}`;
      console.log(`🎯 Fetching Agora token from .NET server: ${url}`);

      // Get auth token for .NET server authentication
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

  // const startCall = async (targetUser, callType) => {
  //     try {
  //       setCallModal({ visible: true, targetUser, callType, channelName: null });
  //       setRemoteUsers([]);
  //       setIsAudioMuted(false);
  //       setIsVideoMuted(false);

  //       // Notify server (optional)
  //       try {
  //         await initiateCall(targetUser.id, callType);
  //       } catch (e) {
  //         console.warn('Server call notify failed (continuing):', e?.message);
  //       }

  //       if (!agoraInitialized) {
  //         Alert.alert('Error', 'Agora not initialized');
  //         setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
  //         return;
  //       }

  //       // 🟢 define your channel & uid
  //       const channelName = `call_${targetUser.id}_${user?.id}`;
  //       const uid = user?.id || Math.floor(Math.random() * 100000);
  //       const withVideo = callType === 'Video';

  //       // 🟢 get token dynamically from backend (server API on port 5262)
  //         const rtcToken = await fetchAgoraToken(channelName, targetUser.id || 0);
  //       console.log('Target User id:', targetUser.id);
  //       console.log('🎫 Agora Token:', rtcToken);

  //       if (!rtcToken) {
  //         Alert.alert('Error', 'Failed to fetch Agora token.');
  //         setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
  //         return;
  //       }

  //       console.log("📞 Joining Agora:", { channelName, uid: targetUser.id, token: rtcToken });
        
  //       // 🟢 now join the channel
  //       const ok = await AgoraService.joinChannel({
  //         token: rtcToken,
  //         channelName,
  //         uid:targetUser.id || 0,
  //         withVideo,
  //       });

  //       //const ok = await AgoraService.joinChannel({ token: "DEV_702387eed4a52fcad5b0e9b041fff1ea79e1b7852bb769c1d9e4b1985654103", channelName: "test", uid: 0, withVideo });


  //       if (!ok) throw new Error('Join channel failed');

  //       setCallModal((prev) => ({ ...prev, channelName }));
  //     } catch (e) {
  //       console.error('startCall error:', e);
  //       Alert.alert('Error', e.message || 'Failed to start call.');
  //       setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
  //     }
  //   };

  const startCall = async (targetUser, callType) => {
  try {
    setCallModal({ visible: true, targetUser, callType, channelName: null });
    setRemoteUsers([]);
    setIsAudioMuted(false);
    setIsVideoMuted(false);

    try {
      await initiateCall(targetUser.id, callType);
    } catch (e) {
      console.warn('Server call notify failed (continuing):', e?.message);
    }

    if (!AgoraService || !agoraInitialized) {
      Alert.alert(
        'Calls Not Available', 
        'Video/Audio calls require a development build. Agora is not available in Expo Go.\n\nTo enable calls, run:\nnpx expo run:ios\nor\nnpx expo run:android'
      );
      setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
      return;
    }

    // consistent channel name (both sides compute same string)
    const channelName = `call_${[user.id, targetUser.id].sort().join('_')}`;
    const uid = user?.id;
    const withVideo = callType === 'Video';

    // get token for yourself (NOT the target)
    const rtcToken = await fetchAgoraToken(channelName, uid);
    console.log('🎫 Agora Token fetched for UID:', uid);
    console.log('📞 Joining Agora:', { channelName, uid, token: rtcToken });

    if (!rtcToken) {
      Alert.alert('Error', 'Failed to fetch Agora token.');
      setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
      return;
    }

    const ok = await AgoraService.joinChannel({
      token: rtcToken,
      channelName,
      uid,
      withVideo,
    });

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
      if (AgoraService && agoraInitialized) {
        await AgoraService.leaveChannel();
      }
    } catch (e) {
      console.error('leave error:', e);
    }
    setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
    setRemoteUsers([]);
    setIsAudioMuted(false);
    setIsVideoMuted(false);
  };

  const toggleMute = async () => {
    if (!AgoraService || !agoraInitialized) return;
    const next = !isAudioMuted;
    setIsAudioMuted(next);
    await AgoraService.muteLocalAudio(next);
  };

  const toggleVideo = async () => {
    if (!AgoraService || !agoraInitialized) return;
    const next = !isVideoMuted;
    setIsVideoMuted(next);
    await AgoraService.muteLocalVideo(next);
  };

  // ---------- EMERGENCY ----------
  const registerDeviceForEmergency = async () => {
    if (!user || user.roleId !== 1) return; // Only patients need device registration
    if (deviceRegisteredRef.current) return; // Already registered or in progress

    try {
      deviceRegisteredRef.current = true; // Mark as in progress
      const deviceId = `device_${user.id}_${Platform.OS}`;
      const deviceName = `${Platform.OS} Device`;
      
      // Check if we already have a token stored
      const storedToken = await AsyncStorage.getItem('emergencyDeviceToken');
      if (storedToken) {
        setDeviceToken(storedToken);
        return;
      }

      // Register device
      const response = await fetch(`${API_BASE_URL}/emergency/test-register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          patientId: user.id,
          deviceId: deviceId,
          deviceName: deviceName,
          deviceType: 'mobile',
          deviceModel: Platform.OS,
          operatingSystem: Platform.OS
        })
      });

      const data = await response.json();
      
      if (data.success && data.deviceToken) {
        setDeviceToken(data.deviceToken);
        await AsyncStorage.setItem('emergencyDeviceToken', data.deviceToken);
        await AsyncStorage.setItem('emergencyDeviceId', deviceId);
        console.log('✅ Device registered for emergency:', data.deviceToken);
      } else {
        console.warn('⚠️ Device registration failed:', data.message);
      }
    } catch (error) {
      console.error('❌ Error registering device:', error);
      deviceRegisteredRef.current = false; // Reset on error so it can retry
      // Don't show alert - this is background operation
    }
  };

  // Valid emergency types: Fall, Cardiac, PanicAttack, Seizure, Overdose, SelfHarm, Unconscious, Other
  // Valid severity levels: Low, Medium, High, Critical
  const sendEmergency = async (emergencyType = 'Cardiac', severity = 'High', message = 'Emergency assistance needed') => {
    if (!user || user.roleId !== 1) {
      Alert.alert('Error', 'Only patients can send emergency alerts');
      return;
    }

    if (!selectedContactDetail) {
      Alert.alert('Error', 'Please select a doctor contact first');
      return;
    }

    // Make sure device is registered
    if (!deviceToken) {
      Alert.alert('Error', 'Device not registered. Please wait a moment and try again.');
      await registerDeviceForEmergency();
      return;
    }

    // Confirm before sending
    Alert.alert(
      'Send Emergency Alert?',
      `This will send an emergency alert to ${selectedContactDetail.firstName} ${selectedContactDetail.lastName} and log an incident. Continue?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Send Emergency',
          style: 'destructive',
          onPress: async () => {
            setSendingEmergency(true);
            try {
              const storedDeviceId = await AsyncStorage.getItem('emergencyDeviceId') || `device_${user.id}_${Platform.OS}`;

              const response = await fetch(`${API_BASE_URL}/emergency/test-emergency`, {
                method: 'POST',
                headers: {
                  'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                  deviceToken: deviceToken,
                  deviceId: storedDeviceId,
                  emergencyType: emergencyType,
                  severity: severity,
                  message: message,
                  heartRate: Math.floor(Math.random() * 30) + 70, // 70-100
                  bloodPressure: `${120 + Math.floor(Math.random() * 20)}/${80 + Math.floor(Math.random() * 10)}`,
                  temperature: parseFloat((98.6 + (Math.random() - 0.5) * 2).toFixed(1)),
                  oxygenSaturation: Math.floor(Math.random() * 5) + 95, // 95-100
                  latitude: 0, // TODO: Get actual location if available
                  longitude: 0
                })
              });

              const data = await response.json();

              if (data.success) {
                Alert.alert(
                  'Emergency Sent!',
                  `Emergency alert sent successfully to ${selectedContactDetail.firstName} ${selectedContactDetail.lastName}.\n\nIncident ID: ${data.incidentId}\n\nThe incident has been logged and will appear in the emergency dashboard.`,
                  [{ text: 'OK' }]
                );
              } else {
                throw new Error(data.message || data.reason || 'Emergency sending failed');
              }
            } catch (error) {
              console.error('Emergency error:', error);
              let errorMessage = error.message || 'Please check your connection and try again.';
              
              // If device token is invalid, try to re-register
              if (errorMessage.includes('Invalid') || errorMessage.includes('device token')) {
                console.log('🔄 Device token invalid, re-registering...');
                await registerDeviceForEmergency();
                errorMessage = 'Device registration issue. Please try again.';
              }
              
              Alert.alert(
                'Error',
                `Failed to send emergency alert: ${errorMessage}`
              );
            } finally {
              setSendingEmergency(false);
            }
          }
        }
      ]
    );
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
                {remoteUsers.length > 0 &&
                  callModal?.channelName &&
                  (isIOS ? RtcRemoteView?.TextureView : RtcRemoteView?.SurfaceView) ? (
                    isIOS ? (
                      <RtcRemoteView.TextureView
                        uid={remoteUsers[0]}
                        channelId={callModal.channelName}
                        style={{ width: '100%', height: '100%' }}
                      />
                    ) : (
                      <RtcRemoteView.SurfaceView
                        uid={remoteUsers[0]}
                        channelId={callModal.channelName}
                        style={{ width: '100%', height: '100%' }}
                      />
                    )
                  ) : (
                    <>
                      <Text style={styles.videoPlaceholder}>Waiting for remote…</Text>
                      {__DEV__ && (
                        <Text style={{ color: '#888', fontSize: 12 }}>
                          Debug: remoteUsers={remoteUsers.length},
                          channel={callModal?.channelName || 'none'},
                          RtcRemoteView={!!RtcRemoteView}
                        </Text>
                      )}
                    </>
                  )}
              </View>
              {/* Local PiP */}
              {callModal?.channelName &&
                (isIOS ? RtcLocalView?.TextureView : RtcLocalView?.SurfaceView) ? (
                <View style={styles.localVideo}>
                  {isIOS ? (
                    <RtcLocalView.TextureView
                      channelId={callModal.channelName}
                      style={{ width: '100%', height: '100%', borderRadius: 8 }}
                    />
                  ) : (
                    <RtcLocalView.SurfaceView
                      channelId={callModal.channelName}
                      style={{ width: '100%', height: '100%', borderRadius: 8 }}
                    />
                  )}
                </View>
              ) : (
                <>
                  <Text style={styles.videoPlaceholder}>Waiting for local preview…</Text>
                  {__DEV__ && (
                    <Text style={{ color: '#888', fontSize: 12 }}>
                      Debug: channel={callModal?.channelName || 'none'},
                      RtcLocalView={!!RtcLocalView}
                    </Text>
                  )}
                </>
              )}
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
          <TouchableOpacity key={contact.id} style={styles.contactItem} onPress={() => openContactDetail(contact)}>
            <View style={styles.contactInfo}>
              <Text style={styles.contactName}>{contact.firstName} {contact.lastName}</Text>
              <Text style={styles.contactRole}>{contact.specialization || contact.roleName || 'User'}</Text>
              {contact.mobilePhone && <Text style={styles.contactPhone}>{contact.mobilePhone}</Text>}
            </View>
            <View style={styles.contactArrow}>
              <Text style={styles.contactArrowText}>›</Text>
            </View>
          </TouchableOpacity>
        ))}
      </ScrollView>
    </SafeAreaView>
  );

  const renderContactDetail = () => (
    <SafeAreaView style={styles.container}>
      <View style={styles.chatHeader}>
        <TouchableOpacity onPress={() => setCurrentView('main')} style={styles.backButton}>
          <Text style={styles.backButtonText}>← Back</Text>
        </TouchableOpacity>
        <Text style={styles.chatTitle}>Contact Details</Text>
        <View style={styles.chatActions}>
          {/* Empty actions to maintain layout */}
        </View>
      </View>

      {selectedContactDetail && (
        <View style={styles.contactDetailContainer}>
          <View style={styles.contactDetailInfo}>
            <Text style={styles.contactDetailName}>
              {selectedContactDetail.firstName} {selectedContactDetail.lastName}
            </Text>
            <Text style={styles.contactDetailRole}>
              {selectedContactDetail.specialization || selectedContactDetail.roleName || 'User'}
            </Text>
            {selectedContactDetail.mobilePhone && (
              <Text style={styles.contactDetailPhone}>📞 {selectedContactDetail.mobilePhone}</Text>
            )}
            {selectedContactDetail.email && (
              <Text style={styles.contactDetailEmail}>✉️ {selectedContactDetail.email}</Text>
            )}
          </View>

          <View style={styles.contactDetailActions}>
            <TouchableOpacity 
              style={styles.contactDetailButton} 
              onPress={() => openChat(selectedContactDetail)}
            >
              <Text style={styles.contactDetailButtonIcon}>💬</Text>
              <Text style={styles.contactDetailButtonText}>Chat</Text>
            </TouchableOpacity>

            <TouchableOpacity 
              style={styles.contactDetailButton} 
              onPress={() => startCall(selectedContactDetail, 'Audio')}
            >
              <Text style={styles.contactDetailButtonIcon}>📞</Text>
              <Text style={styles.contactDetailButtonText}>Audio Call</Text>
            </TouchableOpacity>

            <TouchableOpacity 
              style={styles.contactDetailButton} 
              onPress={() => startCall(selectedContactDetail, 'Video')}
            >
              <Text style={styles.contactDetailButtonIcon}>📹</Text>
              <Text style={styles.contactDetailButtonText}>Video Call</Text>
            </TouchableOpacity>

            {user?.roleId === 1 && (
              <TouchableOpacity 
                style={styles.contactDetailButton} 
                onPress={() => setSmsModalVisible(true)}
              >
                <Text style={styles.contactDetailButtonIcon}>📱</Text>
                <Text style={styles.contactDetailButtonText}>SMS</Text>
              </TouchableOpacity>
            )}

            {user?.roleId === 1 && (
              <TouchableOpacity 
                style={[styles.contactDetailButton, styles.emergencyButton]} 
                onPress={() => sendEmergency()}
                disabled={sendingEmergency}
              >
                <Text style={styles.contactDetailButtonIcon}>🚨</Text>
                <Text style={styles.contactDetailButtonText}>
                  {sendingEmergency ? 'Sending...' : 'Emergency'}
                </Text>
              </TouchableOpacity>
            )}

            {(user?.roleId === 1 || user?.roleId === 2) && (
              <TouchableOpacity 
                style={styles.contactDetailButton} 
                onPress={() => {
                  setCurrentView('documents');
                  // Set the selected patient for document upload
                  if (user?.roleId === 2) { // Doctor uploading for patient
                    setAvailablePatients([selectedContactDetail]);
                  }
                }}
              >
                <Text style={styles.contactDetailButtonIcon}>📄</Text>
                <Text style={styles.contactDetailButtonText}>Documents</Text>
              </TouchableOpacity>
            )}
          </View>
        </View>
      )}
    </SafeAreaView>
  );

  const renderChat = () => (
    <SafeAreaView style={styles.container}>
      <View style={styles.chatHeader}>
        <TouchableOpacity style={styles.backButton} onPress={() => {
          setSelectedContact(null);
          setCurrentView('contact-detail');
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

  // Render different views based on currentView
  if (currentView === 'documents') {
    return (
      <SafeAreaView style={styles.container}>
        <StatusBar style="auto" />
        <View style={styles.chatHeader}>
          <TouchableOpacity onPress={() => setCurrentView('contact-detail')} style={styles.backButton}>
            <Text style={styles.backButtonText}>← Back</Text>
          </TouchableOpacity>
          <Text style={styles.chatTitle}>Document Management</Text>
          <View style={styles.chatActions}>
            {/* Empty actions to maintain layout */}
          </View>
        </View>
        <DocumentUpload 
          patientId={user.id}
          availablePatients={availablePatients}
          showPatientSelector={user.roleId === 2}
          onPatientSelect={(patient) => console.log('Selected patient:', patient)}
          onDocumentUploaded={() => console.log('Document uploaded')}
        />
      </SafeAreaView>
    );
  }

  if (currentView === 'contact-detail') {
    return (
      <View style={styles.container}>
        <StatusBar style="auto" />
        {renderContactDetail()}
        {renderCallModal()}
        <SmsComponent
          visible={smsModalVisible}
          onClose={() => setSmsModalVisible(false)}
          user={user}
          contacts={contacts}
          apiBaseUrl={API_BASE_URL}
        />
      </View>
    );
  }

  if (currentView === 'chat') {
    return (
      <View style={styles.container}>
        <StatusBar style="auto" />
        {renderChat()}
        {renderCallModal()}
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <StatusBar style="auto" />
      {renderContacts()}
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
  headerActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 15,
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
  documentsButton: {
    backgroundColor: '#28a745',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 5,
    marginRight: 8,
  },
  documentsButtonText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: 'bold',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#e9ecef',
  },
  backButton: {
    marginRight: 16,
  },
  backButtonText: {
    fontSize: 16,
    color: '#007bff',
    fontWeight: '600',
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#2c3e50',
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
  // Contact Detail styles
  contactDetailContainer: {
    flex: 1,
    padding: 20,
  },
  contactDetailInfo: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 20,
    marginBottom: 20,
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  contactDetailName: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 8,
  },
  contactDetailRole: {
    fontSize: 16,
    color: '#666',
    marginBottom: 12,
  },
  contactDetailPhone: {
    fontSize: 16,
    color: '#007bff',
    marginBottom: 8,
  },
  contactDetailEmail: {
    fontSize: 16,
    color: '#007bff',
  },
  contactDetailActions: {
    flex: 1,
    gap: 15,
  },
  contactDetailButton: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 20,
    flexDirection: 'row',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  contactDetailButtonIcon: {
    fontSize: 24,
    marginRight: 15,
  },
  contactDetailButtonText: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
  },
  emergencyButton: {
    backgroundColor: '#ffebee',
    borderWidth: 2,
    borderColor: '#dc3545',
  },
  contactArrow: {
    justifyContent: 'center',
    alignItems: 'center',
    width: 30,
  },
  contactArrowText: {
    fontSize: 20,
    color: '#ccc',
  },
});
