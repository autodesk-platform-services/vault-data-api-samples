import { createFileRoute } from '@tanstack/react-router'
import Links from './-explorer/links'

export const Route = createFileRoute('/_workspace/explorer/$id/links')({
  component: Component
})

function Component() {
  const { id } = Route.useParams()
  return <Links id={id} />
}