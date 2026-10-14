import { createFileRoute } from '@tanstack/react-router';
import ItemsPage from '~/features/items/ItemsPage';

export const Route = createFileRoute('/_workspace/items')({ component: ItemsPage });
