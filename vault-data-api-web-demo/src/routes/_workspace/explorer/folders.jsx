import { createFileRoute } from '@tanstack/react-router'
import Folders from '~/features/explorer/Folders'

export const Route = createFileRoute('/_workspace/explorer/folders')({
  component: Component
})

function Component() {
  const { id } = Route.useParams()
  return <Folders id={id} />
}