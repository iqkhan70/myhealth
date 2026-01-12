# Agentic AI Integration Guide - Where Clients Send Messages

## Current Message Flow

### 1. **AI Chat Page** (`/chat`) - 🤖 AI Assistant Chat

**Location**: `SM_MentalHealthApp.Client/Pages/Chat.razor`

**Purpose**: Client chats with AI assistant (HuggingFace/OpenAI)

- Client types message in input field
- Clicks "Send" or presses Enter
- Calls `POST /api/chat/send`
- Server processes via `ChatController` → `ChatService` → `HuggingFaceService`
- AI response displayed in chat interface

**Current Flow**:

```
Client → Chat.razor → POST /api/chat/send → ChatController → ChatService → HuggingFaceService
```

**✅ THIS IS WHERE AGENTIC AI SHOULD BE INTEGRATED** - When client is chatting about service requests

### 2. **Real-Time Chat** (`/real-time-chat`) - 💬 Person-to-Person Chat

**Location**: `SM_MentalHealthApp.Client/Pages/RealTimeChat.razor`

**Purpose**: Direct messaging between clients and SMEs/doctors (human-to-human)

- Uses SignalR/WebSocket for real-time communication
- Client sends message → `RealtimeService` → SignalR Hub → Other person receives
- **NOT for AI** - this is direct human communication
- Used for coordinating with assigned SMEs

**Current Flow**:

```
Client → RealTimeChat.razor → RealtimeService → SignalR Hub → Other User
```

**❌ NOT for agentic AI** - This is person-to-person messaging

### 2. **Real-Time Chat** (`/real-time-chat`) - 💬 Person-to-Person

**Location**: `SM_MentalHealthApp.Client/Pages/RealTimeChat.razor`

**Purpose**: Direct messaging between clients and SMEs/doctors (human-to-human)

- Uses SignalR/WebSocket for real-time communication
- Client sends message → `RealtimeService` → SignalR Hub → Other person receives
- **NOT for AI** - this is direct human communication
- Used for coordinating with assigned SMEs

**Current Flow**:

```
Client → RealTimeChat.razor → RealtimeService → SignalR Hub → Other User
```

**❌ NOT for agentic AI** - This is person-to-person messaging, not AI

### 3. **Service Request Creation** (`/service-requests`)

**Location**: `SM_MentalHealthApp.Client/Components/CreateServiceRequestDialog.razor`

**How it works**:

- Client clicks "Create Service Request"
- Fills form (Title, Description, Type, etc.)
- Description field is where client describes the problem
- Calls `POST /api/servicerequest` to create the request

**Current Flow**:

```
Client → CreateServiceRequestDialog → POST /api/servicerequest → ServiceRequestController → ServiceRequestService
```

**Future**: Could add agentic AI help button here

## Integration Options

### Option 1: Integrate into ChatService (Recommended)

**Best for**: When clients are chatting about service requests

**Implementation**:
Modify `ChatService.SendMessageAsync()` to detect service request context and use agentic AI:

```csharp
// In ChatService.SendMessageAsync()
// After detecting serviceRequestId (line 60), check if we should use agentic AI

if (serviceRequestId.HasValue && userRoleId == Roles.Patient)
{
    // Use agentic AI for service request chats
    var agenticResponse = await _agenticAIService.ProcessServiceRequestAsync(
        patientId,
        prompt,
        serviceRequestId.Value);

    return new ChatResponse
    {
        Id = responseId,
        Message = agenticResponse.Message,
        Provider = "AgenticAI"
    };
}
```

### Option 2: Add to Service Request Creation Dialog

**Best for**: Providing helpful suggestions when creating service requests

**Implementation**:
Add a "Get Help" button in `CreateServiceRequestDialog.razor`:

```razor
<RadzenButton Text="Get Help with Description"
              ButtonStyle="ButtonStyle.Info"
              Click="@GetAgenticHelp" />
```

Then call the agentic AI API:

