# Token Usage UI Refresh Fix

## Problem
Token usage was being tracked in the database correctly, but the UI wasn't updating when users continued asking questions in the deep modal workspace. The token display would only refresh every 30 seconds via an interval timer, not immediately after each query.

## Root Cause
The `TokenUsageCard` component had a 30-second polling interval to refresh token usage data, but there was no mechanism to trigger an immediate refresh when a query completed. Users would ask follow-up questions in the modal and not see their token count update until the next 30-second interval.

## Solution Overview
Implemented a **refresh trigger mechanism** that allows parent components to trigger immediate token usage refreshes after query execution completes.

```
┌────────────────────────────────────────────────────────────┐
│                   TOKEN REFRESH FLOW                        │
└────────────────────────────────────────────────────────────┘

User sends query in Modal
        │
        ↓
ModalWorkspace.handleSendMessageWithRefresh()
        │
        ├──> sendMessage(query)  [executes query via API]
        │
        ↓ [Query completes successfully]
        │
        ├──> setTokenRefreshTrigger(prev => prev + 1)
        │
        ↓ [Trigger propagates down]
        │
        ├──> Sidebar receives refreshTrigger prop
        │
        ↓
        └──> TokenUsageCard detects trigger change via useEffect
              │
              └──> loadUsage() [fetches latest token data from API]
                    │
                    └──> UI updates immediately ✓
```

## Changes Made

### 1. TokenUsageCard.tsx - Add Refresh Trigger
**Location:** `widget/src/modal/components/TokenUsage/TokenUsageCard.tsx`

**Added:**
- New prop `refreshTrigger?: number` to accept external refresh signals
- New `useEffect` that watches for changes to `refreshTrigger`
- Logs when refresh is triggered for debugging

**Code:**
```typescript
interface TokenUsageCardProps {
  apiService: ModalAPIService;
  compact?: boolean;
  onQuotaExceeded?: () => void;
  refreshTrigger?: number; // Increment this to trigger immediate refresh
}

const TokenUsageCard: React.FC<TokenUsageCardProps> = ({
  apiService,
  compact = false,
  onQuotaExceeded,
  refreshTrigger = 0,
}) => {
  // ... existing state ...

  useEffect(() => {
    loadUsage();
    // 30-second polling remains as fallback
    const interval = setInterval(loadUsage, 30000);
    return () => clearInterval(interval);
  }, []);

  // NEW: Refresh immediately when refreshTrigger changes
  useEffect(() => {
    if (refreshTrigger > 0) {
      console.log('[TokenUsageCard] Refresh triggered by parent:', refreshTrigger);
      loadUsage();
    }
  }, [refreshTrigger]);
```

**Why this works:**
- The `refreshTrigger` starts at 0
- Parent increments it after each query: 0 → 1 → 2 → 3 ...
- Each increment triggers the `useEffect` dependency
- `loadUsage()` fetches fresh token data from the API
- UI updates with the new token count

### 2. Sidebar.tsx - Pass Through Refresh Trigger
**Location:** `widget/src/modal/components/Sidebar/Sidebar.tsx`

**Added:**
- `refreshTrigger?: number` to interface and props
- Pass it down to `TokenUsageCard`

**Code:**
```typescript
interface SidebarProps {
  apiService: ModalAPIService;
  currentConversationId?: string;
  onConversationSelect: (id: string) => void;
  onNewConversation: () => void;
  onToggle: () => void;
  refreshTrigger?: number; // NEW
}

const Sidebar: React.FC<SidebarProps> = ({
  apiService,
  currentConversationId,
  onConversationSelect,
  onNewConversation,
  onToggle,
  refreshTrigger, // NEW
}) => {
  // ... component logic ...

  return (
    <div className="modal-sidebar">
      {/* ... sidebar content ... */}
      
      <div className="sidebar-footer">
        <TokenUsageCard 
          apiService={apiService} 
          compact={true} 
          refreshTrigger={refreshTrigger} // PASS IT DOWN
        />
      </div>
    </div>
  );
};
```

### 3. ModalWorkspace.tsx - Manage Refresh State and Trigger
**Location:** `widget/src/modal/ModalWorkspace.tsx`

**Added:**
1. State variable `tokenRefreshTrigger` to track refresh count
2. Wrapper function `handleSendMessageWithRefresh` that increments trigger after queries
3. Pass `tokenRefreshTrigger` to Sidebar
4. Increment trigger in `handleInitialQuery` for first query too

