import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter as Router } from 'react-router-dom'
import { Provider } from 'react-redux'
import { QueryClientProvider } from '@tanstack/react-query'
import { MsalProvider } from '@azure/msal-react'
import App from './App'
import { AppInitializer } from './components/AppInitializer'
import { store } from './lib/redux/store'
import { queryClient } from './lib/query-client'
import { msalInstance } from './lib/msalConfig'
import './styles/globals.css'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <Router>
      <MsalProvider instance={msalInstance}>
        <Provider store={store}>
          <QueryClientProvider client={queryClient}>
            <AppInitializer>
              <App />
            </AppInitializer>
          </QueryClientProvider>
        </Provider>
      </MsalProvider>
    </Router>
  </React.StrictMode>,
)
