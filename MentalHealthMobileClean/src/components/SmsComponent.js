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

const SmsComponent = ({ visible, onClose, user, contacts, apiBaseUrl }) => {
  const [selectedDoctor, setSelectedDoctor] = useState(null);
  const [message, setMessage] = useState('');
  const [sending, setSending] = useState(false);
  const [doctors, setDoctors] = useState([]);

  useEffect(() => {
    if (visible && contacts.length > 0) {
      // Filter contacts to get only doctors
      const doctorContacts = contacts.filter(contact => 
        contact.roleId === 2 || contact.roleName === 'Doctor'
      );
      setDoctors(doctorContacts);
      
      // Auto-select first doctor if only one
      if (doctorContacts.length === 1) {
        setSelectedDoctor(doctorContacts[0]);
      }
    }
  }, [visible, contacts]);

  const sendSms = async () => {
    if (!selectedDoctor) {
      Alert.alert('Error', 'Please select a doctor to send SMS to');
      return;
    }

    if (!message.trim()) {
      Alert.alert('Error', 'Please enter a message');
      return;
    }

    setSending(true);
    try {
      const response = await fetch(`${apiBaseUrl}/mobile/send-sms`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${await getAuthToken()}`,
        },
        body: JSON.stringify({
          targetUserId: selectedDoctor.id,
          message: message.trim(),
        }),
      });

      const data = await response.json();

      if (response.ok) {
        Alert.alert(
          'SMS Sent!',
          `Message sent successfully to Dr. ${selectedDoctor.firstName} ${selectedDoctor.lastName}`,
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
    "I need urgent help",
    "Having a panic attack",
    "Feeling very anxious",
    "Need to talk to you",
    "Having trouble sleeping",
    "Feeling depressed",
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
          <Text style={styles.title}>Send SMS to Doctor</Text>
          <View style={styles.placeholder} />
        </View>

        <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
          {/* Doctor Selection */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Select Doctor</Text>
            {doctors.length === 0 ? (
              <Text style={styles.noDoctorsText}>No doctors available</Text>
            ) : (
              <ScrollView horizontal showsHorizontalScrollIndicator={false}>
                {doctors.map((doctor) => (
                  <TouchableOpacity
                    key={doctor.id}
                    style={[
                      styles.doctorCard,
                      selectedDoctor?.id === doctor.id && styles.selectedDoctorCard,
                    ]}
                    onPress={() => setSelectedDoctor(doctor)}
                  >
                    <Text style={styles.doctorName}>
                      Dr. {doctor.firstName} {doctor.lastName}
                    </Text>
                    <Text style={styles.doctorPhone}>
                      {doctor.mobilePhone || 'No phone number'}
                    </Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            )}
          </View>

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
            style={[styles.sendButton, sending && styles.disabledButton]}
            onPress={sendSms}
            disabled={sending || !selectedDoctor || !message.trim()}
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
              📱 This will send an SMS directly to your doctor's phone number
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
  noDoctorsText: {
    fontSize: 14,
    color: '#666',
    textAlign: 'center',
    padding: 20,
  },
  doctorCard: {
    backgroundColor: 'white',
    padding: 15,
    borderRadius: 10,
    marginRight: 10,
    minWidth: 150,
    borderWidth: 2,
    borderColor: 'transparent',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  selectedDoctorCard: {
    borderColor: '#007bff',
    backgroundColor: '#f0f8ff',
  },
  doctorName: {
    fontSize: 14,
    fontWeight: '600',
    color: '#333',
    marginBottom: 5,
  },
  doctorPhone: {
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
