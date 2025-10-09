import axios from 'axios';
import * as SecureStore from 'expo-secure-store';

const API_BASE_URL = 'http://192.168.86.113:5262/api';

class AuthService {
  constructor() {
    this.api = axios.create({
      baseURL: API_BASE_URL,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  async login(email, password) {
    try {
      console.log('Attempting login for:', email);
      
      const response = await this.api.post('/auth/login', {
        email,
        password,
      });

      console.log('Login response:', response.data);

      if (response.data && response.data.token) {
        return {
          success: true,
          token: response.data.token,
          user: response.data.user,
        };
      } else {
        return {
          success: false,
          error: response.data.message || 'Login failed',
        };
      }
    } catch (error) {
      console.error('Login error:', error);
      
      if (error.response) {
        return {
          success: false,
          error: error.response.data.message || 'Invalid credentials',
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

  async getProfile(token) {
    try {
      const response = await this.api.get('/user/profile', {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      return {
        success: true,
        user: response.data,
      };
    } catch (error) {
      console.error('Get profile error:', error);
      return {
        success: false,
        error: 'Failed to get user profile',
      };
    }
  }

  async refreshToken(token) {
    try {
      const response = await this.api.post('/auth/refresh', {}, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      return {
        success: true,
        token: response.data.token,
      };
    } catch (error) {
      console.error('Refresh token error:', error);
      return {
        success: false,
        error: 'Failed to refresh token',
      };
    }
  }

  async getToken() {
    try {
      const token = await SecureStore.getItemAsync('authToken');
      return token;
    } catch (error) {
      console.error('Error getting token:', error);
      return null;
    }
  }
}

export const authService = new AuthService();
