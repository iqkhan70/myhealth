// Simple test script to verify emergency server endpoints
const API_BASE_URL = 'http://localhost:5000/api/emergency'; // Update with your server URL

async function testEmergencyEndpoints() {
  console.log('🚨 Testing Emergency System Endpoints...\n');

  // Test 1: Register Device
  console.log('1. Testing Device Registration...');
  try {
    const registerResponse = await fetch(`${API_BASE_URL}/register-device`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        patientId: 1, // Use a valid patient ID from your database
        deviceId: 'test-device-' + Date.now(),
        deviceName: 'Test Device',
        deviceType: 'smartphone',
        deviceModel: 'Test Phone',
        operatingSystem: 'Test OS'
      })
    });

    const registerData = await registerResponse.json();
    console.log('✅ Device Registration:', registerData);

    if (registerData.success) {
      const deviceToken = registerData.deviceToken;
      
      // Test 2: Send Test Emergency
      console.log('\n2. Testing Emergency Message...');
      const emergencyResponse = await fetch(`${API_BASE_URL}/test-emergency`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          deviceToken: deviceToken,
          emergencyType: 'Fall',
          severity: 'Critical',
          message: 'Test emergency - patient fell',
          deviceId: 'test-device-' + Date.now(),
          heartRate: 180,
          bloodPressure: '180/120',
          temperature: 98.6,
          oxygenSaturation: 95,
          latitude: 40.7128,
          longitude: -74.0060
        })
      });

      const emergencyData = await emergencyResponse.json();
      console.log('✅ Emergency Message:', emergencyData);

      // Test 3: Get Emergency Incidents
      console.log('\n3. Testing Get Incidents...');
      const incidentsResponse = await fetch(`${API_BASE_URL}/incidents/1`); // Use a valid doctor ID
      const incidentsData = await incidentsResponse.json();
      console.log('✅ Emergency Incidents:', incidentsData);

    } else {
      console.log('❌ Device registration failed:', registerData.message);
    }

  } catch (error) {
    console.error('❌ Test failed:', error.message);
  }
}

// Run the test
testEmergencyEndpoints();
