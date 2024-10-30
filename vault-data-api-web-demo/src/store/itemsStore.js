import { proxy } from 'valtio'
import { authStore } from './auth'
import { 
    itemTransformer,
    fetchItemContentsRecursive,
    propertiesTransformer, 
    propertyDefinitionsTransformer 
} from './helper'

export const itemsStore = proxy({
    loading: false,
    doneSync: false,
    items: [],
    properties:  [],
    propertyDefinitions: [],
})

export const sync = async () => {
    itemsStore.loading = true
    const response = await fetchItemContentsRecursive(authStore.session);
    const items = response.results.map(itemTransformer)
    const properties = response.results.map(propertiesTransformer).reduce((acc, props)=>([...acc, ...props]))
    const propertyDefinitions = Object.values(response.included.propertyDefinition).map(propertyDefinitionsTransformer)
    itemsStore.loading = false
    itemsStore.doneSync = true
    Object.assign(itemsStore, {items, properties, propertyDefinitions})
}