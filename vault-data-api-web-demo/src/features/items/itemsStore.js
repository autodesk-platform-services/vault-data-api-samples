import { proxy } from 'valtio'
import { authStore } from '~/features/authentication/authStore'
import { itemTransformer, fetchItemContentsRecursive } from './itemsApi'
import { propertiesTransformer, propertyDefinitionsTransformer } from '~/api/properties'

export const itemsStore = proxy({
    loading: false,
    doneSync: false,
    items: [],
    properties:  [],
    propertyDefinitions: [],
})

export const sync = async () => {
    itemsStore.loading = true
    const session = authStore.session;
    const response = await fetchItemContentsRecursive(session);
    if (authStore.session !== session) {
        return;
    }
    const items = response.results.map(itemTransformer)
    const properties = response.results.map(propertiesTransformer).reduce((acc, props)=>([...acc, ...props]))
    const propertyDefinitions = Object.values(response.included.propertyDefinition).map(propertyDefinitionsTransformer)
    itemsStore.loading = false
    itemsStore.doneSync = true
    Object.assign(itemsStore, {items, properties, propertyDefinitions})
}
