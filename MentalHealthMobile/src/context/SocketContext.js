import React, { createContext, useContext, useEffect, useState } from 'react';
import io from 'socket.io-client';
import { useAuth } from './AuthContext';

const SocketContext = createContext();

export const useSocket = () => {
  const context = useContext(SocketContext);
  if (!context) {
    throw new Error('useSocket must be used within a SocketProvider');
  }
  return context;
};

export const SocketProvider = ({ children }) => {
  const [socket, setSocket] = useState(null);
  const [isConnected, setIsConnected] = useState(false);
  const [incomingCall, setIncomingCall] = useState(null);
  const [messages, setMessages] = useState([]);
  const { user, token } = useAuth();

  useEffect(() => {
    if (user && token) {
      initializeSocket();
    } else {
      disconnectSocket();
    }

    return () => {
      disconnectSocket();
    };
  }, [user, token]);

  const initializeSocket = () => {
    const newSocket = io('http://192.168.86.113:5262', {
      auth: {
        token: token,
        userId: user.id,
        userType: user.roleId === 2 ? 'doctor' : 'patient'
      },
      transports: ['websocket']
    });

    newSocket.on('connect', () => {
      console.log('Connected to server');
      setIsConnected(true);
    });

    newSocket.on('disconnect', () => {
      console.log('Disconnected from server');
      setIsConnected(false);
    });

    // Call invitation events
    newSocket.on('incoming-call', (callData) => {
      console.log('Incoming call:', callData);
      setIncomingCall(callData);
    });

    newSocket.on('call-accepted', (callData) => {
      console.log('Call accepted:', callData);
      // Navigate to call screen
    });

    newSocket.on('call-rejected', (callData) => {
      console.log('Call rejected:', callData);
      setIncomingCall(null);
    });

    newSocket.on('call-ended', () => {
      console.log('Call ended');
      setIncomingCall(null);
    });

    // Chat events
    newSocket.on('new-message', (message) => {
      console.log('New message:', message);
      setMessages(prev => [...prev, message]);
    });

    // WebRTC signaling events
    newSocket.on('webrtc-offer', (data) => {
      console.log('WebRTC offer received:', data);
    });

    newSocket.on('webrtc-answer', (data) => {
      console.log('WebRTC answer received:', data);
    });

    newSocket.on('webrtc-ice-candidate', (data) => {
      console.log('ICE candidate received:', data);
    });

    setSocket(newSocket);
  };

  const disconnectSocket = () => {
    if (socket) {
      socket.disconnect();
      setSocket(null);
      setIsConnected(false);
    }
  };

  // Call functions
  const initiateCall = (targetUserId, callType) => {
    if (socket && isConnected) {
      socket.emit('initiate-call', {
        targetUserId,
        callType, // 'video' or 'audio'
        callerInfo: {
          id: user.id,
          name: `${user.firstName} ${user.lastName}`,
          role: user.roleId === 2 ? 'Doctor' : 'Patient'
        }
      });
    }
  };

  const acceptCall = (callId) => {
    if (socket && isConnected) {
      socket.emit('accept-call', { callId });
      setIncomingCall(null);
    }
  };

  const rejectCall = (callId) => {
    if (socket && isConnected) {
      socket.emit('reject-call', { callId });
      setIncomingCall(null);
    }
  };

  const endCall = (callId) => {
    if (socket && isConnected) {
      socket.emit('end-call', { callId });
    }
  };

  // Chat functions
  const sendMessage = (targetUserId, message) => {
    if (socket && isConnected) {
      const messageData = {
        targetUserId,
        message,
        senderId: user.id,
        senderName: `${user.firstName} ${user.lastName}`,
        timestamp: new Date().toISOString()
      };
      
      socket.emit('send-message', messageData);
      setMessages(prev => [...prev, { ...messageData, isSent: true }]);
    }
  };

  // WebRTC signaling functions
  const sendWebRTCOffer = (targetUserId, offer) => {
    if (socket && isConnected) {
      socket.emit('webrtc-offer', { targetUserId, offer });
    }
  };

  const sendWebRTCAnswer = (targetUserId, answer) => {
    if (socket && isConnected) {
      socket.emit('webrtc-answer', { targetUserId, answer });
    }
  };

  const sendICECandidate = (targetUserId, candidate) => {
    if (socket && isConnected) {
      socket.emit('webrtc-ice-candidate', { targetUserId, candidate });
    }
  };

  const value = {
    socket,
    isConnected,
    incomingCall,
    messages,
    initiateCall,
    acceptCall,
    rejectCall,
    endCall,
    sendMessage,
    sendWebRTCOffer,
    sendWebRTCAnswer,
    sendICECandidate,
    setMessages
  };

  return (
    <SocketContext.Provider value={value}>
      {children}
    </SocketContext.Provider>
  );
};
