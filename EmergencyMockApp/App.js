import React, { useState } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TouchableOpacity,
  Alert,
  TextInput,
  ScrollView,
  StatusBar,
  SafeAreaView,
} from 'react-native';

const API_BASE_URL = 'http://192.168.86.113:5262/api/emergency'; // Using your computer's IP address

export default function App() {
  const [isRegistered, setIsRegistered] = useState(false);
  const [deviceToken, setDeviceToken] = useState('');
  const [patientId, setPatientId] = useState('');
  const [jwtToken, setJwtToken] = useState('');
  const [deviceId, setDeviceId] = useState('mock-device-' + Date.now());
  const [isLoading, setIsLoading] = useState(false);

  const registerDevice = async () => {
    if (!patientId.trim()) {
      Alert.alert('Error', 'Please enter your Patient ID');
      return;
    }
    // JWT token not required for test endpoint

    setIsLoading(true);
    try {
      // Try real server registration first, fallback to mock if it fails
      try {
        const response = await fetch(`${API_BASE_URL}/test-register`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            patientId: parseInt(patientId),
            deviceId: deviceId,
            deviceName: 'Test Device',
            deviceType: 'smartphone',
            deviceModel: 'Test Model',
            operatingSystem: 'Test OS',
            publicKey: 'test-key'
          })
        });

        const data = await response.json();
        
        if (data.success) {
          setDeviceToken(data.deviceToken);
          setIsRegistered(true);
          Alert.alert('Success', 'Device registered successfully! (Real server response)');
        } else {
          throw new Error(data.message || 'Registration failed');
        }
      } catch (error) {
        console.log('Server registration failed, using mock response:', error.message);
        
        // Fallback to mock response
        const mockResponse = {
          success: true,
          deviceToken: 'mock-device-token-' + Date.now(),
          expiresAt: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString()
        };

        // Simulate network delay
        await new Promise(resolve => setTimeout(resolve, 1000));

        setDeviceToken(mockResponse.deviceToken);
        setIsRegistered(true);
        Alert.alert('Success', 'Device registered successfully! (Mock response - server unavailable)');
      }
    } catch (error) {
      console.error('Registration error:', error);
      Alert.alert('Error', 'Failed to register device. Please check your connection.');
    } finally {
      setIsLoading(false);
    }
  };

  const sendEmergency = async (emergencyType, severity, message) => {
    if (!isRegistered) {
      Alert.alert('Error', 'Please register your device first');
      return;
    }

    setIsLoading(true);
    try {
      // Try real server first, fallback to mock if it fails
      try {
        const response = await fetch(`${API_BASE_URL}/test-emergency`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            deviceToken: deviceToken,
            emergencyType: emergencyType,
            severity: severity,
            message: message,
            deviceId: deviceId,
            heartRate: Math.floor(Math.random() * 50) + 120,
            bloodPressure: '140/90',
            temperature: 98.6 + (Math.random() - 0.5) * 2,
            oxygenSaturation: Math.floor(Math.random() * 10) + 90,
            latitude: 40.7128,
            longitude: -74.0060
          })
        });

        const data = await response.json();
        
        if (data.success) {
          Alert.alert('Emergency Sent!', `Emergency message sent successfully. Incident ID: ${data.incidentId} (Real server response)`);
        } else {
          throw new Error(data.message || 'Emergency sending failed');
        }
      } catch (error) {
        console.log('Server emergency sending failed, using mock response:', error.message);
        
        // Fallback to mock response
        const mockResponse = {
          success: true,
          incidentId: Math.floor(Math.random() * 10000),
          message: 'Emergency message sent successfully'
        };

        // Simulate network delay
        await new Promise(resolve => setTimeout(resolve, 1000));

        Alert.alert('Emergency Sent!', `Emergency message sent successfully. Incident ID: ${mockResponse.incidentId} (Mock response - server unavailable)`);
      }
    } catch (error) {
      console.error('Emergency error:', error);
      Alert.alert('Error', 'Failed to send emergency message. Please check your connection.');
    } finally {
      setIsLoading(false);
    }
  };

  const EmergencyButton = ({ type, severity, message, color }) => (
    <TouchableOpacity
      style={[styles.emergencyButton, { backgroundColor: color }]}
      onPress={() => sendEmergency(type, severity, message)}
      disabled={isLoading}
    >
      <Text style={styles.emergencyButtonText}>{type}</Text>
      <Text style={styles.emergencyButtonSubtext}>{severity}</Text>
    </TouchableOpacity>
  );

  if (!isRegistered) {
    return (
      <SafeAreaView style={styles.container}>
        <StatusBar barStyle="dark-content" />
        <ScrollView contentContainerStyle={styles.scrollContainer}>
          <View style={styles.header}>
            <Text style={styles.title}>🚨 Emergency Mock App</Text>
            <Text style={styles.subtitle}>Register your device for emergency messaging</Text>
                <Text style={styles.instructions}>
                  📝 Will try real server first, falls back to mock if unavailable
                </Text>
          </View>

          <View style={styles.form}>
            <Text style={styles.label}>Patient ID</Text>
            <TextInput
              style={styles.input}
              value={patientId}
              onChangeText={setPatientId}
              placeholder="Enter your Patient ID (1, 7, or 9)"
              keyboardType="numeric"
            />

                {/* JWT token not required for test endpoint */}

            <Text style={styles.label}>Device ID</Text>
            <TextInput
              style={[styles.input, styles.disabledInput]}
              value={deviceId}
              editable={false}
            />

            <TouchableOpacity
              style={[styles.registerButton, isLoading && styles.disabledButton]}
              onPress={registerDevice}
              disabled={isLoading}
            >
              <Text style={styles.registerButtonText}>
                {isLoading ? 'Registering...' : 'Register Device'}
              </Text>
            </TouchableOpacity>
          </View>

          <View style={styles.info}>
            <Text style={styles.infoText}>
              This app simulates emergency messages from a smart device. 
              Once registered, you can test different emergency scenarios.
            </Text>
          </View>
        </ScrollView>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar barStyle="dark-content" />
      <ScrollView contentContainerStyle={styles.scrollContainer}>
        <View style={styles.header}>
          <Text style={styles.title}>✅ Emergency Mock App</Text>
          <Text style={styles.subtitle}>Device registered successfully</Text>
        </View>

        <View style={styles.emergencyGrid}>
          <EmergencyButton
            type="Fall"
            severity="Critical"
            message="Patient fell and is unresponsive"
            color="#e74c3c"
          />
          
          <EmergencyButton
            type="Cardiac"
            severity="Critical"
            message="Cardiac event detected"
            color="#e74c3c"
          />
          
          <EmergencyButton
            type="Panic Attack"
            severity="High"
            message="Patient experiencing panic attack"
            color="#f39c12"
          />
          
          <EmergencyButton
            type="Seizure"
            severity="Critical"
            message="Seizure activity detected"
            color="#e74c3c"
          />
          
          <EmergencyButton
            type="Overdose"
            severity="Critical"
            message="Possible overdose detected"
            color="#e74c3c"
          />
          
          <EmergencyButton
            type="Self Harm"
            severity="High"
            message="Self-harm indicators detected"
            color="#f39c12"
          />
        </View>

        <View style={styles.info}>
          <Text style={styles.infoText}>
            Tap any button to simulate an emergency. The message will be sent to your assigned doctor.
          </Text>
        </View>

        <TouchableOpacity
          style={styles.resetButton}
          onPress={() => {
            setDeviceToken('');
            setIsRegistered(false);
            setPatientId('');
          }}
        >
          <Text style={styles.resetButtonText}>Reset Registration</Text>
        </TouchableOpacity>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8f9fa',
  },
  scrollContainer: {
    flexGrow: 1,
    padding: 20,
  },
  header: {
    alignItems: 'center',
    marginBottom: 30,
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#2c3e50',
    marginTop: 10,
  },
  subtitle: {
    fontSize: 16,
    color: '#7f8c8d',
    marginTop: 5,
    textAlign: 'center',
  },
  instructions: {
    fontSize: 14,
    color: '#007bff',
    textAlign: 'center',
    marginTop: 10,
    marginBottom: 20,
    fontStyle: 'italic',
  },
  form: {
    backgroundColor: 'white',
    padding: 20,
    borderRadius: 10,
    marginBottom: 20,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  label: {
    fontSize: 16,
    fontWeight: '600',
    color: '#2c3e50',
    marginBottom: 8,
  },
  input: {
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 8,
    padding: 12,
    fontSize: 16,
    marginBottom: 16,
  },
  disabledInput: {
    backgroundColor: '#f8f9fa',
    color: '#6c757d',
  },
  registerButton: {
    backgroundColor: '#3498db',
    padding: 15,
    borderRadius: 8,
    alignItems: 'center',
  },
  disabledButton: {
    backgroundColor: '#bdc3c7',
  },
  registerButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  emergencyGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
    marginBottom: 20,
  },
  emergencyButton: {
    width: '48%',
    padding: 20,
    borderRadius: 10,
    alignItems: 'center',
    marginBottom: 15,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.2,
    shadowRadius: 4,
    elevation: 3,
  },
  emergencyButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: 'bold',
    marginTop: 8,
  },
  emergencyButtonSubtext: {
    color: 'white',
    fontSize: 12,
    marginTop: 2,
    opacity: 0.9,
  },
  info: {
    backgroundColor: '#e8f4fd',
    padding: 15,
    borderRadius: 8,
    marginBottom: 20,
  },
  infoText: {
    color: '#2c3e50',
    fontSize: 14,
    textAlign: 'center',
    lineHeight: 20,
  },
  resetButton: {
    backgroundColor: '#95a5a6',
    padding: 12,
    borderRadius: 8,
    alignItems: 'center',
  },
  resetButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
});