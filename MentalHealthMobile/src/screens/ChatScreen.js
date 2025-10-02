import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  KeyboardAvoidingView,
  Platform,
  TouchableOpacity,
  Alert,
} from 'react-native';
import { GiftedChat, Bubble, InputToolbar, Send } from 'react-native-gifted-chat';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '../context/AuthContext';
import { useSocket } from '../context/SocketContext';

export default function ChatScreen({ route, navigation }) {
  const [messages, setMessages] = useState([]);
  const [isTyping, setIsTyping] = useState(false);
  const { user } = useAuth();
  const { sendMessage, messages: socketMessages, isConnected, initiateCall } = useSocket();
  const { targetUser, chatType } = route.params || {};

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

    // Load initial messages
    loadChatHistory();
  }, [targetUser, user.roleId]);

  useEffect(() => {
    // Filter messages for this conversation
    if (targetUser && socketMessages.length > 0) {
      const conversationMessages = socketMessages.filter(msg => 
        (msg.senderId === targetUser.id && msg.targetUserId === user.id) ||
        (msg.senderId === user.id && msg.targetUserId === targetUser.id)
      );
      
      const formattedMessages = conversationMessages.map(msg => ({
        _id: msg.id || Math.random().toString(),
        text: msg.message,
        createdAt: new Date(msg.timestamp),
        user: {
          _id: msg.senderId,
          name: msg.senderName,
          avatar: getAvatarUrl(msg.senderId),
        },
      }));

      setMessages(previousMessages => 
        GiftedChat.append(previousMessages, formattedMessages)
      );
    }
  }, [socketMessages, targetUser, user.id]);

  const loadChatHistory = async () => {
    // TODO: Load chat history from server
    // For now, set some initial messages
    setMessages([
      {
        _id: 1,
        text: 'Hello! How can I help you today?',
        createdAt: new Date(),
        user: {
          _id: targetUser?.id || 2,
          name: targetUser ? `${targetUser.firstName} ${targetUser.lastName}` : 'Doctor',
          avatar: getAvatarUrl(targetUser?.id || 2),
        },
      },
    ]);
  };

  const getAvatarUrl = (userId) => {
    // Generate a simple avatar based on user ID
    const colors = ['#2196F3', '#4CAF50', '#FF9800', '#9C27B0', '#F44336'];
    const color = colors[userId % colors.length];
    return `https://ui-avatars.com/api/?name=${userId}&background=${color.substring(1)}&color=fff`;
  };

  const onSend = useCallback((newMessages = []) => {
    if (!targetUser) {
      Alert.alert('Error', 'No target user selected');
      return;
    }

    if (!isConnected) {
      Alert.alert('Connection Error', 'Not connected to server. Please check your internet connection.');
      return;
    }

    const message = newMessages[0];
    
    // Send message through socket
    sendMessage(targetUser.id, message.text);
    
    // Add to local messages
    setMessages(previousMessages => GiftedChat.append(previousMessages, newMessages));
  }, [targetUser, isConnected, sendMessage]);

  const handleVideoCall = () => {
    if (!targetUser) {
      Alert.alert('Error', 'No target user selected');
      return;
    }

    if (!isConnected) {
      Alert.alert('Connection Error', 'Not connected to server. Please check your internet connection.');
      return;
    }

    const callType = 'video';
    const targetName = `${targetUser.firstName} ${targetUser.lastName}`;
    
    Alert.alert(
      'Video Call',
      `Start video call with ${targetName}?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Call',
          onPress: () => {
            initiateCall(targetUser.id, callType);
            navigation.navigate('VideoCall', { 
              [user.roleId === 2 ? 'patient' : 'doctor']: targetUser,
              callType,
              isInitiator: true 
            });
          },
        },
      ]
    );
  };

  const handleAudioCall = () => {
    if (!targetUser) {
      Alert.alert('Error', 'No target user selected');
      return;
    }

    if (!isConnected) {
      Alert.alert('Connection Error', 'Not connected to server. Please check your internet connection.');
      return;
    }

    const callType = 'audio';
    const targetName = `${targetUser.firstName} ${targetUser.lastName}`;
    
    Alert.alert(
      'Audio Call',
      `Start audio call with ${targetName}?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Call',
          onPress: () => {
            initiateCall(targetUser.id, callType);
            navigation.navigate('AudioCall', { 
              [user.roleId === 2 ? 'patient' : 'doctor']: targetUser,
              callType,
              isInitiator: true 
            });
          },
        },
      ]
    );
  };

  const renderBubble = (props) => {
    return (
      <Bubble
        {...props}
        wrapperStyle={{
          right: {
            backgroundColor: user.roleId === 2 ? '#2196F3' : '#4CAF50',
          },
          left: {
            backgroundColor: '#f0f0f0',
          },
        }}
        textStyle={{
          right: {
            color: '#fff',
          },
          left: {
            color: '#333',
          },
        }}
      />
    );
  };

  const renderInputToolbar = (props) => {
    return (
      <InputToolbar
        {...props}
        containerStyle={styles.inputToolbar}
        primaryStyle={styles.inputPrimary}
      />
    );
  };

  const renderSend = (props) => {
    return (
      <Send {...props}>
        <View style={styles.sendButton}>
          <Ionicons 
            name="send" 
            size={20} 
            color={user.roleId === 2 ? '#2196F3' : '#4CAF50'} 
          />
        </View>
      </Send>
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
    <View style={styles.container}>
      {!isConnected && (
        <View style={styles.connectionBanner}>
          <Text style={styles.connectionBannerText}>
            Disconnected - Messages may not be delivered
          </Text>
        </View>
      )}
      
      <KeyboardAvoidingView
        style={styles.chatContainer}
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        keyboardVerticalOffset={Platform.OS === 'ios' ? 90 : 0}
      >
        <GiftedChat
          messages={messages}
          onSend={onSend}
          user={{
            _id: user.id,
            name: `${user.firstName} ${user.lastName}`,
            avatar: getAvatarUrl(user.id),
          }}
          renderBubble={renderBubble}
          renderInputToolbar={renderInputToolbar}
          renderSend={renderSend}
          alwaysShowSend
          scrollToBottom
          isTyping={isTyping}
          placeholder="Type a message..."
          showUserAvatar
          renderUsernameOnMessage
        />
      </KeyboardAvoidingView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
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
  chatContainer: {
    flex: 1,
  },
  inputToolbar: {
    borderTopWidth: 1,
    borderTopColor: '#e0e0e0',
    backgroundColor: '#fff',
    paddingHorizontal: 8,
  },
  inputPrimary: {
    alignItems: 'center',
  },
  sendButton: {
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 10,
    marginBottom: 5,
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: '#f0f0f0',
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
});
