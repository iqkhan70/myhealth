// Simplified WebRTC Service for Expo Demo
// Note: Real WebRTC requires native modules not available in Expo managed workflow
// This is a demo implementation that simulates the call flow

class WebRTCService {
  constructor() {
    this.localStream = null;
    this.remoteStream = null;
    this.peerConnection = null;
    this.isInitiator = false;
    this.targetUserId = null;
    this.socket = null;
    this.callbacks = {
      onLocalStream: null,
      onRemoteStream: null,
      onCallEnded: null,
      onError: null,
    };
  }

  // Initialize WebRTC with socket connection
  initialize(socket) {
    this.socket = socket;
    this.setupSocketListeners();
  }

  // Set up socket event listeners for WebRTC signaling
  setupSocketListeners() {
    if (!this.socket) return;

    this.socket.on('webrtc-offer', async (data) => {
      if (data.targetUserId === this.targetUserId) {
        await this.handleOffer(data.offer);
      }
    });

    this.socket.on('webrtc-answer', async (data) => {
      if (data.targetUserId === this.targetUserId) {
        await this.handleAnswer(data.answer);
      }
    });

    this.socket.on('webrtc-ice-candidate', async (data) => {
      if (data.targetUserId === this.targetUserId) {
        await this.handleICECandidate(data.candidate);
      }
    });

    this.socket.on('call-ended', () => {
      this.endCall();
    });
  }

  // Start a call (initiate or answer)
  async startCall(targetUserId, isInitiator = false, callType = 'video') {
    try {
      this.targetUserId = targetUserId;
      this.isInitiator = isInitiator;

      // Simulate getting local media stream
      await this.getLocalStream(callType);

      // Simulate creating peer connection
      await this.createPeerConnection();

      // If initiator, create offer
      if (isInitiator) {
        await this.createOffer();
      }

      return true;
    } catch (error) {
      console.error('Error starting call:', error);
      this.handleError(error);
      return false;
    }
  }

  // Simulate getting local media stream
  async getLocalStream(callType = 'video') {
    try {
      // In a real implementation, this would get actual camera/mic access
      // For demo purposes, we'll simulate this
      this.localStream = {
        toURL: () => 'demo://local-stream',
        getTracks: () => [],
        getVideoTracks: () => [{ enabled: true }],
        getAudioTracks: () => [{ enabled: true }]
      };
      
      if (this.callbacks.onLocalStream) {
        this.callbacks.onLocalStream(this.localStream);
      }

      return this.localStream;
    } catch (error) {
      console.error('Error getting local stream:', error);
      throw error;
    }
  }

  // Simulate creating peer connection
  async createPeerConnection() {
    // In a real implementation, this would create an RTCPeerConnection
    this.peerConnection = {
      connectionState: 'connected',
      addTrack: () => {},
      close: () => {}
    };

    // Simulate remote stream after a delay
    setTimeout(() => {
      this.remoteStream = {
        toURL: () => 'demo://remote-stream',
        getTracks: () => [],
        getVideoTracks: () => [{ enabled: true }],
        getAudioTracks: () => [{ enabled: true }]
      };
      
      if (this.callbacks.onRemoteStream) {
        this.callbacks.onRemoteStream(this.remoteStream);
      }
    }, 2000);
  }

  // Create offer (for initiator)
  async createOffer() {
    try {
      // Simulate WebRTC offer
      const offer = { type: 'offer', sdp: 'demo-offer' };
      
      if (this.socket) {
        this.socket.emit('webrtc-offer', {
          targetUserId: this.targetUserId,
          offer: offer
        });
      }
    } catch (error) {
      console.error('Error creating offer:', error);
      throw error;
    }
  }

  // Handle incoming offer
  async handleOffer(offer) {
    try {
      // Simulate handling offer
      const answer = { type: 'answer', sdp: 'demo-answer' };
      
      if (this.socket) {
        this.socket.emit('webrtc-answer', {
          targetUserId: this.targetUserId,
          answer: answer
        });
      }
    } catch (error) {
      console.error('Error handling offer:', error);
      throw error;
    }
  }

  // Handle incoming answer
  async handleAnswer(answer) {
    try {
      // Simulate handling answer
      console.log('Answer received:', answer);
    } catch (error) {
      console.error('Error handling answer:', error);
      throw error;
    }
  }

  // Handle ICE candidate
  async handleICECandidate(candidate) {
    try {
      // Simulate handling ICE candidate
      console.log('ICE candidate received:', candidate);
    } catch (error) {
      console.error('Error handling ICE candidate:', error);
    }
  }

  // End the call
  endCall() {
    try {
      if (this.localStream) {
        this.localStream = null;
      }

      if (this.peerConnection) {
        this.peerConnection.close();
        this.peerConnection = null;
      }

      this.remoteStream = null;
      this.targetUserId = null;
      this.isInitiator = false;

      if (this.callbacks.onCallEnded) {
        this.callbacks.onCallEnded();
      }
    } catch (error) {
      console.error('Error ending call:', error);
    }
  }

  // Toggle camera
  toggleCamera() {
    if (this.localStream) {
      const videoTrack = this.localStream.getVideoTracks()[0];
      if (videoTrack) {
        videoTrack.enabled = !videoTrack.enabled;
      }
    }
  }

  // Toggle microphone
  toggleMicrophone() {
    if (this.localStream) {
      const audioTrack = this.localStream.getAudioTracks()[0];
      if (audioTrack) {
        audioTrack.enabled = !audioTrack.enabled;
      }
    }
  }

  // Switch camera (front/back)
  switchCamera() {
    if (this.localStream) {
      const videoTrack = this.localStream.getVideoTracks()[0];
      if (videoTrack && videoTrack.getCapabilities) {
        const capabilities = videoTrack.getCapabilities();
        if (capabilities.facingMode && capabilities.facingMode.length > 1) {
          const currentFacingMode = videoTrack.getSettings().facingMode;
          const newFacingMode = currentFacingMode === 'user' ? 'environment' : 'user';
          
          videoTrack.applyConstraints({
            facingMode: newFacingMode
          });
        }
      }
    }
  }

  // Set callbacks
  setCallbacks(callbacks) {
    this.callbacks = { ...this.callbacks, ...callbacks };
  }

  // Handle errors
  handleError(error) {
    console.error('WebRTC Error:', error);
    if (this.callbacks.onError) {
      this.callbacks.onError(error);
    }
  }

  // Get connection state
  getConnectionState() {
    return this.peerConnection ? this.peerConnection.connectionState : 'disconnected';
  }

  // Check if call is active
  isCallActive() {
    return this.peerConnection && this.peerConnection.connectionState === 'connected';
  }
}

export default new WebRTCService();