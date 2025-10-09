import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ScrollView,
  Alert,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '../context/AuthContext';
import { useRealtime } from '../context/RealtimeContext';

export default function ProfileScreen() {
  const { user, logout } = useAuth();
  const { isConnected } = useRealtime();

  const handleLogout = () => {
    Alert.alert(
      'Logout',
      'Are you sure you want to logout?',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Logout',
          style: 'destructive',
          onPress: logout,
        },
      ]
    );
  };

  const formatDate = (dateString) => {
    if (!dateString) return 'Never';
    return new Date(dateString).toLocaleDateString();
  };

  const getUserRole = () => {
    switch (user?.roleId) {
      case 1: return 'Patient';
      case 2: return 'Doctor';
      case 3: return 'Admin';
      default: return 'Unknown';
    }
  };

  const getThemeColor = () => {
    return user?.roleId === 2 ? '#2196F3' : '#4CAF50';
  };

  return (
    <ScrollView style={styles.container}>
      {/* Header */}
      <View style={[styles.header, { backgroundColor: getThemeColor() }]}>
        <View style={styles.avatarContainer}>
          <Text style={styles.avatarText}>
            {user?.firstName?.[0]}{user?.lastName?.[0]}
          </Text>
        </View>
        <Text style={styles.userName}>
          {user?.firstName} {user?.lastName}
        </Text>
        <Text style={styles.userRole}>{getUserRole()}</Text>
        
        {/* Connection Status */}
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

      {/* Profile Information */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Profile Information</Text>
        
        <View style={styles.infoItem}>
          <Ionicons name="mail-outline" size={20} color="#666" />
          <View style={styles.infoContent}>
            <Text style={styles.infoLabel}>Email</Text>
            <Text style={styles.infoValue}>{user?.email}</Text>
          </View>
        </View>

        {user?.roleId === 2 && user?.specialization && (
          <View style={styles.infoItem}>
            <Ionicons name="medical-outline" size={20} color="#666" />
            <View style={styles.infoContent}>
              <Text style={styles.infoLabel}>Specialization</Text>
              <Text style={styles.infoValue}>{user.specialization}</Text>
            </View>
          </View>
        )}

        {user?.roleId === 2 && user?.licenseNumber && (
          <View style={styles.infoItem}>
            <Ionicons name="card-outline" size={20} color="#666" />
            <View style={styles.infoContent}>
              <Text style={styles.infoLabel}>License Number</Text>
              <Text style={styles.infoValue}>{user.licenseNumber}</Text>
            </View>
          </View>
        )}

        {user?.mobilePhone && (
          <View style={styles.infoItem}>
            <Ionicons name="call-outline" size={20} color="#666" />
            <View style={styles.infoContent}>
              <Text style={styles.infoLabel}>Mobile Phone</Text>
              <Text style={styles.infoValue}>{user.mobilePhone}</Text>
            </View>
          </View>
        )}

        {user?.dateOfBirth && (
          <View style={styles.infoItem}>
            <Ionicons name="calendar-outline" size={20} color="#666" />
            <View style={styles.infoContent}>
              <Text style={styles.infoLabel}>Date of Birth</Text>
              <Text style={styles.infoValue}>{formatDate(user.dateOfBirth)}</Text>
            </View>
          </View>
        )}

        {user?.gender && (
          <View style={styles.infoItem}>
            <Ionicons name="person-outline" size={20} color="#666" />
            <View style={styles.infoContent}>
              <Text style={styles.infoLabel}>Gender</Text>
              <Text style={styles.infoValue}>{user.gender}</Text>
            </View>
          </View>
        )}
      </View>

      {/* Account Information */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Account Information</Text>
        
        <View style={styles.infoItem}>
          <Ionicons name="time-outline" size={20} color="#666" />
          <View style={styles.infoContent}>
            <Text style={styles.infoLabel}>Member Since</Text>
            <Text style={styles.infoValue}>{formatDate(user?.createdAt)}</Text>
          </View>
        </View>

        <View style={styles.infoItem}>
          <Ionicons name="log-in-outline" size={20} color="#666" />
          <View style={styles.infoContent}>
            <Text style={styles.infoLabel}>Last Login</Text>
            <Text style={styles.infoValue}>{formatDate(user?.lastLoginAt)}</Text>
          </View>
        </View>

        <View style={styles.infoItem}>
          <Ionicons name="shield-checkmark-outline" size={20} color="#666" />
          <View style={styles.infoContent}>
            <Text style={styles.infoLabel}>Account Status</Text>
            <Text style={[styles.infoValue, { color: user?.isActive ? '#4CAF50' : '#f44336' }]}>
              {user?.isActive ? 'Active' : 'Inactive'}
            </Text>
          </View>
        </View>
      </View>

      {/* Settings */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Settings</Text>
        
        <TouchableOpacity style={styles.settingItem}>
          <Ionicons name="notifications-outline" size={20} color="#666" />
          <Text style={styles.settingText}>Notifications</Text>
          <Ionicons name="chevron-forward" size={20} color="#ccc" />
        </TouchableOpacity>

        <TouchableOpacity style={styles.settingItem}>
          <Ionicons name="lock-closed-outline" size={20} color="#666" />
          <Text style={styles.settingText}>Privacy</Text>
          <Ionicons name="chevron-forward" size={20} color="#ccc" />
        </TouchableOpacity>

        <TouchableOpacity style={styles.settingItem}>
          <Ionicons name="help-circle-outline" size={20} color="#666" />
          <Text style={styles.settingText}>Help & Support</Text>
          <Ionicons name="chevron-forward" size={20} color="#ccc" />
        </TouchableOpacity>

        <TouchableOpacity style={styles.settingItem}>
          <Ionicons name="information-circle-outline" size={20} color="#666" />
          <Text style={styles.settingText}>About</Text>
          <Ionicons name="chevron-forward" size={20} color="#ccc" />
        </TouchableOpacity>
      </View>

      {/* Logout Button */}
      <View style={styles.section}>
        <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
          <Ionicons name="log-out-outline" size={20} color="#f44336" />
          <Text style={styles.logoutText}>Logout</Text>
        </TouchableOpacity>
      </View>

      {/* App Version */}
      <View style={styles.versionContainer}>
        <Text style={styles.versionText}>Mental Health Mobile v1.0.0</Text>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  header: {
    paddingTop: 60,
    paddingBottom: 30,
    paddingHorizontal: 20,
    alignItems: 'center',
  },
  avatarContainer: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 15,
  },
  avatarText: {
    color: '#fff',
    fontSize: 32,
    fontWeight: 'bold',
  },
  userName: {
    color: '#fff',
    fontSize: 24,
    fontWeight: 'bold',
    marginBottom: 5,
  },
  userRole: {
    color: 'rgba(255, 255, 255, 0.8)',
    fontSize: 16,
    marginBottom: 15,
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
    color: 'rgba(255, 255, 255, 0.9)',
    fontSize: 14,
  },
  section: {
    backgroundColor: '#fff',
    marginHorizontal: 20,
    marginTop: 20,
    borderRadius: 12,
    padding: 20,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 15,
  },
  infoItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#f0f0f0',
  },
  infoContent: {
    flex: 1,
    marginLeft: 15,
  },
  infoLabel: {
    fontSize: 14,
    color: '#666',
    marginBottom: 2,
  },
  infoValue: {
    fontSize: 16,
    color: '#333',
    fontWeight: '500',
  },
  settingItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 15,
    borderBottomWidth: 1,
    borderBottomColor: '#f0f0f0',
  },
  settingText: {
    flex: 1,
    fontSize: 16,
    color: '#333',
    marginLeft: 15,
  },
  logoutButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 15,
  },
  logoutText: {
    fontSize: 16,
    color: '#f44336',
    fontWeight: '600',
    marginLeft: 8,
  },
  versionContainer: {
    alignItems: 'center',
    paddingVertical: 20,
  },
  versionText: {
    fontSize: 12,
    color: '#999',
  },
});
