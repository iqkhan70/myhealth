# SR-First Agentic AI: Context Management Explanation

## Overview
The agentic AI uses an **SR-first approach** where every conversation must be tied to a Service Request (SR) before the AI can help with service-related questions. This ensures proper data isolation and context management.

## How SR Context Works

### 1. **ClientAgentSession** (Source of Truth)
- **Purpose**: Tracks the active SR context for each client's agent conversation
- **Location**: `ClientAgentSessions` table
- **Key Fields**:
  - `ClientId`: The client/patient
  - `CurrentServiceRequestId`: The active SR for this conversation
  - `State`: Current conversation state (NoActiveSRContext, SelectingExistingSR, CreatingNewSR, InSRContext)
  - `PendingCreatedServiceRequestId`: SR that was just created, waiting for confirmation

### 2. **ChatSession** (Chat History)
- **Purpose**: Tracks individual chat sessions (for message history)
- **Location**: `ChatSessions` table
- **Key Field**: `ServiceRequestId` (optional, links messages to an SR)
- **Note**: This is secondary to `ClientAgentSession` - the agent uses `ClientAgentSession` as the source of truth

## Conversation Flow

### Scenario 1: New Conversation (No SR Context)
1. **User sends message**: "Kitchen faucet issue"
2. **Agent checks**: `ClientAgentSession` has `State = NoActiveSRContext`
3. **Agent responds**: "Is this about an existing request, or a new one?"
4. **User selects**: "New"
5. **Agent asks**: "What would you like the title to be?"
6. **User responds**: "Kitchen faucet leak"
7. **Agent creates SR**: Creates Service Request #123
8. **Agent sets context**: Updates `ClientAgentSession` to `State = InSRContext`, `CurrentServiceRequestId = 123`
9. **Future messages**: All subsequent messages are automatically tied to SR #123

### Scenario 2: Existing SR Context
1. **User sends message**: "yes it is leaking"
2. **Agent checks**: `ClientAgentSession` has `State = InSRContext`, `CurrentServiceRequestId = 123`
3. **Agent uses context**: Knows this is about SR #123 (kitchen faucet)
4. **Agent responds**: Provides help specific to SR #123

### Scenario 3: Switching Between SRs
1. **Current context**: User is chatting about SR #123 (kitchen faucet)
2. **User sends**: "What about my car repair request?"
3. **Agent checks**: `ClientAgentSession` has `CurrentServiceRequestId = 123` (different SR)
4. **Agent detects intent**: User is asking about a different SR
5. **Agent responds**: "I see you're asking about a different service request. Would you like to switch to your car repair request? I can show you your active requests."
6. **User selects**: "Yes, switch to car repair"
7. **Agent updates context**: Sets `CurrentServiceRequestId = 456` (car repair SR)
8. **Future messages**: Now about SR #456

## Key Behaviors

### When SR Context is Set
- **Explicit selection**: User selects an existing SR from a list
- **SR creation**: User creates a new SR, and it's automatically set as active
- **SR reference**: User mentions an SR number (e.g., "SR-123") or title

### When SR Context is Cleared
- **User explicitly clears**: "Start new conversation" or "Clear context"
- **Session timeout**: After extended inactivity (future feature)
- **Manual reset**: Admin/coordinator resets the session

### Multiple SRs
- **Each client has one active SR context** at a time (in `ClientAgentSession`)
- **To switch SRs**: User must explicitly select a different SR
- **Chat history**: Each `ChatSession` can be linked to a specific SR, but the active context is in `ClientAgentSession`

## Why It Didn't Ask About SR

If the agent didn't ask about SR selection, it's likely because:

1. **Existing ChatSession**: A previous chat session already had a `ServiceRequestId`, and the system reused it
2. **Auto-selection**: The old code auto-selected the first active SR (this has been fixed)
3. **ClientAgentSession had context**: The `ClientAgentSession` already had `CurrentServiceRequestId` set from a previous conversation

## Current Fix

The code has been updated to:
1. **NOT auto-select SRs**: `ChatService` no longer automatically picks the first active SR
2. **Enforce SR-first**: `ServiceRequestAgenticAIService` always checks `ClientAgentSession` first
3. **Prompt when needed**: If no SR context exists, the agent will prompt the user to select or create one

## Testing the Fix

To test that SR-first is working:
1. **Clear your ClientAgentSession**: Delete or reset the session for your client ID
2. **Start a new conversation**: Send "Kitchen faucet issue"
3. **Expected behavior**: Agent should ask "Is this about an existing request, or a new one?"
4. **If it doesn't ask**: Check logs to see if `ClientAgentSession` already has a `CurrentServiceRequestId`

## Future Enhancements

1. **SR Selector UI**: Add a dropdown in the chat UI to switch between SRs
2. **SR Commands**: Support commands like "switch to SR-123" or "show my requests"
3. **Multi-SR Context**: Allow users to reference multiple SRs in one conversation (advanced)
4. **SR Auto-suggest**: When user mentions a problem, suggest matching existing SRs

