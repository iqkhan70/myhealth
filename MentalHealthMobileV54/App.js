import React, { useState, useEffect } from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Ionicons } from '@expo/vector-icons';
import * as SecureStore from 'expo-secure-store';

// Import screens
import LoginScreen from './src/screens/LoginScreen';
import DoctorHomeScreen from './src/screens/DoctorHomeScreen';
import PatientHomeScreen from './src/screens/PatientHomeScreen';
import SimpleChatScreen from './src/screens/SimpleChatScreen';
import SimpleVideoCallScreen from './src/screens/SimpleVideoCallScreen';
import SimpleAudioCallScreen from './src/screens/SimpleAudioCallScreen';
import ProfileScreen from './src/screens/ProfileScreen';

// Import context
import { AuthProvider, useAuth } from './src/context/AuthContext';
import { RealtimeProvider } from './src/context/RealtimeContext';
import { AgoraProvider } from './src/context/AgoraContext';

const Stack = createStackNavigator();
const Tab = createBottomTabNavigator();

// Doctor Tab Navigator
function DoctorTabs() {
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        tabBarIcon: ({ focused, color, size }) => {
          let iconName;
          if (route.name === 'Patients') {
            iconName = focused ? 'people' : 'people-outline';
          } else if (route.name === 'Chat') {
            iconName = focused ? 'chatbubbles' : 'chatbubbles-outline';
          } else if (route.name === 'Profile') {
            iconName = focused ? 'person' : 'person-outline';
          }
          return <Ionicons name={iconName} size={size} color={color} />;
        },
        tabBarActiveTintColor: '#2196F3',
        tabBarInactiveTintColor: 'gray',
        headerStyle: {
          backgroundColor: '#2196F3',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      })}
    >
      <Tab.Screen name="Patients" component={DoctorHomeScreen} />
      <Tab.Screen name="Chat" component={SimpleChatScreen} />
      <Tab.Screen name="Profile" component={ProfileScreen} />
    </Tab.Navigator>
  );
}

// Patient Tab Navigator
function PatientTabs() {
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        tabBarIcon: ({ focused, color, size }) => {
          let iconName;
          if (route.name === 'Doctors') {
            iconName = focused ? 'medical' : 'medical-outline';
          } else if (route.name === 'Chat') {
            iconName = focused ? 'chatbubbles' : 'chatbubbles-outline';
          } else if (route.name === 'Profile') {
            iconName = focused ? 'person' : 'person-outline';
          }
          return <Ionicons name={iconName} size={size} color={color} />;
        },
        tabBarActiveTintColor: '#4CAF50',
        tabBarInactiveTintColor: 'gray',
        headerStyle: {
          backgroundColor: '#4CAF50',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      })}
    >
      <Tab.Screen name="Doctors" component={PatientHomeScreen} />
      <Tab.Screen name="Chat" component={SimpleChatScreen} />
      <Tab.Screen name="Profile" component={ProfileScreen} />
    </Tab.Navigator>
  );
}

// Main App Navigator
function AppNavigator() {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return null; // Or a loading screen
  }

  return (
    <NavigationContainer>
      <Stack.Navigator screenOptions={{ headerShown: false }}>
        {user ? (
          <>
            {user.roleId === 2 ? ( // Doctor
              <Stack.Screen name="DoctorMain" component={DoctorTabs} />
            ) : ( // Patient
              <Stack.Screen name="PatientMain" component={PatientTabs} />
            )}
            <Stack.Screen 
              name="VideoCall" 
              component={SimpleVideoCallScreen}
              options={{ 
                headerShown: true,
                title: 'Video Call',
                headerStyle: { backgroundColor: '#2196F3' },
                headerTintColor: '#fff'
              }}
            />
            <Stack.Screen 
              name="AudioCall" 
              component={SimpleAudioCallScreen}
              options={{ 
                headerShown: true,
                title: 'Audio Call',
                headerStyle: { backgroundColor: '#2196F3' },
                headerTintColor: '#fff'
              }}
            />
          </>
        ) : (
          <Stack.Screen name="Login" component={LoginScreen} />
        )}
      </Stack.Navigator>
    </NavigationContainer>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <RealtimeProvider>
        <AgoraProvider>
          <AppNavigator />
        </AgoraProvider>
      </RealtimeProvider>
    </AuthProvider>
  );
}
