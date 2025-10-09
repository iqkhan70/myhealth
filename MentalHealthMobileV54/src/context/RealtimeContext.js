import React, { createContext, useContext, useState, useEffect } from 'react';
import { useAuth } from './AuthContext';

const RealtimeContext = createContext();

export const useRealtime = () => {
  const context = useContext(RealtimeContext);
  if (!context) {
    throw new Error('useRealtime must be used within a RealtimeProvider');
  }
  return context;
};

export const RealtimeProvider = ({ children }) => {
  const [isConnected, setIsConnected] = useState(false);
  const [connectionId, setConnectionId] = useState(null);
  const [messages, setMessages] = useState([]);
  const [incomingCall, setIncomingCall] = useState(null);
  const [chatHistory, setChatHistory] = useState({}); // Store chat history by user ID
  const [pollingInterval, setPollingInterval] = useState(null);
  const [isConnecting, setIsConnecting] = useState(false); // Prevent multiple connection attempts
  const { user, token } = useAuth();

  // Polling interval
  const POLL_INTERVAL = 5000; // 5 seconds for better stability
  const API_BASE_URL = 'http://192.168.86.113:5262/api';

  useEffect(() => {
    if (user && token) {
      connect();
    } else {
      disconnect();
    }

    return () => {
      disconnect();
    };
  }, [user, token]);

  // Single polling management effect
  useEffect(() => {
    if (isConnected && connectionId) {
      if (!pollingInterval) {
        console.log('Connection established, starting polling...');
        startPolling();
      }
    } else if (!isConnected && user && token) {
      console.log('Connection lost, attempting to reconnect...');
      const reconnectTimer = setTimeout(() => {
        connect();
      }, 3000); // Wait 3 seconds before reconnecting
      
      return () => clearTimeout(reconnectTimer);
    }
  }, [isConnected, connectionId, user, token]);

  // Periodic connection health check
  useEffect(() => {
    if (!isConnected || !connectionId) {
      // If not connected, try to reconnect
      console.log('Health check: Not connected, attempting to reconnect...');
      connect();
      return;
    }

    const healthCheckInterval = setInterval(async () => {
      try {
        // Test connection by making a simple request
        const response = await fetch(`${API_BASE_URL}/realtime/poll`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
          },
          body: JSON.stringify({
            connectionId: connectionId
          })
        });

        if (response.status === 400) {
          console.log('Health check failed - connection invalid, reconnecting...');
          setConnectionId(null);
          setIsConnected(false);
          // Stop any existing polling
          if (pollingInterval) {
            clearInterval(pollingInterval);
            setPollingInterval(null);
          }
          await connect();
        } else if (!response.ok) {
          console.log('Health check failed with status:', response.status, 'reconnecting...');
          setConnectionId(null);
          setIsConnected(false);
          if (pollingInterval) {
            clearInterval(pollingInterval);
            setPollingInterval(null);
          }
          await connect();
        }
      } catch (error) {
        console.log('Health check error, reconnecting...', error);
        setConnectionId(null);
        setIsConnected(false);
        if (pollingInterval) {
          clearInterval(pollingInterval);
          setPollingInterval(null);
        }
        await connect();
      }
    }, 30000); // Check every 30 seconds

    return () => clearInterval(healthCheckInterval);
  }, [isConnected, connectionId, token]);


  const connect = async () => {
    // Prevent multiple simultaneous connection attempts
    if (isConnecting) {
      console.log('Connection already in progress, skipping...');
      return;
    }

    try {
      setIsConnecting(true);
      console.log('Connecting to realtime service...');
      console.log('Token:', token ? 'Present' : 'Missing');
      console.log('User:', user ? `ID: ${user.id}` : 'Missing');
      
      // Don't attempt connection if no token or user
      if (!token || !user) {
        console.log('Cannot connect: Missing token or user');
        setIsConnected(false);
        return;
      }
      
      // Validate token format (basic check)
      if (typeof token !== 'string' || token.length < 10) {
        console.log('Cannot connect: Invalid token format');
        setIsConnected(false);
        return;
      }

      // Clear any existing connection state
      setConnectionId(null);
      setIsConnected(false);
      
      const response = await fetch(`${API_BASE_URL}/realtime/connect`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          UserId: user.id
        })
      });

      if (response.ok) {
        const data = await response.json();
        setConnectionId(data.connectionId);
        setIsConnected(true);
        console.log('Realtime connected:', data.connectionId);
        
        // Start polling for messages
        startPolling();
      } else {
        const errorData = await response.text();
        console.error('Failed to connect to realtime service:', response.status, errorData);
        setIsConnected(false);
      }
    } catch (error) {
      console.error('Error connecting to realtime service:', error);
      setIsConnected(false);
    } finally {
      setIsConnecting(false);
    }
  };

  // Load chat history for a specific user
  const loadChatHistory = async (otherUserId) => {
    if (!connectionId || !isConnected) {
      console.log('Not connected, cannot load chat history');
      return [];
    }

    try {
      console.log('Loading chat history for user:', otherUserId);
      const response = await fetch(`${API_BASE_URL}/realtime/get-message-history`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          connectionId: connectionId,
          otherUserId: otherUserId
        })
      });

      if (response.ok) {
        const data = await response.json();
        console.log('Chat history loaded:', data.messages?.length || 0, 'messages');
        
        // Store chat history for this user
        setChatHistory(prev => ({
          ...prev,
          [otherUserId]: data.messages || []
        }));
        
        // Also update global messages to ensure they're available
        if (data.messages && data.messages.length > 0) {
          setMessages(prev => {
            const existingIds = new Set(prev.map(msg => msg.id));
            const newMessages = data.messages.filter(msg => !existingIds.has(msg.id));
            if (newMessages.length > 0) {
              console.log('Adding chat history messages to global state:', newMessages.length);
              return [...prev, ...newMessages];
            }
            return prev;
          });
        }
        
        return data.messages || [];
      } else {
        console.error('Failed to load chat history:', response.status);
        return [];
      }
    } catch (error) {
      console.error('Error loading chat history:', error);
      return [];
    }
  };

  const disconnect = async () => {
    // Clear polling interval
    if (pollingInterval) {
      clearInterval(pollingInterval);
      setPollingInterval(null);
      console.log('Polling stopped');
    }
    
    if (connectionId) {
      try {
        await fetch(`${API_BASE_URL}/realtime/disconnect`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
          },
          body: JSON.stringify({
            connectionId: connectionId
          })
        });
      } catch (error) {
        console.error('Error disconnecting:', error);
      }
    }
    
    setConnectionId(null);
    setIsConnected(false);
    setMessages([]);
    setIncomingCall(null);
  };

  const startPolling = () => {
    // Don't start polling if already running
    if (pollingInterval) {
      console.log('Polling already running, skipping start');
      return;
    }

    // Don't start polling if not connected
    if (!connectionId || !isConnected) {
      console.log('Cannot start polling - not connected');
      return;
    }
    
    console.log('Starting polling for connection:', connectionId);
    
    const poll = async () => {
      if (!connectionId || !isConnected) {
        console.log('Polling skipped - connectionId:', connectionId, 'isConnected:', isConnected);
        return;
      }

        try {
          const response = await fetch(`${API_BASE_URL}/realtime/poll`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
              connectionId: connectionId
            })
          });

          if (response.ok) {
            const data = await response.json();
          
          // Handle new messages
          if (data.messages && data.messages.length > 0) {
            // Process all messages at once to avoid race conditions
            const newMessages = data.messages;
            
            if (newMessages.length > 0) {
              console.log('Processing', newMessages.length, 'new messages from polling');
              
              // Update chat history for all messages at once
              setChatHistory(prev => {
                const newChatHistory = { ...prev };
                
                newMessages.forEach(message => {
                  const otherUserId = message.senderId === user.id ? message.receiverId : message.senderId;
                  
                  const existingMessages = newChatHistory[otherUserId] || [];
                  
                  // Simple duplicate detection - just check by ID
                  const messageExists = existingMessages.some(existingMsg => existingMsg.id === message.id);
                  
                  if (!messageExists) {
                    console.log('Adding new message:', message.message, 'for user:', otherUserId);
                    
                    // Ensure message has a unique ID
                    if (!message.id || message.id === '') {
                      message.id = `msg_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
                    }
                    
                    // Ensure message has a proper timestamp
                    if (!message.timestamp || message.timestamp === '') {
                      message.timestamp = new Date().toISOString();
                    }
                    
                    const updatedMessages = [...existingMessages, message];
                    // Use database ordering - no client-side sorting needed
                    newChatHistory[otherUserId] = updatedMessages;
                  } else {
                    console.log('Skipping duplicate message:', message.message);
                  }
                });
                
                return newChatHistory;
              });
            }
          } else {
            console.log('No new messages in poll response');
          }

          // Handle incoming calls
          if (data.calls && data.calls.length > 0) {
            const latestCall = data.calls[data.calls.length - 1];
            setIncomingCall(latestCall);
          }
        } else if (response.status === 400) {
          // Connection invalid, stop polling and let health check handle reconnection
          console.log('Connection invalid (400), stopping polling...');
          setConnectionId(null);
          setIsConnected(false);
          // Stop polling immediately
          if (pollingInterval) {
            clearInterval(pollingInterval);
            setPollingInterval(null);
          }
        } else {
          console.log('Poll failed with status:', response.status);
          const errorText = await response.text();
          console.log('Poll error response:', errorText);
          
          // If it's a connection error, try to reconnect
          if (response.status === 401 || response.status === 403) {
            console.log('Authentication error, reconnecting...');
            setConnectionId(null);
            setIsConnected(false);
            // Don't await to avoid blocking polling
            connect();
          }
        }
      } catch (error) {
        console.error('Error polling for messages:', error);
        // Don't stop polling on error, just log it
      }
    };

    // Start polling immediately and then every interval
    console.log('Starting polling with interval:', POLL_INTERVAL, 'ms (', POLL_INTERVAL/1000, 'seconds)');
    poll();
    const interval = setInterval(poll, POLL_INTERVAL);
    setPollingInterval(interval);
    console.log('Polling interval set:', interval);
  };

  // Send message
  const sendMessage = async (targetUserId, message) => {
    if (!connectionId || !isConnected) {
      console.error('Not connected to realtime service');
      return;
    }

    try {
      const response = await fetch(`${API_BASE_URL}/realtime/send-message`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          connectionId: connectionId,
          targetUserId: targetUserId,
          message: message
        })
      });

      if (response.ok) {
        const data = await response.json();
        console.log('Message sent:', data);
        
        // Add message to local state and chat history
        const localMessage = {
          id: Date.now().toString(),
          targetUserId,
          message,
          senderId: user.id,
          senderName: `${user.firstName} ${user.lastName}`,
          timestamp: new Date().toISOString(),
          isSent: true
        };
        
        // Add to global messages
        setMessages(prev => [...prev, localMessage]);
        
        // Add to chat history for immediate display
        setChatHistory(prev => {
          const existingMessages = prev[targetUserId] || [];
          return {
            ...prev,
            [targetUserId]: [...existingMessages, localMessage]
          };
        });
      } else {
        console.error('Failed to send message:', response.status);
      }
    } catch (error) {
      console.error('Error sending message:', error);
    }
  };

  // Initiate call
  const initiateCall = async (targetUserId, callType) => {
    if (!connectionId || !isConnected) {
      console.error('Not connected to realtime service');
      return;
    }

    try {
      const response = await fetch(`${API_BASE_URL}/realtime/initiate-call`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          connectionId: connectionId,
          targetUserId: targetUserId,
          callType: callType
        })
      });

      if (response.ok) {
        const data = await response.json();
        console.log('Call initiated:', data);
      } else {
        console.error('Failed to initiate call:', response.status);
      }
    } catch (error) {
      console.error('Error initiating call:', error);
    }
  };

  // Accept call
  const acceptCall = (callId) => {
    console.log('Call accepted:', callId);
    setIncomingCall(null);
  };

  // Reject call
  const rejectCall = (callId) => {
    console.log('Call rejected:', callId);
    setIncomingCall(null);
  };

  // End call
  const endCall = (callId) => {
    console.log('Call ended:', callId);
  };

  // Manual refresh function
  const refreshMessages = async () => {
    if (!connectionId || !isConnected) {
      console.log('Not connected, cannot refresh messages');
      return;
    }

    try {
      console.log('Manual refresh triggered');
      const response = await fetch(`${API_BASE_URL}/realtime/poll`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          connectionId: connectionId
        })
      });

      if (response.ok) {
        const data = await response.json();
        console.log('Manual refresh response:', data);
        
        // Handle new messages
        if (data.messages && data.messages.length > 0) {
          console.log('Manual refresh - received new messages:', data.messages.length);
          
          // Update global messages
          setMessages(prev => {
            const newMessages = data.messages.filter(newMsg => 
              !prev.some(existingMsg => existingMsg.id === newMsg.id)
            );
            if (newMessages.length > 0) {
              console.log('Manual refresh - adding new messages to global state:', newMessages.length);
              return [...prev, ...newMessages];
            }
            return prev;
          });
          
          // Update chat history for each message
          data.messages.forEach(message => {
            const otherUserId = message.senderId === user.id ? message.receiverId : message.senderId;
            setChatHistory(prev => {
              const existingMessages = prev[otherUserId] || [];
              const messageExists = existingMessages.some(existingMsg => existingMsg.id === message.id);
              
              if (!messageExists) {
                console.log('Manual refresh - adding message to chat history for user:', otherUserId);
                return {
                  ...prev,
                  [otherUserId]: [...existingMessages, message]
                };
              }
              return prev;
            });
          });
        } else {
          console.log('Manual refresh - no new messages');
        }
      }
    } catch (error) {
      console.error('Error during manual refresh:', error);
    }
  };

  // Get messages for a specific user (only from chat history to avoid duplicates)
  const getMessagesForUser = (otherUserId) => {
    const historyMessages = chatHistory[otherUserId] || [];
    
    // Return messages in database order - no client-side sorting needed
    return historyMessages;
  };

  // Force refresh - restart polling
  const forceRefresh = async () => {
    console.log('Force refresh triggered - restarting polling');
    if (isConnected && connectionId) {
      startPolling();
    }
  };

  // Debug function to check polling status
  const debugPollingStatus = () => {
    console.log('=== POLLING DEBUG ===');
    console.log('isConnected:', isConnected);
    console.log('connectionId:', connectionId);
    console.log('pollingInterval:', pollingInterval);
    console.log('user:', user ? `ID: ${user.id}` : 'null');
    console.log('token:', token ? 'Present' : 'Missing');
    console.log('===================');
  };

  const value = {
    isConnected,
    connectionId,
    messages,
    incomingCall,
    chatHistory,
    sendMessage,
    initiateCall,
    acceptCall,
    rejectCall,
    endCall,
    loadChatHistory,
    getMessagesForUser,
    refreshMessages,
    forceRefresh,
    debugPollingStatus
  };

  return (
    <RealtimeContext.Provider value={value}>
      {children}
    </RealtimeContext.Provider>
  );
};
