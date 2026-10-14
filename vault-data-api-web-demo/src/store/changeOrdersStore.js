import { proxy } from 'valtio'
import { authStore } from './auth'
import { 
    changeOrderTransformer,
    fetchCoContentsRecursive,
    propertiesTransformer, 
    propertyDefinitionsTransformer 
} from './helper'

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
