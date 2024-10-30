import { createFileRoute } from '@tanstack/react-router'
import Files  from '../-explorer/files';

export const Route = createFileRoute('/_workspace/explorer/files')({
  component: Component
})

function Component() {
  const { id } = Route.useParams()
  return <Files id={id} />
}