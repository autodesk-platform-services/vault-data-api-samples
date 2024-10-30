import { createFileRoute, Navigate } from '@tanstack/react-router'

const Home = () => {
    return <Navigate to="/explorer" replace />
}

export const Route = createFileRoute('/')({
  component: Home
})