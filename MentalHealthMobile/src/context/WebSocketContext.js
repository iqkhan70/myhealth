import React, { createContext, useContext, useEffect, useState } from 'react';
import { useAuth } from './AuthContext';

const WebSocketContext = createContext();

export const useWebSocket = () => {
  const context = useContext(WebSocketContext);
  if (!context) {
    throw new Error('useWebSocket must be used within a WebSocketProvider');
  }
  return context;
};

export const WebSocketProvider = ({ children }) => {
  const [socket, setSocket] = useState(null);
  const [isConnected, setIsConnected] = useState(false);
  const [incomingCall, setIncomingCall] = useState(null);
  const [messages, setMessages] = useState([]);
  const { user, token } = useAuth();

  useEffect(() => {
    if (user && token) {
      initializeWebSocket();
    } else {
      disconnectWebSocket();
    }

    return () => {
      disconnectWebSocket();
    };
  }, [user, token]);

  const initializeWebSocket = () => {
    try {
      console.log('Initializing WebSocket connection...');
      
      // Create WebSocket connection to our custom WebSocket endpoint
      const wsUrl = `ws://192.168.86.113:5262/api/websocket/connect`;
      const newSocket = new WebSocket(wsUrl);

      newSocket.onopen = () => {
        console.log('WebSocket connected');
        setIsConnected(true);
        
        // Send authentication message
        const authMessage = {
          type: 'auth',
          token: token
        };
        newSocket.send(JSON.stringify(authMessage));
      };

      newSocket.onclose = (event) => {
        console.log('WebSocket disconnected:', event);
        setIsConnected(false);
      };

      newSocket.onerror = (error) => {
        console.error('WebSocket error:', error);
        setIsConnected(false);
      };

      newSocket.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data);
          console.log('WebSocket message received:', data);
          
          // Handle different message types
          switch (data.type) {
            case 'auth-success':
              console.log('Authentication successful:', data);
              break;
            case 'auth-error':
              console.error('Authentication failed:', data.message);
              setIsConnected(false);
              break;
            case 'new-message':
              setMessages(prev => [...prev, data]);
              break;
            case 'incoming-call':
              setIncomingCall(data);
              break;
            case 'call-accepted':
              console.log('Call accepted:', data);
              setIncomingCall(null);
              break;
            case 'call-rejected':
              console.log('Call rejected:', data);
              setIncomingCall(null);
              break;
            case 'call-ended':
              console.log('Call ended:', data);
              setIncomingCall(null);
              break;
            case 'message-sent':
              console.log('Message sent successfully:', data);
              break;
            case 'call-initiated':
              console.log('Call initiated successfully:', data);
              break;
            case 'error':
              console.error('WebSocket error:', data.message);
              break;
            default:
              console.log('Unknown message type:', data.type, data);
          }
        } catch (error) {
          console.error('Error parsing WebSocket message:', error);
          console.log('Raw message data:', event.data);
        }
      };

      setSocket(newSocket);

    } catch (error) {
      console.error('Error initializing WebSocket:', error);
      setIsConnected(false);
    }
  };

  const disconnectWebSocket = () => {
    if (socket) {
      socket.close();
      setSocket(null);
      setIsConnected(false);
    }
  };

  // Call functions
  const initiateCall = (targetUserId, callType) => {
    if (socket && isConnected) {
      const message = {
        type: 'initiate-call',
        targetUserId: targetUserId,
        callType: callType
      };
      socket.send(JSON.stringify(message));
      console.log('Call initiated:', { targetUserId, callType });
    }
  };

  const acceptCall = (callId) => {
    if (socket && isConnected) {
      const message = {
        type: 'accept-call',
        callId: callId
      };
      socket.send(JSON.stringify(message));
      console.log('Call accepted:', callId);
      setIncomingCall(null);
    }
  };

  const rejectCall = (callId) => {
    if (socket && isConnected) {
      const message = {
        type: 'reject-call',
        callId: callId
      };
      socket.send(JSON.stringify(message));
      console.log('Call rejected:', callId);
      setIncomingCall(null);
    }
  };

  const endCall = (callId) => {
    if (socket && isConnected) {
      const message = {
        type: 'end-call',
        callId: callId
      };
      socket.send(JSON.stringify(message));
      console.log('Call ended:', callId);
    }
  };

  // Chat functions
  const sendMessage = (targetUserId, message) => {
    if (socket && isConnected) {
      const messageData = {
        type: 'send-message',
        targetUserId: targetUserId,
        message: message
      };
      socket.send(JSON.stringify(messageData));
      console.log('Message sent:', { targetUserId, message });
      
      // Add message to local state
      const localMessage = {
        id: Date.now().toString(),
        targetUserId,
        message,
        senderId: user.id,
        senderName: `${user.firstName} ${user.lastName}`,
        timestamp: new Date().toISOString(),
        isSent: true
      };
      setMessages(prev => [...prev, localMessage]);
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
    setMessages
  };

  return (
    <WebSocketContext.Provider value={value}>
      {children}
    </WebSocketContext.Provider>
  );
};
