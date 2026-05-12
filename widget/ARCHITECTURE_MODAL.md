# AI Analytics Modal Workspace - Architecture

## Overview

This document outlines the architecture for the fullscreen AI analytics modal workspace that extends the existing AI widget.

## Design Principles

1. **Zero Friction Integration**: Single script tag, no client code changes
2. **Complete Isolation**: Shadow DOM prevents CSS/JS conflicts
3. **Seamless Context Transfer**: Widget state flows into modal workspace
4. **Lazy Loading**: Modal bundle loads only when opened
5. **Framework Agnostic**: Works with any client application

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Client Website                            │
│  <script src="ai-widget.js" data-api-base-url="..." />      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    AI Widget Loader                          │
│  - Reads config from script tag                              │
│  - Creates Shadow DOM containers                             │
│  - Loads widget bundle (50KB)                                │
│  - Lazy loads modal bundle on demand                         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ├─────────────────┬──────────────┐
                              ▼                 ▼              ▼
┌──────────────────┐  ┌──────────────────┐  ┌─────────────────┐
│  Floating Widget │  │  Modal Workspace │  │  State Manager  │
│  (Always Visible)│  │  (Lazy Loaded)   │  │  (EventBus)     │
│                  │  │                  │  │                 │
│  - Quick Query   │  │  - Left Sidebar  │  │  - Context      │
│  - Mini Insights │  │  - Main Area     │  │  - Conversations│
│  - "Explore" BTN │  │  - Right Panel   │  │  - Transfer     │
└──────────────────┘  └──────────────────┘  └─────────────────┘
```

## Component Structure

### 1. Loader System (`loader.ts`)

**Responsibilities:**
- Read configuration from script tag
- Create Shadow DOM containers for widget AND modal
- Load widget bundle immediately
- Lazy load modal bundle on first "Explore" click
- Manage global event bus for state transfer

**Bundle Strategy:**
```
ai-widget.js (5KB) → Loads widget bundle (50KB)
                  → Lazy loads modal bundle (200KB) when needed
```

### 2. Floating Widget (`widget/`)

**Components:**
- `FloatingButton.tsx`: Minimized state
- `WidgetPanel.tsx`: Expanded state with mini insights
- `QueryBar.tsx`: Quick query input
- `InsightCard.tsx`: Compact insight preview
- `SuggestedQueries.tsx`: Quick actions

**New Addition:**
- **"Explore Deeper" Button**: Triggers modal workspace open

### 3. Modal Workspace (`modal/`)

**New Components:**

```
modal/
├── ModalWorkspace.tsx         # Main container
├── ModalHeader.tsx            # Title, close, minimize
├── components/
│   ├── Sidebar/
│   │   ├── ConversationList.tsx
│   │   ├── SavedAnalyses.tsx
│   │   └── SuggestedQueries.tsx
│   ├── MainWorkspace/
│   │   ├── QueryInput.tsx
│   │   ├── ConversationThread.tsx
│   │   ├── ResponseRenderer.tsx
│   │   ├── ChartRenderer.tsx
│   │   └── TableRenderer.tsx
│   └── RightPanel/
│       ├── Risks.tsx
│       ├── Opportunities.tsx
│       └── Recommendations.tsx
├── hooks/
│   ├── useConversation.ts
│   ├── useCharts.ts
│   └── useKeyboardShortcuts.ts
└── styles/
    └── modal.css
```

### 4. State Management (`state/`)

**StateManager.ts:**
- Central event bus for widget ↔ modal communication
- Manages conversation context
- Handles state transfer when opening modal

**Flow:**
```typescript
Widget Query → State Manager → Modal Workspace
  {
    conversationId: "uuid",
    query: "Why are sales down?",
    insights: [...],
    timestamp: "...",
  }
```

## Shadow DOM Strategy

### Why Shadow DOM?

1. **Complete CSS Isolation**: Widget/modal styles never leak to client
2. **No Class Name Conflicts**: Use simple class names without BEM
3. **Framework Independence**: Works with React, Vue, Angular, etc.
4. **Security**: Prevents client JS from tampering with widget

### Implementation:

```javascript
// Loader creates two Shadow DOM containers
const widgetContainer = document.createElement('div');
widgetContainer.id = 'ai-widget-root';
const widgetShadow = widgetContainer.attachShadow({ mode: 'open' });

const modalContainer = document.createElement('div');
modalContainer.id = 'ai-modal-root';
const modalShadow = modalContainer.attachShadow({ mode: 'open' });

