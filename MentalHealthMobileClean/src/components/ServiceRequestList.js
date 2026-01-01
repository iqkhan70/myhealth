import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  FlatList,
  ActivityIndicator,
  RefreshControl,
  Alert,
} from 'react-native';
import ServiceRequestService from '../services/ServiceRequestService';

const ServiceRequestList = ({ 
  onServiceRequestSelect,
  user = null,
  onCreateRequest = null
}) => {
  const [serviceRequests, setServiceRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  useEffect(() => {
    loadServiceRequests();
  }, []);

  const loadServiceRequests = async () => {
    try {
      setLoading(true);
      const data = await ServiceRequestService.getServiceRequests();
      // Handle OData response format (data.value) or direct array
      const requests = Array.isArray(data) ? data : (data.value || []);
      setServiceRequests(requests);
    } catch (error) {
      console.error('Error loading service requests:', error);
      Alert.alert('Error', 'Failed to load service requests');
    } finally {
      setLoading(false);
    }
  };

  const onRefresh = async () => {
    setRefreshing(true);
    await loadServiceRequests();
    setRefreshing(false);
  };

  const getStatusColor = (status) => {
    switch (status?.toLowerCase()) {
      case 'active':
        return '#28a745';
      case 'pending':
        return '#ffc107';
      case 'completed':
        return '#6c757d';
      case 'archived':
        return '#6c757d';
      case 'onhold':
        return '#fd7e14';
      case 'cancelled':
        return '#dc3545';
      default:
        return '#17a2b8';
    }
  };

  const renderServiceRequest = ({ item }) => {
    // Handle both camelCase and PascalCase from API
    const status = item.status || item.Status || 'Active';
    const statusColor = getStatusColor(status);
    const assignments = item.assignments || item.Assignments || [];
    const assignedSmes = assignments
      .filter(a => (a.isActive !== false && a.IsActive !== false))
      .map(a => a.smeUserName || a.SmeUserName || 'Unknown')
      .join(', ') || 'No assignments';

    return (
      <TouchableOpacity
        style={styles.serviceRequestCard}
        onPress={() => onServiceRequestSelect && onServiceRequestSelect(item)}
      >
        <View style={styles.cardHeader}>
          <Text style={styles.title}>{item.title || item.Title}</Text>
          <View style={[styles.statusBadge, { backgroundColor: statusColor }]}>
            <Text style={styles.statusText}>{status}</Text>
          </View>
        </View>
        
        <View style={styles.cardBody}>
          <Text style={styles.clientName}>Client: {item.clientName || item.ClientName}</Text>
          <Text style={styles.type}>Type: {item.type || item.Type || 'General'}</Text>
          {(item.description || item.Description) && (
            <Text style={styles.description} numberOfLines={2}>
              {item.description || item.Description}
            </Text>
          )}
          <Text style={styles.assignedSmes}>Assigned: {assignedSmes}</Text>
          <Text style={styles.date}>
            Created: {new Date(item.createdAt || item.CreatedAt).toLocaleDateString()}
          </Text>
        </View>
      </TouchableOpacity>
    );
  };

  if (loading && !refreshing) {
    return (
      <View style={styles.centerContainer}>
        <ActivityIndicator size="large" color="#007bff" />
        <Text style={styles.loadingText}>Loading service requests...</Text>
      </View>
    );
  }

  if (serviceRequests.length === 0) {
    return (
      <View style={styles.centerContainer}>
        <Text style={styles.emptyText}>No service requests found</Text>
        <TouchableOpacity style={styles.refreshButton} onPress={loadServiceRequests}>
          <Text style={styles.refreshButtonText}>Refresh</Text>
        </TouchableOpacity>
      </View>
    );
  }

  const isPatient = user?.roleId === 1;

  return (
    <View style={styles.container}>
      {isPatient && (
        <View style={styles.createButtonContainer}>
          <TouchableOpacity
            style={styles.createButton}
            onPress={() => {
              if (onCreateRequest) {
                onCreateRequest();
              }
            }}
          >
            <Text style={styles.createButtonText}>+ Create Service Request</Text>
          </TouchableOpacity>
        </View>
      )}
      <FlatList
        data={serviceRequests}
        renderItem={renderServiceRequest}
        keyExtractor={(item) => (item.id || item.Id || Math.random()).toString()}
        contentContainerStyle={styles.listContainer}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  listContainer: {
    padding: 16,
  },
  createButtonContainer: {
    padding: 16,
    paddingBottom: 8,
  },
  createButton: {
    backgroundColor: '#28a745',
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 6,
    alignItems: 'center',
    justifyContent: 'center',
  },
  createButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  serviceRequestCard: {
    backgroundColor: '#fff',
    borderRadius: 8,
    padding: 16,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  title: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
    flex: 1,
    marginRight: 8,
  },
  statusBadge: {
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
  },
  statusText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '600',
  },
  cardBody: {
    gap: 8,
  },
  clientName: {
    fontSize: 16,
    color: '#555',
    fontWeight: '500',
  },
  type: {
    fontSize: 14,
    color: '#666',
  },
  description: {
    fontSize: 14,
    color: '#777',
    marginTop: 4,
  },
  assignedSmes: {
    fontSize: 14,
    color: '#007bff',
    marginTop: 4,
  },
  date: {
    fontSize: 12,
    color: '#999',
    marginTop: 4,
  },
  centerContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  loadingText: {
    marginTop: 12,
    fontSize: 16,
    color: '#666',
  },
  emptyText: {
    fontSize: 16,
    color: '#666',
    marginBottom: 16,
  },
  refreshButton: {
    backgroundColor: '#007bff',
    paddingHorizontal: 20,
    paddingVertical: 10,
    borderRadius: 6,
  },
  refreshButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
});

export default ServiceRequestList;

