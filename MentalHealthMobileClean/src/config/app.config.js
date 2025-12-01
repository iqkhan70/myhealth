/**
 * App Configuration
 * 
 * Centralized configuration for the mobile app.
 * Update these values when your server IP or environment changes.
 */

const AppConfig = {
  // Server IP Address
  // Update this when your Mac's IP address changes
  // For local development: use your Mac's local IP (e.g., 192.168.86.25)
  // For production: use your DigitalOcean server IP
  SERVER_IP: '192.168.86.29',
  
  // Server Port
  // Mobile app connects directly to the server API (not through the Blazor client)
  SERVER_PORT: 5262,
  
  // Use HTTPS (true) or HTTP (false)
  // NOTE: HTTPS is REQUIRED for Agora video/audio calls to work
  // For development with self-signed certificates, the app is configured to bypass
  // certificate validation (see network_security_config.xml for Android and app.json for iOS)
  USE_HTTPS: false,
  
  // Development mode: Allow self-signed certificates (iOS/Android may still reject)
  // This is a flag for documentation - actual handling depends on platform
  ALLOW_SELF_SIGNED_CERT: true,
  
  // API Base URL (automatically constructed)
  get API_BASE_URL() {
    const protocol = this.USE_HTTPS ? 'https' : 'http';
    return `${protocol}://${this.SERVER_IP}:${this.SERVER_PORT}/api`;
  },
  
  // Get API Base URL for web platform (uses localhost)
  getWebApiBaseUrl() {
    const protocol = this.USE_HTTPS ? 'https' : 'http';
    return `${protocol}://localhost:${this.SERVER_PORT}/api`;
  },
  
  // Get API Base URL for mobile platform (uses configured IP)
  getMobileApiBaseUrl() {
    return this.API_BASE_URL;
  }
};

export default AppConfig;

