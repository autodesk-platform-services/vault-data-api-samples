import { StrictMode } from 'react'
import ReactDOM from 'react-dom/client'
import { RouterProvider, createRouter } from '@tanstack/react-router'
import { ConfigProvider } from 'antd';

import { AuthProvider } from '~/provider/auth';

// Import the generated route tree
import { routeTree } from './routeTree.gen'

import './main.css'
// Create a new router instance
const router = createRouter({ routeTree })
const theme = {
  token: {
    colorPrimary: "#0529ba",
    colorInfo: "#0529ba",
    borderRadius: 0,
  },
};

// Render the app
const rootElement = document.getElementById('root')
if (!rootElement.innerHTML) {
  const root = ReactDOM.createRoot(rootElement)
  root.render(
    // <StrictMode>
      <ConfigProvider theme={theme}> 
        <AuthProvider>
          <RouterProvider router={router} />
        </AuthProvider>
      </ConfigProvider>
    // </StrictMode>,
  )
}