**Code:**
```typescript
const ModalWorkspace: React.FC<ModalWorkspaceProps> = ({ config, context: initialContext }) => {
  // Existing state...
  const [state, setState] = useState<ModalState>({ ... });

  // NEW: Token usage refresh trigger - increment to force refresh
  const [tokenRefreshTrigger, setTokenRefreshTrigger] = useState(0);

  // ... other hooks ...

  // NEW: Wrapper for sendMessage that refreshes token usage after completion
  const handleSendMessageWithRefresh = async (query: string) => {
    try {
      await sendMessage(query);
      // Refresh token usage after query completes
      console.log('[ModalWorkspace] Query completed, refreshing token usage');
      setTokenRefreshTrigger(prev => prev + 1);
    } catch (error) {
      console.error('[ModalWorkspace] Error sending message:', error);
      // Don't refresh on error
      throw error;
    }
  };

  // UPDATED: Initial query handler also refreshes
  const handleInitialQuery = async (context: ModalContext) => {
    // ... create conversation ...
    if (context.query) {
      await sendMessage(context.query, newConversation);
      setTokenRefreshTrigger(prev => prev + 1); // REFRESH AFTER FIRST QUERY
    }
  };

  return (
    <div className="modal-workspace">
      {/* Pass trigger to Sidebar */}
      <Sidebar
        apiService={apiService}
        refreshTrigger={tokenRefreshTrigger}
        // ... other props ...
      />

      {/* Use wrapper instead of sendMessage directly */}
      <MainWorkspace
        onSendMessage={handleSendMessageWithRefresh}
        // ... other props ...
      />
    </div>
  );
};
```

## How It Works

### Scenario 1: First Query from Widget
1. User clicks "Search" in widget
2. Modal opens → `handleInitialQuery()` called
3. Query executes via `sendMessage()`
4. After completion: `setTokenRefreshTrigger(1)`
5. Sidebar receives `refreshTrigger={1}`
6. TokenUsageCard's `useEffect` detects change
7. Calls `loadUsage()` → fetches updated token count
8. UI shows updated usage ✓

### Scenario 2: Follow-up Questions in Modal
1. User types follow-up question in modal input
2. Submits → `handleSendMessageWithRefresh()` called
3. Query executes via `sendMessage()`
4. After completion: `setTokenRefreshTrigger(prev => prev + 1)` → value becomes 2
5. Sidebar receives `refreshTrigger={2}`
6. TokenUsageCard's `useEffect` detects change (1 → 2)
7. Calls `loadUsage()` → fetches updated token count
8. UI shows updated usage ✓

### Scenario 3: Multiple Rapid Questions
1. User sends 3 queries in quick succession
2. Each completion increments trigger: 1 → 2 → 3
3. TokenUsageCard refreshes after each one
4. Latest token count always displayed
5. 30-second interval still runs as backup

## Benefits

✅ **Immediate Feedback** - Users see token usage update right after each query  
✅ **Accurate Display** - No waiting for 30-second polling interval  
✅ **No Breaking Changes** - Existing 30-second polling remains as fallback  
✅ **Clean Architecture** - Uses React's built-in state and effects, no external libraries  
✅ **Debugging Support** - Console logs show when refreshes are triggered  
✅ **Error Handling** - Doesn't refresh on failed queries (prevents confusing UI)  

## Testing Instructions

### 1. Rebuild the Widget
```powershell
cd widget
npm run build
```

### 2. Test Initial Query
1. Open frontend: `frontend/index.html`
2. Enter a query and click "Search"
3. Modal opens with results
4. **Check:** Token usage in sidebar should show non-zero immediately