```csharp
private async Task GetAgenticHelp()
{
    var request = new ProcessServiceRequestRequest
    {
        ClientId = AuthService.CurrentUser.Id,
        ClientMessage = request.Description, // What they've typed so far
        ServiceRequestId = null // Not created yet
    };

    var response = await Http.PostAsJsonAsync("api/agenticai/process-service-request", request);
    // Show response in a helpful tooltip or modal
}
```

### Option 3: Dedicated Service Request Chat Interface

**Best for**: Separate chat interface specifically for service requests

**Implementation**:
Create a new page `ServiceRequestChat.razor` that:

- Shows service request details
- Has chat interface
- Always uses agentic AI for responses
- Links to specific service request

## Recommended Integration: Option 1 (AI Chat Only)

**Important**: Agentic AI should be integrated into **AI Chat** (`/chat`), NOT Real-Time Chat (`/real-time-chat`).

- **AI Chat** = Client ↔ AI Assistant (where agentic AI goes)
- **Real-Time Chat** = Client ↔ Human SME (person-to-person, no AI)

### Step 1: Modify ChatService

Add agentic AI service injection:

```csharp
// In ChatService constructor
private readonly IServiceRequestAgenticAIService _agenticAIService;

public ChatService(
    // ... existing parameters
    IServiceRequestAgenticAIService agenticAIService)
{
    // ... existing assignments
    _agenticAIService = agenticAIService;
}
```

### Step 2: Detect Service Request Context for Patients

In `SendMessageAsync()`, the code currently detects `serviceRequestId` for Doctors/Attorneys/SMEs (line 49). We need to also detect it for **Patients**:

```csharp
// Around line 47-63 in ChatService.cs - MODIFY THIS SECTION
int? serviceRequestId = null;

// For Doctors/Attorneys/SMEs (existing logic)
if (!isGenericMode && patientId > 0 && (userRoleId == Shared.Constants.Roles.Doctor || userRoleId == Shared.Constants.Roles.Attorney || userRoleId == Shared.Constants.Roles.Sme))
{
    var defaultSr = await _serviceRequestService.GetDefaultServiceRequestForClientAsync(patientId);
    if (defaultSr != null)
    {
        var isAssigned = await _serviceRequestService.IsSmeAssignedToServiceRequestAsync(defaultSr.Id, userId);
        if (isAssigned)
        {
            serviceRequestId = defaultSr.Id;
            _logger.LogInformation("Using ServiceRequest {ServiceRequestId} for chat session", serviceRequestId);
        }
    }
}
// NEW: For Patients chatting about their service request
else if (!isGenericMode && userRoleId == Shared.Constants.Roles.Patient && patientId == userId)
{
    // Get patient's active service requests
    var activeServiceRequests = await _serviceRequestService.GetServiceRequestsAsync(clientId: patientId);
    var activeSr = activeServiceRequests.FirstOrDefault(sr => sr.Status == "Active" || sr.Status == "Pending");
    if (activeSr != null)
    {
        serviceRequestId = activeSr.Id;
        _logger.LogInformation("Patient {PatientId} chatting about ServiceRequest {ServiceRequestId}", patientId, serviceRequestId);
    }
}
```

### Step 3: Use Agentic AI for Service Request Chats

After detecting `serviceRequestId`, use agentic AI:

```csharp
// Around line 92-93 in ChatService.cs - REPLACE THIS
// OLD: var response = await _huggingFaceService.GenerateResponse(roleBasedPrompt, isGenericMode);

// NEW: Check if we should use agentic AI
string response;
if (serviceRequestId.HasValue && userRoleId == Shared.Constants.Roles.Patient && patientId == userId)
{
    _logger.LogInformation("Using agentic AI for service request chat: SR {ServiceRequestId}", serviceRequestId.Value);

    // Use agentic AI instead of regular chat
    var agenticResponse = await _agenticAIService.ProcessServiceRequestAsync(
        patientId,
        prompt,
        serviceRequestId.Value);

    response = agenticResponse.Message;
}
else
{
    // Use regular HuggingFace service for other cases
    response = await _huggingFaceService.GenerateResponse(roleBasedPrompt, isGenericMode);
}
```

