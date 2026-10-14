import { createFileRoute } from '@tanstack/react-router'
import Links from '~/features/explorer/Links'

export const Route = createFileRoute('/_workspace/explorer/$id/links')({
  component: Component
})

function Component() {
  const { id } = Route.useParams()
  return <Links id={id} />
}