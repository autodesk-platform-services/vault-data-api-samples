import { createFileRoute } from '@tanstack/react-router'
import 'rapidoc';
import { authStore } from '~/store/auth';
import { useSnapshot } from 'valtio'
import { Alert } from 'antd';

export const Route = createFileRoute('/_workspace/playground')({
  component: ApiDoc
})

function ApiDoc() {
  const {session} = useSnapshot(authStore)
  return (<>
        <Alert
            message={`Session Info: ${session.userName}(id=${session.userId}) -  ${session.vaultName}(id=${session.vaultId}) - Token(${session.token})`}
            banner
            type="info"
            showIcon
        />
    <rapi-doc
      spec-url = "/spec.json"
      show-header = "false"
      bg-color = "#f9f9fa"
      nav-bg-color = "#3f4d67"
      nav-text-color = "#a9b7d0"
      nav-hover-bg-color = "#333f54"
      nav-hover-text-color = "#fff"
      nav-accent-color = "#f87070"
      primary-color = "#5c7096"
      theme = "light"
      render-style = "focused"
      show-method-in-nav-bar = "as-colored-block"
      use-path-in-nav-bar="true"
      style = {{ height: "calc(100vh - 60px)", width: "100%" }}
      api-key-name="authorization"
      api-key-location="header"
      api-key-value={session.token}
    >
    </rapi-doc>
    </>
  );
}