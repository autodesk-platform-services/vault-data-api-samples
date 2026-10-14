import { createFileRoute } from '@tanstack/react-router';
import ChangeOrdersPage from '~/features/changeOrders/ChangeOrdersPage';

export const Route = createFileRoute('/_workspace/changeorders')({ component: ChangeOrdersPage });
