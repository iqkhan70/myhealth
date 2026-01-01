import React, { useState } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  Alert,
  ActivityIndicator,
} from 'react-native';
import ServiceRequestService from '../services/ServiceRequestService';

const CreateServiceRequestForm = ({ user, onSuccess, onCancel }) => {
  const [title, setTitle] = useState('');
  const [type, setType] = useState('General');
  const [description, setDescription] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const typeOptions = [
    'General',
    'Medical',
    'Legal',
    'Therapy',
    'Consultation',
    'Follow-up',
    'Emergency',
  ];

  const handleSubmit = async () => {
    if (!title.trim()) {
      Alert.alert('Validation Error', 'Please enter a title for the service request.');
      return;
    }

    if (!user || !user.id) {
      Alert.alert('Error', 'User information is missing. Please try again.');
      return;
    }

    setIsSubmitting(true);

    try {
      const serviceRequestData = {
        clientId: user.id,
        title: title.trim(),
        type: type || 'General',
        description: description.trim() || null,
        status: 'Active',
        smeUserId: null, // Patients cannot assign SMEs
      };

      await ServiceRequestService.createServiceRequest(serviceRequestData);
      
      Alert.alert(
        'Success',
        'Service request created successfully. A coordinator will review and assign it to an appropriate SME.',
        [
          {
            text: 'OK',
            onPress: () => {
              setTitle('');
              setType('General');
              setDescription('');
              if (onSuccess) {
                onSuccess();
              }
            },
          },
        ]
      );
    } catch (error) {
      console.error('Error creating service request:', error);
      Alert.alert(
        'Error',
        error.message || 'Failed to create service request. Please try again.'
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.contentContainer}>
      <View style={styles.form}>
        <Text style={styles.label}>Title <Text style={styles.required}>*</Text></Text>
        <TextInput
          style={styles.input}
          value={title}
          onChangeText={setTitle}
          placeholder="Enter service request title..."
          maxLength={200}
          editable={!isSubmitting}
        />

        <Text style={styles.label}>Type</Text>
        <View style={styles.typeContainer}>
          {typeOptions.map((option) => (
            <TouchableOpacity
              key={option}
              style={[
                styles.typeButton,
                type === option && styles.typeButtonActive,
              ]}
              onPress={() => setType(option)}
              disabled={isSubmitting}
            >
              <Text
                style={[
                  styles.typeButtonText,
                  type === option && styles.typeButtonTextActive,
                ]}
              >
                {option}
              </Text>
            </TouchableOpacity>
          ))}
        </View>

        <Text style={styles.label}>Description (Optional)</Text>
        <TextInput
          style={[styles.input, styles.textArea]}
          value={description}
          onChangeText={setDescription}
          placeholder="Enter description..."
          multiline
          numberOfLines={4}
          maxLength={1000}
          editable={!isSubmitting}
        />

        <View style={styles.buttonContainer}>
          <TouchableOpacity
            style={[styles.button, styles.cancelButton]}
            onPress={onCancel}
            disabled={isSubmitting}
          >
            <Text style={styles.cancelButtonText}>Cancel</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.button, styles.submitButton, isSubmitting && styles.buttonDisabled]}
            onPress={handleSubmit}
            disabled={isSubmitting}
          >
            {isSubmitting ? (
              <ActivityIndicator color="#fff" />
            ) : (
              <Text style={styles.submitButtonText}>Create</Text>
            )}
          </TouchableOpacity>
        </View>

        <View style={styles.infoBox}>
          <Text style={styles.infoText}>
            ℹ️ After creating this service request, a coordinator will review it and assign it to an appropriate SME.
          </Text>
        </View>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  contentContainer: {
    padding: 16,
  },
  form: {
    backgroundColor: '#fff',
    borderRadius: 8,
    padding: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  label: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
    marginBottom: 8,
    marginTop: 12,
  },
  required: {
    color: '#dc3545',
  },
  input: {
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 6,
    padding: 12,
    fontSize: 16,
    backgroundColor: '#fff',
  },
  textArea: {
    minHeight: 100,
    textAlignVertical: 'top',
  },
  typeContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    marginTop: 8,
  },
  typeButton: {
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: '#007bff',
    backgroundColor: '#fff',
  },
  typeButtonActive: {
    backgroundColor: '#007bff',
  },
  typeButtonText: {
    color: '#007bff',
    fontSize: 14,
    fontWeight: '500',
  },
  typeButtonTextActive: {
    color: '#fff',
  },
  buttonContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 24,
    gap: 12,
  },
  button: {
    flex: 1,
    paddingVertical: 12,
    borderRadius: 6,
    alignItems: 'center',
    justifyContent: 'center',
  },
  cancelButton: {
    backgroundColor: '#6c757d',
  },
  submitButton: {
    backgroundColor: '#28a745',
  },
  buttonDisabled: {
    opacity: 0.6,
  },
  cancelButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  submitButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  infoBox: {
    marginTop: 16,
    padding: 12,
    backgroundColor: '#e7f3ff',
    borderRadius: 6,
    borderLeftWidth: 4,
    borderLeftColor: '#007bff',
  },
  infoText: {
    fontSize: 14,
    color: '#004085',
    lineHeight: 20,
  },
});

export default CreateServiceRequestForm;

