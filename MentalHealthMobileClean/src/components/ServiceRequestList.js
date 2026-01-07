import React, { useState, useEffect, useMemo } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  FlatList,
  ActivityIndicator,
  RefreshControl,
  Alert,
  TextInput,
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
  const [searchQuery, setSearchQuery] = useState('');

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

  // Filter service requests based on search query (must be before early returns)
  const filteredServiceRequests = useMemo(() => {
    if (!searchQuery.trim()) {
      return serviceRequests;
    }

    const query = searchQuery.toLowerCase().trim();
    return serviceRequests.filter((sr) => {
      const title = (sr.title || sr.Title || '').toLowerCase();
      const description = (sr.description || sr.Description || '').toLowerCase();
      const clientName = (sr.clientName || sr.ClientName || '').toLowerCase();
      const type = (sr.type || sr.Type || '').toLowerCase();
      const status = (sr.status || sr.Status || '').toLowerCase();
      const assignedSmes = (sr.assignments || sr.Assignments || [])
        .map(a => (a.smeUserName || a.SmeUserName || '').toLowerCase())
        .join(' ');

      return (
        title.includes(query) ||
        description.includes(query) ||
        clientName.includes(query) ||
        type.includes(query) ||
        status.includes(query) ||
        assignedSmes.includes(query)
      );
    });
  }, [serviceRequests, searchQuery]);

  const handleDeleteServiceRequest = async (item) => {
    Alert.alert(
      'Delete Service Request',
      `Are you sure you want to delete "${item.title || item.Title}"? This action cannot be undone.`,
      [
        {
          text: 'Cancel',
          style: 'cancel',
        },
        {
          text: 'Delete',
          style: 'destructive',
          onPress: async () => {
            try {
              await ServiceRequestService.deleteServiceRequest(item.id || item.Id);
              Alert.alert('Success', 'Service request deleted successfully');
              // Reload the list
              await loadServiceRequests();
            } catch (error) {
              console.error('Error deleting service request:', error);
              Alert.alert('Error', 'Failed to delete service request. Please try again.');
            }
          },
        },
      ]
    );
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

    const isPatient = user?.roleId === 1;

    return (
      <View style={styles.serviceRequestCard}>
        <TouchableOpacity
          onPress={() => onServiceRequestSelect && onServiceRequestSelect(item)}
          style={styles.cardContent}
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
        
        {/* Delete button for patients */}
        {isPatient && (
          <TouchableOpacity
            style={styles.deleteButton}
            onPress={() => handleDeleteServiceRequest(item)}
          >
            <Text style={styles.deleteButtonText}>🗑️ Delete</Text>
          </TouchableOpacity>
        )}
      </View>
    );
  };

  const isPatient = user?.roleId === 1;

  if (loading && !refreshing) {
    return (
      <View style={styles.centerContainer}>
        <ActivityIndicator size="large" color="#007bff" />
        <Text style={styles.loadingText}>Loading service requests...</Text>
      </View>
    );
  }

  if (serviceRequests.length === 0 && !searchQuery.trim()) {
    return (
      <View style={styles.centerContainer}>
        <Text style={styles.emptyText}>No service requests found</Text>
        <TouchableOpacity style={styles.refreshButton} onPress={loadServiceRequests}>
          <Text style={styles.refreshButtonText}>Refresh</Text>
        </TouchableOpacity>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* Search Bar */}
      <View style={styles.searchContainer}>
        <TextInput
          style={styles.searchInput}
          placeholder="Search service requests..."
          value={searchQuery}
          onChangeText={setSearchQuery}
          placeholderTextColor="#999"
        />
        {searchQuery.length > 0 && (
          <TouchableOpacity
            style={styles.clearButton}
            onPress={() => setSearchQuery('')}
          >
            <Text style={styles.clearButtonText}>✕</Text>
          </TouchableOpacity>
        )}
      </View>

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
        data={filteredServiceRequests}
        renderItem={renderServiceRequest}
        keyExtractor={(item) => (item.id || item.Id || Math.random()).toString()}
        contentContainerStyle={styles.listContainer}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
        ListEmptyComponent={
          searchQuery.trim() ? (
            <View style={styles.centerContainer}>
              <Text style={styles.emptyText}>No service requests found matching "{searchQuery}"</Text>
            </View>
          ) : null
        }
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  searchContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#e0e0e0',
  },
  searchInput: {
    flex: 1,
    height: 40,
    backgroundColor: '#f5f5f5',
    borderRadius: 20,
    paddingHorizontal: 16,
    fontSize: 16,
    color: '#333',
  },
  clearButton: {
    marginLeft: 8,
    width: 30,
    height: 30,
    alignItems: 'center',
    justifyContent: 'center',
  },
  clearButtonText: {
    fontSize: 18,
    color: '#666',
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
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
    overflow: 'hidden',
  },
  cardContent: {
    padding: 16,
  },
  deleteButton: {
    backgroundColor: '#dc3545',
    paddingVertical: 12,
    paddingHorizontal: 16,
    alignItems: 'center',
    borderTopWidth: 1,
    borderTopColor: '#e0e0e0',
  },
  deleteButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
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

