import AsyncStorage from '@react-native-async-storage/async-storage';
import { Platform } from 'react-native';
import * as FileSystem from 'expo-file-system/legacy'; // Use legacy API for compatibility
import AppConfig from '../config/app.config';

// API Configuration - uses centralized config
const getApiBaseUrl = () => {
  const isWeb = Platform.OS === 'web' && typeof window !== 'undefined';
  // For web/localhost development, use HTTPS (server runs on HTTPS)
  if (isWeb) return AppConfig.getWebApiBaseUrl();
  // For mobile: use HTTPS with configured server IP
  return AppConfig.getMobileApiBaseUrl();
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
      console.log('📤 Initiating upload request:', JSON.stringify(uploadRequest, null, 2));
      console.log('📤 API URL:', `${this.baseUrl}/DocumentUpload/initiate`);
      
      const response = await fetch(`${this.baseUrl}/DocumentUpload/initiate`, {
        method: 'POST',
        headers,
        body: JSON.stringify(uploadRequest),
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('❌ Upload initiation failed:', {
          status: response.status,
          statusText: response.statusText,
          error: errorText
        });
        
        let errorMessage = `HTTP error! status: ${response.status}`;
        try {
          const errorJson = JSON.parse(errorText);
          errorMessage = errorJson.message || errorJson.Message || errorMessage;
        } catch (e) {
          errorMessage = errorText || errorMessage;
        }
        
        throw new Error(errorMessage);
      }

      const result = await response.json();
      console.log('✅ Upload initiated successfully:', result);
      return result;
    } catch (error) {
      console.error('❌ Error initiating upload:', error);
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
      console.log('📤 Uploading to S3:', { uploadUrl: uploadUrl.substring(0, 50) + '...', fileUri, contentType });
      
      // For S3 presigned PUT URLs, we need to upload the raw file content
      // Read the file as base64 using legacy API
      const fileBase64 = await FileSystem.readAsStringAsync(fileUri, {
        encoding: FileSystem.EncodingType?.Base64 ?? 'base64',
      });
      
      console.log('📤 File read as base64, length:', fileBase64.length);
      
      // Convert base64 to binary for S3
      // Use XMLHttpRequest for better binary support in React Native
      return new Promise((resolve, reject) => {
        // Decode base64 to binary
        let binaryString;
        if (Platform.OS === 'web' && typeof atob !== 'undefined') {
          binaryString = atob(fileBase64);
        } else {
          // Manual base64 decode for React Native
          const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=';
          let output = '';
          let i = 0;
          const cleanBase64 = fileBase64.replace(/[^A-Za-z0-9\+\/\=]/g, '');
          while (i < cleanBase64.length) {
            const enc1 = chars.indexOf(cleanBase64.charAt(i++));
            const enc2 = chars.indexOf(cleanBase64.charAt(i++));
            const enc3 = chars.indexOf(cleanBase64.charAt(i++));
            const enc4 = chars.indexOf(cleanBase64.charAt(i++));
            const chr1 = (enc1 << 2) | (enc2 >> 4);
            const chr2 = ((enc2 & 15) << 4) | (enc3 >> 2);
            const chr3 = ((enc3 & 3) << 6) | enc4;
            output += String.fromCharCode(chr1);
            if (enc3 !== 64) output += String.fromCharCode(chr2);
            if (enc4 !== 64) output += String.fromCharCode(chr3);
          }
          binaryString = output;
        }
        
        // Convert to Uint8Array
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
          bytes[i] = binaryString.charCodeAt(i);
        }
        
        console.log('📤 Uploading', bytes.length, 'bytes to S3...');
        console.log('📤 Content-Type:', contentType);
        console.log('📤 Upload URL (first 100 chars):', uploadUrl.substring(0, 100));
        
        // For S3 presigned PUT URLs, we must use the EXACT Content-Type
        // and not add any extra headers, otherwise signature will mismatch
        // Use fetch with ArrayBuffer directly
        fetch(uploadUrl, {
          method: 'PUT',
          body: bytes.buffer, // Send ArrayBuffer directly
          headers: {
            'Content-Type': contentType, // Must match exactly what was used to generate presigned URL
            // Don't add any other headers - S3 presigned URLs are sensitive to header changes
          },
        })
          .then(async (response) => {
            if (!response.ok) {
              const errorText = await response.text();
              console.error('❌ S3 upload failed:', response.status, response.statusText);
              console.error('❌ S3 error response:', errorText);
              throw new Error(`S3 upload failed: ${response.status} - ${errorText}`);
            }
            console.log('✅ S3 upload successful');
            resolve(true);
          })
          .catch((error) => {
            console.error('❌ Error uploading to S3:', error);
            reject(error);
          });
      });
    } catch (error) {
      console.error('❌ Error uploading to S3:', error);
      console.error('❌ Error details:', error.message, error.stack);
      throw error;
    }
  }

  // Get documents list
  async getDocuments(patientId, filters = {}, serviceRequestId = null) {
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
        ...(serviceRequestId && { serviceRequestId: serviceRequestId.toString() }),
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
      // Use MimeType from uploadRequest (PascalCase) - must match exactly what was used to generate presigned URL
      await this.uploadToS3(initiateResponse.uploadUrl, fileUri, uploadRequest.MimeType);

      // Step 3: Complete upload
      // Use the S3 key returned from the initiate response
      // The server generates: documents/{PatientId}/{ContentGuid}/{FileName}
      if (!initiateResponse.s3Key) {
        throw new Error('S3 key not returned from initiate upload');
      }
      const completeResponse = await this.completeUpload(
        initiateResponse.contentId, 
        initiateResponse.s3Key
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
