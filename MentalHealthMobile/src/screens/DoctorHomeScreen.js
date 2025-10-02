import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  Alert,
  RefreshControl,
  ActivityIndicator,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '../context/AuthContext';
import { useRealtime } from '../context/RealtimeContext';
import { patientService } from '../services/patientService';

export default function DoctorHomeScreen({ navigation }) {
  const [patients, setPatients] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const { user, token } = useAuth();
  const { initiateCall, isConnected } = useRealtime();

  useEffect(() => {
    loadPatients();
  }, []);

  const loadPatients = async () => {
    try {
      setIsLoading(true);
      const result = await patientService.getAssignedPatients(token);
      
      if (result.success) {
        setPatients(result.patients);
      } else {
        Alert.alert('Error', result.error);
      }
    } catch (error) {
      console.error('Error loading patients:', error);
      Alert.alert('Error', 'Failed to load patients');
    } finally {
      setIsLoading(false);
    }
  };

  const onRefresh = async () => {
    setRefreshing(true);
    await loadPatients();
    setRefreshing(false);
  };

  const handleVideoCall = (patient) => {
    if (!isConnected) {
      Alert.alert('Connection Error', 'Not connected to server. Please check your internet connection.');
      return;
    }

    Alert.alert(
      'Video Call',
      `Start video call with ${patient.firstName} ${patient.lastName}?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Call',
          onPress: () => {
            initiateCall(patient.id, 'video');
            navigation.navigate('VideoCall', { 
              patient,
              callType: 'video',
              isInitiator: true 
            });
          },
        },
      ]
    );
  };

  const handleAudioCall = (patient) => {
    if (!isConnected) {
      Alert.alert('Connection Error', 'Not connected to server. Please check your internet connection.');
      return;
    }

    Alert.alert(
      'Audio Call',
      `Start audio call with ${patient.firstName} ${patient.lastName}?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Call',
          onPress: () => {
            initiateCall(patient.id, 'audio');
            navigation.navigate('AudioCall', { 
              patient,
              callType: 'audio',
              isInitiator: true 
            });
          },
        },
      ]
    );
  };

  const handleChat = (patient) => {
    navigation.navigate('Chat', { 
      targetUser: patient,
      chatType: 'direct'
    });
  };

  const getStatusColor = (lastLoginAt) => {
    if (!lastLoginAt) return '#ccc';
    
    const lastLogin = new Date(lastLoginAt);
    const now = new Date();
    const diffHours = (now - lastLogin) / (1000 * 60 * 60);
    
    if (diffHours < 1) return '#4CAF50'; // Online (green)
    if (diffHours < 24) return '#FF9800'; // Recently active (orange)
    return '#ccc'; // Offline (gray)
  };

  const getStatusText = (lastLoginAt) => {
    if (!lastLoginAt) return 'Never logged in';
    
    const lastLogin = new Date(lastLoginAt);
    const now = new Date();
    const diffHours = (now - lastLogin) / (1000 * 60 * 60);
    
    if (diffHours < 1) return 'Online';
    if (diffHours < 24) return `${Math.floor(diffHours)}h ago`;
    if (diffHours < 168) return `${Math.floor(diffHours / 24)}d ago`;
    return 'Long time ago';
  };

  const renderPatient = ({ item: patient }) => (
    <View style={styles.patientCard}>
      <View style={styles.patientHeader}>
        <View style={styles.patientInfo}>
          <View style={styles.avatarContainer}>
            <Text style={styles.avatarText}>
              {patient.firstName?.[0]}{patient.lastName?.[0]}
            </Text>
          </View>
          <View style={styles.patientDetails}>
            <Text style={styles.patientName}>
              {patient.firstName} {patient.lastName}
            </Text>
            <Text style={styles.patientEmail}>{patient.email}</Text>
            <View style={styles.statusContainer}>
              <View 
                style={[
                  styles.statusDot, 
                  { backgroundColor: getStatusColor(patient.lastLoginAt) }
                ]} 
              />
              <Text style={styles.statusText}>
                {getStatusText(patient.lastLoginAt)}
              </Text>
            </View>
          </View>
        </View>
      </View>

      <View style={styles.actionButtons}>
        <TouchableOpacity 
          style={[styles.actionButton, styles.videoButton]}
          onPress={() => handleVideoCall(patient)}
        >
          <Ionicons name="videocam" size={20} color="#fff" />
          <Text style={styles.actionButtonText}>Video</Text>
        </TouchableOpacity>

        <TouchableOpacity 
          style={[styles.actionButton, styles.audioButton]}
          onPress={() => handleAudioCall(patient)}
        >
          <Ionicons name="call" size={20} color="#fff" />
          <Text style={styles.actionButtonText}>Audio</Text>
        </TouchableOpacity>

        <TouchableOpacity 
          style={[styles.actionButton, styles.chatButton]}
          onPress={() => handleChat(patient)}
        >
          <Ionicons name="chatbubbles" size={20} color="#fff" />
          <Text style={styles.actionButtonText}>Chat</Text>
        </TouchableOpacity>
      </View>
    </View>
  );

  if (isLoading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#2196F3" />
        <Text style={styles.loadingText}>Loading patients...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>My Patients</Text>
        <View style={styles.connectionStatus}>
          <View 
            style={[
              styles.connectionDot, 
              { backgroundColor: isConnected ? '#4CAF50' : '#f44336' }
            ]} 
          />
          <Text style={styles.connectionText}>
            {isConnected ? 'Connected' : 'Disconnected'}
          </Text>
        </View>
      </View>

      {patients.length === 0 ? (
        <View style={styles.emptyContainer}>
          <Ionicons name="people-outline" size={64} color="#ccc" />
          <Text style={styles.emptyText}>No patients assigned</Text>
          <Text style={styles.emptySubtext}>
            Patients will appear here once they are assigned to you
          </Text>
        </View>
      ) : (
        <FlatList
          data={patients}
          renderItem={renderPatient}
          keyExtractor={(item) => item.id.toString()}
          contentContainerStyle={styles.listContainer}
          refreshControl={
            <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
          }
          showsVerticalScrollIndicator={false}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  header: {
    backgroundColor: '#2196F3',
    paddingHorizontal: 20,
    paddingTop: 60,
    paddingBottom: 20,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  headerTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#fff',
  },
  connectionStatus: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  connectionDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    marginRight: 6,
  },
  connectionText: {
    color: '#fff',
    fontSize: 12,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    marginTop: 10,
    fontSize: 16,
    color: '#666',
  },
  listContainer: {
    padding: 20,
  },
  patientCard: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 2,
    },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  patientHeader: {
    marginBottom: 16,
  },
  patientInfo: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  avatarContainer: {
    width: 50,
    height: 50,
    borderRadius: 25,
    backgroundColor: '#2196F3',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  avatarText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  patientDetails: {
    flex: 1,
  },
  patientName: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 4,
  },
  patientEmail: {
    fontSize: 14,
    color: '#666',
    marginBottom: 6,
  },
  statusContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  statusDot: {
    width: 6,
    height: 6,
    borderRadius: 3,
    marginRight: 6,
  },
  statusText: {
    fontSize: 12,
    color: '#666',
  },
  actionButtons: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  actionButton: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 12,
    borderRadius: 8,
    marginHorizontal: 4,
  },
  videoButton: {
    backgroundColor: '#4CAF50',
  },
  audioButton: {
    backgroundColor: '#FF9800',
  },
  chatButton: {
    backgroundColor: '#9C27B0',
  },
  actionButtonText: {
    color: '#fff',
    fontSize: 14,
    fontWeight: '600',
    marginLeft: 6,
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 40,
  },
  emptyText: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#666',
    marginTop: 16,
    marginBottom: 8,
  },
  emptySubtext: {
    fontSize: 14,
    color: '#999',
    textAlign: 'center',
    lineHeight: 20,
  },
});
