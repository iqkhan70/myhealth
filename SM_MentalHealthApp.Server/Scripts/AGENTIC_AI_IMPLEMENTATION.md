# Agentic AI Implementation for Service Requests

## Overview

This implementation adds an agentic AI system specifically designed for service requests (plumbing, car repair, lawn care, etc.). The system learns from each client interaction and adapts its communication style to provide personalized, supportive responses.

## What Was Created

### 1. Database Schema (`AddClientProfileSystem.sql`)
- **ClientProfiles**: Stores per-client communication preferences and interaction data
- **ClientInteractionPatterns**: Learned patterns about how clients interact
- **ClientKeywordReactions**: Tracks how clients react to specific keywords
- **ClientServicePreferences**: Tracks client preferences for different service types
- **ClientInteractionHistory**: Detailed history of all interactions for learning

### 2. Models (`ClientProfileModels.cs`)
- `ClientProfile`: Main profile entity
- `ClientInteractionPattern`: Learned interaction patterns
- `ClientKeywordReaction`: Keyword reaction tracking
- `ClientServicePreference`: Service type preferences
- `ClientInteractionHistory`: Interaction history
- `MessageAnalysis`: Analysis results from message processing
- `ResponseStrategy`: Strategy determined by agent
- `AgenticResponse`: Final response with suggestions

### 3. Services

#### `IClientProfileService` / `ClientProfileService`
Manages client profiles, interaction patterns, keyword reactions, and service preferences.

#### `IServiceRequestAgenticAIService` / `ServiceRequestAgenticAIService`
Core agentic AI service that:
- Analyzes client messages (sentiment, urgency, emotional state)
- Determines response strategy (tone, information level, approach)
- Generates adaptive responses using LLM
- Learns from interactions to improve over time

### 4. API Controller (`AgenticAIController`)
- `POST /api/agenticai/process-service-request`: Process a service request message

## How It Works

### Step 1: Client Sends Message
```
Client: "My sink is leaking and water is everywhere! This is a disaster!"
```

### Step 2: Message Analysis
The system analyzes:
- **Sentiment**: Panic (detected from keywords like "disaster", "everywhere")
- **Urgency**: Critical (detected from "leaking", "water everywhere")
- **Emotional State**: Panic
- **Information Need**: Moderate (based on message length and profile)
- **Concerns**: Water/Plumbing Issue, Safety Concern

### Step 3: Response Strategy
Based on analysis and client profile:
- **Tone**: Supportive (client is panicked)
- **Information Level**: Minimal (don't overwhelm)
- **Approach**: Reassuring (calm them first, then guide)

### Step 4: Generate Adaptive Response
The LLM generates a personalized response:
```
"I understand this is stressful! Let's get this sorted quickly. 
First, can you turn off the water supply under the sink? 
Once that's done, we can assess the situation and I'll guide 
you through the next steps. You've got this!"
```

### Step 5: Learning
The system learns:
- Client responds well to step-by-step guidance during emergencies
- Information tolerance adjusted based on response
- Keyword reactions updated ("leaking" = negative reaction)
- Interaction history stored for future reference

## Setup Instructions

### 1. Run Database Migration
```bash
mysql -u root -p mentalhealthdb < SM_MentalHealthApp.Server/Scripts/AddClientProfileSystem.sql
```

### 2. Services Already Registered
The services are already registered in `DependencyInjection.cs`:
- `IClientProfileService` → `ClientProfileService`
- `IServiceRequestAgenticAIService` → `ServiceRequestAgenticAIService`

### 3. API Usage

#### Example Request
```http
POST /api/agenticai/process-service-request
Authorization: Bearer <token>
Content-Type: application/json

{
  "clientId": 123,
  "clientMessage": "My sink is leaking and water is everywhere!",
  "serviceRequestId": 456
}
```

#### Example Response
```json
{
  "success": true,
  "response": {
    "message": "I understand this is stressful! Let's get this sorted quickly...",
    "suggestedActions": [
      "Assess immediate safety",
      "Review service request details"
    ],
    "confidence": 0.85,
    "analysis": {
      "sentiment": "Panic",
      "urgency": "Critical",
      "informationNeed": "Minimal",
      "emotionalState": "Panic",
      "concerns": ["Water/Plumbing Issue", "Safety Concern"]
    },
    "strategy": {
      "tone": "Supportive",
      "informationLevel": "Minimal",
      "approach": "Reassuring",
      "confidence": 0.85
    }
  }
}
```

## Key Features

### 1. Adaptive Communication
- Adjusts tone based on client emotional state
- Balances information (not too much, not too little)
- Adapts to client's communication style over time

### 2. Learning System
- Learns from each interaction
- Builds knowledge base per client
- Tracks keyword reactions
- Remembers successful patterns

### 3. Personalization
- Each client gets a unique profile
- Responses tailored to individual preferences
- References past successful interactions

### 4. Safety & Support
- Detects urgency and emotional state
- Provides appropriate level of support
- Suggests actionable steps
- Makes clients feel heard and understood

## Integration Points

### With ChatService
You can integrate this with your existing `ChatService`:

```csharp
// In ChatService, check if it's a service request context
if (isServiceRequestContext)
{
    var agenticResponse = await _agenticAIService.ProcessServiceRequestAsync(
        patientId, 
        prompt, 
        serviceRequestId);
    return agenticResponse.Message;
}
```

### With ServiceRequestService
The agentic AI can access service request details to provide context-aware responses.

## Monitoring & Improvement

### View Client Profiles
```sql
SELECT * FROM ClientProfiles WHERE ClientId = 123;
```

### View Interaction History
```sql
SELECT * FROM ClientInteractionHistory 
WHERE ClientId = 123 
ORDER BY CreatedAt DESC 
LIMIT 50;
```

### View Learned Patterns
```sql
SELECT * FROM ClientInteractionPatterns 
WHERE ClientId = 123;
```

## Next Steps

1. **Run the migration** to create the database tables
2. **Test the API** with sample service requests
3. **Monitor learning** by checking client profiles over time
4. **Integrate with UI** to show agentic AI responses in chat
5. **Fine-tune prompts** based on real-world usage

## Notes

- The system starts with default values and learns over time
- Each client gets their own profile automatically
- Learning happens in the background (non-blocking)
- All interactions are logged for analysis and improvement
- The system respects client privacy and only uses interaction data

