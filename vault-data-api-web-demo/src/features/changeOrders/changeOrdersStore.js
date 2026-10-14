import { proxy } from 'valtio'
import { authStore } from '~/features/authentication/authStore'
import { changeOrderTransformer, fetchCoContentsRecursive } from './changeOrdersApi'
import { propertiesTransformer, propertyDefinitionsTransformer } from '~/api/properties'

export const changeOrdersStore = proxy({
    loading: false,
    doneSync: false,
    changeOrders: [],
    properties:  [],
    propertyDefinitions: [],
})

export const sync = async () => {
    changeOrdersStore.loading = true
    const session = authStore.session;
    const response = await fetchCoContentsRecursive(session);
    if (authStore.session !== session) {
        return;
    }
    const changeOrders = response.results.map(changeOrderTransformer)
    const properties = response.results.map(propertiesTransformer).reduce((acc, props)=>([...acc, ...props]))
    const propertyDefinitions = Object.values(response.included.propertyDefinition).map(propertyDefinitionsTransformer)
    changeOrdersStore.loading = false
    changeOrdersStore.doneSync = true
    Object.assign(changeOrdersStore, {changeOrders, properties, propertyDefinitions})
}
