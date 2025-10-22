import AsyncStorage from '@react-native-async-storage/async-storage';
import { Platform } from 'react-native';

// API Configuration
const getApiBaseUrl = () => {
  const isWeb = Platform.OS === 'web' && typeof window !== 'undefined';
  if (isWeb) return 'http://localhost:5262/api';
  return 'http://192.168.86.27:5262/api';
};

const API_BASE_URL = getApiBaseUrl();

class DocumentUploadService {
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

  // Initiate document upload
  async initiateUpload(uploadRequest) {
    try {
      const headers = await this.getHeaders();
      const response = await fetch(`${this.baseUrl}/DocumentUpload/initiate`, {
        method: 'POST',
        headers,
        body: JSON.stringify(uploadRequest),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error initiating upload:', error);
      throw error;
    }
  }

  // Complete document upload
  async completeUpload(contentId, s3Key) {
    try {
      const headers = await this.getHeaders();
      const response = await fetch(`${this.baseUrl}/DocumentUpload/complete/${contentId}`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ s3Key }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error completing upload:', error);
      throw error;
    }
  }

  // Upload file to S3
  async uploadToS3(uploadUrl, fileUri, contentType) {
    try {
      const formData = new FormData();
      formData.append('file', {
        uri: fileUri,
        type: contentType,
        name: fileUri.split('/').pop(),
      });

      const response = await fetch(uploadUrl, {
        method: 'PUT',
        body: formData,
        headers: {
          'Content-Type': contentType,
        },
      });

      if (!response.ok) {
        throw new Error(`S3 upload failed: ${response.status}`);
      }

      return true;
    } catch (error) {
      console.error('Error uploading to S3:', error);
      throw error;
    }
  }

  // Get documents list
  async getDocuments(patientId, filters = {}) {
    try {
      const headers = await this.getHeaders();
      const queryParams = new URLSearchParams({
        patientId: patientId.toString(),
        page: filters.page || 1,
        pageSize: filters.pageSize || 20,
        ...(filters.type && { type: filters.type }),
        ...(filters.category && { category: filters.category }),
        ...(filters.fromDate && { fromDate: filters.fromDate }),
        ...(filters.toDate && { toDate: filters.toDate }),
      });

      const response = await fetch(`${this.baseUrl}/DocumentUpload/list?${queryParams}`, {
        method: 'GET',
        headers,
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error getting documents:', error);
      throw error;
    }
  }

  // Get specific document
  async getDocument(contentId) {
    try {
      const headers = await this.getHeaders();
      const response = await fetch(`${this.baseUrl}/DocumentUpload/${contentId}`, {
        method: 'GET',
        headers,
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error getting document:', error);
      throw error;
    }
  }

  // Get download URL
  async getDownloadUrl(contentId) {
    try {
      const headers = await this.getHeaders();
      const response = await fetch(`${this.baseUrl}/DocumentUpload/${contentId}/download`, {
        method: 'GET',
        headers,
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      return result.downloadUrl;
    } catch (error) {
      console.error('Error getting download URL:', error);
      throw error;
    }
  }

  // Get thumbnail URL
  async getThumbnailUrl(contentId) {
    try {
      const headers = await this.getHeaders();
      const response = await fetch(`${this.baseUrl}/DocumentUpload/${contentId}/thumbnail`, {
        method: 'GET',
        headers,
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      return result.thumbnailUrl;
    } catch (error) {
      console.error('Error getting thumbnail URL:', error);
      throw error;
    }
  }

  // Delete document
  async deleteDocument(contentId) {
    try {
      const headers = await this.getHeaders();
      const response = await fetch(`${this.baseUrl}/DocumentUpload/${contentId}`, {
        method: 'DELETE',
        headers,
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error deleting document:', error);
      throw error;
    }
  }

  // Get categories
  async getCategories() {
    try {
      const headers = await this.getHeaders();
      const response = await fetch(`${this.baseUrl}/DocumentUpload/categories`, {
        method: 'GET',
        headers,
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      return result.categories;
    } catch (error) {
      console.error('Error getting categories:', error);
      throw error;
    }
  }

  // Get validation rules
  async getValidationRules() {
    try {
      const headers = await this.getHeaders();
      const response = await fetch(`${this.baseUrl}/DocumentUpload/validation-rules`, {
        method: 'GET',
        headers,
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error getting validation rules:', error);
      throw error;
    }
  }

  // Complete upload process (initiate + upload to S3 + complete)
  async uploadDocument(fileUri, uploadRequest) {
    try {
      // Step 1: Initiate upload
      const initiateResponse = await this.initiateUpload(uploadRequest);
      
      if (!initiateResponse.success) {
        throw new Error(initiateResponse.message || 'Failed to initiate upload');
      }

      // Step 2: Upload to S3
      await this.uploadToS3(initiateResponse.uploadUrl, fileUri, uploadRequest.contentType);

      // Step 3: Complete upload
      const completeResponse = await this.completeUpload(
        initiateResponse.contentId, 
        `documents/${uploadRequest.patientId}/${uploadRequest.fileName}`
      );

      if (!completeResponse.success) {
        throw new Error(completeResponse.message || 'Failed to complete upload');
      }

      return completeResponse;
    } catch (error) {
      console.error('Error uploading document:', error);
      throw error;
    }
  }

  // Utility method to get file info
  getFileInfo(fileUri) {
    const fileName = fileUri.split('/').pop();
    const extension = fileName.split('.').pop().toLowerCase();
    
    let contentType = 'application/octet-stream';
    let type = 'Other';

    // Determine content type and type based on extension
    switch (extension) {
      case 'jpg':
      case 'jpeg':
        contentType = 'image/jpeg';
        type = 'Image';
        break;
      case 'png':
        contentType = 'image/png';
        type = 'Image';
        break;
      case 'gif':
        contentType = 'image/gif';
        type = 'Image';
        break;
      case 'mp4':
        contentType = 'video/mp4';
        type = 'Video';
        break;
      case 'avi':
        contentType = 'video/avi';
        type = 'Video';
        break;
      case 'mov':
        contentType = 'video/quicktime';
        type = 'Video';
        break;
      case 'mp3':
        contentType = 'audio/mp3';
        type = 'Audio';
        break;
      case 'wav':
        contentType = 'audio/wav';
        type = 'Audio';
        break;
      case 'pdf':
        contentType = 'application/pdf';
        type = 'Document';
        break;
      case 'doc':
        contentType = 'application/msword';
        type = 'Document';
        break;
      case 'docx':
        contentType = 'application/vnd.openxmlformats-officedocument.wordprocessingml.document';
        type = 'Document';
        break;
    }

    return {
      fileName,
      contentType,
      type,
      extension
    };
  }

  // Format file size
  formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }
}

export default new DocumentUploadService();
