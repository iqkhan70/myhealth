const express = require('express');
const http = require('http');
const WebSocket = require('ws');
const path = require('path');

const app = express();
const server = http.createServer(app);
const wss = new WebSocket.Server({ server });

// Store active connections
const connections = new Map();
const rooms = new Map();

// WebSocket connection handling
wss.on('connection', (ws, req) => {
  console.log('🔌 New WebSocket connection');
  
  ws.on('message', (message) => {
    try {
      const data = JSON.parse(message);
      console.log('📨 Received:', data.type);
      
      switch (data.type) {
        case 'join':
          handleJoin(ws, data);
          break;
        case 'offer':
          handleOffer(ws, data);
          break;
        case 'answer':
          handleAnswer(ws, data);
          break;
        case 'ice-candidate':
          handleIceCandidate(ws, data);
          break;
        case 'leave':
          handleLeave(ws, data);
          break;
      }
    } catch (error) {
      console.error('❌ Error parsing message:', error);
    }
  });
  
  ws.on('close', () => {
    console.log('🔌 WebSocket connection closed');
    // Clean up connections
    for (const [roomId, room] of rooms.entries()) {
      if (room.peers.has(ws)) {
        room.peers.delete(ws);
        if (room.peers.size === 0) {
          rooms.delete(roomId);
        } else {
          // Notify other peers that someone left
          room.peers.forEach(peer => {
            if (peer !== ws && peer.readyState === WebSocket.OPEN) {
              peer.send(JSON.stringify({
                type: 'peer-left',
                roomId: roomId
              }));
            }
          });
        }
      }
    }
  });
});

function handleJoin(ws, data) {
  const { roomId, userId } = data;
  console.log(`👤 User ${userId} joining room ${roomId}`);
  
  if (!rooms.has(roomId)) {
    rooms.set(roomId, { peers: new Set() });
  }
  
  const room = rooms.get(roomId);
  room.peers.add(ws);
  ws.roomId = roomId;
  ws.userId = userId;
  
  // Notify existing peers about new user
  room.peers.forEach(peer => {
    if (peer !== ws && peer.readyState === WebSocket.OPEN) {
      peer.send(JSON.stringify({
        type: 'peer-joined',
        roomId: roomId,
        userId: userId
      }));
    }
  });
  
  // Send list of existing peers to new user
  const existingPeers = Array.from(room.peers)
    .filter(peer => peer !== ws)
    .map(peer => peer.userId);
    
  ws.send(JSON.stringify({
    type: 'joined',
    roomId: roomId,
    existingPeers: existingPeers
  }));
}

function handleOffer(ws, data) {
  const { roomId, targetUserId, offer } = data;
  console.log(`📤 Offer from ${ws.userId} to ${targetUserId} in room ${roomId}`);
  
  const room = rooms.get(roomId);
  if (room) {
    room.peers.forEach(peer => {
      if (peer.userId === targetUserId && peer.readyState === WebSocket.OPEN) {
        peer.send(JSON.stringify({
          type: 'offer',
          fromUserId: ws.userId,
          offer: offer
        }));
      }
    });
  }
}

function handleAnswer(ws, data) {
  const { roomId, targetUserId, answer } = data;
  console.log(`📤 Answer from ${ws.userId} to ${targetUserId} in room ${roomId}`);
  
  const room = rooms.get(roomId);
  if (room) {
    room.peers.forEach(peer => {
      if (peer.userId === targetUserId && peer.readyState === WebSocket.OPEN) {
        peer.send(JSON.stringify({
          type: 'answer',
          fromUserId: ws.userId,
          answer: answer
        }));
      }
    });
  }
}

function handleIceCandidate(ws, data) {
  const { roomId, targetUserId, candidate } = data;
  console.log(`📤 ICE candidate from ${ws.userId} to ${targetUserId} in room ${roomId}`);
  
  const room = rooms.get(roomId);
  if (room) {
    room.peers.forEach(peer => {
      if (peer.userId === targetUserId && peer.readyState === WebSocket.OPEN) {
        peer.send(JSON.stringify({
          type: 'ice-candidate',
          fromUserId: ws.userId,
          candidate: candidate
        }));
      }
    });
  }
}

function handleLeave(ws, data) {
  const { roomId } = data;
  console.log(`👋 User ${ws.userId} leaving room ${roomId}`);
  
  const room = rooms.get(roomId);
  if (room) {
    room.peers.delete(ws);
    if (room.peers.size === 0) {
      rooms.delete(roomId);
    }
  }
}

// Serve static files
app.use(express.static(path.join(__dirname, 'SM_MentalHealthApp.Client')));

const PORT = 3004;
server.listen(PORT, '0.0.0.0', () => {
  console.log('🚀 WebRTC Signaling Server running on port', PORT);
  console.log('📱 Mobile can access: ws://192.168.86.113:3004');
  console.log('💻 Web can access: ws://localhost:3004');
  console.log('🔗 WebSocket endpoint: ws://localhost:3004');
});
