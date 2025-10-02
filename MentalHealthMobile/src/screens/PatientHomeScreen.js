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
import { doctorService } from '../services/doctorService';

export default function PatientHomeScreen({ navigation }) {
  const [doctors, setDoctors] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const { user, token } = useAuth();
  const { initiateCall, isConnected } = useRealtime();

  useEffect(() => {
    loadDoctors();
  }, []);

  const loadDoctors = async () => {
    try {
      setIsLoading(true);
      const result = await doctorService.getAssignedDoctors(token);
      
      if (result.success) {
        setDoctors(result.doctors);
      } else {
        Alert.alert('Error', result.error);
      }
    } catch (error) {
      console.error('Error loading doctors:', error);
      Alert.alert('Error', 'Failed to load doctors');
    } finally {
      setIsLoading(false);
    }
  };

  const onRefresh = async () => {
    setRefreshing(true);
    await loadDoctors();
    setRefreshing(false);
  };

  const handleVideoCall = (doctor) => {
    if (!isConnected) {
      Alert.alert('Connection Error', 'Not connected to server. Please check your internet connection.');
      return;
    }

    Alert.alert(
      'Video Call Request',
      `Request video call with Dr. ${doctor.firstName} ${doctor.lastName}?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Request Call',
          onPress: () => {
            initiateCall(doctor.id, 'video');
            navigation.navigate('VideoCall', { 
              doctor,
              callType: 'video',
              isInitiator: true 
            });
          },
        },
      ]
    );
  };

  const handleAudioCall = (doctor) => {
    if (!isConnected) {
      Alert.alert('Connection Error', 'Not connected to server. Please check your internet connection.');
      return;
    }

    Alert.alert(
      'Audio Call Request',
      `Request audio call with Dr. ${doctor.firstName} ${doctor.lastName}?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Request Call',
          onPress: () => {
            initiateCall(doctor.id, 'audio');
            navigation.navigate('AudioCall', { 
              doctor,
              callType: 'audio',
              isInitiator: true 
            });
          },
        },
      ]
    );
  };

  const handleChat = (doctor) => {
    navigation.navigate('Chat', { 
      targetUser: doctor,
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

  const renderDoctor = ({ item: doctor }) => (
    <View style={styles.doctorCard}>
      <View style={styles.doctorHeader}>
        <View style={styles.doctorInfo}>
          <View style={styles.avatarContainer}>
            <Text style={styles.avatarText}>
              {doctor.firstName?.[0]}{doctor.lastName?.[0]}
            </Text>
          </View>
          <View style={styles.doctorDetails}>
            <Text style={styles.doctorName}>
              Dr. {doctor.firstName} {doctor.lastName}
            </Text>
            <Text style={styles.specialization}>{doctor.specialization}</Text>
            <Text style={styles.doctorEmail}>{doctor.email}</Text>
            <View style={styles.statusContainer}>
              <View 
                style={[
                  styles.statusDot, 
                  { backgroundColor: getStatusColor(doctor.lastLoginAt) }
                ]} 
              />
              <Text style={styles.statusText}>
                {getStatusText(doctor.lastLoginAt)}
              </Text>
            </View>
          </View>
        </View>
      </View>

      <View style={styles.actionButtons}>
        <TouchableOpacity 
          style={[styles.actionButton, styles.videoButton]}
          onPress={() => handleVideoCall(doctor)}
        >
          <Ionicons name="videocam" size={20} color="#fff" />
          <Text style={styles.actionButtonText}>Video</Text>
        </TouchableOpacity>

        <TouchableOpacity 
          style={[styles.actionButton, styles.audioButton]}
          onPress={() => handleAudioCall(doctor)}
        >
          <Ionicons name="call" size={20} color="#fff" />
          <Text style={styles.actionButtonText}>Audio</Text>
        </TouchableOpacity>

        <TouchableOpacity 
          style={[styles.actionButton, styles.chatButton]}
          onPress={() => handleChat(doctor)}
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
        <ActivityIndicator size="large" color="#4CAF50" />
        <Text style={styles.loadingText}>Loading doctors...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>My Doctors</Text>
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

      {doctors.length === 0 ? (
        <View style={styles.emptyContainer}>
          <Ionicons name="medical-outline" size={64} color="#ccc" />
          <Text style={styles.emptyText}>No doctors assigned</Text>
          <Text style={styles.emptySubtext}>
            Your assigned doctors will appear here
          </Text>
        </View>
      ) : (
        <FlatList
          data={doctors}
          renderItem={renderDoctor}
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
    backgroundColor: '#4CAF50',
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
  doctorCard: {
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
  doctorHeader: {
    marginBottom: 16,
  },
  doctorInfo: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  avatarContainer: {
    width: 50,
    height: 50,
    borderRadius: 25,
    backgroundColor: '#4CAF50',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  avatarText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  doctorDetails: {
    flex: 1,
  },
  doctorName: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 2,
  },
  specialization: {
    fontSize: 14,
    color: '#4CAF50',
    fontWeight: '600',
    marginBottom: 4,
  },
  doctorEmail: {
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
    backgroundColor: '#2196F3',
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
