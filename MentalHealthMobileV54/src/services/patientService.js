import axios from 'axios';

const API_BASE_URL = 'http://192.168.86.113:5262/api';

class PatientService {
  constructor() {
    this.api = axios.create({
      baseURL: API_BASE_URL,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  async getAssignedPatients(token) {
    try {
      console.log('Getting assigned patients...');
      
      const response = await this.api.get('/mobile/doctor/patients', {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      console.log('Assigned patients response:', response.data);

      if (response.data) {
        return {
          success: true,
          patients: response.data,
        };
      } else {
        return {
          success: false,
          error: 'No data received',
        };
      }
    } catch (error) {
      console.error('Get assigned patients error:', error);
      
      if (error.response) {
        return {
          success: false,
          error: error.response.data.message || 'Failed to get patients',
        };
      } else if (error.request) {
        return {
          success: false,
          error: 'Network error. Please check your connection.',
        };
      } else {
        return {
          success: false,
          error: 'An unexpected error occurred.',
        };
      }
    }
  }

  async getPatientDetails(patientId, token) {
    try {
      const response = await this.api.get(`/user/${patientId}`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      return {
        success: true,
        patient: response.data,
      };
    } catch (error) {
      console.error('Get patient details error:', error);
      return {
        success: false,
        error: 'Failed to get patient details',
      };
    }
  }

  async getPatientStats(patientId, token) {
    try {
      const response = await this.api.get(`/user/${patientId}/stats`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      return {
        success: true,
        stats: response.data,
      };
    } catch (error) {
      console.error('Get patient stats error:', error);
      return {
        success: false,
        error: 'Failed to get patient stats',
      };
    }
  }
}

export const patientService = new PatientService();