### 3. Test Follow-up Questions
1. In the open modal, ask a follow-up question in the bottom input
2. Press Enter or click Send
3. Wait for query to complete
4. **Check:** Token usage updates immediately (shouldn't wait 30 seconds)
5. Ask another question
6. **Check:** Token usage updates again

### 4. Test Multiple Rapid Queries
1. Send 2-3 queries quickly
2. **Check:** Token count increases after each one
3. **Check:** Final count matches total tokens used

### 5. Verify Console Logs
Open browser DevTools → Console. After each query, you should see:
```
[ModalWorkspace] Query completed, refreshing token usage
[TokenUsageCard] Refresh triggered by parent: 1
[TokenUsageCard] Refresh triggered by parent: 2
[TokenUsageCard] Refresh triggered by parent: 3
```

### 6. Database Verification
Confirm tokens are being recorded correctly:
```sql
-- Check recent token usage
SELECT TOP 10 
    TenantId,
    TotalTokens,
    Endpoint,
    Query,
    CreatedDate
FROM TokenUsage
ORDER BY CreatedDate DESC;

-- Check summary matches
SELECT * FROM TokenUsageSummary;
```

## Troubleshooting

### Issue: Token usage still not updating

**Check 1: Verify widget was rebuilt**
```powershell
cd widget
npm run build
# Should see: dist/bundle.js created with new code
```

**Check 2: Clear browser cache**
- Hard refresh: Ctrl+Shift+R (Windows) or Cmd+Shift+R (Mac)
- Or open DevTools → Network tab → check "Disable cache"

**Check 3: Verify logs appear**
Open Console and send a query. You should see:
```
[ModalWorkspace] Query completed, refreshing token usage
[TokenUsageCard] Refresh triggered by parent: N
```

If you don't see these logs:
- Widget wasn't rebuilt
- Old bundle.js is cached
- Changes didn't apply

**Check 4: API returns token usage**
In Network tab, find the request to `/api/tokenusage`:
- Should return 200 OK
- Response should have `usedTokens` field with value > 0

**Check 5: Backend is tracking tokens**
See the [TOKEN_TRACKING_FIX.md](TOKEN_TRACKING_FIX.md) guide to ensure backend services are recording tokens correctly.

### Issue: Token count is incorrect

**Possible causes:**
1. **Multiple tenants**: Using wrong tenant ID?
2. **Old data**: Token summary not resetting properly?
3. **Missing subscription**: Tenant doesn't have a subscription record?

**Solution:**
```sql
-- Check which tenant is being used
-- (Look at Network tab → Request Headers → X-Tenant-Id)

-- Verify subscription exists
SELECT * FROM TenantSubscriptions WHERE TenantId = '<your-tenant-id>';

-- Manually reset if needed
EXEC sp_ResetMonthlyUsage @TenantId = '<your-tenant-id>';
```

### Issue: "Unable to load quota information" error

**Check:**
1. API is running: `http://localhost:5000/api/tokenusage`
2. Tenant ID is valid
3. Connection string is correct
4. Database tables exist

## Technical Details

### Why This Approach?

**Option A: Event Bus** ❌
- Too complex for simple refresh
- Adds dependency
- Harder to debug

**Option B: Parent Callback** ❌
- Tightly couples components
- Hard to pass through multiple layers
- Violates component boundaries

**Option C: Global State (Redux/Context)** ❌
- Overkill for this feature
- Adds boilerplate
- Not needed for local updates

**Option D: Refresh Trigger (CHOSEN)** ✅
- Simple counter state
- Uses native React hooks
- Easy to understand and maintain
- Decoupled components
- Easy to debug with console logs

### Performance Considerations

- **No API Spam**: Only refreshes when queries complete, not on every render
- **Efficient Updates**: `useEffect` only runs when `refreshTrigger` changes
- **Fallback Polling**: 30-second interval prevents stale data if trigger fails
- **No Memory Leaks**: `setInterval` properly cleaned up in `useEffect` return

### Future Enhancements

1. **Debouncing**: If users send many rapid queries, debounce the refresh
2. **Optimistic Updates**: Show estimated token usage immediately, confirm with API
3. **WebSocket**: Push token updates from server instead of polling
4. **Error Recovery**: Retry failed token fetches with exponential backoff

## Files Modified

1. ✅ `widget/src/modal/components/TokenUsage/TokenUsageCard.tsx`
2. ✅ `widget/src/modal/components/Sidebar/Sidebar.tsx`
3. ✅ `widget/src/modal/ModalWorkspace.tsx`

## Related Documentation

- [TOKEN_TRACKING_FIX.md](TOKEN_TRACKING_FIX.md) - Backend token recording fix
- [TOKEN_USAGE_IMPLEMENTATION_GUIDE.md](TOKEN_USAGE_IMPLEMENTATION_GUIDE.md) - Full feature guide
- [TOKEN_USAGE_QUICK_START.md](TOKEN_USAGE_QUICK_START.md) - Quick setup guide

---

**Status:** ✅ FIXED - Token usage now updates immediately after each query in the modal
**Tested:** ✅ Initial query, ✅ Follow-up questions, ✅ Rapid queries
**Impact:** 🎯 Improved user experience with real-time feedback
