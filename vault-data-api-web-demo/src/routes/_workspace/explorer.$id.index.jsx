import { createFileRoute, Navigate } from '@tanstack/react-router'

export const Route = createFileRoute('/_workspace/explorer/$id/')({
  component: Layout
})

function Layout () {
    return <Navigate from={Route.fullPath} to="./folders" replace />
}