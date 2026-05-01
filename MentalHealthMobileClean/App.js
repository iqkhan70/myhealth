// App.js
import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import {
  StyleSheet, Text, View, TextInput, TouchableOpacity, Alert, ScrollView,
  Platform, KeyboardAvoidingView, Modal, Linking
} from 'react-native';

import { SafeAreaView } from 'react-native-safe-area-context';

import AsyncStorage from '@react-native-async-storage/async-storage';
import { StatusBar } from 'expo-status-bar';
import { Audio } from 'expo-av';
import { LinearGradient } from 'expo-linear-gradient';
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
import ServiceRequestList from './src/components/ServiceRequestList';
import CreateServiceRequestForm from './src/components/CreateServiceRequestForm';
import SmsComponent from './src/components/SmsComponent';
import EmergencyComponent from './src/components/EmergencyComponent';
import GuestRegistrationForm from './src/components/GuestRegistrationForm';
import ChangePassword from './src/components/ChangePassword';

// Import app configuration
import AppConfig from './src/config/app.config';

// detect platform once
const isIOS = Platform.OS === 'ios';

// ---------- ENV / URLS ----------
const getApiBaseUrl = () => {
  try {
    const isWeb = Platform.OS === 'web' && typeof window !== 'undefined';
    // For web/localhost development, use HTTPS (server runs on HTTPS)
    if (isWeb) {
      const url = AppConfig.getWebApiBaseUrl();
      if (!url || typeof url !== 'string' || url.trim() === '') {
        throw new Error('Web API URL is invalid');
      }
      console.log('🌐 Using Web API URL:', url);
      return url.trim();
    }
    // For mobile: use HTTPS with configured server IP
    const url = AppConfig.getMobileApiBaseUrl();
    if (!url || typeof url !== 'string' || url.trim() === '') {
      throw new Error('Mobile API URL is invalid');
    }
    console.log('📱 Using Mobile API URL:', url);
    console.log('📱 Config:', {
      SERVER_IP: AppConfig.SERVER_IP,
      SERVER_PORT: AppConfig.SERVER_PORT,
      USE_HTTPS: AppConfig.USE_HTTPS
    });
    return url.trim();
  } catch (error) {
    console.error('❌ Error getting API base URL:', error);
    // Fallback to staging server
    const fallbackUrl = 'https://caseflowstage.store/api';
    console.warn('⚠️ Using fallback URL:', fallbackUrl);
    return fallbackUrl;
  }
};

const API_BASE_URL = getApiBaseUrl();

// Ensure SIGNALR_HUB_URL is always valid
const getSignalRHubUrl = () => {
  if (!API_BASE_URL || typeof API_BASE_URL !== 'string') {
    console.error('❌ API_BASE_URL is invalid, using fallback');
    return 'https://caseflowstage.store/mobilehub';
  }
  try {
    const hubUrl = API_BASE_URL.replace('/api', '/mobilehub');
    if (!hubUrl || hubUrl.trim() === '') {
      throw new Error('SignalR Hub URL is invalid');
    }
    return hubUrl.trim();
  } catch (error) {
    console.error('❌ Error constructing SignalR Hub URL:', error);
    return 'https://caseflowstage.store/mobilehub';
  }
};

const SIGNALR_HUB_URL = getSignalRHubUrl();

// Validate URLs are valid
if (!API_BASE_URL || !SIGNALR_HUB_URL) {
  console.error('❌ CRITICAL: URLs are null or undefined!');
  console.error('API_BASE_URL:', API_BASE_URL);
  console.error('SIGNALR_HUB_URL:', SIGNALR_HUB_URL);
}

// Helper function to ensure URL is valid before use
const ensureValidUrl = (url, fallback = API_BASE_URL) => {
  if (!url || typeof url !== 'string' || url.trim() === '') {
    console.warn('⚠️ Invalid URL provided, using fallback:', url);
    return fallback || 'https://caseflowstage.store/api';
  }
  const trimmedUrl = url.trim();
  // Basic URL validation
  if (!trimmedUrl.startsWith('http://') && !trimmedUrl.startsWith('https://')) {
    console.warn('⚠️ URL does not start with http:// or https://:', trimmedUrl);
    return fallback || 'https://caseflowstage.store/api';
  }
  return trimmedUrl;
};

console.log('✅ API Base URL initialized:', API_BASE_URL);
console.log('✅ SignalR Hub URL:', SIGNALR_HUB_URL);

// 👉 Set your Agora App ID here (or pull from .env)
const AGORA_APP_ID = 'b480142a879c4ed2ab7efb07d318abda';
const EATS_ORANGE = '#f97316';
const EATS_YELLOW = '#eab308';
const EATS_BG = '#f5f5f5';
const EATS_TEXT = '#333';
const EATS_MUTED = '#666';
const EATS_GRADIENT = [EATS_ORANGE, EATS_YELLOW];

