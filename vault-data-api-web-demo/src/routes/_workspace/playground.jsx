import { createFileRoute } from '@tanstack/react-router';
import PlaygroundPage from '~/features/playground/PlaygroundPage';

export const Route = createFileRoute('/_workspace/playground')({ component: PlaygroundPage });
