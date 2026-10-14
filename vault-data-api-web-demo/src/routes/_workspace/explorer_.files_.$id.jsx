import { createFileRoute } from '@tanstack/react-router';
import FilePreviewPage from '~/features/explorer/FilePreviewPage';

export const Route = createFileRoute('/_workspace/explorer/files/$id')({ component: Component });

function Component() {
  const { id } = Route.useParams();
  return <FilePreviewPage id={id} />;
}
