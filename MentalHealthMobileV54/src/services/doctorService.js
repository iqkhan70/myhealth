import axios from 'axios';

const API_BASE_URL = 'http://192.168.86.113:5262/api';

class DoctorService {
  constructor() {
    this.api = axios.create({
      baseURL: API_BASE_URL,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  async getAssignedDoctors(token) {
    try {
      console.log('Getting assigned doctors...');
      
      const response = await this.api.get('/mobile/patient/doctors', {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      console.log('Assigned doctors response:', response.data);

      if (response.data) {
        return {
          success: true,
          doctors: response.data,
        };
      } else {
        return {
          success: false,
          error: 'No data received',
        };
      }
    } catch (error) {
      console.error('Get assigned doctors error:', error);
      
      if (error.response) {
        return {
          success: false,
          error: error.response.data.message || 'Failed to get doctors',
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

  async getDoctorDetails(doctorId, token) {
    try {
      const response = await this.api.get(`/user/${doctorId}`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      return {
        success: true,
        doctor: response.data,
      };
    } catch (error) {
      console.error('Get doctor details error:', error);
      return {
        success: false,
        error: 'Failed to get doctor details',
      };
    }
  }

  async getAllDoctors(token) {
    try {
      const response = await this.api.get('/admin/doctors', {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      return {
        success: true,
        doctors: response.data,
      };
    } catch (error) {
      console.error('Get all doctors error:', error);
      return {
        success: false,
        error: 'Failed to get doctors',
      };
    }
  }
}

export const doctorService = new DoctorService();
