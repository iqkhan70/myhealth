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

const EATS_ORANGE = '#f97316';
const EATS_BG = '#f5f5f5';
const EATS_TEXT = '#333';
const EATS_MUTED = '#666';
const EATS_SOFT = '#fff7ed';
const EATS_BORDER = '#fed7aa';

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
        <ActivityIndicator size="large" color={EATS_ORANGE} />
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
    backgroundColor: EATS_BG,
  },
  searchContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: EATS_BG,
  },
  searchInput: {
    flex: 1,
    height: 46,
    backgroundColor: '#fff',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#eeeeee',
    paddingHorizontal: 16,
    fontSize: 16,
    color: EATS_TEXT,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.05,
    shadowRadius: 8,
    elevation: 2,
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
    backgroundColor: EATS_SOFT,
    paddingVertical: 14,
    paddingHorizontal: 16,
    borderRadius: 18,
    borderWidth: 1,
    borderColor: EATS_BORDER,
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: EATS_ORANGE,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.08,
    shadowRadius: 10,
    elevation: 4,
  },
  createButtonText: {
    color: '#9a3412',
    fontSize: 16,
    fontWeight: '800',
  },
  serviceRequestCard: {
    backgroundColor: '#fff',
    borderRadius: 18,
    marginBottom: 12,
    borderWidth: 1,
    borderColor: '#eeeeee',
    shadowColor: EATS_ORANGE,
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.07,
    shadowRadius: 12,
    elevation: 3,
    overflow: 'hidden',
  },
  cardContent: {
    padding: 16,
  },
  deleteButton: {
    backgroundColor: '#fff5f5',
    paddingVertical: 12,
    paddingHorizontal: 16,
    alignItems: 'center',
    borderTopWidth: 1,
    borderTopColor: '#fee2e2',
  },
  deleteButtonText: {
    color: '#dc2626',
    fontSize: 16,
    fontWeight: '700',
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  title: {
    fontSize: 18,
    fontWeight: '800',
    color: EATS_TEXT,
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
    color: EATS_TEXT,
    fontWeight: '700',
  },
  type: {
    fontSize: 14,
    color: EATS_MUTED,
  },
  description: {
    fontSize: 14,
    color: EATS_MUTED,
    marginTop: 4,
    lineHeight: 19,
  },
  assignedSmes: {
    fontSize: 14,
    color: EATS_ORANGE,
    marginTop: 4,
    fontWeight: '700',
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
    backgroundColor: EATS_SOFT,
    paddingHorizontal: 20,
    paddingVertical: 12,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: EATS_BORDER,
  },
  refreshButtonText: {
    color: '#9a3412',
    fontSize: 16,
    fontWeight: '800',
  },
});

export default ServiceRequestList;

