import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

class SignalRService {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.listeners = {
      onMessageReceived: null,
      onIncomingCall: null,
      onCallAccepted: null,
      onCallRejected: null,
      onCallEnded: null,
      onUserStatusChanged: null,
      onConnectionStateChanged: null,
    };
  }

  async initialize(hubUrl, token) {
    try {
      console.log('🔗 SignalR: Initializing connection to:', hubUrl);
      
      this.connection = new HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => token,
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Information)
        .build();

      // Set up event handlers
      this.setupEventHandlers();

      // Start connection
      await this.connection.start();
      this.isConnected = true;
      console.log('✅ SignalR: Connected successfully');
      
      if (this.listeners.onConnectionStateChanged) {
        this.listeners.onConnectionStateChanged('Connected');
      }

      return true;
    } catch (error) {
      console.error('❌ SignalR: Connection failed:', error);
      this.isConnected = false;
      
      if (this.listeners.onConnectionStateChanged) {
        this.listeners.onConnectionStateChanged('Disconnected');
      }
      
      return false;
    }
  }

  setupEventHandlers() {
    if (!this.connection) return;

    // Handle incoming messages
    this.connection.on('new-message', (message) => {
      console.log('📨 SignalR: New message received:', message);
      if (this.listeners.onMessageReceived) {
        this.listeners.onMessageReceived(message);
      }
    });

    // Handle incoming calls
    this.connection.on('incoming-call', (callData) => {
      console.log('📞 SignalR: Incoming call:', callData);
      if (this.listeners.onIncomingCall) {
        this.listeners.onIncomingCall(callData);
      }
    });

    // Handle call accepted
    this.connection.on('call-accepted', (callData) => {
      console.log('✅ SignalR: Call accepted:', callData);
      if (this.listeners.onCallAccepted) {
        this.listeners.onCallAccepted(callData);
      }
    });

    // Handle call rejected
    this.connection.on('call-rejected', (callData) => {
      console.log('❌ SignalR: Call rejected:', callData);
      if (this.listeners.onCallRejected) {
        this.listeners.onCallRejected(callData);
      }
    });

    // Handle call ended
    this.connection.on('call-ended', (callData) => {
      console.log('👋 SignalR: Call ended:', callData);
      if (this.listeners.onCallEnded) {
        this.listeners.onCallEnded(callData);
      }
    });

    // Handle user status changes
    this.connection.on('user-status-changed', (statusData) => {
      console.log('👤 SignalR: User status changed:', statusData);
      if (this.listeners.onUserStatusChanged) {
        this.listeners.onUserStatusChanged(statusData);
      }
    });

    // Handle connection state changes
    this.connection.onclose((error) => {
      console.log('🔌 SignalR: Connection closed:', error);
      this.isConnected = false;
      if (this.listeners.onConnectionStateChanged) {
        this.listeners.onConnectionStateChanged('Disconnected');
      }
    });

    this.connection.onreconnecting((error) => {
      console.log('🔄 SignalR: Reconnecting:', error);
      this.isConnected = false;
      if (this.listeners.onConnectionStateChanged) {
        this.listeners.onConnectionStateChanged('Reconnecting');
      }
    });

    this.connection.onreconnected((connectionId) => {
      console.log('✅ SignalR: Reconnected:', connectionId);
      this.isConnected = true;
      if (this.listeners.onConnectionStateChanged) {
        this.listeners.onConnectionStateChanged('Connected');
      }
    });
  }

  // Send message
  async sendMessage(targetUserId, message) {
    if (!this.isConnected || !this.connection) {
      console.warn('⚠️ SignalR: Not connected, cannot send message');
      return false;
    }

    try {
      await this.connection.invoke('SendMessage', targetUserId, message);
      console.log('📤 SignalR: Message sent successfully');
      return true;
    } catch (error) {
      console.error('❌ SignalR: Failed to send message:', error);
      return false;
    }
  }

  // Accept call
  async acceptCall(callId) {
    if (!this.isConnected || !this.connection) {
      console.warn('⚠️ SignalR: Not connected, cannot accept call');
      return false;
    }

    try {
      await this.connection.invoke('AcceptCall', callId);
      console.log('✅ SignalR: Call accepted successfully');
      return true;
    } catch (error) {
      console.error('❌ SignalR: Failed to accept call:', error);
      return false;
    }
  }

  // Reject call
  async rejectCall(callId) {
    if (!this.isConnected || !this.connection) {
      console.warn('⚠️ SignalR: Not connected, cannot reject call');
      return false;
    }

    try {
      await this.connection.invoke('RejectCall', callId);
      console.log('❌ SignalR: Call rejected successfully');
      return true;
    } catch (error) {
      console.error('❌ SignalR: Failed to reject call:', error);
      return false;
    }
  }

  // End call
  async endCall(callId) {
    if (!this.isConnected || !this.connection) {
      console.warn('⚠️ SignalR: Not connected, cannot end call');
      return false;
    }

    try {
      await this.connection.invoke('EndCall', callId);
      console.log('👋 SignalR: Call ended successfully');
      return true;
    } catch (error) {
      console.error('❌ SignalR: Failed to end call:', error);
      return false;
    }
  }

  // Set event listeners
  setEventListener(event, callback) {
    if (this.listeners.hasOwnProperty(event)) {
      this.listeners[event] = callback;
    }
  }

  // Disconnect
  async disconnect() {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log('🔌 SignalR: Disconnected successfully');
      } catch (error) {
        console.error('❌ SignalR: Error during disconnect:', error);
      }
      this.connection = null;
      this.isConnected = false;
    }
  }

  // Get connection state
  getConnectionState() {
    if (!this.connection) return 'Disconnected';
    return this.connection.state;
  }
}

// Export singleton instance
export default new SignalRService();
