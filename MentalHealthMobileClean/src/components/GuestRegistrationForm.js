import React, { useState } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TextInput,
  TouchableOpacity,
  Alert,
  ScrollView,
  Platform,
} from 'react-native';
import AppConfig from '../config/app.config';

const GuestRegistrationForm = ({ onBack, onSuccess }) => {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [gender, setGender] = useState('');
  const [mobilePhone, setMobilePhone] = useState('');
  const [reason, setReason] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const getApiBaseUrl = () => {
    return AppConfig.getMobileApiBaseUrl();
  };

  const validateForm = () => {
    if (!firstName.trim()) {
      Alert.alert('Validation Error', 'First name is required');
      return false;
    }
    if (!lastName.trim()) {
      Alert.alert('Validation Error', 'Last name is required');
      return false;
    }
    if (!email.trim()) {
      Alert.alert('Validation Error', 'Email is required');
      return false;
    }
    if (!email.includes('@')) {
      Alert.alert('Validation Error', 'Please enter a valid email address');
      return false;
    }
    if (!dateOfBirth) {
      Alert.alert('Validation Error', 'Date of birth is required');
      return false;
    }
    if (!gender.trim()) {
      Alert.alert('Validation Error', 'Gender is required');
      return false;
    }
    if (!mobilePhone.trim()) {
      Alert.alert('Validation Error', 'Mobile phone is required');
      return false;
    }
    if (!reason.trim()) {
      Alert.alert('Validation Error', 'Reason for request is required');
      return false;
    }
    if (reason.trim().length < 10) {
      Alert.alert('Validation Error', 'Please provide a more detailed reason (at least 10 characters)');
      return false;
    }
    return true;
  };

  const handleSubmit = async () => {
    if (!validateForm()) {
      return;
    }

    setSubmitting(true);
    try {
      const apiUrl = getApiBaseUrl();
      // Parse and validate date
      const dateObj = new Date(dateOfBirth);
      if (isNaN(dateObj.getTime())) {
        Alert.alert('Validation Error', 'Please enter a valid date in YYYY-MM-DD format');
        setSubmitting(false);
        return;
      }

      const response = await fetch(`${apiUrl}/UserRequest/create`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          email: email.trim().toLowerCase(),
          dateOfBirth: dateObj.toISOString(),
          gender: gender.trim(),
          mobilePhone: mobilePhone.trim(),
          reason: reason.trim(),
        }),
      });

      const data = await response.json();

      if (response.ok) {
        Alert.alert(
          'Request Submitted',
          'Your registration request has been submitted successfully. You will receive an SMS and email with your login credentials once your request is approved by an administrator.',
          [
            {
              text: 'OK',
              onPress: () => {
                if (onSuccess) onSuccess();
                if (onBack) onBack();
              },
            },
          ]
        );
      } else {
        const errorMessage = data.message || 'Failed to submit registration request. Please try again.';
        Alert.alert('Error', errorMessage);
      }
    } catch (error) {
      console.error('Guest registration error:', error);
      Alert.alert(
        'Error',
        'Network error. Please check your connection and try again.'
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.contentContainer}>
      <View style={styles.header}>
        <Text style={styles.title}>Guest Registration</Text>
        <Text style={styles.subtitle}>Please fill in all required information</Text>
      </View>

      <View style={styles.form}>
        <View style={styles.inputGroup}>
          <Text style={styles.label}>First Name *</Text>
          <TextInput
            style={styles.input}
            placeholder="Enter first name"
            value={firstName}
            onChangeText={setFirstName}
            autoCapitalize="words"
          />
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.label}>Last Name *</Text>
          <TextInput
            style={styles.input}
            placeholder="Enter last name"
            value={lastName}
            onChangeText={setLastName}
            autoCapitalize="words"
          />
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.label}>Email *</Text>
          <TextInput
            style={styles.input}
            placeholder="Enter email address"
            value={email}
            onChangeText={setEmail}
            autoCapitalize="none"
            keyboardType="email-address"
          />
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.label}>Date of Birth *</Text>
          <TextInput
            style={styles.input}
            placeholder="YYYY-MM-DD"
            value={dateOfBirth}
            onChangeText={setDateOfBirth}
            keyboardType="default"
          />
          <Text style={styles.hint}>Format: YYYY-MM-DD (e.g., 1990-01-15)</Text>
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.label}>Gender *</Text>
          <View style={styles.genderContainer}>
            {['Male', 'Female', 'Other', 'Prefer not to say'].map((option) => (
              <TouchableOpacity
                key={option}
                style={[
                  styles.genderOption,
                  gender === option && styles.genderOptionSelected,
                ]}
                onPress={() => setGender(option)}
              >
                <Text
                  style={[
                    styles.genderOptionText,
                    gender === option && styles.genderOptionTextSelected,
                  ]}
                >
                  {option}
                </Text>
              </TouchableOpacity>
            ))}
          </View>
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.label}>Mobile Phone *</Text>
          <TextInput
            style={styles.input}
            placeholder="Enter mobile phone number"
            value={mobilePhone}
            onChangeText={setMobilePhone}
            keyboardType="phone-pad"
          />
          <Text style={styles.hint}>Include country code (e.g., +1234567890)</Text>
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.label}>Reason for Request *</Text>
          <TextInput
            style={[styles.input, styles.textArea]}
            placeholder="Please describe why you need access to the health app (e.g., need to consult with a doctor, manage health records, etc.)"
            value={reason}
            onChangeText={setReason}
            multiline
            numberOfLines={4}
            textAlignVertical="top"
          />
          <Text style={styles.hint}>Minimum 10 characters. This helps us assign you to the appropriate doctor.</Text>
          <Text style={styles.characterCount}>
            {reason.length}/1000 characters
          </Text>
        </View>

        <TouchableOpacity
          style={[styles.submitButton, submitting && styles.submitButtonDisabled]}
          onPress={handleSubmit}
          disabled={submitting}
        >
          <Text style={styles.submitButtonText}>
            {submitting ? 'Submitting...' : 'Submit Request'}
          </Text>
        </TouchableOpacity>

        <TouchableOpacity style={styles.backButton} onPress={onBack}>
          <Text style={styles.backButtonText}>← Back to Login</Text>
        </TouchableOpacity>
      </View>

      <View style={styles.infoBox}>
        <Text style={styles.infoText}>
          ℹ️ Your request will be reviewed by an administrator. You will receive an SMS with your login credentials once approved.
        </Text>
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
    padding: 20,
  },
  header: {
    marginBottom: 30,
    alignItems: 'center',
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 16,
    color: '#666',
    textAlign: 'center',
  },
  form: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 20,
    marginBottom: 20,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  inputGroup: {
    marginBottom: 20,
  },
  label: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
    marginBottom: 8,
  },
  input: {
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 8,
    padding: 12,
    fontSize: 16,
    backgroundColor: '#fff',
  },
  textArea: {
    minHeight: 100,
    textAlignVertical: 'top',
  },
  characterCount: {
    fontSize: 12,
    color: '#666',
    textAlign: 'right',
    marginTop: 4,
  },
  hint: {
    fontSize: 12,
    color: '#666',
    marginTop: 4,
    fontStyle: 'italic',
  },
  genderContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 10,
  },
  genderOption: {
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: '#ddd',
    backgroundColor: '#fff',
  },
  genderOptionSelected: {
    backgroundColor: '#007bff',
    borderColor: '#007bff',
  },
  genderOptionText: {
    fontSize: 14,
    color: '#333',
  },
  genderOptionTextSelected: {
    color: '#fff',
    fontWeight: '600',
  },
  submitButton: {
    backgroundColor: '#007bff',
    borderRadius: 8,
    padding: 16,
    alignItems: 'center',
    marginTop: 10,
  },
  submitButtonDisabled: {
    backgroundColor: '#ccc',
  },
  submitButtonText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  backButton: {
    marginTop: 15,
    alignItems: 'center',
  },
  backButtonText: {
    color: '#007bff',
    fontSize: 16,
  },
  infoBox: {
    backgroundColor: '#e3f2fd',
    borderRadius: 8,
    padding: 15,
    borderLeftWidth: 4,
    borderLeftColor: '#2196f3',
  },
  infoText: {
    fontSize: 14,
    color: '#1976d2',
    lineHeight: 20,
  },
});

export default GuestRegistrationForm;