// Mount React apps inside Shadow DOM
ReactDOM.createRoot(widgetShadow.querySelector('#mount')).render(<Widget />);
ReactDOM.createRoot(modalShadow.querySelector('#mount')).render(<Modal />);
```

### Styling Inside Shadow DOM:

- All CSS bundled into JS
- Injected into Shadow DOM on mount
- No external stylesheets needed
- Custom properties for theming

## Context Transfer Protocol

### Widget → Modal Transfer:

```typescript
interface ModalContext {
  // Transfer existing conversation
  conversationId?: string;
  
  // Transfer current query
  query?: string;
  
  // Transfer insights
  insights?: AIInsight[];
  
  // Transfer query result
  queryResult?: QueryResponse;
  
  // User preferences
  theme: 'light' | 'dark';
}

// Event-based transfer
window.dispatchEvent(new CustomEvent('aiWidgetOpenModal', {
  detail: modalContext
}));
```

### Modal → Widget Updates:

```typescript
// Notify widget of conversation updates
window.dispatchEvent(new CustomEvent('aiModalConversationUpdate', {
  detail: {
    conversationId: string;
    lastQuery: string;
    timestamp: string;
  }
}));
```

## Lazy Loading Strategy

### Phase 1: Initial Page Load
- Only `ai-widget.js` loader (5KB) loads
- Creates widget container
- Registers event listeners

### Phase 2: Widget Interaction
- User opens widget
- Widget bundle (50KB) loads
- React mounts inside Shadow DOM

### Phase 3: Modal Trigger
- User clicks "Explore Deeper"
- Modal bundle (200KB) lazy loads
- Modal mounts in separate Shadow DOM
- Context transfers from widget

### Bundle Separation:

```javascript
// webpack.config.js
module.exports = {
  entry: {
    loader: './src/loader.ts',
    widget: './src/widget/index.tsx',
    modal: './src/modal/index.tsx',
  },
  output: {
    filename: 'ai-[name].js',
    library: 'AIQuery',
    libraryTarget: 'umd',
  }
};
```

## API Integration

### No Backend Changes Required

Modal uses same API service as widget:

```typescript
// Reuse existing APIService
import APIService from '../services/api';

// Modal extends with conversation management
class ModalAPIService extends APIService {
  async loadConversation(id: string): Promise<Conversation> { }
  async listConversations(): Promise<Conversation[]> { }
  async saveAnalysis(analysis: Analysis): Promise<void> { }
}
```

### API Endpoints (Already Exist):

- `POST /api/query/execute` - Execute query
- `GET /api/conversations/{id}` - Load conversation
- `GET /api/conversations` - List conversations
- `POST /api/conversations/{id}/messages` - Add message
- `GET /api/chart/{conversationId}` - Get chart data

## Modal UX Behavior

### Opening:
- Smooth scale + fade animation (300ms)
- Backdrop blur (backdrop-filter)
- Focus trap activated
- Body scroll locked

### Closing:
- ESC key
- Backdrop click
- Close button
- Minimize to widget

### Responsive:
- Desktop: 90vw × 90vh centered modal
- Tablet: 95vw × 95vh
- Mobile: Fullscreen takeover

### Keyboard Shortcuts:
- `ESC` - Close modal
- `Cmd/Ctrl + K` - Focus query input
- `Cmd/Ctrl + Enter` - Submit query
- `Cmd/Ctrl + /` - Toggle sidebar

## Performance Targets

| Metric | Target |
|--------|--------|
| Initial Loader | < 5KB |
| Widget Bundle | < 50KB |
| Modal Bundle | < 200KB |
| Time to Interactive (Widget) | < 1s |
| Modal Load Time | < 2s |
| First Paint | < 100ms |

## Security Considerations

1. **API Key**: Never exposed in client code (passed via script tag)
2. **Shadow DOM**: Prevents client JS injection
3. **CSP Compatible**: No inline scripts, all bundled
4. **XSS Protection**: Sanitize all query inputs
5. **CORS**: Backend must allow widget origin

## Browser Support

- Chrome/Edge 90+
- Firefox 88+
- Safari 14+
- No IE11 support

## Deployment

### CDN Structure:
```
https://cdn.yourdomain.com/
├── ai-widget.js          (loader + widget)
├── ai-modal.js           (modal workspace)
├── ai-widget.js.map
└── ai-modal.js.map
```

### Integration:
```html
<!-- Single script tag -->
<script 
  src="https://cdn.yourdomain.com/ai-widget.js"
  data-api-base-url="https://api.yourdomain.com"
  data-api-key="CLIENT_API_KEY"
  data-tenant-id="TENANT_ID"
  data-theme="light">
</script>
```

## Next Steps

1. ✅ Create modal workspace components
2. ✅ Implement state manager with event bus
3. ✅ Build lazy loading system
4. ✅ Update loader for dual Shadow DOM
5. ✅ Create webpack split configuration
6. ✅ Add keyboard shortcuts
7. ✅ Test isolation and integration
