import { proxy } from 'valtio'
import { derive } from 'derive-valtio'
import { authStore } from './auth'
import Apis from '~/api'
import * as aq from 'arquero';

import { 
    fetchFolderContentsRecursive, 
    folderTransformer1, 
    fileTransformer1, 
    linkTransformer1, 
    propertiesTransformer, 
    propertyDefinitionsTransformer 
} from './helper'
import _ from 'lodash'

export const filesStore = proxy({
    loading: false,
    doneSync: false,
    files: [],
    folders: [],
    links: [],
    properties:  [],
    propertyDefinitions: [],
})

export const sync = async () => {
    filesStore.loading = true
    const response = await fetchFolderContentsRecursive(authStore.session);
    const collections = response.results.reduce((acc, e)=>{
        acc[e['entityType']].push(e)
        return acc
    }, {
        'Folder': [],
        'Link': [],
        'FileVersion': [],
    })
    const folders = folderTransformer1(collections.Folder)
    const files = fileTransformer1(collections.FileVersion)
    const links = linkTransformer1(collections.Link)
    // const folders = collections.Folder.map(folderTransformer)
    // const files = collections.FileVersion.map(fileTransformer)
    // const links = collections.Link.map(linkTransformer)
    // const properties = response.results.map(propertiesTransformer).reduce((acc, props)=>([...acc, ...props]))
    // const propertyDefinitions = Object.values(response.included.propertyDefinition).map(propertyDefinitionsTransformer)
    filesStore.loading = false
    filesStore.doneSync = true
    // console.log(folders, files, links, properties, propertyDefinitions)
    Object.assign(filesStore, {folders, files, links, /*properties, propertyDefinitions*/})
}

export const getFileAttachment = async (id) => {
    const session = authStore.session;
    return await Apis.vault.getFileVersionVisualizationAttachment({
        pathParams:{
            vaultId: session.vaultId,
            id
        },
        headers:{
            'authorization': session.token,
        },
    })
}

export const translateModel = async (id) => {
    const session = authStore.session;
    return await Apis.vault.getFileVersionLmvRoot({
        pathParams:{
            vaultId: session.vaultId,
            id
        },
        headers:{
            'authorization': session.token,
        },
    })
}
  

export const explorer = derive({
    tree: (get) => {
        const doneSync = get(filesStore).doneSync
        if(!doneSync){
            return null
        }
        const folders = get(filesStore).folders
        const grouped = folders.groupby('parentId').objects({ grouped: true })
        const childrenOf = (pid) => {
            return (grouped.get(pid) || []).map((item) => ({
            key: `${pid}-${item.id}`,
            title: item.name,
            id: item.id,
            children: childrenOf(item.id),
            }));
        }
        return childrenOf('1');
    }
})