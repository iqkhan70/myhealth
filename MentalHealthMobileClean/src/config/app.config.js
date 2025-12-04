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
  // For production: use your DigitalOcean server IP (159.65.242.79)
  SERVER_IP: '159.65.242.79',  // DigitalOcean server
  
  // Server Port
  // Mobile app connects directly to the server API (not through the Blazor client)
  // For DigitalOcean: use port 443 (HTTPS via Nginx)
  // For local: use port 5262
  SERVER_PORT: 443,  // DigitalOcean uses 443, local uses 5262
  
  // Use HTTPS (true) or HTTP (false)
  // NOTE: HTTPS is REQUIRED for Agora video/audio calls to work
  // DigitalOcean server uses HTTPS on port 443
  // For development with self-signed certificates, the app is configured to bypass
  // certificate validation (see network_security_config.xml for Android and app.json for iOS)
  USE_HTTPS: true,  // DigitalOcean uses HTTPS
  
  // Development mode: Allow self-signed certificates (iOS/Android may still reject)
  // This is a flag for documentation - actual handling depends on platform
  ALLOW_SELF_SIGNED_CERT: true,
  
  // API Base URL (automatically constructed)
  get API_BASE_URL() {
    const protocol = this.USE_HTTPS ? 'https' : 'http';
    // For HTTPS on port 443 (default), omit the port number
    if (this.USE_HTTPS && this.SERVER_PORT === 443) {
      return `${protocol}://${this.SERVER_IP}/api`;
    }
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