### Step 3: Register Service in DependencyInjection

Already done! ✅ The service is registered in `DependencyInjection.cs`

## Testing the Integration

### Test Scenario 1: Client Creates Service Request

1. Client goes to `/service-requests`
2. Clicks "Create Service Request"
3. Types: "My sink is leaking and water is everywhere!"
4. Creates the request
5. **NEW**: Client can now chat about this service request and get agentic AI responses

### Test Scenario 2: Client Chats About Service Request

1. Client goes to `/chat`
2. System detects they have an active service request
3. Client types: "How do I fix my leaking sink?"
4. **Agentic AI responds** with personalized, adaptive guidance
5. Client continues conversation
6. **Agentic AI learns** from each interaction

## API Endpoints

### Direct Agentic AI Endpoint

```
POST /api/agenticai/process-service-request
Authorization: Bearer <token>
Content-Type: application/json

{
  "clientId": 123,
  "clientMessage": "My sink is leaking!",
  "serviceRequestId": 456  // Optional
}
```

### Integrated Chat Endpoint (After Integration)

```
POST /api/chat/send
Authorization: Bearer <token>
Content-Type: application/json

{
  "prompt": "My sink is leaking!",
  "patientId": 123,
  "userId": 123,
  "userRoleId": 1,  // Patient
  "isGenericMode": false
}
// If serviceRequestId is detected, agentic AI is used automatically
```

## Where Clients Actually Send Messages

### Web App:

1. **`/chat`** - Main chat interface

   - File: `SM_MentalHealthApp.Client/Pages/Chat.razor`
   - Endpoint: `POST /api/chat/send`
   - Current: Uses HuggingFaceService
   - **After Integration**: Will use AgenticAI for service requests

2. **`/service-requests`** - Service request list
   - File: `SM_MentalHealthApp.Client/Pages/ServiceRequests.razor`
   - Creates service requests (not messages yet)
   - **Future**: Could add chat interface here

### Mobile App:

1. **Service Request Form**
   - File: `MentalHealthMobileClean/src/components/CreateServiceRequestForm.js`
   - Creates service requests
   - **Future**: Could add agentic AI help here

## Next Steps

1. **Integrate into ChatService** (Option 1) - Recommended
2. **Test with real service requests**
3. **Monitor learning** - Check client profiles improve over time
4. **Add UI indicators** - Show when agentic AI is responding
5. **Gather feedback** - See if clients find responses helpful

## Example Integration Code

Here's the complete integration code for `ChatService.cs`:

```csharp
// Add to constructor
private readonly IServiceRequestAgenticAIService _agenticAIService;

// In SendMessageAsync(), after serviceRequestId detection (around line 60)
if (serviceRequestId.HasValue && userRoleId == Roles.Patient && patientId == userId)
{
    try
    {
        _logger.LogInformation("Using agentic AI for service request {ServiceRequestId}", serviceRequestId.Value);

        var agenticResponse = await _agenticAIService.ProcessServiceRequestAsync(
            patientId,
            prompt,
            serviceRequestId.Value);

        await _chatHistoryService.AddMessageAsync(
            session.Id,
            MessageRole.Assistant,
            agenticResponse.Message,
            MessageType.Response,
            false,
            null);

        await _chatHistoryService.UpdateSessionActivityAsync(session.Id);

        return new ChatResponse
        {
            Id = Guid.NewGuid().ToString(),
            Message = agenticResponse.Message,
            Provider = "AgenticAI"
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error using agentic AI, falling back to regular chat");
        // Fall through to regular chat
    }
}

// Continue with regular chat flow...
```

This way, when a patient chats about their service request, they automatically get agentic AI responses that learn and adapt!
