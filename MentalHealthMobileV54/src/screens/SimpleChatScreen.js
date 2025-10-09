import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TextInput,
  TouchableOpacity,
  ScrollView,
  KeyboardAvoidingView,
  Platform,
  Alert,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '../context/AuthContext';
import { useRealtime } from '../context/RealtimeContext';

export default function SimpleChatScreen({ route, navigation }) {
  const [newMessage, setNewMessage] = useState('');
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const [showScrollButton, setShowScrollButton] = useState(false);
  const scrollViewRef = useRef(null);
  const { user } = useAuth();
  const { sendMessage, isConnected, initiateCall, loadChatHistory, getMessagesForUser, messages: globalMessages, forceRefresh } = useRealtime();
  const { targetUser, chatType } = route.params || {};
  
  // Get messages for the current target user using the reliable method
  const messages = targetUser ? 
    getMessagesForUser(targetUser.id).map(msg => ({
      id: msg.id || `msg_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      text: msg.message || '',
      sender: msg.senderId === user.id ? 'me' : 'other',
      timestamp: msg.timestamp ? new Date(msg.timestamp) : new Date(),
      senderName: msg.senderName || 'Unknown'
    })) : [];

  // Debug log for messages (simplified)
  useEffect(() => {
    if (messages.length > 0) {
      console.log('Chat screen - messages updated:', messages.length, 'messages');
    }
  }, [messages.length]);

  // Auto-scroll to bottom only when new messages are added (not on every refresh)
  useEffect(() => {
    if (messages.length > 0 && scrollViewRef.current) {
      // Only scroll if we're at or near the bottom (within 100px)
      // This prevents jumping when user is reading older messages
      const isNearBottom = true; // For now, always scroll for new messages
      
      if (isNearBottom) {
        // Small delay to ensure the message is rendered
        setTimeout(() => {
          scrollViewRef.current?.scrollToEnd({ animated: true });
        }, 100);
      }
    }
  }, [messages.length]); // Only trigger on message count change, not content change


  // Function to scroll to bottom
  const scrollToBottom = () => {
    if (scrollViewRef.current) {
      scrollViewRef.current.scrollToEnd({ animated: true });
      setShowScrollButton(false);
    }
  };

  // Handle scroll events
  const handleScroll = (event) => {
    const { contentOffset, contentSize, layoutMeasurement } = event.nativeEvent;
    const isAtBottom = contentOffset.y + layoutMeasurement.height >= contentSize.height - 20;
    setShowScrollButton(!isAtBottom && messages.length > 3);
  };

  // Load chat history when screen comes into focus
  useEffect(() => {
    const unsubscribe = navigation.addListener('focus', () => {
      console.log('Chat screen focused - refreshing messages');
      if (isConnected && targetUser) {
        // Force refresh to get latest messages
        forceRefresh();
        loadChatHistoryForUser();
      }
    });

    return unsubscribe;
  }, [navigation, isConnected, targetUser]);

  // Gentle periodic refresh every 10 seconds to ensure messages are up to date
  useEffect(() => {
    if (targetUser && isConnected) {
      const interval = setInterval(() => {
        console.log('Gentle refresh for messages...');
        forceRefresh();
      }, 10000); // 10 seconds - much less frequent
      
      return () => clearInterval(interval);
    }
  }, [targetUser, isConnected]);

  useEffect(() => {
    // Set navigation header
    navigation.setOptions({
      headerShown: true,
      title: targetUser ? 
        `${targetUser.firstName} ${targetUser.lastName}` : 
        'Chat',
      headerStyle: {
        backgroundColor: user.roleId === 2 ? '#2196F3' : '#4CAF50',
      },
      headerTintColor: '#fff',
      headerLeft: () => (
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => navigation.goBack()}
        >
          <Ionicons name="arrow-back" size={24} color="#fff" />
        </TouchableOpacity>
      ),
      headerRight: () => (
        <View style={styles.headerButtons}>
          <TouchableOpacity
            style={styles.headerButton}
            onPress={() => handleVideoCall()}
          >
            <Ionicons name="videocam" size={24} color="#fff" />
          </TouchableOpacity>
          <TouchableOpacity
            style={styles.headerButton}
            onPress={() => handleAudioCall()}
          >
            <Ionicons name="call" size={24} color="#fff" />
          </TouchableOpacity>
        </View>
      ),
    });

    // Load chat history when targetUser changes
    if (targetUser && isConnected) {
      loadChatHistoryForUser();
    }
  }, [targetUser, user.roleId, isConnected]);

  // Load chat history for the current target user
  const loadChatHistoryForUser = async () => {
    if (!targetUser || !isConnected) return;
    
    setIsLoadingHistory(true);
    try {
      console.log('Loading chat history for user:', targetUser.id);
      await loadChatHistory(targetUser.id);
      console.log('Chat history loaded for user:', targetUser.id);
    } catch (error) {
      console.error('Error loading chat history:', error);
    } finally {
      setIsLoadingHistory(false);
    }
  };

  // Load chat history when target user changes
  useEffect(() => {
    if (targetUser && isConnected) {
      loadChatHistoryForUser();
    }
  }, [targetUser, isConnected]);




  const handleSendMessage = () => {
    if (!newMessage.trim() || !targetUser) return;

    // Store the message to send
    const messageToSend = newMessage.trim();
    
    // Clear input immediately for better UX
    setNewMessage('');

    // Send via realtime service if connected
    if (isConnected) {
      sendMessage(targetUser.id, messageToSend);
      
      // Gentle refresh to ensure message appears
      setTimeout(() => {
        forceRefresh();
      }, 500);
      
      // Single, smooth scroll to bottom after sending
      setTimeout(() => {
        scrollToBottom();
      }, 1000);
    } else {
      console.warn('Not connected to realtime service');
      // Restore message if not connected
      setNewMessage(messageToSend);
    }
  };

  const handleVideoCall = () => {
    if (!targetUser) return;

    Alert.alert(
      'Video Call',
      `Start video call with ${targetUser.firstName} ${targetUser.lastName}?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Call',
          onPress: () => {
            if (isConnected) {
              initiateCall(targetUser.id, 'video');
            }
            navigation.navigate('VideoCall', { 
              [user.roleId === 2 ? 'patient' : 'doctor']: targetUser,
              callType: 'video',
              isInitiator: true 
            });
          },
        },
      ]
    );
  };

  const handleAudioCall = () => {
    if (!targetUser) return;

    Alert.alert(
      'Audio Call',
      `Start audio call with ${targetUser.firstName} ${targetUser.lastName}?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Call',
          onPress: () => {
            if (isConnected) {
              initiateCall(targetUser.id, 'audio');
            }
            navigation.navigate('AudioCall', { 
              [user.roleId === 2 ? 'patient' : 'doctor']: targetUser,
              callType: 'audio',
              isInitiator: true 
            });
          },
        },
      ]
    );
  };

  if (!targetUser) {
    return (
      <View style={styles.noTargetContainer}>
        <Ionicons name="chatbubbles-outline" size={64} color="#ccc" />
        <Text style={styles.noTargetText}>Select a conversation to start chatting</Text>
      </View>
    );
  }

  return (
    <KeyboardAvoidingView 
      style={styles.container} 
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
      keyboardVerticalOffset={Platform.OS === 'ios' ? 90 : 0}
    >
      {!isConnected && (
        <View style={styles.connectionBanner}>
          <Text style={styles.connectionBannerText}>
            Disconnected - Messages may not be delivered
          </Text>
        </View>
      )}
      
      <ScrollView 
        ref={scrollViewRef}
        style={styles.messagesContainer}
        contentContainerStyle={styles.messagesContent}
        keyboardShouldPersistTaps="handled"
        onScroll={handleScroll}
        scrollEventThrottle={16}
      >
        {isLoadingHistory ? (
          <View style={styles.loadingContainer}>
            <Text style={styles.loadingText}>Loading chat history...</Text>
          </View>
        ) : (
          messages.map((message, index) => (
            <View 
              key={`${message.id || 'msg'}-${index}`} 
              style={[
                styles.messageContainer,
                message.sender === 'me' ? styles.myMessage : styles.otherMessage
              ]}
            >
              <Text style={[
                styles.messageText,
                message.sender === 'me' ? styles.myMessageText : styles.otherMessageText
              ]}>
                {message.text}
              </Text>
              <Text style={styles.messageTime}>
                {message.timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
              </Text>
            </View>
          ))
        )}
      </ScrollView>

      {/* Scroll to bottom button */}
      {showScrollButton && (
        <TouchableOpacity
          style={styles.scrollToBottomButton}
          onPress={scrollToBottom}
        >
          <Ionicons name="chevron-down" size={24} color="#fff" />
        </TouchableOpacity>
      )}

      <View style={styles.inputContainer}>
        <TextInput
          style={styles.textInput}
          value={newMessage}
          onChangeText={setNewMessage}
          placeholder="Type a message..."
          placeholderTextColor="#999"
          multiline
          maxLength={500}
          returnKeyType="send"
          onSubmitEditing={handleSendMessage}
          blurOnSubmit={false}
        />
        <TouchableOpacity 
          style={[styles.sendButton, !newMessage.trim() && styles.sendButtonDisabled]}
          onPress={handleSendMessage}
          disabled={!newMessage.trim()}
        >
          <Ionicons name="send" size={20} color="#fff" />
        </TouchableOpacity>
      </View>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
  },
  backButton: {
    marginLeft: 10,
    padding: 5,
  },
  headerButtons: {
    flexDirection: 'row',
    marginRight: 10,
  },
  headerButton: {
    marginLeft: 15,
    padding: 5,
  },
  connectionBanner: {
    backgroundColor: '#f44336',
    paddingVertical: 8,
    paddingHorizontal: 16,
  },
  connectionBannerText: {
    color: '#fff',
    fontSize: 14,
    textAlign: 'center',
  },
  messagesContainer: {
    flex: 1,
    padding: 16,
  },
  messagesContent: {
    paddingBottom: 20,
  },
  messageContainer: {
    marginBottom: 12,
    maxWidth: '80%',
    padding: 12,
    borderRadius: 18,
  },
  myMessage: {
    alignSelf: 'flex-end',
    backgroundColor: '#2196F3',
  },
  otherMessage: {
    alignSelf: 'flex-start',
    backgroundColor: '#f0f0f0',
  },
  messageText: {
    fontSize: 16,
    marginBottom: 4,
  },
  myMessageText: {
    color: '#fff',
  },
  otherMessageText: {
    color: '#333',
  },
  messageTime: {
    fontSize: 12,
    opacity: 0.7,
  },
  inputContainer: {
    flexDirection: 'row',
    padding: 16,
    paddingBottom: Platform.OS === 'ios' ? 20 : 16,
    borderTopWidth: 1,
    borderTopColor: '#e0e0e0',
    alignItems: 'flex-end',
    backgroundColor: '#fff',
  },
  textInput: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 12,
    marginRight: 12,
    maxHeight: 100,
    minHeight: 40,
    fontSize: 16,
    backgroundColor: '#f9f9f9',
  },
  sendButton: {
    backgroundColor: '#2196F3',
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
  },
  sendButtonDisabled: {
    backgroundColor: '#ccc',
  },
  noTargetContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#f5f5f5',
  },
  noTargetText: {
    fontSize: 16,
    color: '#666',
    marginTop: 16,
    textAlign: 'center',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  loadingText: {
    fontSize: 16,
    color: '#666',
    fontStyle: 'italic',
  },
  scrollToBottomButton: {
    position: 'absolute',
    bottom: 80,
    right: 20,
    backgroundColor: '#2196F3',
    width: 50,
    height: 50,
    borderRadius: 25,
    justifyContent: 'center',
    alignItems: 'center',
    elevation: 5,
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 2,
    },
    shadowOpacity: 0.25,
    shadowRadius: 3.84,
  },
});