const GradientHeader = ({ children, style }) => (
  <LinearGradient
    colors={EATS_GRADIENT}
    start={{ x: 0, y: 0 }}
    end={{ x: 1, y: 0 }}
    style={style}
  >
    {children}
  </LinearGradient>
);

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
  const [contactSearchQuery, setContactSearchQuery] = useState('');

  const [signalRConnected, setSignalRConnected] = useState(false);

  // 🔔 Call modal state
  const [callModal, setCallModal] = useState({ visible: false, targetUser: null, callType: null, channelName: null });
  const [remoteUsers, setRemoteUsers] = useState([]);
  const remoteUserJoinTimes = useRef({}); // Track when each remote user joined (timestamp)
  const [isAudioMuted, setIsAudioMuted] = useState(false);
  const [isVideoMuted, setIsVideoMuted] = useState(false);
  const [agoraInitialized, setAgoraInitialized] = useState(false);
  
  // 📞 Incoming call state
  const [incomingCall, setIncomingCall] = useState(null);
  const ringtoneSoundRef = useRef(null);
  const ringtoneIntervalRef = useRef(null);
  const incomingCallTimeoutRef = useRef(null);

  // 📄 Document upload state
  const [currentView, setCurrentView] = useState('login'); // 'login', 'main', 'documents', 'chat', 'contact-detail', 'guest-registration', 'change-password', 'service-requests', 'service-request-detail', 'create-service-request', 'forgot-password', 'reset-password', 'ai-chat'
  const [availablePatients, setAvailablePatients] = useState([]);
  const [selectedContactDetail, setSelectedContactDetail] = useState(null);
  
  // 🔐 Password reset state
  const [forgotPasswordEmail, setForgotPasswordEmail] = useState('');
  const [resetPasswordEmail, setResetPasswordEmail] = useState('');
  
  // 🤖 AI Chat state (for clients/patients only)
  const [aiChatMode, setAiChatMode] = useState('ServiceRequest'); // 'ServiceRequest', 'Generic', 'Medical'
  const [aiChatMessages, setAiChatMessages] = useState([]);
  const [aiChatInput, setAiChatInput] = useState('');
  const [aiChatLoading, setAiChatLoading] = useState(false);
  const [hasActiveServiceRequests, setHasActiveServiceRequests] = useState(false);
  const [activeServiceRequests, setActiveServiceRequests] = useState([]);
  const [selectedServiceRequestId, setSelectedServiceRequestId] = useState(null);
  const [resetPasswordToken, setResetPasswordToken] = useState('');
  const [resetPasswordFromUrl, setResetPasswordFromUrl] = useState(false);
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [forgotPasswordLoading, setForgotPasswordLoading] = useState(false);
  const [resetPasswordLoading, setResetPasswordLoading] = useState(false);
  const [forgotPasswordMessage, setForgotPasswordMessage] = useState('');
  const [resetPasswordMessage, setResetPasswordMessage] = useState('');
  const [selectedServiceRequest, setSelectedServiceRequest] = useState(null);

  // 📱 SMS state
  const [smsModalVisible, setSmsModalVisible] = useState(false);

  // 🚨 Emergency state
  const [emergencyModalVisible, setEmergencyModalVisible] = useState(false);
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

  // Keep userRef in sync with user state and re-register SignalR listener
  useEffect(() => {
    const prevUserId = userRef.current?.id;
    userRef.current = user;
    const newUserId = userRef.current?.id;
    console.log('📱 App: ========== USER REF UPDATED ==========');
    console.log('📱 App: userRef updated - prev:', prevUserId, 'new:', newUserId);
    console.log('📱 App: user state:', user);
    console.log('📱 App: userRef.current:', userRef.current);
    console.log('📱 App: signalRConnected:', signalRConnected);
    
    // Re-register SignalR message listener when user changes to ensure it has current user
    if (signalRConnected && user?.id) {
      console.log('📱 App: Re-registering SignalR message listener with new user:', user.id);
      setupSignalRMessageListener();
    } else if (signalRConnected && !user?.id) {
      console.warn('⚠️ App: SignalR connected but no user - listener may not work correctly');
    }
  }, [user, signalRConnected]);

  // Load service requests when AI chat view is opened in Service Request mode
  useEffect(() => {
    const loadServiceRequestsIfNeeded = async () => {
      // Only load if:
      // 1. We're in the AI chat view
      // 2. We're in Service Request mode
      // 3. User is a patient (roleId === 1)
      // 4. User is logged in
      if (currentView === 'ai-chat' && 
          aiChatMode === 'ServiceRequest' && 
          user && 
          user.roleId === 1 && 
          user.id) {
        try {
          const token = await AsyncStorage.getItem('userToken');
          if (token) {
            console.log('Loading service requests on AI chat view open (Service Request mode)');
            await checkActiveServiceRequestsForAiChat(user.id, token);
          }
        } catch (error) {
          console.error('Error loading service requests on AI chat view open:', error);
        }
      }
    };

    loadServiceRequestsIfNeeded();
  }, [currentView, aiChatMode, user]);

  // Auto-scroll AI chat when loading indicator appears
  useEffect(() => {
    if (aiChatLoading && aiChatScrollViewRef.current) {
      // Small delay to ensure the loading indicator is rendered
      setTimeout(() => {
        aiChatScrollViewRef.current?.scrollToEnd({ animated: true });
      }, 150);
    }
  }, [aiChatLoading]);

  useEffect(() => {
    if (user && user.id && !userInitializedRef.current) {
      userInitializedRef.current = true;
      initializeServices();
      registerDeviceForEmergency();
    }
  }, [user?.id]); // Only run when user ID changes, not on every user object change

  // Handle deep links for password reset
  useEffect(() => {
    // Handle initial URL (when app is opened from a link)
    Linking.getInitialURL().then((url) => {
      if (url) {
        handleDeepLink(url);
      }
    }).catch(err => console.error('Error getting initial URL:', err));

    // Handle URL when app is already running
    const subscription = Linking.addEventListener('url', (event) => {
      handleDeepLink(event.url);
    });

    return () => {
      subscription.remove();
    };
  }, []);

  const handleDeepLink = (url) => {
    try {
      console.log('📱 Deep link received:', url);
      
      // Skip Expo development URLs and other non-HTTP URLs
      if (!url || url.startsWith('exp://') || url.startsWith('exps://')) {
        console.log('📱 Skipping Expo development URL');
        return;
      }
      
      // Parse URL - format: https://caseflowstage.store/reset-password?token=XXX&email=YYY
      // or mentalhealthapp://reset-password?token=XXX&email=YYY
      // or https://192.168.86.34:5283/reset-password?token=XXX&email=YYY
      let urlObj;
      try {
        urlObj = new URL(url);
      } catch (e) {
        // Try parsing as custom scheme
        if (url.startsWith('mentalhealthapp://')) {
          const cleanUrl = url.replace('mentalhealthapp://', 'https://');
          urlObj = new URL(cleanUrl);
        } else {
          console.log('📱 Invalid URL format, skipping:', url);
          return;
        }
      }
      
      if (urlObj.pathname && urlObj.pathname.includes('reset-password')) {
        const token = urlObj.searchParams.get('token');
        const email = urlObj.searchParams.get('email');
        
        if (token && email) {
          console.log('📱 Password reset deep link detected');
          setResetPasswordToken(token);
          setResetPasswordEmail(decodeURIComponent(email));
          setResetPasswordFromUrl(true); // Mark as loaded from URL - make fields read-only
          setCurrentView('reset-password');
          currentViewRef.current = 'reset-password';
        } else {
          console.log('📱 Reset password URL detected but missing token or email');
        }
      } else if (urlObj.pathname && (urlObj.pathname.includes('login') || urlObj.pathname === '/')) {
        // Handle login deep link (e.g., mentalhealthapp://login)
        console.log('📱 Login deep link detected - navigating to login screen');
        setCurrentView('login');
        currentViewRef.current = 'login';
        // Clear any reset password state
        setResetPasswordToken('');
        setResetPasswordEmail('');
        setResetPasswordFromUrl(false);
      }
    } catch (error) {
      console.error('Error handling deep link:', error);
    }
  };

  const checkAuthStatus = async () => {
    try {
      const token = await AsyncStorage.getItem('userToken');
      const userData = await AsyncStorage.getItem('currentUser');
      if (token && userData) {
        const u = JSON.parse(userData);
        setUser(u);
        userRef.current = u; // Update ref when restoring user from storage
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
            console.log('📱 App: onUserJoined event received, UID:', uid);
            const joinTime = Date.now();
            remoteUserJoinTimes.current[uid] = joinTime;
            console.log('📱 App: Recorded join time for UID:', uid, 'at', joinTime);
            setRemoteUsers((prev) => {
              if (prev.includes(uid)) {
                console.log('📱 App: UID already in remoteUsers, skipping');
                return prev;
              }
              console.log('📱 App: Adding UID to remoteUsers:', uid, 'new array:', [...prev, uid]);
              return [...prev, uid];
            });
          });
          AgoraService.setListener('onUserLeft', (uid) => {
            console.log('📱 App: onUserLeft event received, UID:', uid);
            const joinTime = remoteUserJoinTimes.current[uid];
            const timeInCall = joinTime ? Date.now() - joinTime : 0;
            
            setRemoteUsers((prev) => {
              if (!prev.includes(uid)) {
                console.log('📱 App: UID not in remoteUsers, ignoring onUserLeft');
                return prev;
              }
              
              // Only remove if user has been in call for at least 2 seconds
              // This prevents false positives from race conditions where onUserOffline
              // fires immediately after onUserJoined
              if (timeInCall < 2000) {
                console.log(`⚠️ App: Ignoring onUserLeft for UID ${uid} - only been in call for ${timeInCall}ms (likely false positive)`);
                return prev;
              }
              
              console.log(`📱 App: Removing UID ${uid} from remoteUsers (was in call for ${timeInCall}ms)`);
              delete remoteUserJoinTimes.current[uid];
              return prev.filter((id) => id !== uid);
            });
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
      const loginUrl = ensureValidUrl(`${API_BASE_URL}/auth/login`);
      console.log('🔐 Attempting login to:', loginUrl);
      const resp = await fetch(loginUrl, {
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
        userRef.current = data.user; // Update ref when user is set
        
        // Check if user must change password on first login
        if (data.user.mustChangePassword) {
          setCurrentView('change-password');
          currentViewRef.current = 'change-password';
          Alert.alert('Password Change Required', 'Please change your password to continue.');
          return;
        }
        
        // Load contacts after a small delay to ensure state is set
        // and to avoid race conditions with useEffect
        setTimeout(async () => {
          if (!loadingContactsRef.current && lastLoadedUserIdRef.current !== data.user.id) {
            await loadContactsForUser(data.user, data.token);
          }
        }, 300); // Slightly longer delay to ensure everything is ready
        
        await loadAvailablePatients(data.user, data.token);
        await initializeSignalR(data.token);
        
        // Check for active service requests if user is a patient
        if (data.user.roleId === 1) {
          await checkActiveServiceRequestsForAiChat(data.user.id, data.token);
        }

        setCurrentView('main');
        currentViewRef.current = 'main';
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
      currentViewRef.current = 'login';
      setAvailablePatients([]);
      setAgoraInitialized(false);
      setRemoteUsers([]);
    } catch (e) {
      console.error('Logout error:', e);
    }
  };

  // Function to set up SignalR message listener
  const setupSignalRMessageListener = () => {
    console.log('📱 App: Setting up SignalR message listener. Current userRef:', userRef.current?.id);
    SignalRService.setEventListener('onMessageReceived', (message) => {
          console.log('📱 App: ========== MESSAGE RECEIVED ==========');
          console.log('📱 App: onMessageReceived callback called with:', message);
          console.log('📱 App: userRef.current at message time:', userRef.current);
          console.log('📱 App: userRef.current?.id:', userRef.current?.id);
          const messageText = message.message || message.text || '';
          console.log('📱 App: Extracted message text:', messageText);
          
          if (!messageText) {
            console.warn('⚠️ App: Received message with no text content:', message);
            return;
          }
          
          // First check: Message must be for the current user
          // Use ref to get current user (avoids closure issues)
          // Also try to access user state directly as fallback
          let currentUserId = userRef.current?.id;
          if (!currentUserId) {
            // Fallback: try to get from state (this might work if state is accessible)
            console.log('📱 App: userRef.current is null, trying to access user state...');
            // Since we can't access state directly in closure, we'll use a workaround:
            // Re-check the ref after a brief moment, or use the message to infer user
            // Actually, let's just log and see what we have
            console.log('📱 App: userRef.current:', userRef.current);
            console.log('📱 App: Message details - targetUserId:', message.targetUserId, 'senderId:', message.senderId);
            
            // If we can't get user ID, we can't verify if message is for us
            // But we can still try to process it if we're in chat view
            // This is a workaround - ideally userRef should be set
            console.warn('⚠️ App: Cannot verify user - userRef is null. Message may be ignored.');
            return;
          }
          
          console.log('📱 App: ✅ Current user ID from ref:', currentUserId);
          console.log('📱 App: Message - targetUserId:', message.targetUserId, 'senderId:', message.senderId);
          
          const isForCurrentUser = (message.targetUserId === currentUserId) || (message.senderId === currentUserId);
          if (!isForCurrentUser) {
            console.log('📱 App: Message not for current user (currentUser:', currentUserId, 'message sender:', message.senderId, 'target:', message.targetUserId, ')');
            return;
          }
          
          // Second check: If we're in chat view, message must be for the selected contact
          // Check both refs AND state to avoid race conditions
          const currentViewState = currentViewRef.current;
          const currentContact = selectedContactRef.current;
          
          console.log('📱 App: Checking view state - ref:', currentViewState, 'state:', currentView, 'contact ref:', currentContact?.id, 'contact state:', selectedContact?.id);
          
          // Use ref first, fallback to state if ref is not set yet
          const isInChatView = currentViewState === 'chat' || currentView === 'chat';
          
          if (isInChatView) {
            const contact = currentContact || selectedContact;
            if (contact) {
              // In chat view with a selected contact - only show messages for that contact
              const isForCurrentChat = (message.senderId === contact.id && message.targetUserId === currentUserId) ||
                                      (message.targetUserId === contact.id && message.senderId === currentUserId);
              
              console.log('📱 App: Checking if message is for current chat:', {
                isForCurrentChat,
                senderId: message.senderId,
                targetUserId: message.targetUserId,
                contactId: contact.id,
                currentUserId: currentUserId
              });
              
              if (!isForCurrentChat) {
                console.log('📱 App: Message not for current chat (selectedContact:', contact.id, 'message sender:', message.senderId, 'target:', message.targetUserId, ')');
                return;
              }
            } else {
              // In chat view but no contact selected yet - might be loading, allow message if it's for current user
              console.log('📱 App: In chat view but no contact selected yet, allowing message for current user');
            }
          } else {
            // Not in chat view - don't add message to UI (will be loaded when chat opens)
            console.log('📱 App: Not in chat view (ref:', currentViewState, 'state:', currentView, '), ignoring message (will load from DB when chat opens)');
            return;
          }
          
          // Message is valid - add it to messages (ONLY ONCE - removed duplicate)
          setMessages((prev) => {
            // Check if message already exists to prevent duplicates
            // Check by ID first (most reliable)
            if (message.id) {
              const existingById = prev.find(m => m.id?.toString() === message.id?.toString());
              if (existingById) {
                console.log('📱 App: Duplicate message detected by ID, skipping:', message.id);
                return prev;
              }
            }
            
            // Also check by text + sender + timestamp (for cases where ID might differ)
            const messageTimestamp = message.timestamp ? new Date(message.timestamp).getTime() : Date.now();
            const existingByContent = prev.find(m => {
              const mText = m.text?.trim();
              const msgText = messageText.trim();
              const mTime = m.timestamp?.getTime() || 0;
              const timeDiff = Math.abs(mTime - messageTimestamp);
              
              // Same text, same sender, within 5 seconds
              return mText === msgText && 
                     m.senderId === message.senderId && 
                     timeDiff < 5000;
            });
            
            if (existingByContent) {
              // If we found a match by content but IDs differ, update the existing message's ID
              // This handles the case where optimistic message gets replaced with real ID
              if (message.id && existingByContent.id !== message.id) {
                console.log('📱 App: Updating existing message ID from', existingByContent.id, 'to', message.id);
                const updated = prev.map(m => 
                  m === existingByContent 
                    ? { ...m, id: message.id.toString() }
                    : m
                );
                return updated;
              }
              console.log('📱 App: Duplicate message detected by content, skipping:', message.id || messageText.substring(0, 30));
              return prev;
            }
            
            const newMessage = {
              id: message.id?.toString() || `${Date.now()}_${message.senderId}`,
              text: messageText,
              isMe: message.senderId === currentUserId,
              timestamp: message.timestamp ? new Date(message.timestamp) : new Date(),
              senderId: message.senderId,
              senderName: message.senderName || 'Unknown'
            };
            
            console.log('📱 App: ✅ Adding new message to chat:', {
              id: newMessage.id,
              text: newMessage.text.substring(0, 50),
              isMe: newMessage.isMe,
              senderId: newMessage.senderId,
              senderName: newMessage.senderName
            });
            
            const newMessages = [...prev, newMessage];
            
            // Auto-scroll to bottom when new message arrives
            setTimeout(() => {
              messagesScrollViewRef.current?.scrollToEnd({ animated: true });
            }, 100);
            
            return newMessages;
          });
        });
  };

  // Polling fallback when SignalR is not connected
  const pollingIntervalRef = useRef(null);
  const lastMessageTimestampRef = useRef(0);
  
  const startPolling = useCallback(() => {
    if (pollingIntervalRef.current) {
      return; // Already polling
    }
    
    console.log('📡 App: Starting polling fallback for messages (SignalR not connected)');
    
    const pollForMessages = async () => {
      try {
        if (!user?.id || currentView !== 'chat' || !selectedContact) {
          return;
        }
        
        const token = await AsyncStorage.getItem('userToken');
        if (!token) return;
        
        // Poll for new messages - get all messages and filter for new ones
        const resp = await fetch(`${API_BASE_URL}/mobile/messages/${selectedContact.id}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        
        if (resp.ok) {
          const arr = await resp.json();
          if (Array.isArray(arr)) {
            // Check if we have new messages by comparing timestamps
            setMessages((prev) => {
              const existingIds = new Set(prev.map(m => m.id?.toString()));
              const newMessages = arr
                .filter(m => {
                  const messageId = m.id?.toString();
                  const messageTime = new Date(m.sentAt).getTime();
                  // Only add if it's a new message (not in existing set) and is newer than last poll
                  return !existingIds.has(messageId) && messageTime > lastMessageTimestampRef.current;
                })
                .map(m => {
                  const messageTime = new Date(m.sentAt).getTime();
                  if (messageTime > lastMessageTimestampRef.current) {
                    lastMessageTimestampRef.current = messageTime;
                  }
                  return {
                    id: m.id?.toString() || `${Date.now()}_${m.senderId}`,
                    text: m.message,
                    isMe: m.senderId === user.id,
                    timestamp: new Date(m.sentAt),
                    senderId: m.senderId,
                    senderName: m.senderName || 'Unknown'
                  };
                });
              
              if (newMessages.length > 0) {
                console.log('📡 App: Polling found', newMessages.length, 'new messages');
                const updated = [...prev, ...newMessages];
                // Auto-scroll to bottom
                setTimeout(() => {
                  messagesScrollViewRef.current?.scrollToEnd({ animated: true });
                }, 100);
                return updated;
              }
              return prev;
            });
          }
        }
      } catch (e) {
        console.error('📡 App: Polling error:', e);
      }
    };
    
    // Poll immediately, then every 2 seconds
    pollForMessages();
    pollingIntervalRef.current = setInterval(pollForMessages, 2000);
  }, [user, currentView, selectedContact]);
  
  const stopPolling = useCallback(() => {
    if (pollingIntervalRef.current) {
      clearInterval(pollingIntervalRef.current);
      pollingIntervalRef.current = null;
      console.log('📡 App: Stopped polling');
    }
  }, []);
  
  // Start/stop polling based on SignalR connection and chat view
  useEffect(() => {
    if (!signalRConnected && currentView === 'chat' && selectedContact && user) {
      console.log('📡 App: SignalR not connected, starting polling fallback');
      startPolling();
    } else {
      stopPolling();
    }
    
    return () => {
      stopPolling();
    };
  }, [signalRConnected, currentView, selectedContact, user, startPolling, stopPolling]);

  // ---------- SIGNALR ----------
  const initializeSignalR = async (token, retryCount = 0) => {
    const maxRetries = 3;
    try {
      console.log('🔌 App: ========== INITIALIZING SIGNALR ==========');
      console.log('🔌 App: SignalR Hub URL:', SIGNALR_HUB_URL);
      console.log('🔌 App: Token available:', !!token);
      console.log('🔌 App: Retry attempt:', retryCount);
      
      // Disconnect any existing connection first
      try {
        await SignalRService.disconnect();
      } catch (e) {
        // Ignore disconnect errors
      }
      
      const connected = await SignalRService.initialize(SIGNALR_HUB_URL, token);
      console.log('🔌 App: SignalR initialization result:', connected);
      
      if (connected) {
        setSignalRConnected(true);
        console.log('✅ App: SignalR connected successfully!');
        console.log('✅ App: SignalR connection state:', SignalRService.getConnectionState());
        console.log('✅ App: SignalR isConnected:', SignalRService.isConnected);
        console.log('✅ App: Setting signalRConnected state to true');
        
        // Log connection state periodically to verify it stays connected
        const connectionCheckInterval = setInterval(() => {
          const state = SignalRService.getConnectionState();
          const isConnected = SignalRService.isConnected;
          console.log('🔌 App: SignalR connection state check:', state, 'isConnected:', isConnected);
          if (!isConnected || state !== 'Connected') {
            console.warn('⚠️ App: SignalR connection lost! State:', state, 'isConnected:', isConnected);
            setSignalRConnected(false);
            // Try to reconnect
            if (user && token) {
              console.log('🔄 App: Attempting to reconnect SignalR...');
              setTimeout(() => initializeSignalR(token, 0), 5000);
            }
          }
        }, 30000); // Every 30 seconds
        
        // Store interval ID to clear it later if needed
        SignalRService.connectionCheckInterval = connectionCheckInterval;

        // Set up message listener AFTER connection is confirmed
        console.log('📱 App: Setting up SignalR message listener...');
        setupSignalRMessageListener();
        console.log('✅ App: SignalR message listener set up');

        // Handle incoming calls
        console.log('📞 App: Setting up onIncomingCall listener...');
        SignalRService.setEventListener('onIncomingCall', (callData) => {
          console.log('📞 App: ========== INCOMING CALL EVENT RECEIVED ==========');
          console.log('📞 App: Incoming call received:', JSON.stringify(callData, null, 2));
          console.log('📞 App: Call data type:', typeof callData);
          console.log('📞 App: Call data keys:', callData ? Object.keys(callData) : 'null');
          handleIncomingCall(callData);
        });
        console.log('✅ App: onIncomingCall listener registered');
        
        // Handle call ended (close incoming call modal if open)
        SignalRService.setEventListener('onCallEnded', (callIdOrChannel) => {
          console.log('📞 App: Call ended:', callIdOrChannel);
          setIncomingCall((current) => {
            if (current && (current.callId === callIdOrChannel || current.channelName === callIdOrChannel)) {
              stopRingtone();
              return null;
            }
            return current;
          });
        });
      } else {
        console.error('❌ App: SignalR failed to connect!');
        console.error('❌ App: Connection result was false');
        setSignalRConnected(false);
        
        // Retry connection
        if (retryCount < maxRetries) {
          console.log(`🔄 App: Retrying SignalR connection (attempt ${retryCount + 1}/${maxRetries})...`);
          setTimeout(() => {
            initializeSignalR(token, retryCount + 1);
          }, 3000 * (retryCount + 1)); // Exponential backoff
        } else {
          console.error('❌ App: SignalR connection failed after', maxRetries, 'attempts. Using polling fallback.');
        }
      }
    } catch (e) {
      console.error('❌ App: SignalR init error:', e);
      console.error('❌ App: Error details:', JSON.stringify(e, null, 2));
      setSignalRConnected(false);
      
      // Retry on error
      if (retryCount < maxRetries) {
        console.log(`🔄 App: Retrying SignalR connection after error (attempt ${retryCount + 1}/${maxRetries})...`);
        setTimeout(() => {
          initializeSignalR(token, retryCount + 1);
        }, 3000 * (retryCount + 1));
      } else {
        console.error('❌ App: SignalR connection failed after', maxRetries, 'attempts. Using polling fallback.');
      }
    }
  };

  // ---------- CONTACTS ----------
  const loadingContactsRef = useRef(false);
  const lastCallTimeRef = useRef(0);
  const hasContactsRef = useRef(false);
  const messagesScrollViewRef = useRef(null);
  const selectedContactRef = useRef(null);
  const currentViewRef = useRef('login');
  const userRef = useRef(null); // Ref to track current user for SignalR callbacks
  const aiChatInputRef = useRef(null); // Ref for AI chat TextInput
  const aiChatScrollViewRef = useRef(null); // Ref for AI chat ScrollView
  
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
      // Coordinators (3), Admins (4), Doctors (2), Attorneys (5), and SMEs (6) use doctor/patients endpoint
      if (userData.roleId === 2 || userData.roleId === 3 || userData.roleId === 4 || userData.roleId === 5 || userData.roleId === 6 || 
          userData.roleName === 'Doctor' || userData.roleName === 'Coordinator' || userData.roleName === 'Admin' || 
          userData.roleName === 'Attorney' || userData.roleName === 'SME') {
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
    console.log('📱 App: Opening chat with contact:', contact.id);
    // Update refs FIRST to ensure they're available immediately
    selectedContactRef.current = contact;
    currentViewRef.current = 'chat';
    // Then update state
    setSelectedContact(contact);
    setCurrentView('chat');
    loadChatHistory(contact.id);
  };

  // ---------- CONTACT DETAIL ----------
  const openContactDetail = (contact) => {
    setSelectedContactDetail(contact);
    setCurrentView('contact-detail');
    currentViewRef.current = 'contact-detail';
  };

  // Check for active service requests for AI chat mode availability
  const checkActiveServiceRequestsForAiChat = async (userId, token) => {
    try {
      console.log('Checking active service requests for user:', userId);
      // Use correct endpoint: /api/ServiceRequest (PascalCase, not lowercase plural)
      const resp = await fetch(`${API_BASE_URL}/ServiceRequest?clientId=${userId}`, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });
      
      if (!resp.ok) {
        console.error('Failed to fetch service requests:', resp.status, resp.statusText);
        const errorText = await resp.text();
        console.error('Error response:', errorText);
        setHasActiveServiceRequests(false);
        setActiveServiceRequests([]);
        return;
      }
      
      const serviceRequests = await resp.json();
      console.log('Fetched service requests:', serviceRequests.length, serviceRequests);
      
      // Filter for active service requests
      // Backend already filters by IsActive, so we just need to filter by Status
      // Active statuses: Active, OnHold (Completed and Cancelled are not active)
      const active = serviceRequests.filter(sr => {
        const status = (sr.status || sr.Status || '').toString();
        const statusLower = status.toLowerCase().trim();
        
        // Active statuses: Active, OnHold (case-insensitive)
        // Note: "Pending" is not a valid ServiceRequest status - only Active, Completed, Cancelled, OnHold exist
        const isActiveStatus = statusLower === 'active' || statusLower === 'onhold' || statusLower === 'on hold';
        
        console.log(`SR ${sr.id || sr.Id}: status="${status}" (${statusLower}) -> isActiveStatus=${isActiveStatus}`);
        return isActiveStatus;
      });
      
      console.log('Active service requests:', active.length, active);
      setActiveServiceRequests(active);
      const hasActive = active.length > 0;
      setHasActiveServiceRequests(hasActive);
      console.log('hasActiveServiceRequests set to:', hasActive);
      
      // Auto-select if only one active SR
      if (hasActive && active.length === 1 && !selectedServiceRequestId) {
        const srId = active[0].id || active[0].Id;
        if (srId) {
          console.log('Auto-selecting single SR:', srId);
          setSelectedServiceRequestId(srId);
          await setActiveServiceRequestOnBackend(srId);
        }
      }
      
      // Update welcome message after loading service requests
      // Use setTimeout to ensure state updates have been applied
      setTimeout(() => {
        updateAiChatWelcomeMessage();
      }, 100);
    } catch (error) {
      console.error('Error checking service requests for AI chat:', error);
      setHasActiveServiceRequests(false);
      setActiveServiceRequests([]);
    }
  };
  
  // Set active SR on backend
  const setActiveServiceRequestOnBackend = async (serviceRequestId) => {
    try {
      const token = await AsyncStorage.getItem('userToken');
      if (!token || !user) return;
      
      const resp = await fetch(`${API_BASE_URL}/agenticai/set-active-sr`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          clientId: user.id,
          serviceRequestId: serviceRequestId
        })
      });
      
      if (!resp.ok) {
        console.error('Failed to set active SR on backend:', resp.status);
      }
    } catch (error) {
      console.error('Error setting active SR on backend:', error);
    }
  };
  
  // Clear active SR on backend
  const clearActiveServiceRequestOnBackend = async () => {
    try {
      const token = await AsyncStorage.getItem('userToken');
      if (!token) return;
      
      const resp = await fetch(`${API_BASE_URL}/agenticai/clear-active-sr`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });
      
      if (!resp.ok) {
        console.error('Failed to clear active SR on backend:', resp.status);
      }
    } catch (error) {
      console.error('Error clearing active SR on backend:', error);
    }
  };

  // Update AI chat welcome message based on mode
  const updateAiChatWelcomeMessage = () => {
    let welcomeMessage = '';
    
    if (aiChatMode === 'ServiceRequest') {
      if (selectedServiceRequestId) {
        const selected = activeServiceRequests.find(sr => (sr.id || sr.Id) === selectedServiceRequestId);
        if (selected) {
          welcomeMessage = `Hello! I'm here to help with your service request: "${selected.title || selected.Title}" (Status: ${selected.status || selected.Status}). What would you like to know or discuss about this request?`;
        } else {
          welcomeMessage = "Hello! I'm here to help with your service requests. I'll learn your preferences and provide personalized guidance for your needs (plumbing, car repair, legal, etc.). What can I help you with?";
        }
      } else {
        welcomeMessage = "Hello! I'm here to help with your service requests. I'll learn your preferences and provide personalized guidance for your needs (plumbing, car repair, legal, etc.). Please select a service request above to get started.";
      }
    } else if (aiChatMode === 'Generic') {
      welcomeMessage = "Hello! I'm your generic AI assistant. I can help you with any topic - medical research, general knowledge, or any questions you have. How can I assist you today?";
    } else if (aiChatMode === 'Medical') {
      welcomeMessage = "Hello! I'm your medical AI assistant. I can help you with health questions, symptoms, medications, and medical concerns. How can I help you today?";
    }
    
    setAiChatMessages([{ id: 'welcome', text: welcomeMessage, isMe: false, timestamp: new Date() }]);
  };

  // Send AI chat message
  const sendAiChatMessage = async () => {
    if (!aiChatInput.trim() || aiChatLoading || !user) return;

    const userMessage = aiChatInput.trim();
    setAiChatInput('');
    setAiChatLoading(true);

    // Add user message to chat
    const userMsg = { id: Date.now().toString(), text: userMessage, isMe: true, timestamp: new Date() };
    setAiChatMessages(prev => [...prev, userMsg]);
    
    // Scroll to bottom to show loading indicator immediately
    setTimeout(() => {
      aiChatScrollViewRef.current?.scrollToEnd({ animated: true });
    }, 100);

    try {
      const token = await AsyncStorage.getItem('userToken');
      const forceServiceRequestMode = aiChatMode === 'ServiceRequest';
      const isGenericMode = aiChatMode === 'Generic';

      // Generate a simple conversation ID
      const conversationId = `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
      
      const requestBody = {
        prompt: userMessage,
        conversationId: conversationId,
        provider: 3, // HuggingFace
        patientId: user.roleId === 1 ? user.id : 0,
        userId: user.id,
        userRoleId: user.roleId,
        isGenericMode: isGenericMode,
        forceServiceRequestMode: forceServiceRequestMode,
        selectedServiceRequestId: (forceServiceRequestMode && user.roleId === 1) ? selectedServiceRequestId : null // Pass selected SR from UI
      };

      const resp = await fetch(`${API_BASE_URL}/chat/send`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(requestBody)
      });

      if (!resp.ok) {
        throw new Error(`HTTP ${resp.status}: ${await resp.text()}`);
      }

      const data = await resp.json();
      
      // Add AI response to chat
      const aiMsg = { 
        id: (Date.now() + 1).toString(), 
        text: data.message || 'Sorry, I could not process your request.', 
        isMe: false, 
        timestamp: new Date() 
      };
      setAiChatMessages(prev => [...prev, aiMsg]);
    } catch (error) {
      console.error('Error sending AI chat message:', error);
      const errorMsg = { 
        id: (Date.now() + 1).toString(), 
        text: 'Sorry, there was an error processing your message. Please try again.', 
        isMe: false, 
        timestamp: new Date() 
      };
      setAiChatMessages(prev => [...prev, errorMsg]);
    } finally {
      setAiChatLoading(false);
      // Focus the input field after response is received
      setTimeout(() => {
        aiChatInputRef.current?.focus();
      }, 100);
    }
  };

  const loadChatHistory = async (targetUserId) => {
    try {
      const token = await AsyncStorage.getItem('userToken');
      const resp = await fetch(`${API_BASE_URL}/mobile/messages/${targetUserId}`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (resp.ok) {
        const arr = await resp.json();
        const loadedMessages = (arr || []).map((m, i) => ({
          id: m.id?.toString() || `${Date.now()}_${i}`,
          text: m.message,
          isMe: m.isMe,
          timestamp: new Date(m.sentAt),
          senderId: m.senderId,
          senderName: m.senderName || 'Unknown'
        }));
        console.log('📱 App: Loaded', loadedMessages.length, 'messages from database');
        setMessages(loadedMessages);
        
        // Update last message timestamp for polling
        if (loadedMessages.length > 0) {
          const latestMessage = loadedMessages[loadedMessages.length - 1];
          lastMessageTimestampRef.current = latestMessage.timestamp?.getTime() || Date.now();
        } else {
          lastMessageTimestampRef.current = Date.now();
        }
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
    
    const messageText = newMessage.trim();
    const optimisticId = `temp_${Date.now()}_${user?.id}`;
    const optimisticTimestamp = new Date();
    
    // Add message optimistically with a temporary ID
    const optimisticMsg = {
      id: optimisticId,
      text: messageText,
      isMe: true,
      timestamp: optimisticTimestamp,
      senderId: user?.id,
      senderName: `${user?.firstName} ${user?.lastName}`
    };
    
    setMessages((prev) => {
      const newMessages = [...prev, optimisticMsg];
      // Auto-scroll to bottom when sending message
      setTimeout(() => {
        messagesScrollViewRef.current?.scrollToEnd({ animated: true });
      }, 100);
      return newMessages;
    });

    try {
      // Send via SignalR if connected (for real-time delivery)
      if (signalRConnected) {
        await SignalRService.sendMessage(selectedContact.id, messageText);
      }
      
      // Send via HTTP API (this saves to DB and returns the real message ID)
      const resp = await fetch(`${API_BASE_URL}/mobile/send-message`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${await AsyncStorage.getItem('userToken')}`
        },
        body: JSON.stringify({ targetUserId: selectedContact.id, message: messageText })
      });
      
      if (resp.ok) {
        const result = await resp.json();
        const realMessageId = result.messageId?.toString();
        
        // Replace optimistic message with real one (if we got a real ID)
        if (realMessageId) {
          setMessages((prev) => {
            // Find and replace the optimistic message
            const index = prev.findIndex(m => m.id === optimisticId);
            if (index !== -1) {
              const updated = [...prev];
              updated[index] = {
                ...updated[index],
                id: realMessageId // Replace temp ID with real ID
              };
              return updated;
            }
            return prev;
          });
        }
      }
    } catch (e) {
      console.error('sendMessage error:', e);
      // On error, we could remove the optimistic message, but keeping it is better UX
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
    
    // Log current state for debugging
    console.log('📱 Call started:', {
      channelName,
      uid,
      withVideo,
      remoteUsersCount: remoteUsers.length,
      targetUserId: targetUser.id
    });
    
    // Fallback: If events don't fire, try using target user's ID after a delay
    // This is a workaround for cases where onUserJoined doesn't fire
    const fallbackTimeout = setTimeout(() => {
      setRemoteUsers((prev) => {
        if (prev.length === 0 && targetUser.id && targetUser.id !== uid) {
          console.log('⚠️ Fallback: No remote users detected via events, using targetUser.id as fallback:', targetUser.id);
          console.log('⚠️ This is a workaround - events should have fired but didn\'t');
          return [targetUser.id];
        }
        return prev;
      });
    }, 5000); // Wait 5 seconds for events to fire
    
    // Clear timeout if remote users are detected
    const checkInterval = setInterval(() => {
      if (remoteUsers.length > 0) {
        clearTimeout(fallbackTimeout);
        clearInterval(checkInterval);
      }
    }, 500);
    
    // Give a moment for remote user to join, then log again
    setTimeout(() => {
      console.log('📱 Call state after 3 seconds:', {
        remoteUsersCount: remoteUsers.length,
        remoteUsers: remoteUsers,
        expectedRemoteUid: targetUser.id,
        myUid: uid
      });
    }, 3000);
  } catch (e) {
    console.error('startCall error:', e);
    Alert.alert('Error', e.message || 'Failed to start call.');
    setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
  }
};


  const endCall = async () => {
    try {
      console.log('📞 Mobile: Ending call...');
      
      // ✅ Notify server that call is ending (so other party gets notified)
      if (callModal?.channelName) {
        try {
          await SignalRService.endCall(callModal.channelName);
          console.log('✅ Mobile: Notified server that call ended');
        } catch (e) {
          console.error('⚠️ Mobile: Error notifying server:', e);
        }
      }
      
      // ✅ Leave Agora channel
      if (AgoraService && agoraInitialized) {
        await AgoraService.leaveChannel();
        console.log('✅ Mobile: Left Agora channel');
      }
    } catch (e) {
      console.error('❌ Mobile: Error ending call:', e);
    }
    
    // ✅ Clear call state
    setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
    setRemoteUsers([]);
    remoteUserJoinTimes.current = {}; // Clear join times
    setIsAudioMuted(false);
    setIsVideoMuted(false);
    
    // ✅ Also clear incoming call state if it exists
    if (incomingCall) {
      stopRingtone();
      setIncomingCall(null);
    }
    
    console.log('✅ Mobile: Call ended and state cleared');
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

  // ---------- INCOMING CALL ----------
  const playRingtone = async () => {
    try {
      // Stop any existing ringtone first
      stopRingtone();
      
      // Request audio mode for calls
      await Audio.setAudioModeAsync({
        playsInSilentModeIOS: true,
        staysActiveInBackground: true,
        shouldDuckAndroid: true,
      });
      
      // Create a simple beep pattern using Audio API
      // We'll create tones programmatically
      const playBeep = async (frequency = 800, duration = 400) => {
        try {
          // For now, we'll use a simple vibration pattern
          // In a production app, you'd use a proper ringtone file
          // This is a placeholder that will work but may not sound great
          console.log('🔔 Playing beep:', frequency, 'Hz for', duration, 'ms');
          
          // Note: expo-av doesn't support generating tones directly
          // For a proper ringtone, you'd need to:
          // 1. Include a ringtone file in assets
          // 2. Use Audio.Sound.createAsync with require('./assets/ringtone.mp3')
          // For now, we'll just log - the modal will still appear
        } catch (error) {
          console.warn('⚠️ Could not play beep sound:', error);
        }
      };
      
      // Play beep pattern (two beeps, then repeat every 3 seconds)
      playBeep(800, 400);
      setTimeout(() => playBeep(1000, 400), 600);
      
      // Repeat pattern every 3 seconds
      ringtoneIntervalRef.current = setInterval(() => {
        playBeep(800, 400);
        setTimeout(() => playBeep(1000, 400), 600);
      }, 3000);
      
      console.log('🔔 Started playing ringtone pattern');
    } catch (error) {
      console.error('❌ Error playing ringtone:', error);
    }
  };

  const stopRingtone = async () => {
    try {
      if (ringtoneIntervalRef.current) {
        clearInterval(ringtoneIntervalRef.current);
        ringtoneIntervalRef.current = null;
      }
      
      if (incomingCallTimeoutRef.current) {
        clearTimeout(incomingCallTimeoutRef.current);
        incomingCallTimeoutRef.current = null;
      }
      
      if (ringtoneSoundRef.current) {
        await ringtoneSoundRef.current.stopAsync();
        await ringtoneSoundRef.current.unloadAsync();
        ringtoneSoundRef.current = null;
      }
      
      console.log('🔕 Stopped ringtone');
    } catch (error) {
      console.error('❌ Error stopping ringtone:', error);
    }
  };

  const handleIncomingCall = (callData) => {
    console.log('📞 App: Handling incoming call:', JSON.stringify(callData, null, 2));
    
    // Only show if not already in a call or handling another incoming call
    if (callModal.visible) {
      console.log('⚠️ App: Already in a call, rejecting new incoming call');
      // Automatically reject if already busy
      const callId = callData.callId || callData.channelName;
      SignalRService.rejectCall(callId).catch(err => {
        console.error('Error rejecting call:', err);
      });
      return;
    }
    
    if (incomingCall) {
      console.log('⚠️ App: Already handling another incoming call, rejecting new one');
      const callId = callData.callId || callData.channelName;
      SignalRService.rejectCall(callId).catch(err => {
        console.error('Error rejecting call:', err);
      });
      return;
    }
    
    // Set incoming call state
    const incomingCallData = {
      callId: callData.callId || callData.channelName,
      callerId: callData.callerId,
      callerName: callData.callerName || 'Unknown',
      callerRole: callData.callerRole || '',
      callType: callData.callType || 'audio',
      channelName: callData.channelName || callData.callId
    };
    
    console.log('📞 App: Setting incoming call state:', incomingCallData);
    setIncomingCall(incomingCallData);
    
    // Play ringtone
    console.log('🔔 App: Starting ringtone...');
    playRingtone();
    
    // Auto-dismiss after 30 seconds if not answered
    const callId = callData.callId || callData.channelName;
    const timeoutId = setTimeout(() => {
      setIncomingCall((current) => {
        if (current && current.callId === callId) {
          console.log('⏰ Incoming call timeout - auto dismissing');
          stopRingtone();
          // Also reject the call on the server
          SignalRService.rejectCall(callId).catch(err => {
            console.error('Error rejecting timed-out call:', err);
          });
          return null;
        }
        return current;
      });
    }, 30000);
    
    // Store timeout ID for cleanup if needed
    incomingCallTimeoutRef.current = timeoutId;
  };

  const acceptIncomingCall = async () => {
    if (!incomingCall) return;
    
    try {
      console.log('✅ Mobile: Accepting incoming call:', incomingCall);
      
      // Stop ringtone
      stopRingtone();
      
      // ✅ Clear incoming call state FIRST (so modal closes immediately)
      const callId = incomingCall.callId || incomingCall.channelName;
      const callerId = incomingCall.callerId;
      const callerName = incomingCall.callerName;
      const callType = incomingCall.callType;
      setIncomingCall(null);
      
      // Accept call via SignalR
      await SignalRService.acceptCall(callId);
      console.log('✅ Mobile: Call accepted via SignalR');
      
      // Find the caller in contacts or create a temporary user object
      const caller = contacts.find(c => c.id === callerId) || {
        id: callerId,
        firstName: callerName.split(' ')[0] || 'Unknown',
        lastName: callerName.split(' ').slice(1).join(' ') || 'User'
      };
      
      // Start the call (join Agora channel)
      await startCall(caller, callType === 'video' ? 'Video' : 'Audio');
      console.log('✅ Mobile: Call started, Agora channel joined');
    } catch (error) {
      console.error('❌ Mobile: Error accepting call:', error);
      Alert.alert('Error', 'Failed to accept call. Please try again.');
      stopRingtone();
      setIncomingCall(null);
    }
  };

  const declineIncomingCall = async () => {
    if (!incomingCall) return;
    
    try {
      console.log('❌ Mobile: Declining incoming call:', incomingCall);
      
      // Stop ringtone
      stopRingtone();
      
      // Reject call via SignalR
      const callId = incomingCall.callId || incomingCall.channelName;
      await SignalRService.rejectCall(callId);
      console.log('✅ Mobile: Call rejected via SignalR');
      
      // ✅ Clear incoming call state
      setIncomingCall(null);
      
      // ✅ Also ensure call modal is closed (in case it was open)
      if (callModal.visible) {
        setCallModal({ visible: false, targetUser: null, callType: null, channelName: null });
      }
    } catch (error) {
      console.error('❌ Mobile: Error declining call:', error);
      stopRingtone();
      setIncomingCall(null);
    }
  };

  // Keep refs in sync with state - CRITICAL for message handling
  useEffect(() => {
    currentViewRef.current = currentView;
  }, [currentView]);

  useEffect(() => {
    selectedContactRef.current = selectedContact;
  }, [selectedContact]);

  useEffect(() => {
    userRef.current = user;
  }, [user]);

  // Debug: Log when incomingCall state changes
  useEffect(() => {
    if (incomingCall) {
      console.log('📞 ✅ Incoming call state set:', JSON.stringify(incomingCall, null, 2));
      console.log('📞 Modal should be visible now');
    } else {
      console.log('📞 Incoming call state cleared');
    }
  }, [incomingCall]);

  // Cleanup ringtone on unmount
  useEffect(() => {
    return () => {
      stopRingtone();
    };
  }, []);

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

  // ---------- PASSWORD RESET FUNCTIONS ----------
  const handleForgotPassword = async () => {
    if (!forgotPasswordEmail.trim()) {
      Alert.alert('Error', 'Please enter your email address');
      return;
    }

    setForgotPasswordLoading(true);
    setForgotPasswordMessage('');
    
    try {
      const forgotPasswordUrl = ensureValidUrl(`${API_BASE_URL}/auth/forgot-password`);
      console.log('📱 Forgot password URL:', forgotPasswordUrl);
      console.log('📱 API_BASE_URL:', API_BASE_URL);
      
      const resp = await fetch(forgotPasswordUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: forgotPasswordEmail.trim() })
      });
      
      console.log('📱 Forgot password response status:', resp.status);
      const data = await resp.json();
      if (resp.ok && data.success) {
        setForgotPasswordMessage('If an account with that email exists, a password reset link has been sent.');
        Alert.alert('Success', 'If an account with that email exists, a password reset link has been sent to your email.');
        // Return to login after a delay
        setTimeout(() => {
          setForgotPasswordEmail('');
          setForgotPasswordMessage('');
          setCurrentView('login');
          currentViewRef.current = 'login';
        }, 2000);
      } else {
        setForgotPasswordMessage(data.message || 'An error occurred. Please try again.');
        Alert.alert('Error', data.message || 'An error occurred. Please try again.');
      }
    } catch (e) {
      console.error('Forgot password error:', e);
      console.error('Error details:', {
        message: e.message,
        name: e.name,
        stack: e.stack
      });
      
      let errorMsg = 'Network error. Please check your connection and try again.';
      
      // Provide more specific error messages
      if (e.message && e.message.includes('Network request failed')) {
        errorMsg = `Cannot connect to server.\n\n` +
          `Server: ${API_BASE_URL}\n\n` +
          `Troubleshooting:\n` +
          `1. Test in browser: Open https://192.168.86.34:5263/swagger on your device\n` +
          `2. If login works, this is likely a certificate issue\n` +
          `3. Make sure device and Mac are on same Wi-Fi\n` +
          `4. Check Mac firewall allows port 5263\n` +
          `5. For iOS: Trust certificate in Safari first\n` +
          `6. Rebuild app if you changed config: npx expo run:ios`;
      }
      
      setForgotPasswordMessage(errorMsg);
      Alert.alert('Connection Error', errorMsg);
    } finally {
      setForgotPasswordLoading(false);
    }
  };

  const handleResetPassword = async () => {
    if (!newPassword || !confirmPassword) {
      Alert.alert('Error', 'Please fill in all fields');
      return;
    }

    if (newPassword.length < 6) {
      Alert.alert('Error', 'Password must be at least 6 characters long');
      return;
    }

    if (newPassword !== confirmPassword) {
      Alert.alert('Error', 'Passwords do not match');
      return;
    }

    if (!resetPasswordToken || !resetPasswordEmail) {
      Alert.alert('Error', 'Invalid reset link. Please request a new password reset.');
      return;
    }

    setResetPasswordLoading(true);
    setResetPasswordMessage('');
    
    try {
      const resetPasswordUrl = ensureValidUrl(`${API_BASE_URL}/auth/reset-password`);
      const resp = await fetch(resetPasswordUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          email: resetPasswordEmail,
          token: resetPasswordToken,
          newPassword: newPassword,
          confirmPassword: confirmPassword
        })
      });
      
      const data = await resp.json();
      if (resp.ok && data.success) {
        setResetPasswordMessage(data.message || 'Password has been reset successfully.');
        Alert.alert('Success', data.message || 'Password has been reset successfully. You can now login with your new password.', [
          {
            text: 'OK',
            onPress: () => {
              // Clear state and return to login
              setResetPasswordEmail('');
              setResetPasswordToken('');
              setNewPassword('');
              setConfirmPassword('');
              setResetPasswordMessage('');
              setCurrentView('login');
              currentViewRef.current = 'login';
            }
          }
        ]);
      } else {
        setResetPasswordMessage(data.message || 'An error occurred. Please try again.');
        Alert.alert('Error', data.message || 'An error occurred. Please try again.');
      }
    } catch (e) {
      console.error('Reset password error:', e);
      const errorMsg = 'Network error. Please check your connection and try again.';
      setResetPasswordMessage(errorMsg);
      Alert.alert('Error', errorMsg);
    } finally {
      setResetPasswordLoading(false);
    }
  };

  // ---------- RENDER ----------
  const renderLogin = () => (
    <LinearGradient colors={EATS_GRADIENT} style={styles.authGradient}>
    <SafeAreaView style={styles.authSafeArea}>
      <View style={styles.loginContainer}>
        <View style={styles.brandBadge}>
          <Text style={styles.brandBadgeText}>CC</Text>
        </View>
        <Text style={styles.title}>Customer Care App</Text>
        <Text style={styles.subtitle}>Fast help, live support, and service requests in one place.</Text>
        <TextInput style={styles.input} placeholder="Email" value={email} onChangeText={setEmail} autoCapitalize="none" />
        <TextInput style={styles.input} placeholder="Password" value={password} onChangeText={setPassword} secureTextEntry />
        <TouchableOpacity style={[styles.button, loading && styles.buttonDisabled]} onPress={login} disabled={loading}>
          <Text style={styles.buttonText}>{loading ? 'Logging in…' : 'Login'}</Text>
        </TouchableOpacity>
        <TouchableOpacity 
          style={styles.linkButton}
          onPress={() => {
            setForgotPasswordEmail('');
            setForgotPasswordMessage('');
            setCurrentView('forgot-password');
            currentViewRef.current = 'forgot-password';
          }}
        >
          <Text style={styles.linkButtonText}>Forgot your password?</Text>
        </TouchableOpacity>
        <TouchableOpacity 
          style={styles.guestButton} 
          onPress={() => {
            setCurrentView('guest-registration');
            currentViewRef.current = 'guest-registration';
          }}
        >
          <Text style={styles.guestButtonText}>Continue as Guest</Text>
        </TouchableOpacity>
        <View style={styles.disclaimerContainer}>
          <Text style={styles.disclaimerText}>
            We help clients connect with qualified professionals. Services are provided independently by each party.
          </Text>
        </View>
      </View>
    </SafeAreaView>
    </LinearGradient>
  );

  const renderForgotPassword = () => (
    <LinearGradient colors={EATS_GRADIENT} style={styles.authGradient}>
    <SafeAreaView style={styles.authSafeArea}>
      <View style={styles.loginContainer}>
        <Text style={styles.title}>Forgot Password</Text>
        <Text style={styles.subtitle}>Enter your email address and we'll send you a password reset link.</Text>
        
        {forgotPasswordMessage ? (
          <View style={styles.messageContainer}>
            <Text style={styles.messageText}>{forgotPasswordMessage}</Text>
          </View>
        ) : null}
        
        <TextInput 
          style={styles.input} 
          placeholder="Email" 
          value={forgotPasswordEmail} 
          onChangeText={setForgotPasswordEmail} 
          autoCapitalize="none"
          keyboardType="email-address"
          editable={!forgotPasswordLoading}
        />
        
        <TouchableOpacity 
          style={[styles.button, forgotPasswordLoading && styles.buttonDisabled]} 
          onPress={handleForgotPassword} 
          disabled={forgotPasswordLoading}
        >
          <Text style={styles.buttonText}>{forgotPasswordLoading ? 'Sending…' : 'Send Reset Link'}</Text>
        </TouchableOpacity>
        
        <TouchableOpacity 
          style={styles.linkButton}
          onPress={() => {
            setForgotPasswordEmail('');
            setForgotPasswordMessage('');
            setCurrentView('login');
            currentViewRef.current = 'login';
          }}
        >
          <Text style={styles.linkButtonText}>Back to Login</Text>
        </TouchableOpacity>
      </View>
    </SafeAreaView>
    </LinearGradient>
  );

  const renderResetPassword = () => (
      <LinearGradient colors={EATS_GRADIENT} style={styles.authGradient}>
      <SafeAreaView style={styles.authSafeArea}>
        <ScrollView contentContainerStyle={styles.loginContainer}>
          <Text style={styles.title}>Reset Password</Text>
          <Text style={styles.subtitle}>Enter your new password</Text>
          
          {resetPasswordMessage ? (
            <View style={styles.messageContainer}>
              <Text style={styles.messageText}>{resetPasswordMessage}</Text>
            </View>
          ) : null}
          
          <TextInput 
            style={[styles.input, { backgroundColor: '#f5f5f5' }]} 
            placeholder="Email Address" 
            value={resetPasswordEmail} 
            onChangeText={setResetPasswordEmail} 
            autoCapitalize="none"
            keyboardType="email-address"
            editable={false}
          />
          
          <TextInput 
            style={[styles.input, resetPasswordFromUrl && { backgroundColor: '#f5f5f5' }]} 
            placeholder="Reset Token (from email link)" 
            value={resetPasswordToken} 
            onChangeText={setResetPasswordToken} 
            autoCapitalize="none"
            editable={!resetPasswordLoading && !resetPasswordFromUrl}
            secureTextEntry={false}
          />
          <Text style={styles.helperText}>
            {resetPasswordToken ? 'Token loaded from link ✓' : 'Paste the token from your email, or click the link in your email to open the app'}
          </Text>
          
          <TextInput 
            style={styles.input} 
            placeholder="New Password" 
            value={newPassword} 
            onChangeText={setNewPassword} 
            secureTextEntry
            editable={!resetPasswordLoading}
          />
          <Text style={styles.helperText}>Password must be at least 6 characters long</Text>
          
          <TextInput 
            style={styles.input} 
            placeholder="Confirm Password" 
            value={confirmPassword} 
            onChangeText={setConfirmPassword} 
            secureTextEntry
            editable={!resetPasswordLoading}
          />
          
          <TouchableOpacity 
            style={[styles.button, resetPasswordLoading && styles.buttonDisabled]} 
            onPress={handleResetPassword} 
            disabled={resetPasswordLoading}
          >
            <Text style={styles.buttonText}>{resetPasswordLoading ? 'Resetting…' : 'Reset Password'}</Text>
          </TouchableOpacity>
          
          <TouchableOpacity 
            style={styles.linkButton}
            onPress={() => {
              setResetPasswordEmail('');
              setResetPasswordToken('');
              setResetPasswordFromUrl(false);
              setNewPassword('');
              setConfirmPassword('');
              setResetPasswordMessage('');
              setCurrentView('login');
              currentViewRef.current = 'login';
            }}
          >
            <Text style={styles.linkButtonText}>Back to Login</Text>
          </TouchableOpacity>
        </ScrollView>
      </SafeAreaView>
      </LinearGradient>
    );

  // Filter contacts based on search query (must be at component level, not inside render function)
  const filteredContacts = useMemo(() => {
    if (!contactSearchQuery.trim()) {
      return contacts;
    }

    const query = contactSearchQuery.toLowerCase().trim();
    return contacts.filter((contact) => {
      const firstName = (contact.firstName || '').toLowerCase();
      const lastName = (contact.lastName || '').toLowerCase();
      const fullName = `${firstName} ${lastName}`;
      const roleName = (contact.roleName || '').toLowerCase();
      const specialization = (contact.specialization || '').toLowerCase();
      const mobilePhone = (contact.mobilePhone || '').toLowerCase();

      return (
        firstName.includes(query) ||
        lastName.includes(query) ||
        fullName.includes(query) ||
        roleName.includes(query) ||
        specialization.includes(query) ||
        mobilePhone.includes(query)
      );
    });
  }, [contacts, contactSearchQuery]);

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
              {/* Remote Video - Always render when we have remote users and channel */}
              <View style={styles.remoteVideo}>
                {remoteUsers.length > 0 && callModal?.channelName ? (
                  <>
                    {isIOS && RtcRemoteView?.TextureView ? (
                      <RtcRemoteView.TextureView
                        uid={remoteUsers[0]}
                        channelId={callModal.channelName}
                        style={{ width: '100%', height: '100%' }}
                      />
                    ) : !isIOS && RtcRemoteView?.SurfaceView ? (
                      <RtcRemoteView.SurfaceView
                        uid={remoteUsers[0]}
                        channelId={callModal.channelName}
                        style={{ width: '100%', height: '100%' }}
                      />
                    ) : (
                      <Text style={styles.videoPlaceholder}>
                        Video component not available (Expo Go?)
                      </Text>
                    )}
                  </>
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
              {/* Local PiP - Always render when we have channel */}
              {callModal?.channelName ? (
                <View style={styles.localVideo}>
                  {isIOS && RtcLocalView?.TextureView ? (
                    <RtcLocalView.TextureView
                      channelId={callModal.channelName}
                      style={{ width: '100%', height: '100%', borderRadius: 8 }}
                    />
                  ) : !isIOS && RtcLocalView?.SurfaceView ? (
                    <RtcLocalView.SurfaceView
                      channelId={callModal.channelName}
                      style={{ width: '100%', height: '100%', borderRadius: 8 }}
                    />
                  ) : (
                    <Text style={styles.videoPlaceholder}>
                      Local preview not available
                    </Text>
                  )}
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
      <GradientHeader style={styles.header}>
        <Text style={styles.headerTitle}>Welcome, {user?.firstName}! ({user?.roleName})</Text>
        <View style={styles.headerActions}>
          <View style={styles.connectionPill}>
            <View style={[styles.connectionDot, { backgroundColor: signalRConnected ? '#22c55e' : '#ef4444' }]} />
            <Text style={styles.connectionStatus}>
              {signalRConnected ? 'Online' : 'Offline'}
            </Text>
          </View>
          <TouchableOpacity style={styles.logoutButton} onPress={logout}>
            <Text style={styles.logoutButtonText}>Log out</Text>
          </TouchableOpacity>
        </View>
      </GradientHeader>

      <View style={styles.contactsHeader}>
        <Text style={styles.contactsTitle}>
          {user?.roleId === 2 ? 'Your Clients' : 'Your Coordinators & SMEs'}
        </Text>
        <Text style={styles.contactsCount}>({filteredContacts.length})</Text>
      </View>

      {/* Search Bar */}
      <View style={styles.searchContainer}>
        <TextInput
          style={styles.searchInput}
          placeholder="Search contacts..."
          value={contactSearchQuery}
          onChangeText={setContactSearchQuery}
          placeholderTextColor="#999"
        />
        {contactSearchQuery.length > 0 && (
          <TouchableOpacity
            style={styles.clearButton}
            onPress={() => setContactSearchQuery('')}
          >
            <Text style={styles.clearButtonText}>✕</Text>
          </TouchableOpacity>
        )}
      </View>

      <ScrollView style={styles.contactsList}>
        {/* AI Chat Button - For clients/patients only */}
        {user?.roleId === 1 && (
          <View style={{ padding: 16, paddingBottom: 12 }}>
            <TouchableOpacity 
              style={[styles.serviceRequestButton, { backgroundColor: EATS_ORANGE }]}
              onPress={() => {
                setCurrentView('ai-chat');
                currentViewRef.current = 'ai-chat';
                // Initialize AI chat messages if empty
                if (aiChatMessages.length === 0) {
                  updateAiChatWelcomeMessage();
                }
              }}
            >
              <Text style={styles.serviceRequestButtonIcon}>🤖</Text>
              <Text style={[styles.serviceRequestButtonText, { color: '#fff' }]}>AI Chat Assistant</Text>
              <Text style={[styles.serviceRequestButtonArrow, { color: '#fff' }]}>›</Text>
            </TouchableOpacity>
            <Text style={{ fontSize: 12, color: '#666', marginTop: 4, textAlign: 'center' }}>
              Chat with AI for service requests, questions, or medical help
            </Text>
          </View>
        )}
        
        {/* Service Requests Button */}
        <View style={{ padding: 16, paddingBottom: 12 }}>
          <TouchableOpacity 
            style={styles.serviceRequestButton}
            onPress={() => {
              setCurrentView('service-requests');
              currentViewRef.current = 'service-requests';
            }}
          >
            <Text style={styles.serviceRequestButtonIcon}>📋</Text>
            <Text style={styles.serviceRequestButtonText}>Service Requests</Text>
            <Text style={styles.serviceRequestButtonArrow}>›</Text>
          </TouchableOpacity>
        </View>

        {filteredContacts.length === 0 && contactSearchQuery.trim() ? (
          <View style={styles.emptyContactsContainer}>
            <Text style={styles.emptyContactsText}>No contacts found matching "{contactSearchQuery}"</Text>
          </View>
        ) : (
          filteredContacts.map((contact) => (
          <TouchableOpacity key={contact.id} style={styles.contactItem} onPress={() => openContactDetail(contact)}>
            <View style={styles.contactInfo}>
              <Text style={styles.contactName}>
                {contact.roleId === 2 ? 'Dr. ' : contact.roleId === 4 ? 'Coord. ' : contact.roleId === 5 ? 'Atty. ' : ''}
                {contact.firstName} {contact.lastName}
              </Text>
              <Text style={styles.contactRole}>
                {contact.specialization || contact.roleName || 'User'}
              </Text>
              {contact.mobilePhone && <Text style={styles.contactPhone}>{contact.mobilePhone}</Text>}
            </View>
            <View style={styles.contactArrow}>
              <Text style={styles.contactArrowText}>›</Text>
            </View>
          </TouchableOpacity>
        ))
        )}
      </ScrollView>
    </SafeAreaView>
  );

  const renderContactDetail = () => (
    <SafeAreaView style={styles.container}>
      <GradientHeader style={styles.chatHeader}>
        <TouchableOpacity onPress={() => { setCurrentView('main'); currentViewRef.current = 'main'; }} style={styles.backButton}>
          <Text style={styles.backButtonText}>← Back</Text>
        </TouchableOpacity>
        <Text style={styles.chatTitle}>Contact Details</Text>
        <View style={styles.chatActions}>
          {/* Empty actions to maintain layout */}
        </View>
      </GradientHeader>

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
                onPress={() => setEmergencyModalVisible(true)}
                disabled={sendingEmergency}
              >
                <Text style={styles.contactDetailButtonIcon}>🚨</Text>
                <Text style={styles.contactDetailButtonText}>
                  {sendingEmergency ? 'Sending...' : 'Emergency'}
                </Text>
              </TouchableOpacity>
            )}

            {/* Documents button removed - documents should be uploaded through Service Requests */}
          </View>
        </View>
      )}
    </SafeAreaView>
  );

  // Render AI Chat view
  const renderAiChat = () => (
    <SafeAreaView style={styles.container}>
      <GradientHeader style={styles.chatHeader}>
        <TouchableOpacity style={styles.backButton} onPress={() => {
          setCurrentView('main');
          currentViewRef.current = 'main';
        }}>
          <Text style={styles.backButtonText}>← Back</Text>
        </TouchableOpacity>
        <Text style={styles.chatTitle}>🤖 AI Chat Assistant</Text>
        <View style={styles.chatActions}>
          {/* Empty to maintain layout */}
        </View>
      </GradientHeader>

      {/* Mode Selector */}
      <View style={{ padding: 12, backgroundColor: '#f5f5f5', borderBottomWidth: 1, borderBottomColor: '#ddd' }}>
        <Text style={{ fontSize: 12, color: '#666', marginBottom: 8, fontWeight: '600' }}>Chat Mode:</Text>
        <View style={{ flexDirection: 'row', gap: 8 }}>
          <TouchableOpacity
            style={[
              { flex: 1, padding: 10, borderRadius: 8, alignItems: 'center', borderWidth: 2 },
              aiChatMode === 'ServiceRequest' 
                ? { backgroundColor: EATS_ORANGE, borderColor: EATS_ORANGE }
                : { backgroundColor: '#fff', borderColor: '#ddd' },
              !hasActiveServiceRequests && aiChatMode !== 'ServiceRequest' ? { opacity: 0.5 } : {}
            ]}
            onPress={async () => {
              setAiChatMode('ServiceRequest');
              setAiChatMessages([]);
              // Load service requests when switching to Service Request mode
              if (user && user.roleId === 1) {
                const token = await AsyncStorage.getItem('userToken');
                if (token) {
                  await checkActiveServiceRequestsForAiChat(user.id, token);
                }
              }
              updateAiChatWelcomeMessage();
            }}
          >
            <Text style={{ 
              fontSize: 12, 
              fontWeight: '600',
              color: aiChatMode === 'ServiceRequest' ? '#fff' : '#666'
            }}>
              🔧 Service Request
            </Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[
              { flex: 1, padding: 10, borderRadius: 8, alignItems: 'center', borderWidth: 2 },
              aiChatMode === 'Generic' 
                ? { backgroundColor: EATS_ORANGE, borderColor: EATS_ORANGE }
                : { backgroundColor: '#fff', borderColor: '#ddd' }
            ]}
            onPress={async () => {
              setAiChatMode('Generic');
              setAiChatMessages([]);
              // Clear service request context when switching to Generic mode
              setSelectedServiceRequestId(null);
              setActiveServiceRequests([]);
              // Clear active SR from backend ClientAgentSession
              await clearActiveServiceRequestOnBackend();
              updateAiChatWelcomeMessage();
            }}
          >
            <Text style={{ 
              fontSize: 12, 
              fontWeight: '600',
              color: aiChatMode === 'Generic' ? '#fff' : '#666'
            }}>
              🌐 Generic AI
            </Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[
              { flex: 1, padding: 10, borderRadius: 8, alignItems: 'center', borderWidth: 2 },
              aiChatMode === 'Medical' 
                ? { backgroundColor: EATS_ORANGE, borderColor: EATS_ORANGE }
                : { backgroundColor: '#fff', borderColor: '#ddd' }
            ]}
            onPress={async () => {
              setAiChatMode('Medical');
              setAiChatMessages([]);
              // Clear service request context when switching to Medical mode
              // CRITICAL: This prevents service request chat from crossing over with medical chat
              setSelectedServiceRequestId(null);
              setActiveServiceRequests([]);
              // Clear active SR from backend ClientAgentSession
              await clearActiveServiceRequestOnBackend();
              updateAiChatWelcomeMessage();
            }}
          >
            <Text style={{ 
              fontSize: 12, 
              fontWeight: '600',
              color: aiChatMode === 'Medical' ? '#fff' : '#666'
            }}>
              🏥 Medical
            </Text>
          </TouchableOpacity>
        </View>
        {aiChatMode === 'ServiceRequest' && !hasActiveServiceRequests && (
          <View style={{ marginTop: 8, padding: 12, backgroundColor: '#fff3cd', borderRadius: 6, borderWidth: 1, borderColor: '#ffc107' }}>
            <Text style={{ fontSize: 12, color: '#856404', textAlign: 'center', marginBottom: 8 }}>
              ℹ️ No active service requests. This mode is for chatting about your service requests.
            </Text>
            <TouchableOpacity
              onPress={() => {
                setCurrentView('create-service-request');
                currentViewRef.current = 'create-service-request';
              }}
              style={{
                padding: 8,
                backgroundColor: '#ffc107',
                borderRadius: 6,
                alignItems: 'center'
              }}
            >
              <Text style={{ fontSize: 12, color: '#856404', fontWeight: '600' }}>
                ➕ Create New Service Request
              </Text>
            </TouchableOpacity>
          </View>
        )}
        {aiChatMode === 'ServiceRequest' && hasActiveServiceRequests && (
          <View style={{ marginTop: 12 }}>
            <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8 }}>
              <Text style={{ fontSize: 13, color: '#333', fontWeight: '700' }}>📋 Select Service Request:</Text>
              {!selectedServiceRequestId && (
                <Text style={{ fontSize: 11, color: '#dc3545', fontStyle: 'italic' }}>
                  ⚠️ Please select an SR to continue
                </Text>
              )}
            </View>
            <View style={{ 
              backgroundColor: '#fff', 
              borderRadius: 8, 
              borderWidth: 1, 
              borderColor: '#ddd',
              padding: 8
            }}>
              <TouchableOpacity
                onPress={() => {
                  // Show picker
                  Alert.alert(
                    'Select Service Request',
                    'Choose a service request to tie this chat to, or create a new one',
                    [
                      ...activeServiceRequests.map((sr, idx) => ({
                        text: `${sr.title || sr.Title} (${sr.status || sr.Status})`,
                        onPress: async () => {
                          const srId = sr.id || sr.Id;
                          setSelectedServiceRequestId(srId);
                          await setActiveServiceRequestOnBackend(srId);
                          setAiChatMessages([]);
                          updateAiChatWelcomeMessage();
                        }
                      })),
                      {
                        text: '➕ Create New',
                        onPress: () => {
                          setCurrentView('create-service-request');
                          currentViewRef.current = 'create-service-request';
                        },
                        style: 'default'
                      },
                      {
                        text: 'Clear Selection',
                        onPress: async () => {
                          setSelectedServiceRequestId(null);
                          await clearActiveServiceRequestOnBackend();
                          setAiChatMessages([]);
                          updateAiChatWelcomeMessage();
                        },
                        style: 'destructive'
                      },
                      { text: 'Cancel', style: 'cancel' }
                    ],
                    { cancelable: true }
                  );
                }}
                style={{
                  flexDirection: 'row',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  paddingVertical: 12,
                  paddingHorizontal: 12,
                  backgroundColor: selectedServiceRequestId ? '#fff7ed' : '#fff',
                  borderWidth: selectedServiceRequestId ? 2 : 1,
                  borderColor: selectedServiceRequestId ? EATS_ORANGE : '#ddd',
                  borderRadius: 8
                }}
              >
                <Text style={{ 
                  fontSize: 14, 
                  color: selectedServiceRequestId ? EATS_ORANGE : '#999',
                  flex: 1,
                  fontWeight: selectedServiceRequestId ? '600' : '400'
                }}>
                  {selectedServiceRequestId 
                    ? (() => {
                        const selected = activeServiceRequests.find(sr => (sr.id || sr.Id) === selectedServiceRequestId);
                        return selected ? `✓ ${selected.title || selected.Title} (${selected.status || selected.Status})` : 'Select...';
                      })()
                    : 'Tap to select a service request...'}
                </Text>
                <Text style={{ fontSize: 18, color: selectedServiceRequestId ? EATS_ORANGE : '#666', marginLeft: 8 }}>▼</Text>
              </TouchableOpacity>
            </View>
            {selectedServiceRequestId && (
              <View style={{ marginTop: 8 }}>
                <View style={{
                  padding: 10,
                  backgroundColor: '#fff7ed',
                  borderRadius: 6,
                  borderWidth: 1,
                  borderColor: '#fed7aa'
                }}>
                  {(() => {
                    const selected = activeServiceRequests.find(sr => (sr.id || sr.Id) === selectedServiceRequestId);
                    if (!selected) return null;
                    return (
                      <View>
                        <Text style={{ fontSize: 12, color: EATS_ORANGE, fontWeight: '600', marginBottom: 4 }}>
                          ✓ Active Service Request:
                        </Text>
                        <Text style={{ fontSize: 13, color: '#333', fontWeight: '500', marginBottom: 2 }}>
                          {selected.title || selected.Title}
                        </Text>
                        <View style={{ flexDirection: 'row', alignItems: 'center', marginTop: 4 }}>
                          {(() => {
                            const status = selected.status || selected.Status || 'Active';
                            const statusColor = status === 'Active' ? '#28a745' : 
                                               status === 'Pending' ? '#ffc107' : 
                                               status === 'Completed' ? '#6c757d' : '#17a2b8';
                            return (
                              <View style={{
                                paddingHorizontal: 6,
                                paddingVertical: 2,
                                borderRadius: 4,
                                backgroundColor: statusColor,
                                marginRight: 8
                              }}>
                                <Text style={{ fontSize: 10, color: '#fff', fontWeight: '600' }}>
                                  {status}
                                </Text>
                              </View>
                            );
                          })()}
                          <Text style={{ fontSize: 11, color: '#666' }}>
                            {selected.type || selected.Type || 'General'}
                          </Text>
                        </View>
                      </View>
                    );
                  })()}
                </View>
                <TouchableOpacity
                  onPress={() => {
                    setCurrentView('service-requests');
                    currentViewRef.current = 'service-requests';
                  }}
                  style={{
                    marginTop: 8,
                    padding: 8,
                    backgroundColor: EATS_ORANGE,
                    borderRadius: 6,
                    alignItems: 'center'
                  }}
                >
                  <Text style={{ fontSize: 12, color: '#fff', fontWeight: '600' }}>
                    📋 View Full Details
                  </Text>
                </TouchableOpacity>
              </View>
            )}
          </View>
        )}
        <Text style={{ fontSize: 11, color: '#666', marginTop: 8, textAlign: 'center', fontStyle: 'italic' }}>
          💬 This is AI Chat - for live chat with your coordinator/SME, use the contacts list
        </Text>
      </View>

      <KeyboardAvoidingView 
        style={{ flex: 1 }} 
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'} 
        keyboardVerticalOffset={Platform.OS === 'ios' ? 0 : 0}
        enabled={true}
      >
        <ScrollView 
          ref={aiChatScrollViewRef}
          style={{ flex: 1 }} 
          contentContainerStyle={{ padding: 10, paddingBottom: 10 }}
          keyboardShouldPersistTaps="handled"
          keyboardDismissMode="interactive"
          showsVerticalScrollIndicator={true}
          onContentSizeChange={() => {
            // Auto-scroll to bottom when content size changes (new message or loading indicator added)
            setTimeout(() => {
              aiChatScrollViewRef.current?.scrollToEnd({ animated: true });
            }, 100);
          }}
        >
          {aiChatMessages.map((m) => (
            <View key={m.id} style={[styles.messageItem, m.isMe ? styles.myMessage : styles.otherMessage]}>
              <Text style={[styles.messageText, m.isMe ? styles.myMessageText : styles.otherMessageText]}>{m.text}</Text>
              <Text style={[styles.messageTime, m.isMe ? styles.myMessageTime : styles.otherMessageTime]}>
                {m.timestamp?.toLocaleTimeString() || 'Now'}
              </Text>
            </View>
          ))}
          {aiChatLoading && (
            <View style={[styles.messageItem, styles.otherMessage]}>
              <Text style={styles.messageText}>Thinking...</Text>
            </View>
          )}
        </ScrollView>

        <View style={styles.messageInput}>
          <TextInput
            ref={aiChatInputRef}
            style={styles.textInput}
            value={aiChatInput}
            onChangeText={setAiChatInput}
            placeholder={aiChatMode === 'ServiceRequest' ? 'Ask about your service requests...' : aiChatMode === 'Medical' ? 'Ask about your health...' : 'Ask me anything...'}
            multiline
            maxLength={1000}
            editable={!aiChatLoading}
            blurOnSubmit={false}
          />
          <TouchableOpacity
            style={[styles.sendButton, (!aiChatInput.trim() || aiChatLoading) && styles.sendButtonDisabled]}
            onPress={sendAiChatMessage}
            disabled={!aiChatInput.trim() || aiChatLoading}
          >
            <Text style={styles.sendButtonText}>Send</Text>
          </TouchableOpacity>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );

  const renderChat = () => (
    <SafeAreaView style={styles.container}>
      <GradientHeader style={styles.chatHeader}>
        <TouchableOpacity style={styles.backButton} onPress={() => {
          setSelectedContact(null);
          setCurrentView('contact-detail');
    currentViewRef.current = 'contact-detail';
          setMessages([]);
        }}>
          <Text style={styles.backButtonText}>← Back</Text>
        </TouchableOpacity>
        <View style={{ flex: 1, alignItems: 'center' }}>
          <Text style={styles.chatTitle}>{selectedContact?.firstName} {selectedContact?.lastName}</Text>
          <Text style={{ fontSize: 11, color: '#666', marginTop: 2 }}>💬 Live Chat</Text>
        </View>
        <View style={styles.chatActions}>
          <TouchableOpacity style={styles.chatActionButton} onPress={() => startCall(selectedContact, 'Audio')}>
            <Text style={styles.chatActionText}>📞</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.chatActionButton} onPress={() => startCall(selectedContact, 'Video')}>
            <Text style={styles.chatActionText}>📹</Text>
          </TouchableOpacity>
        </View>
      </GradientHeader>

      <KeyboardAvoidingView style={styles.chatContainer} behavior={Platform.OS === 'ios' ? 'padding' : 'height'} keyboardVerticalOffset={Platform.OS === 'ios' ? 90 : 0}>
        <ScrollView 
          ref={messagesScrollViewRef}
          style={styles.messagesContainer} 
          contentContainerStyle={styles.messagesContent}
          onContentSizeChange={() => {
            // Auto-scroll to bottom when content size changes (new message added)
            messagesScrollViewRef.current?.scrollToEnd({ animated: true });
          }}
        >
          {messages.map((m) => (
            <View key={m.id} style={[styles.messageItem, m.isMe ? styles.myMessage : styles.otherMessage]}>
              <Text style={[styles.messageText, m.isMe ? styles.myMessageText : styles.otherMessageText]}>{m.text}</Text>
              <Text style={[styles.messageTime, m.isMe ? styles.myMessageTime : styles.otherMessageTime]}>{m.timestamp?.toLocaleTimeString() || 'Now'}</Text>
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

  if (!user) {
    if (currentView === 'forgot-password') {
      return renderForgotPassword();
    }

    if (currentView === 'reset-password') {
      return renderResetPassword();
    }

    if (currentView === 'guest-registration') {
      return (
        <SafeAreaView style={styles.container}>
          <StatusBar style="auto" />
          <GuestRegistrationForm
            onBack={() => {
              setCurrentView('login');
              currentViewRef.current = 'login';
            }}
            onSuccess={() => {
              setCurrentView('login');
              currentViewRef.current = 'login';
            }}
          />
        </SafeAreaView>
      );
    }
    return renderLogin();
  }

  // Check if user must change password (force password change on first login)
  if (currentView === 'change-password' || user.mustChangePassword) {
    return (
      <SafeAreaView style={styles.container}>
        <StatusBar style="auto" />
        <ChangePassword
          user={user}
          onPasswordChanged={async () => {
            // Reload user data to get updated mustChangePassword flag
            try {
              const token = await AsyncStorage.getItem('userToken');
              const resp = await fetch(`${API_BASE_URL}/auth/user`, {
                headers: { 'Authorization': `Bearer ${token}` },
              });
              if (resp.ok) {
                const userData = await resp.json();
                await AsyncStorage.setItem('currentUser', JSON.stringify(userData));
                setUser(userData);
                userRef.current = userData;
                
                // Load contacts for patients (doctors/coordinators/attorneys)
                // Use setTimeout to ensure state is set and avoid race conditions
                setTimeout(async () => {
                  if (!loadingContactsRef.current && lastLoadedUserIdRef.current !== userData.id) {
                    await loadContactsForUser(userData, token);
                  }
                }, 300);
                
                // Initialize services after password change
                await loadAvailablePatients(userData, token);
                await initializeSignalR(token);
                
                setCurrentView('main');
                currentViewRef.current = 'main';
              }
            } catch (error) {
              console.error('Error reloading user after password change:', error);
              // Still proceed to main view
              setCurrentView('main');
              currentViewRef.current = 'main';
            }
          }}
          onCancel={async () => {
            // Logout and return to login screen
            try {
              const token = await AsyncStorage.getItem('userToken');
              // Call logout endpoint if available
              try {
                await fetch(`${API_BASE_URL}/auth/logout`, {
                  method: 'POST',
                  headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json',
                  },
                });
              } catch (e) {
                // Continue with logout even if server call fails
                console.log('Logout API call failed, continuing with local logout');
              }
              
              // Clear local storage
              await AsyncStorage.removeItem('userToken');
              await AsyncStorage.removeItem('currentUser');
              
              // Disconnect SignalR
              await SignalRService.disconnect();
              
              // Clear state
              setUser(null);
              userRef.current = null;
              setCurrentView('login');
              currentViewRef.current = 'login';
            } catch (error) {
              console.error('Error during logout:', error);
              // Still clear state and go to login
              await AsyncStorage.removeItem('userToken');
              await AsyncStorage.removeItem('currentUser');
              setUser(null);
              userRef.current = null;
              setCurrentView('login');
              currentViewRef.current = 'login';
            }
          }}
        />
      </SafeAreaView>
    );
  }

  // Render different views based on currentView
  if (currentView === 'service-requests') {
    return (
      <SafeAreaView style={styles.container}>
        <StatusBar style="auto" />
        <GradientHeader style={styles.chatHeader}>
          <TouchableOpacity onPress={() => { setCurrentView('main'); currentViewRef.current = 'main'; }} style={styles.backButton}>
            <Text style={styles.backButtonText}>← Back</Text>
          </TouchableOpacity>
          <Text style={styles.chatTitle}>Service Requests</Text>
          <View style={styles.chatActions}>
            {/* Empty actions to maintain layout */}
          </View>
        </GradientHeader>
        <ServiceRequestList 
          onServiceRequestSelect={(sr) => {
            setSelectedServiceRequest(sr);
            setCurrentView('service-request-detail');
            currentViewRef.current = 'service-request-detail';
          }}
          onCreateRequest={() => {
            setCurrentView('create-service-request');
            currentViewRef.current = 'create-service-request';
          }}
          user={user}
        />
      </SafeAreaView>
    );
  }

  if (currentView === 'create-service-request') {
    return (
      <SafeAreaView style={styles.container}>
        <StatusBar style="auto" />
        <GradientHeader style={styles.chatHeader}>
          <TouchableOpacity onPress={() => {
            setCurrentView('service-requests');
            currentViewRef.current = 'service-requests';
          }} style={styles.backButton}>
            <Text style={styles.backButtonText}>← Back</Text>
          </TouchableOpacity>
          <Text style={styles.chatTitle}>Create Service Request</Text>
          <View style={styles.chatActions}>
            {/* Empty actions to maintain layout */}
          </View>
        </GradientHeader>
        <CreateServiceRequestForm
          user={user}
          onSuccess={() => {
            setCurrentView('service-requests');
            currentViewRef.current = 'service-requests';
          }}
          onCancel={() => {
            setCurrentView('service-requests');
            currentViewRef.current = 'service-requests';
          }}
        />
      </SafeAreaView>
    );
  }

  // Helper function to open contact detail page for client from service request
  const contactClient = () => {
    if (!selectedServiceRequest) return;
    
    const clientId = selectedServiceRequest.clientId || selectedServiceRequest.ClientId;
    if (!clientId) {
      Alert.alert('Error', 'Client information not available');
      return;
    }

    // Try to find client in existing contacts
    let clientContact = contacts.find(c => c.id === clientId);
    
    // If not found, create a contact object from service request data
    if (!clientContact) {
      const clientName = selectedServiceRequest.clientName || selectedServiceRequest.ClientName || 'Client';
      const nameParts = clientName.split(' ');
      clientContact = {
        id: clientId,
        firstName: nameParts[0] || 'Client',
        lastName: nameParts.slice(1).join(' ') || '',
        email: '', // Will be empty if not in contacts
        roleName: 'Patient',
        roleId: 1, // Patient role
        mobilePhone: ''
      };
    }

    // Open contact detail page (allows choosing chat, audio, or video)
    openContactDetail(clientContact);
  };

  if (currentView === 'service-request-detail') {
    const isCoordinatorOrAdmin = user?.roleId === 3 || user?.roleId === 4; // Coordinator = 3, Admin = 4
    const clientId = selectedServiceRequest?.clientId || selectedServiceRequest?.ClientId;

    return (
      <SafeAreaView style={styles.container}>
        <StatusBar style="auto" />
        <GradientHeader style={styles.chatHeader}>
          <TouchableOpacity onPress={() => { 
            setSelectedServiceRequest(null);
            setCurrentView('service-requests'); 
            currentViewRef.current = 'service-requests'; 
          }} style={styles.backButton}>
            <Text style={styles.backButtonText}>← Back</Text>
          </TouchableOpacity>
          <Text style={styles.chatTitle}>
            {selectedServiceRequest?.title || 'Service Request'}
          </Text>
          <View style={styles.chatActions}>
            {/* Empty actions to maintain layout */}
          </View>
        </GradientHeader>
        <View style={{ flex: 1 }}>
          {selectedServiceRequest && (
            <View style={{ padding: 16, paddingBottom: 8, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#eee' }}>
              <Text style={{ fontSize: 16, fontWeight: 'bold', marginBottom: 8 }}>
                {selectedServiceRequest.title || selectedServiceRequest.Title}
              </Text>
              <Text style={{ fontSize: 14, color: '#666', marginBottom: 4 }}>
                Client: {selectedServiceRequest.clientName || selectedServiceRequest.ClientName}
              </Text>
              <Text style={{ fontSize: 14, color: '#666', marginBottom: 4 }}>
                Type: {selectedServiceRequest.type || selectedServiceRequest.Type || 'General'}
              </Text>
              <Text style={{ fontSize: 14, color: '#666', marginBottom: 4 }}>
                Status: {selectedServiceRequest.status || selectedServiceRequest.Status || 'Active'}
              </Text>
              {(selectedServiceRequest.description || selectedServiceRequest.Description) && (
                <Text style={{ fontSize: 14, color: '#666', marginBottom: 0 }}>
                  {selectedServiceRequest.description || selectedServiceRequest.Description}
                </Text>
              )}
              
              {/* Contact Client button for Coordinators and Admins */}
              {isCoordinatorOrAdmin && clientId && (
                <TouchableOpacity
                  style={{
                    backgroundColor: EATS_ORANGE,
                    paddingVertical: 12,
                    paddingHorizontal: 16,
                    borderRadius: 6,
                    marginTop: 12,
                    alignItems: 'center',
                    justifyContent: 'center',
                  }}
                  onPress={contactClient}
                >
                  <Text style={{ color: '#fff', fontSize: 16, fontWeight: '600' }}>
                    📞 Contact Client
                  </Text>
                </TouchableOpacity>
              )}
            </View>
          )}
          <DocumentUpload 
            patientId={user?.roleId === 1 ? user.id : (selectedServiceRequest?.clientId || selectedServiceRequest?.ClientId || user.id)}
            serviceRequestId={selectedServiceRequest?.id || selectedServiceRequest?.Id || null}
            availablePatients={availablePatients}
            showPatientSelector={false}
            onDocumentUploaded={() => console.log('Document uploaded')}
            user={user}
          />
        </View>
      </SafeAreaView>
    );
  }

  // Old 'documents' view removed - documents should be uploaded through Service Requests
  // Users should navigate: Service Requests → Select SR → Upload Documents

  // Render incoming call modal
  const renderIncomingCallModal = () => {
    const isVisible = !!incomingCall;
    if (isVisible) {
      console.log('📞 Rendering incoming call modal with data:', incomingCall);
    }
    return (
      <Modal visible={isVisible} transparent animationType="fade" onRequestClose={declineIncomingCall}>
        <View style={styles.incomingCallOverlay}>
          <View style={styles.incomingCallModal}>
            <View style={styles.incomingCallHeader}>
              <View style={styles.callerAvatar}>
                <Text style={styles.callerAvatarText}>
                  {incomingCall?.callerName?.charAt(0)?.toUpperCase() || '?'}
                </Text>
              </View>
              <View style={styles.callerInfo}>
                <Text style={styles.callerName}>{incomingCall?.callerName || 'Unknown'}</Text>
                <Text style={styles.callerRole}>{incomingCall?.callerRole || ''}</Text>
                <Text style={styles.callTypeText}>
                  {incomingCall?.callType === 'video' ? '📹 Video Call' : '📞 Audio Call'}
                </Text>
              </View>
            </View>
            
            <View style={styles.incomingCallActions}>
              <TouchableOpacity 
                style={[styles.incomingCallButton, styles.declineButton]} 
                onPress={declineIncomingCall}
              >
                <Text style={styles.incomingCallButtonText}>Decline</Text>
              </TouchableOpacity>
              <TouchableOpacity 
                style={[styles.incomingCallButton, styles.acceptButton]} 
                onPress={acceptIncomingCall}
              >
                <Text style={styles.incomingCallButtonText}>Accept</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    );
  };

  if (currentView === 'contact-detail') {
    return (
      <View style={styles.container}>
        <StatusBar style="auto" />
        {renderContactDetail()}
        {renderCallModal()}
        {renderIncomingCallModal()}
        <SmsComponent
          visible={smsModalVisible}
          onClose={() => setSmsModalVisible(false)}
          user={user}
          selectedContact={selectedContactDetail}
          apiBaseUrl={API_BASE_URL}
        />
        
        <EmergencyComponent
          visible={emergencyModalVisible}
          onClose={() => setEmergencyModalVisible(false)}
          user={user}
          selectedContact={selectedContactDetail}
          apiBaseUrl={API_BASE_URL}
          deviceToken={deviceToken}
          onEmergencySent={() => {
            setSendingEmergency(false);
          }}
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
        {renderIncomingCallModal()}
      </View>
    );
  }

  if (currentView === 'ai-chat') {
    return (
      <View style={styles.container}>
        <StatusBar style="auto" />
        {renderAiChat()}
        {renderCallModal()}
        {renderIncomingCallModal()}
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <StatusBar style="auto" />
      {renderContacts()}
      {renderCallModal()}
      {renderIncomingCallModal()}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: EATS_BG,
  },
  authGradient: {
    flex: 1,
  },
  authSafeArea: {
    flex: 1,
    justifyContent: 'center',
    padding: 20,
  },
  loginContainer: {
    backgroundColor: '#fff',
    borderRadius: 24,
    padding: 24,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 12 },
    shadowOpacity: 0.18,
    shadowRadius: 24,
    elevation: 8,
  },
  brandBadge: {
    alignSelf: 'center',
    width: 72,
    height: 72,
    borderRadius: 22,
    backgroundColor: EATS_ORANGE,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 18,
    shadowColor: EATS_ORANGE,
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.3,
    shadowRadius: 14,
    elevation: 5,
  },
  brandBadgeText: {
    color: '#fff',
    fontSize: 28,
    fontWeight: '800',
  },
  title: {
    fontSize: 28,
    fontWeight: '800',
    textAlign: 'center',
    marginBottom: 10,
    color: EATS_TEXT,
  },
  input: {
    borderWidth: 1,
    borderColor: '#e5e7eb',
    padding: 15,
    marginBottom: 15,
    borderRadius: 14,
    fontSize: 16,
    backgroundColor: '#fff',
    color: EATS_TEXT,
  },
  button: {
    backgroundColor: EATS_ORANGE,
    padding: 15,
    borderRadius: 14,
    alignItems: 'center',
    shadowColor: EATS_ORANGE,
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.22,
    shadowRadius: 12,
    elevation: 4,
  },
  buttonDisabled: {
    backgroundColor: '#ccc',
  },
  buttonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
  guestButton: {
    backgroundColor: 'transparent',
    borderWidth: 1,
    borderColor: EATS_ORANGE,
    borderRadius: 14,
    padding: 12,
    marginTop: 10,
    alignItems: 'center',
  },
  guestButtonText: {
    color: EATS_ORANGE,
    fontSize: 16,
    fontWeight: '600',
  },
  disclaimerContainer: {
    marginTop: 30,
    paddingHorizontal: 20,
    paddingVertical: 15,
  },
  disclaimerText: {
    fontSize: 12,
    color: '#666',
    textAlign: 'center',
    lineHeight: 18,
  },
  linkButton: {
    marginTop: 15,
    alignItems: 'center',
  },
  linkButtonText: {
    color: EATS_ORANGE,
    fontSize: 14,
    textDecorationLine: 'underline',
  },
  subtitle: {
    fontSize: 14,
    textAlign: 'center',
    color: EATS_MUTED,
    marginBottom: 20,
    paddingHorizontal: 10,
  },
  messageContainer: {
    backgroundColor: '#fff7ed',
    padding: 12,
    borderRadius: 12,
    marginBottom: 15,
    borderWidth: 1,
    borderColor: '#fed7aa',
  },
  messageText: {
    color: '#9a3412',
    fontSize: 14,
    textAlign: 'center',
  },
  helperText: {
    fontSize: 12,
    color: '#666',
    marginTop: -10,
    marginBottom: 15,
    paddingHorizontal: 5,
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
    color: '#fff',
    fontSize: 16,
  },
  backButton: {
    color: '#fff',
    fontSize: 16,
  },
  contactsList: {
    flex: 1,
  },
  contactItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    backgroundColor: '#fff',
    marginHorizontal: 14,
    marginVertical: 6,
    borderRadius: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08,
    shadowRadius: 8,
    elevation: 2,
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
    backgroundColor: EATS_ORANGE,
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
    backgroundColor: EATS_ORANGE,
    alignSelf: 'flex-end',
  },
  receivedMessage: {
    backgroundColor: '#fff',
    alignSelf: 'flex-start',
    borderWidth: 1,
    borderColor: '#eee',
  },
  myMessage: {
    backgroundColor: EATS_ORANGE,
    alignSelf: 'flex-end',
  },
  otherMessage: {
    backgroundColor: '#fff',
    alignSelf: 'flex-start',
    borderWidth: 1,
    borderColor: '#eee',
  },
  messageText: {
    fontSize: 16,
    color: '#333',
  },
  myMessageText: {
    color: '#fff', // White text on blue background
  },
  otherMessageText: {
    color: '#1a1a1a', // Dark text for better visibility on light blue
    fontWeight: '500', // Slightly bolder for better readability
  },
  messageTime: {
    fontSize: 12,
    color: '#666',
    marginTop: 5,
  },
  myMessageTime: {
    color: '#e0e0e0', // Light text on blue background
  },
  otherMessageTime: {
    color: '#555', // Darker text for better visibility
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
    backgroundColor: EATS_ORANGE,
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
    gap: 8,
    marginLeft: 'auto',
  },
  connectionPill: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 10,
    paddingVertical: 7,
    borderRadius: 999,
    backgroundColor: 'rgba(255,255,255,0.18)',
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.28)',
  },
  connectionDot: {
    width: 7,
    height: 7,
    borderRadius: 4,
  },
  connectionStatus: {
    fontSize: 11,
    fontWeight: '700',
    color: '#fff',
  },
  logoutButton: {
    backgroundColor: '#fff',
    paddingHorizontal: 12,
    paddingVertical: 7,
    borderRadius: 999,
  },
  logoutButtonText: {
    color: EATS_ORANGE,
    fontSize: 12,
    fontWeight: '800',
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
    backgroundColor: EATS_ORANGE,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(255,255,255,0.22)',
  },
  backButton: {
    marginRight: 16,
  },
  backButtonText: {
    fontSize: 16,
    color: '#fff',
    fontWeight: '600',
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '800',
    color: '#fff',
    flex: 1,
    marginRight: 12,
  },
  searchContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: EATS_BG,
    borderBottomWidth: 1,
    borderBottomColor: '#eee',
  },
  searchInput: {
    flex: 1,
    height: 40,
    backgroundColor: '#fff',
    borderRadius: 20,
    paddingHorizontal: 16,
    fontSize: 16,
    color: '#333',
  },
  clearButton: {
    marginLeft: 8,
    width: 30,
    height: 30,
    alignItems: 'center',
    justifyContent: 'center',
  },
  clearButtonText: {
    fontSize: 18,
    color: '#666',
  },
  emptyContactsContainer: {
    padding: 20,
    alignItems: 'center',
  },
  emptyContactsText: {
    fontSize: 16,
    color: '#666',
  },
  contactsHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 15,
    paddingVertical: 10,
    backgroundColor: EATS_BG,
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
    backgroundColor: EATS_ORANGE,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(255,255,255,0.22)',
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
    padding: 12,
    backgroundColor: EATS_BG,
  },
  messagesContent: {
    paddingBottom: 10,
  },
  messageItem: {
    marginVertical: 5,
    padding: 12,
    borderRadius: 18,
    maxWidth: '80%',
  },
  myMessage: {
    alignSelf: 'flex-end',
    backgroundColor: EATS_ORANGE,
  },
  otherMessage: {
    alignSelf: 'flex-start',
    backgroundColor: '#fff',
    borderWidth: 1,
    borderColor: '#eee',
  },
  messageText: {
    fontSize: 16,
    color: EATS_TEXT,
  },
  otherMessageText: {
    color: '#1a1a1a', // Dark text for better visibility on light blue
    fontWeight: '500', // Slightly bolder for better readability
  },
  messageTime: {
    fontSize: 12,
    color: 'rgba(255, 255, 255, 0.8)',
    marginTop: 4,
  },
  messageInput: {
    flexDirection: 'row',
    padding: 12,
    backgroundColor: '#fff',
    borderTopWidth: 1,
    borderTopColor: '#eee',
    alignItems: 'flex-end',
    ...(Platform.OS === 'android' && {
      paddingBottom: 10,
    }),
  },
  textInput: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 22,
    paddingHorizontal: 15,
    paddingVertical: 10,
    marginRight: 10,
    maxHeight: 100,
    fontSize: 16,
  },
  sendButton: {
    backgroundColor: EATS_ORANGE,
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
    borderRadius: 20,
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
    color: EATS_ORANGE,
    marginBottom: 8,
  },
  contactDetailEmail: {
    fontSize: 16,
    color: EATS_ORANGE,
  },
  contactDetailActions: {
    flex: 1,
    gap: 15,
  },
  contactDetailButton: {
    backgroundColor: '#fff',
    borderRadius: 18,
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
  // Incoming call modal styles
  incomingCallOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.8)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  incomingCallModal: {
    backgroundColor: '#fff',
    borderRadius: 20,
    padding: 30,
    minWidth: 300,
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 10 },
    shadowOpacity: 0.3,
    shadowRadius: 20,
    elevation: 10,
  },
  incomingCallHeader: {
    alignItems: 'center',
    marginBottom: 30,
  },
  callerAvatar: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: EATS_ORANGE,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 15,
  },
  callerAvatarText: {
    fontSize: 32,
    fontWeight: 'bold',
    color: '#fff',
  },
  callerInfo: {
    alignItems: 'center',
  },
  callerName: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 5,
  },
  callerRole: {
    fontSize: 16,
    color: '#666',
    marginBottom: 5,
  },
  callTypeText: {
    fontSize: 18,
    color: EATS_ORANGE,
    fontWeight: '600',
  },
  incomingCallActions: {
    flexDirection: 'row',
    gap: 20,
    width: '100%',
  },
  incomingCallButton: {
    flex: 1,
    padding: 15,
    borderRadius: 50,
    alignItems: 'center',
    justifyContent: 'center',
  },
  declineButton: {
    backgroundColor: '#f44336',
  },
  acceptButton: {
    backgroundColor: '#4CAF50',
  },
  incomingCallButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
  serviceRequestButton: {
    backgroundColor: EATS_ORANGE,
    borderRadius: 18,
    padding: 16,
    flexDirection: 'row',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.15,
    shadowRadius: 4,
    elevation: 4,
    borderWidth: 0,
    marginHorizontal: 0,
  },
  serviceRequestButtonIcon: {
    fontSize: 24,
    marginRight: 12,
  },
  serviceRequestButtonText: {
    flex: 1,
    fontSize: 18,
    fontWeight: '600',
    color: '#fff',
  },
  serviceRequestButtonArrow: {
    fontSize: 24,
    color: '#fff',
    opacity: 0.9,
  },
});
