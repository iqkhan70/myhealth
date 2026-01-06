import React, { useState, useEffect } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TextInput,
  TouchableOpacity,
  Alert,
  ScrollView,
  Modal,
  ActivityIndicator,
} from 'react-native';

const SmsComponent = ({ visible, onClose, user, selectedContact, apiBaseUrl }) => {
  const [message, setMessage] = useState('');
  const [sending, setSending] = useState(false);

  useEffect(() => {
    if (visible) {
      // Reset message when modal opens
      setMessage('');
    }
  }, [visible]);

  const sendSms = async () => {
    if (!selectedContact) {
      Alert.alert('Error', 'No contact selected. Please select an SME, Attorney, or Doctor first.');
      return;
    }

    if (!message.trim()) {
      Alert.alert('Error', 'Please enter a message');
      return;
    }

    // Get contact display name
    const contactName = selectedContact.roleId === 2 
      ? `Dr. ${selectedContact.firstName} ${selectedContact.lastName}`
      : `${selectedContact.firstName} ${selectedContact.lastName}`;

    setSending(true);
    try {
      const response = await fetch(`${apiBaseUrl}/mobile/send-sms`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${await getAuthToken()}`,
        },
        body: JSON.stringify({
          targetUserId: selectedContact.id,
          message: message.trim(),
        }),
      });

      const data = await response.json();

      if (response.ok) {
        Alert.alert(
          'SMS Sent!',
          `Message sent successfully to ${contactName}`,
          [
            {
              text: 'OK',
              onPress: () => {
                setMessage('');
                onClose();
              },
            },
          ]
        );
      } else {
        Alert.alert('Error', data.message || 'Failed to send SMS');
      }
    } catch (error) {
      console.error('SMS Error:', error);
      Alert.alert('Error', 'Failed to send SMS. Please check your connection.');
    } finally {
      setSending(false);
    }
  };

  const getAuthToken = async () => {
    try {
      const AsyncStorage = require('@react-native-async-storage/async-storage').default;
      return await AsyncStorage.getItem('userToken');
    } catch (error) {
      console.error('Error getting auth token:', error);
      return null;
    }
  };

  const quickMessages = [
    "I need immediate assistance",
    "Time-sensitive issue",
    "Feeling overwhelmed",
    "Service-related issue",
    "Impact from an incident",
    "Discomfort",
    "Assessment",
    "Resolution",
    "Subject Matter Expert",
    "Background information",
    "Professional guidance",
    "Observed issues",
    "Resolution progress",
    "Scheduled review",
    "Recommended action"
  ];

  const insertQuickMessage = (quickMsg) => {
    setMessage(prev => prev ? `${prev} ${quickMsg}` : quickMsg);
  };

  return (
    <Modal visible={visible} animationType="slide" presentationStyle="pageSheet">
      <View style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity onPress={onClose} style={styles.closeButton}>
            <Text style={styles.closeButtonText}>✕</Text>
          </TouchableOpacity>
          <Text style={styles.title}>Send SMS</Text>
          <View style={styles.placeholder} />
        </View>

        <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
          {/* Selected Contact Display */}
          {selectedContact && (
            <View style={styles.section}>
              <Text style={styles.sectionTitle}>Sending SMS To</Text>
              <View style={styles.selectedContactCard}>
                <Text style={styles.contactName}>
                  {selectedContact.roleId === 2 
                    ? `Dr. ${selectedContact.firstName} ${selectedContact.lastName}`
                    : `${selectedContact.firstName} ${selectedContact.lastName}`}
                </Text>
                <Text style={styles.contactRole}>
                  {selectedContact.roleName || 'Contact'}
                </Text>
                {selectedContact.mobilePhone && (
                  <Text style={styles.contactPhone}>
                    📱 {selectedContact.mobilePhone}
                  </Text>
                )}
              </View>
            </View>
          )}

          {/* Quick Messages */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Quick Messages</Text>
            <View style={styles.quickMessagesContainer}>
              {quickMessages.map((quickMsg, index) => (
                <TouchableOpacity
                  key={index}
                  style={styles.quickMessageButton}
                  onPress={() => insertQuickMessage(quickMsg)}
                >
                  <Text style={styles.quickMessageText}>{quickMsg}</Text>
                </TouchableOpacity>
              ))}
            </View>
          </View>

          {/* Message Input */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Your Message</Text>
            <TextInput
              style={styles.messageInput}
              value={message}
              onChangeText={setMessage}
              placeholder="Type your message here..."
              multiline
              numberOfLines={4}
              textAlignVertical="top"
            />
            <Text style={styles.characterCount}>
              {message.length}/160 characters
            </Text>
          </View>

          {/* Send Button */}
          <TouchableOpacity
            style={[styles.sendButton, (sending || !selectedContact || !message.trim()) && styles.disabledButton]}
            onPress={sendSms}
            disabled={sending || !selectedContact || !message.trim()}
          >
            {sending ? (
              <ActivityIndicator color="white" />
            ) : (
              <Text style={styles.sendButtonText}>Send SMS</Text>
            )}
          </TouchableOpacity>

          {/* Info */}
          <View style={styles.infoSection}>
            <Text style={styles.infoText}>
              📱 This will send an SMS directly to the selected contact's phone number
            </Text>
            <Text style={styles.infoText}>
              ⚠️ Use this for urgent matters only
            </Text>
          </View>
        </ScrollView>
      </View>
    </Modal>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8f9fa',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 20,
    paddingVertical: 15,
    backgroundColor: 'white',
    borderBottomWidth: 1,
    borderBottomColor: '#e0e0e0',
  },
  closeButton: {
    width: 30,
    height: 30,
    alignItems: 'center',
    justifyContent: 'center',
  },
  closeButtonText: {
    fontSize: 18,
    color: '#666',
  },
  title: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
  },
  placeholder: {
    width: 30,
  },
  content: {
    flex: 1,
    padding: 20,
  },
  section: {
    marginBottom: 25,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
    marginBottom: 10,
  },
  selectedContactCard: {
    backgroundColor: 'white',
    padding: 15,
    borderRadius: 10,
    borderWidth: 2,
    borderColor: '#007bff',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  contactName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
    marginBottom: 5,
  },
  contactRole: {
    fontSize: 14,
    color: '#666',
    marginBottom: 5,
  },
  contactPhone: {
    fontSize: 12,
    color: '#666',
  },
  quickMessagesContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  quickMessageButton: {
    backgroundColor: '#e3f2fd',
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: '#bbdefb',
  },
  quickMessageText: {
    fontSize: 12,
    color: '#1976d2',
  },
  messageInput: {
    backgroundColor: 'white',
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 10,
    padding: 15,
    fontSize: 16,
    minHeight: 100,
    textAlignVertical: 'top',
  },
  characterCount: {
    fontSize: 12,
    color: '#666',
    textAlign: 'right',
    marginTop: 5,
  },
  sendButton: {
    backgroundColor: '#28a745',
    padding: 15,
    borderRadius: 10,
    alignItems: 'center',
    marginTop: 10,
  },
  disabledButton: {
    backgroundColor: '#6c757d',
  },
  sendButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  infoSection: {
    backgroundColor: '#fff3cd',
    padding: 15,
    borderRadius: 10,
    marginTop: 20,
    borderLeftWidth: 4,
    borderLeftColor: '#ffc107',
  },
  infoText: {
    fontSize: 14,
    color: '#856404',
    marginBottom: 5,
  },
});

export default SmsComponent;
