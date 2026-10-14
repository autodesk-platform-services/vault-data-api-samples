import { createFileRoute } from '@tanstack/react-router';
import ExplorerPage from '~/features/explorer/ExplorerPage';

export const Route = createFileRoute('/_workspace/explorer')({ component: Component });

function Component() {
  return <ExplorerPage basePath={Route.fullPath} />;
}
