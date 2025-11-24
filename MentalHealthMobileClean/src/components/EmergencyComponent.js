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
  Platform,
} from 'react-native';

const EmergencyComponent = ({ visible, onClose, user, contacts, apiBaseUrl, deviceToken, onEmergencySent }) => {
  const [selectedDoctor, setSelectedDoctor] = useState(null);
  const [emergencyMessage, setEmergencyMessage] = useState('');
  const [severity, setSeverity] = useState('High');
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
      
      // Reset form when modal opens
      setEmergencyMessage('');
      setSeverity('High');
    }
  }, [visible, contacts]);

  const sendEmergency = async () => {
    if (!selectedDoctor) {
      Alert.alert('Error', 'Please select a doctor to send emergency alert to');
      return;
    }

    if (!emergencyMessage.trim()) {
      Alert.alert('Error', 'Please describe the emergency situation');
      return;
    }

    if (!deviceToken) {
      Alert.alert('Error', 'Device not registered. Please wait a moment and try again.');
      return;
    }

    // Confirm before sending
    Alert.alert(
      'Send Emergency Alert?',
      `This will send an emergency alert to Dr. ${selectedDoctor.firstName} ${selectedDoctor.lastName} and log an incident in the emergency dashboard.\n\nSeverity: ${severity}\n\nContinue?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Send Emergency',
          style: 'destructive',
          onPress: async () => {
            setSending(true);
            try {
              const AsyncStorage = require('@react-native-async-storage/async-storage').default;
              const storedDeviceId = await AsyncStorage.getItem('emergencyDeviceId') || `device_${user.id}_${Platform.OS}`;

              const response = await fetch(`${apiBaseUrl}/emergency/test-emergency`, {
                method: 'POST',
                headers: {
                  'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                  deviceToken: deviceToken,
                  deviceId: storedDeviceId,
                  emergencyType: 'Other', // Use 'Other' for custom messages
                  severity: severity,
                  message: emergencyMessage.trim(),
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
                  `Emergency alert sent successfully to Dr. ${selectedDoctor.firstName} ${selectedDoctor.lastName}.\n\nIncident ID: ${data.incidentId}\n\nThe incident has been logged and will appear in the emergency dashboard.`,
                  [
                    {
                      text: 'OK',
                      onPress: () => {
                        setEmergencyMessage('');
                        if (onEmergencySent) {
                          onEmergencySent();
                        }
                        onClose();
                      },
                    },
                  ]
                );
              } else {
                throw new Error(data.message || data.reason || 'Emergency sending failed');
              }
            } catch (error) {
              console.error('Emergency Error:', error);
              let errorMessage = error.message || 'Please check your connection and try again.';
              
              // If device token is invalid, notify user
              if (errorMessage.includes('Invalid') || errorMessage.includes('device token')) {
                errorMessage = 'Device registration issue. Please try again.';
              }
              
              Alert.alert('Error', `Failed to send emergency alert: ${errorMessage}`);
            } finally {
              setSending(false);
            }
          }
        }
      ]
    );
  };

  const quickMessages = [
    "Car accident - need immediate attention",
    "Workplace injury - urgent follow-up needed",
    "Severe pain after treatment",
    "Accident at home - reporting for records",
    "Sports injury - need consultation",
    "Slip and fall incident",
    "Other emergency situation",
  ];

  const insertQuickMessage = (quickMsg) => {
    setEmergencyMessage(quickMsg);
  };

  const severityOptions = [
    { label: 'Low', value: 'Low', color: '#28a745' },
    { label: 'Medium', value: 'Medium', color: '#ffc107' },
    { label: 'High', value: 'High', color: '#fd7e14' },
    { label: 'Critical', value: 'Critical', color: '#dc3545' },
  ];

  return (
    <Modal visible={visible} animationType="slide" presentationStyle="pageSheet">
      <View style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity onPress={onClose} style={styles.closeButton}>
            <Text style={styles.closeButtonText}>✕</Text>
          </TouchableOpacity>
          <Text style={styles.title}>🚨 Emergency Alert</Text>
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

          {/* Severity Selection */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Severity Level</Text>
            <View style={styles.severityContainer}>
              {severityOptions.map((option) => (
                <TouchableOpacity
                  key={option.value}
                  style={[
                    styles.severityButton,
                    severity === option.value && styles.selectedSeverityButton,
                    { borderColor: option.color }
                  ]}
                  onPress={() => setSeverity(option.value)}
                >
                  <Text style={[
                    styles.severityText,
                    severity === option.value && { color: option.color, fontWeight: '600' }
                  ]}>
                    {option.label}
                  </Text>
                </TouchableOpacity>
              ))}
            </View>
          </View>

          {/* Quick Messages */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Quick Templates</Text>
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

          {/* Emergency Message Input */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Describe the Emergency</Text>
            <Text style={styles.sectionSubtitle}>
              Provide details about the accident, injury, or situation that requires immediate attention
            </Text>
            <TextInput
              style={styles.messageInput}
              value={emergencyMessage}
              onChangeText={setEmergencyMessage}
              placeholder="Example: Car accident occurred at 3pm. Experiencing neck pain and headache. Need immediate consultation..."
              multiline
              numberOfLines={6}
              textAlignVertical="top"
            />
            <Text style={styles.characterCount}>
              {emergencyMessage.length} characters
            </Text>
          </View>

          {/* Send Button */}
          <TouchableOpacity
            style={[styles.sendButton, (sending || !selectedDoctor || !emergencyMessage.trim()) && styles.disabledButton]}
            onPress={sendEmergency}
            disabled={sending || !selectedDoctor || !emergencyMessage.trim()}
          >
            {sending ? (
              <ActivityIndicator color="white" />
            ) : (
              <Text style={styles.sendButtonText}>🚨 Send Emergency Alert</Text>
            )}
          </TouchableOpacity>

          {/* Info */}
          <View style={styles.infoSection}>
            <Text style={styles.infoTitle}>⚠️ Important Information</Text>
            <Text style={styles.infoText}>
              • This emergency alert will be sent to the selected doctor
            </Text>
            <Text style={styles.infoText}>
              • The incident will be logged in the emergency dashboard
            </Text>
            <Text style={styles.infoText}>
              • Use this for accidents, injuries, or urgent situations requiring immediate attention
            </Text>
            <Text style={styles.infoText}>
              • For non-urgent matters, please use regular messaging
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
    backgroundColor: '#fff3cd',
    borderBottomWidth: 2,
    borderBottomColor: '#ffc107',
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
    fontWeight: '700',
    color: '#dc3545',
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
  sectionSubtitle: {
    fontSize: 13,
    color: '#666',
    marginBottom: 10,
    fontStyle: 'italic',
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
    borderColor: '#dc3545',
    backgroundColor: '#ffebee',
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
  severityContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 10,
  },
  severityButton: {
    backgroundColor: 'white',
    paddingHorizontal: 15,
    paddingVertical: 10,
    borderRadius: 20,
    borderWidth: 2,
    minWidth: 80,
    alignItems: 'center',
  },
  selectedSeverityButton: {
    backgroundColor: '#fff3cd',
  },
  severityText: {
    fontSize: 14,
    color: '#666',
  },
  quickMessagesContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  quickMessageButton: {
    backgroundColor: '#ffebee',
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: '#ffcdd2',
  },
  quickMessageText: {
    fontSize: 12,
    color: '#c62828',
  },
  messageInput: {
    backgroundColor: 'white',
    borderWidth: 2,
    borderColor: '#ffcdd2',
    borderRadius: 10,
    padding: 15,
    fontSize: 16,
    minHeight: 120,
    textAlignVertical: 'top',
  },
  characterCount: {
    fontSize: 12,
    color: '#666',
    textAlign: 'right',
    marginTop: 5,
  },
  sendButton: {
    backgroundColor: '#dc3545',
    padding: 18,
    borderRadius: 10,
    alignItems: 'center',
    marginTop: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.3,
    shadowRadius: 4,
    elevation: 5,
  },
  disabledButton: {
    backgroundColor: '#6c757d',
    shadowOpacity: 0,
    elevation: 0,
  },
  sendButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '700',
  },
  infoSection: {
    backgroundColor: '#fff3cd',
    padding: 15,
    borderRadius: 10,
    marginTop: 20,
    borderLeftWidth: 4,
    borderLeftColor: '#ffc107',
  },
  infoTitle: {
    fontSize: 14,
    fontWeight: '700',
    color: '#856404',
    marginBottom: 8,
  },
  infoText: {
    fontSize: 13,
    color: '#856404',
    marginBottom: 5,
    lineHeight: 20,
  },
});

export default EmergencyComponent;

