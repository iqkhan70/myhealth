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

const EmergencyComponent = ({ visible, onClose, user, selectedContact, apiBaseUrl, deviceToken, onEmergencySent }) => {
  const [emergencyMessage, setEmergencyMessage] = useState('');
  const [severity, setSeverity] = useState('High');
  const [sending, setSending] = useState(false);

  useEffect(() => {
    if (visible) {
      // Reset form when modal opens
      setEmergencyMessage('');
      setSeverity('High');
    }
  }, [visible]);

  const sendEmergency = async () => {
    if (!selectedContact) {
      Alert.alert('Error', 'No contact selected. Please select an SME, Attorney, or Doctor first.');
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

    // Get contact display name
    const contactName = selectedContact.roleId === 2 
      ? `Dr. ${selectedContact.firstName} ${selectedContact.lastName}`
      : `${selectedContact.firstName} ${selectedContact.lastName}`;

    // Confirm before sending
    Alert.alert(
      'Send Emergency Alert?',
      `This will send an emergency alert to ${contactName} and log an incident in the emergency dashboard.\n\nSeverity: ${severity}\n\nContinue?`,
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

              // Build request body - vital signs are optional and not relevant for chiropractor emergencies
              const requestBody = {
                deviceToken: deviceToken,
                deviceId: storedDeviceId,
                emergencyType: 'Other', // Use 'Other' for custom messages
                severity: severity,
                message: emergencyMessage.trim(),
                // Vital signs removed - not relevant for chiropractor use case
                // If needed in future, can be added as optional fields
                latitude: 0, // TODO: Get actual location if available
                longitude: 0
              };

              const response = await fetch(`${apiBaseUrl}/emergency/test-emergency`, {
                method: 'POST',
                headers: {
                  'Content-Type': 'application/json',
                },
                body: JSON.stringify(requestBody)
              });

              const data = await response.json();

              if (data.success) {
                Alert.alert(
                  'Emergency Sent!',
                  `Emergency alert sent successfully to ${contactName}.\n\nIncident ID: ${data.incidentId}\n\nThe incident has been logged and will appear in the emergency dashboard.`,
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
    "Incident requiring immediate attention",
    "Work-related issue requiring urgent follow-up",
    "Significant discomfort following recent service",
    "Incident at a residence - reporting for documentation",
    "Activity-related issue requiring consultation",
    "Other-related incident",
    "Other time-sensitive situation"
  ];

  const insertQuickMessage = (quickMsg) => {
    setEmergencyMessage(quickMsg);
  };

  // For emergency situations, we primarily use High and Critical
  // Low and Medium are available but less prominent for non-urgent reporting
  const severityOptions = [
    { label: 'High', value: 'High', color: '#fd7e14', recommended: true },
    { label: 'Critical', value: 'Critical', color: '#dc3545', recommended: true },
    { label: 'Medium', value: 'Medium', color: '#ffc107', recommended: false },
    { label: 'Low', value: 'Low', color: '#28a745', recommended: false },
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
          {/* Selected Contact Display */}
          {selectedContact && (
            <View style={styles.section}>
              <Text style={styles.sectionTitle}>Sending Emergency Alert To</Text>
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

          {/* Severity Selection */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Severity Level</Text>
            <Text style={styles.sectionSubtitle}>
              Select the urgency level. High and Critical are recommended for emergencies.
            </Text>
            <View style={styles.severityContainer}>
              {severityOptions.map((option) => (
                <TouchableOpacity
                  key={option.value}
                  style={[
                    styles.severityButton,
                    severity === option.value && styles.selectedSeverityButton,
                    { borderColor: option.color },
                    !option.recommended && styles.lowPrioritySeverity
                  ]}
                  onPress={() => setSeverity(option.value)}
                >
                  <Text style={[
                    styles.severityText,
                    severity === option.value && { color: option.color, fontWeight: '600' },
                    !option.recommended && styles.lowPriorityText
                  ]}>
                    {option.label}
                  </Text>
                  {option.recommended && (
                    <Text style={styles.recommendedBadge}>Recommended</Text>
                  )}
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
            style={[styles.sendButton, (sending || !selectedContact || !emergencyMessage.trim()) && styles.disabledButton]}
            onPress={sendEmergency}
            disabled={sending || !selectedContact || !emergencyMessage.trim()}
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
              • This emergency alert will be sent to the selected contact
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
  selectedContactCard: {
    backgroundColor: 'white',
    padding: 15,
    borderRadius: 10,
    borderWidth: 2,
    borderColor: '#dc3545',
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
  severityContainer: {
    flexDirection: 'row',
    flexWrap: 'nowrap',
    justifyContent: 'space-between',
    gap: 8,
  },
  severityButton: {
    backgroundColor: 'white',
    paddingHorizontal: 10,
    paddingVertical: 10,
    borderRadius: 20,
    borderWidth: 2,
    flex: 1,
    minWidth: 0,
    alignItems: 'center',
    position: 'relative',
    marginHorizontal: 2,
  },
  selectedSeverityButton: {
    backgroundColor: '#fff3cd',
  },
  lowPrioritySeverity: {
    opacity: 0.6,
    borderStyle: 'dashed',
  },
  severityText: {
    fontSize: 12,
    color: '#666',
  },
  lowPriorityText: {
    fontSize: 11,
  },
  recommendedBadge: {
    fontSize: 10,
    color: '#28a745',
    fontWeight: '700',
    marginTop: 2,
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

