/**
 * App Configuration
 * 
 * Centralized configuration for the mobile app.
 * Update these values when your server IP or environment changes.
 */

const AppConfig = {
  // Server IP Address
  // STAGING: caseflowstage.store (current)
  // For local development: use your Mac's local IP (e.g., 192.168.86.25)
  // For production: use your production DNS (when ready)
  // To update: Run ./switch-to-digitalocean.sh or ./update-mobile-config-from-droplet.sh
  SERVER_IP: '104.236.221.170',  // Staging DNS
  
  // Server Port
  // Mobile app connects directly to the server API (not through the Blazor client)
  // For staging/production: use port 443 (HTTPS via Nginx)
  // For local: use port 5263 for HTTPS (5262 for HTTP)
  SERVER_PORT: 443,  // Staging/Production uses 443 (HTTPS via Nginx), local HTTPS uses 5263

  // Use HTTPS (true) or HTTP (false)
  // NOTE: HTTPS is REQUIRED for Agora video/audio calls to work
  // Staging/Production server uses HTTPS on port 443
  // For development with self-signed certificates, the app is configured to bypass
  // certificate validation (see network_security_config.xml for Android and app.json for iOS)
  USE_HTTPS: true,  // Staging/Production uses HTTPS
  
  // Development mode: Allow self-signed certificates (iOS/Android may still reject)
  // This is a flag for documentation - actual handling depends on platform
  ALLOW_SELF_SIGNED_CERT: false,
  
  // API Base URL (automatically constructed)
  get API_BASE_URL() {
    try {
      if (!this.SERVER_IP || typeof this.SERVER_IP !== 'string' || this.SERVER_IP.trim() === '') {
        throw new Error('SERVER_IP is invalid');
      }
      const protocol = this.USE_HTTPS ? 'https' : 'http';
      const serverIp = this.SERVER_IP.trim();
      // For HTTPS on port 443 (default), omit the port number
      if (this.USE_HTTPS && this.SERVER_PORT === 443) {
        const url = `${protocol}://${serverIp}/api`;
        if (!url || url.trim() === '') {
          throw new Error('Generated API URL is invalid');
        }
        return url;
      }
      const url = `${protocol}://${serverIp}:${this.SERVER_PORT}/api`;
      if (!url || url.trim() === '') {
        throw new Error('Generated API URL is invalid');
      }
      return url;
    } catch (error) {
      console.error('❌ Error constructing API_BASE_URL:', error);
      // Fallback to staging
      return 'https://caseflowstage.store/api';
    }
  },
  
  // Get API Base URL for web platform (uses localhost)
  getWebApiBaseUrl() {
    try {
      const protocol = this.USE_HTTPS ? 'https' : 'http';
      const url = `${protocol}://localhost:${this.SERVER_PORT}/api`;
      if (!url || url.trim() === '') {
        throw new Error('Generated Web API URL is invalid');
      }
      return url;
    } catch (error) {
      console.error('❌ Error constructing getWebApiBaseUrl:', error);
      return 'https://localhost:5262/api';
    }
  },
  
  // Get API Base URL for mobile platform (uses configured IP)
  getMobileApiBaseUrl() {
    try {
      const url = this.API_BASE_URL;
      if (!url || typeof url !== 'string' || url.trim() === '') {
        throw new Error('API_BASE_URL is invalid');
      }
      return url;
    } catch (error) {
      console.error('❌ Error in getMobileApiBaseUrl:', error);
      return 'https://caseflowstage.store/api';
    }

  }
};

export default AppConfig;

