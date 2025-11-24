/**
 * Fetch utility with better error handling for self-signed certificates
 * 
 * This is a wrapper around the standard fetch API that provides:
 * - Better error messages for SSL/certificate issues
 * - Consistent error handling across the app
 * - Support for development with self-signed certificates
 * 
 * Note: The actual certificate bypass happens at the native level:
 * - Android: network_security_config.xml
 * - iOS: App Transport Security settings in app.json
 * 
 * This utility just provides better error messages and consistent API.
 */

import { Platform } from 'react-native';
import AppConfig from '../config/app.config';

/**
 * Enhanced fetch wrapper with better error handling
 * @param {string} url - The URL to fetch
 * @param {RequestInit} options - Fetch options (method, headers, body, etc.)
 * @returns {Promise<Response>} - Fetch response
 */
export const fetchWithCertBypass = async (url, options = {}) => {
  try {
    const response = await fetch(url, options);
    return response;
  } catch (error) {
    // Provide helpful error messages for common SSL/certificate issues
    if (error.message && (
      error.message.includes('Network request failed') ||
      error.message.includes('SSL') ||
      error.message.includes('certificate') ||
      error.message.includes('CERT') ||
      error.message.includes('TLS')
    )) {
      const isHttps = url.startsWith('https://');
      const serverIp = AppConfig.SERVER_IP;
      
      console.error('🔒 SSL/Certificate Error:', error.message);
      console.error('📱 Platform:', Platform.OS);
      console.error('🌐 URL:', url);
      
      // Create a more helpful error message
      const helpfulError = new Error(
        `SSL Certificate Error: Cannot connect to ${serverIp}\n\n` +
        `This usually means:\n` +
        `1. The server is using a self-signed certificate\n` +
        `2. The certificate needs to be trusted on your device\n\n` +
        `Solutions:\n` +
        `• Android: The app should automatically trust user certificates via network_security_config.xml\n` +
        `• iOS: Check App Transport Security settings in app.json\n` +
        `• Make sure you're using HTTPS (required for Agora)\n` +
        `• Verify the server IP is correct: ${serverIp}\n\n` +
        `Original error: ${error.message}`
      );
      
      helpfulError.originalError = error;
      helpfulError.isSSLError = true;
      throw helpfulError;
    }
    
    // Re-throw other errors as-is
    throw error;
  }
};

/**
 * Fetch JSON with automatic parsing and error handling
 * @param {string} url - The URL to fetch
 * @param {RequestInit} options - Fetch options
 * @returns {Promise<any>} - Parsed JSON response
 */
export const fetchJson = async (url, options = {}) => {
  const response = await fetchWithCertBypass(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
  });
  
  if (!response.ok) {
    const errorText = await response.text();
    let errorData;
    try {
      errorData = JSON.parse(errorText);
    } catch {
      errorData = { message: errorText };
    }
    
    const error = new Error(errorData.message || `HTTP ${response.status}: ${response.statusText}`);
    error.status = response.status;
    error.data = errorData;
    throw error;
  }
  
  return await response.json();
};

export default fetchWithCertBypass;

