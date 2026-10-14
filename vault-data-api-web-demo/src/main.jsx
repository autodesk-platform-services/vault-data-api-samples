import ReactDOM from 'react-dom/client'
import { RouterProvider, createRouter } from '@tanstack/react-router'
import { ConfigProvider } from 'antd';

import { AuthProvider } from '~/features/authentication/AuthProvider';
import { initializeAuth, setSessionReset } from '~/features/authentication/authStore';
import { resetExplorer } from '~/features/explorer/filesStore';
import { itemsStore } from '~/features/items/itemsStore';
import { changeOrdersStore } from '~/features/changeOrders/changeOrdersStore';
import { restoreConnection } from '~/features/connection/connection';

// Import the generated route tree
import { routeTree } from './routeTree.gen'

import './main.css'
const theme = {
  token: {
    colorPrimary: "#0529ba",
    colorInfo: "#0529ba",
    borderRadius: 0,
  },
};

// Render the app
const rootElement = document.getElementById('root')
async function bootstrap() {
  rootElement.textContent = 'Preparing sign-in...';
  setSessionReset(() => {
    resetExplorer();
    Object.assign(itemsStore, { loading: false, doneSync: false, items: [], properties: [], propertyDefinitions: [] });
    Object.assign(changeOrdersStore, { loading: false, doneSync: false, changeOrders: [], properties: [], propertyDefinitions: [] });
  });
  await restoreConnection();
  await initializeAuth();
  const router = createRouter({ routeTree });
  const root = ReactDOM.createRoot(rootElement)
  root.render(
      <ConfigProvider theme={theme}> 
        <AuthProvider>
          <RouterProvider router={router} />
        </AuthProvider>
      </ConfigProvider>
  )
}
bootstrap();
