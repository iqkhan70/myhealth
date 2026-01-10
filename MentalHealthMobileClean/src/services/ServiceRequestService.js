import AsyncStorage from '@react-native-async-storage/async-storage';
import { Platform } from 'react-native';
import AppConfig from '../config/app.config';

// API Configuration - uses centralized config
const getApiBaseUrl = () => {
  const isWeb = Platform.OS === 'web' && typeof window !== 'undefined';
  if (isWeb) return AppConfig.getWebApiBaseUrl();
  return AppConfig.getMobileApiBaseUrl();
};

const API_BASE_URL = getApiBaseUrl();

class ServiceRequestService {
  constructor() {
    this.baseUrl = API_BASE_URL;
  }

  // Get authentication token from storage
  async getAuthToken() {
    try {
      const token = await AsyncStorage.getItem('userToken');
      return token;
    } catch (error) {
      console.error('Error getting auth token:', error);
      return null;
    }
  }

  // Get headers with authentication
  async getHeaders() {
    const token = await this.getAuthToken();
    return {
      'Content-Type': 'application/json',
      'Authorization': token ? `Bearer ${token}` : '',
    };
  }

  // Get all service requests for the current user (SME sees assigned, Patient sees their own)
  async getServiceRequests() {
    try {
      const headers = await this.getHeaders();
      const response = await fetch(`${this.baseUrl}/ServiceRequest`, {
        method: 'GET',
        headers,
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      // Handle OData response format
      if (data && typeof data === 'object' && 'value' in data) {
        return data.value;
      }
      // Handle direct array response
      if (Array.isArray(data)) {
        return data;
      }
      // Fallback
      return [];
    } catch (error) {
      console.error('Error fetching service requests:', error);
      throw error;
    }
  }

  // Get service request by ID
  async getServiceRequestById(id) {
    try {
      const headers = await this.getHeaders();
      const response = await fetch(`${this.baseUrl}/ServiceRequest/${id}`, {
        method: 'GET',
        headers,
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error fetching service request:', error);
      throw error;
    }
  }

  // Get default service request for a client (for backward compatibility)
  async getDefaultServiceRequestForClient(clientId) {
    try {
      const headers = await this.getHeaders();
      const response = await fetch(`${this.baseUrl}/ServiceRequest/default/${clientId}`, {
        method: 'GET',
        headers,
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error fetching default service request:', error);
      throw error;
    }
  }

  // Create a new service request
  async createServiceRequest(serviceRequestData) {
    try {
      const headers = await this.getHeaders();
      // Match the pattern used by other services (baseUrl already includes /api)
      // Controller route is [Route("api/[controller]")], so use /ServiceRequest
      const url = `${this.baseUrl}/ServiceRequest`;
      console.log('Creating service request at:', url);
      console.log('Request data:', JSON.stringify(serviceRequestData, null, 2));
      
      const response = await fetch(url, {
        method: 'POST',
        headers,
        body: JSON.stringify(serviceRequestData),
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('Create SR failed:', {
          status: response.status,
          statusText: response.statusText,
          url: url,
          error: errorText
        });
        throw new Error(`HTTP error! status: ${response.status}, message: ${errorText}`);
      }

      const result = await response.json();
      console.log('Service request created successfully:', result);
      return result;
    } catch (error) {
      console.error('Error creating service request:', error);
      throw error;
    }
  }

  // Delete (soft delete) a service request
  async deleteServiceRequest(id) {
    try {
      const headers = await this.getHeaders();
      const response = await fetch(`${this.baseUrl}/ServiceRequest/${id}`, {
        method: 'DELETE',
        headers,
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`HTTP error! status: ${response.status}, message: ${errorText}`);
      }

      return true;
    } catch (error) {
      console.error('Error deleting service request:', error);
      throw error;
    }
  }
}

export default new ServiceRequestService();

