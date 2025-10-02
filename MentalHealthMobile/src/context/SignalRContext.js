import React, { createContext, useContext, useEffect, useState } from 'react';
import * as SignalR from '@microsoft/signalr';
import { useAuth } from './AuthContext';

const SignalRContext = createContext();

export const useSignalR = () => {
  const context = useContext(SignalRContext);
  if (!context) {
    throw new Error('useSignalR must be used within a SignalRProvider');
  }
  return context;
};

export const SignalRProvider = ({ children }) => {
  const [connection, setConnection] = useState(null);
  const [isConnected, setIsConnected] = useState(false);
  const [incomingCall, setIncomingCall] = useState(null);
  const [messages, setMessages] = useState([]);
  const { user, token } = useAuth();

  useEffect(() => {
    if (user && token) {
      initializeConnection();
    } else {
      disconnectConnection();
    }

    return () => {
      disconnectConnection();
    };
  }, [user, token]);

  const initializeConnection = async () => {
    try {
      console.log('Initializing SignalR connection...');
      
      const newConnection = new SignalR.HubConnectionBuilder()
        .withUrl('http://192.168.86.113:5262/mobilehub', {
          accessTokenFactory: () => token,
          transport: SignalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect()
        .build();

      // Connection events
      newConnection.onclose((error) => {
        console.log('SignalR connection closed:', error);
        setIsConnected(false);
      });

      newConnection.onreconnecting((error) => {
        console.log('SignalR reconnecting:', error);
        setIsConnected(false);
      });

      newConnection.onreconnected((connectionId) => {
        console.log('SignalR reconnected:', connectionId);
        setIsConnected(true);
      });

      // Chat events
      newConnection.on('new-message', (message) => {
        console.log('New message received:', message);
        setMessages(prev => [...prev, message]);
      });

      // Call events
      newConnection.on('incoming-call', (callData) => {
        console.log('Incoming call:', callData);
        setIncomingCall(callData);
      });

      newConnection.on('call-accepted', (callData) => {
        console.log('Call accepted:', callData);
        setIncomingCall(null);
      });

      newConnection.on('call-rejected', (callData) => {
        console.log('Call rejected:', callData);
        setIncomingCall(null);
      });

      newConnection.on('call-ended', (callData) => {
        console.log('Call ended:', callData);
        setIncomingCall(null);
      });

      // Start the connection
      await newConnection.start();
      console.log('SignalR connection started successfully');
      setIsConnected(true);
      setConnection(newConnection);

    } catch (error) {
      console.error('Error starting SignalR connection:', error);
      setIsConnected(false);
    }
  };

  const disconnectConnection = async () => {
    if (connection) {
      try {
        await connection.stop();
        console.log('SignalR connection stopped');
      } catch (error) {
        console.error('Error stopping SignalR connection:', error);
      }
      setConnection(null);
      setIsConnected(false);
    }
  };

  // Call functions
  const initiateCall = async (targetUserId, callType) => {
    if (connection && isConnected) {
      try {
        await connection.invoke('InitiateCall', targetUserId, callType);
        console.log('Call initiated:', { targetUserId, callType });
      } catch (error) {
        console.error('Error initiating call:', error);
      }
    }
  };

  const acceptCall = async (callId) => {
    if (connection && isConnected) {
      try {
        await connection.invoke('AcceptCall', callId);
        console.log('Call accepted:', callId);
        setIncomingCall(null);
      } catch (error) {
        console.error('Error accepting call:', error);
      }
    }
  };

  const rejectCall = async (callId) => {
    if (connection && isConnected) {
      try {
        await connection.invoke('RejectCall', callId);
        console.log('Call rejected:', callId);
        setIncomingCall(null);
      } catch (error) {
        console.error('Error rejecting call:', error);
      }
    }
  };

  const endCall = async (callId) => {
    if (connection && isConnected) {
      try {
        await connection.invoke('EndCall', callId);
        console.log('Call ended:', callId);
      } catch (error) {
        console.error('Error ending call:', error);
      }
    }
  };

  // Chat functions
  const sendMessage = async (targetUserId, message) => {
    if (connection && isConnected) {
      try {
        await connection.invoke('SendMessage', targetUserId, message);
        console.log('Message sent:', { targetUserId, message });
        
        // Add message to local state
        const messageData = {
          id: Date.now().toString(),
          targetUserId,
          message,
          senderId: user.id,
          senderName: `${user.firstName} ${user.lastName}`,
          timestamp: new Date().toISOString(),
          isSent: true
        };
        setMessages(prev => [...prev, messageData]);
      } catch (error) {
        console.error('Error sending message:', error);
      }
    }
  };

  const value = {
    connection,
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
    <SignalRContext.Provider value={value}>
      {children}
    </SignalRContext.Provider>
  );
};
