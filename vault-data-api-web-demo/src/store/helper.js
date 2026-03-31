import { invalidateCache } from 'alova'
import Apis from '~/api'
import * as aq from 'arquero';

const buildVaultApiError = (payload, fallbackMessage) => {
    const parts = []
    if(payload?.title){
        parts.push(String(payload.title))
    }
    if(payload?.detail){
        parts.push(String(payload.detail))
    }
    if(payload?.errorCode){
        parts.push(`errorCode=${payload.errorCode}`)
    }
    const message = parts.join(' | ') || fallbackMessage
    const error = new Error(message)
    error.vaultPayload = payload
    return error
}

export const initialResponse = () => ({
    included: {
        folder: {},
        propertyDefinition: {},
    },
    results: [],
})


export const mergeResonse = (res1 = initialResponse(), res2 = initialResponse()) => {
    if(res1.included?.folder && res2.included?.folder){
        Object.assign(res1.included.folder, res2.included.folder)
    }
    if(res1.included?.propertyDefinition, res2.included?.propertyDefinition){
        Object.assign(res1.included.propertyDefinition, res2.included.propertyDefinition)
    }
    res1.results = res1.results.concat(res2.results)
}
export const folderTransformer1 = (data = []) => {
    const foldersData = data.reduce((acc, o)=>{
        acc['name'].push(o['name']);
        acc['id'].push(o['id']);
        acc['parentId'].push(o['parentFolderId']);
        acc['fullName'].push(o['fullName']);
        acc['category'].push(o['category']);
        acc['categoryColor'].push(o['categoryColor']);
        acc['stateColor'].push(o['stateColor']);
        acc['subfolderCount'].push(o['subfolderCount']);
        acc['isLibrary'].push(o['isLibrary']);
        acc['isCloaked'].push(o['isCloaked']);
        acc['isReadOnly'].push(o['isReadOnly']);
        acc['createDate'].push(o['createDate']);
        acc['createUserName'].push(o['createUserName']);
        // acc['properties'].push(o['properties']);
        acc['entityType'].push(o['entityType']);
        acc['url'].push(o['url']);
        return acc
    }, {
        name: [],
        id: [],
        parentId: [],
        fullName: [],
        category: [],
        categoryColor: [],
        stateColor: [],
        subfolderCount: [],
        isLibrary: [],
        isCloaked: [],
        isReadOnly: [],
        createDate: [],
        createUserName: [],
        entityType: [],
        url: [],
    })
    return aq.table(foldersData)
}

export const folderTransformer = ({
    name,
    id,
    parentFolderId,
    fullName,
    category,
    categoryColor,
    stateColor,
    subfolderCount,
    isLibrary,
    isCloaked,
    isReadOnly,
    createDate,
    createUserName,
    entityType,
    url,
}) => {
    return {
        name,
        id,
        parentId: parentFolderId,
        fullName,
        category,
        categoryColor,
        stateColor,
        subfolderCount,
        isLibrary,
        isCloaked,
        isReadOnly,
        createDate,
        createUserName,
        entityType,
        url,
    }
}

export const fileTransformer1 = (data = []) => {
    const filesData = data.reduce((acc, o)=>{
        acc['name'].push(o['name']);
        acc['id'].push(o['id']);
        acc['state'].push(o['state']);
        acc['stateColor'].push(o['stateColor']);
        acc['revision'].push(o['revision']);
        acc['category'].push(o['category']);
        acc['categoryColor'].push(o['categoryColor']);
        acc['classification'].push(o['classification']);
        acc['lastModifiedDate'].push(o['lastModifiedDate']);
        acc['isCheckedOut'].push(o['isCheckedOut']);
        acc['hasVisualizationAttachment'].push(o['hasVisualizationAttachment']);
        acc['visualizationAttachmentStatus'].push(o['visualizationAttachmentStatus']);
        acc['size'].push(o['size']);
        acc['isCloaked'].push(o['isCloaked']);
        acc['checkinDate'].push(o['checkinDate']);
        acc['checkoutDate'].push(o['checkoutDate']);
        acc['isHidden'].push(o['isHidden']);
        acc['isReadOnly'].push(o['isReadOnly']);
        acc['parentId'].push(o['parentFolderId']);
        acc['masterId'].push(o['file'].id);
        acc['lifecycleDefinitionUrl'].push(
            o['lifecycleState']?.lifecycleDefinition?.url || o['lifecycleDefinition']?.url || ''
        );
        acc['lifecycleDefinitionName'].push(
            o['lifecycleState']?.lifecycleDefinition?.displayName ||
            o['lifecycleState']?.lifecycleDefinition?.name ||
            o['lifecycleDefinition']?.displayName ||
            o['lifecycleDefinition']?.name ||
            ''
        );
        acc['lifecycleStateUrl'].push(o['lifecycleState']?.url || '');
        acc['lifecycleStateName'].push(
            o['lifecycleState']?.displayName || o['lifecycleState']?.name || o['state'] || ''
        );
        acc['createDate'].push(o['createDate']);
        acc['createUserName'].push(o['createUserName']);
        // acc['properties'].push(o['properties']);
        acc['version'].push(o['version']);
        acc['entityType'].push(o['entityType']);
        acc['url'].push(o['url']);
        return acc
    }, {
        name: [],
        id: [],
        state: [],
        stateColor: [],
        revision: [],
        category: [],
        categoryColor: [],
        classification: [],
        lastModifiedDate: [],
        isCheckedOut: [],
        hasVisualizationAttachment: [],
        visualizationAttachmentStatus: [],
        size: [],
        isCloaked: [],
        checkinDate: [],
        checkoutDate: [],
        isHidden: [],
        isReadOnly: [],
        parentId: [],
        masterId: [],
        lifecycleDefinitionUrl: [],
        lifecycleDefinitionName: [],
        lifecycleStateUrl: [],
        lifecycleStateName: [],
        createDate: [],
        createUserName: [],
        version: [],
        entityType: [],
        url: [],
    })
    return aq.table(filesData)
}

export const fileTransformer = ({
    name,
    id,
    state,
    stateColor,
    revision,
    category,
    categoryColor,
    classification,
    lastModifiedDate,
    isCheckedOut,
    hasVisualizationAttachment,
    visualizationAttachmentStatus,
    size,
    isCloaked,
    checkinDate,
    checkoutDate,
    isHidden,
    isReadOnly,
    parentFolderId,
    file,
    lifecycleState,
    lifecycleDefinition,
    createDate,
    createUserName,
    version,
    entityType,
    url,
}) => {
    return {
        name,
        id,
        state,
        stateColor,
        revision,
        category,
        categoryColor,
        classification,
        lastModifiedDate,
        isCheckedOut,
        hasVisualizationAttachment,
        visualizationAttachmentStatus,
        size,
        isCloaked,
        checkinDate,
        checkoutDate,
        isHidden,
        isReadOnly,
        parentId: parentFolderId,
        masterId: file.id,
        lifecycleDefinitionUrl:
            lifecycleState?.lifecycleDefinition?.url || lifecycleDefinition?.url || '',
        lifecycleDefinitionName:
            lifecycleState?.lifecycleDefinition?.displayName ||
            lifecycleState?.lifecycleDefinition?.name ||
            lifecycleDefinition?.displayName ||
            lifecycleDefinition?.name ||
            '',
        lifecycleStateUrl: lifecycleState?.url || '',
        lifecycleStateName: lifecycleState?.displayName || lifecycleState?.name || state || '',
        createDate,
        createUserName,
        version,
        entityType,
        url,
    }
}

export const linkTransformer1 = (data = []) => {
    const linksData = data.reduce((acc, o)=>{
        acc['name'].push(o['name']);
        acc['id'].push(o['id']);
        acc['fromEntityId'].push(o['fromEntity'].id);
        acc['toEntityId'].push(o['toEntity'].id);
        acc['createDate'].push(o['createDate']);
        acc['createUserName'].push(o['createUserName']);
        acc['entityType'].push(`${o['toEntity']['entityType']}${o['entityType']}`);
        acc['url'].push(o['url']);
        return acc
    }, {
        name: [],
        id: [],
        fromEntityId: [],
        toEntityId: [],
        createDate: [],
        createUserName: [],
        entityType: [],
        url: [],
    })
    return aq.table(linksData)
}

export const linkTransformer = ({
    name,
    id,
    fromEntity,
    toEntity,
    createDate,
    entityType,
    createUserName,
    url
}) => {
    return {
        name,
        id,
        fromEntityId: fromEntity.id,
        toEntityId: toEntity.id,
        createDate,
        createUserName,
        entityType: `${toEntity.entityType}${entityType}`,
        url,
    }
}

export const itemTransformer = ({
    name,
    id,
    revision,
    lastModifiedUserName,
    lastModifiedDate,
    number,
    title,
    description,
    comment,
    state,
    stateColor,
    category,
    categoryColor,
    isReadOnly,
    isCloaked,
    item,
    version,
    entityType,
    url,
}) => {
    return {
        name,
        id,
        revision,
        lastModifiedUserName,
        lastModifiedDate,
        number,
        title,
        description,
        comment,
        state,
        stateColor,
        category,
        categoryColor,
        isReadOnly,
        isCloaked,
        masterId: item.id,
        version,
        entityType,
        url,
    }
}

export const changeOrderTransformer = ({
    name,
        id,
        assignees,
        number,
        title,
        description,
        approveDeadline,
        lastModifiedDate,
        lastModifiedUserId,
        closeDate,
        lastTouchedDate,
        state,
        stateColor,
        numberOfAttachments,
        isReadOnly,
        entityType,
        url,
}) => {
    return {
        name,
        id,
        assignees: (assignees || []).map(a => a.id).join(','),
        number,
        title,
        description,
        approveDeadline,
        lastModifiedDate,
        lastModifiedUserId,
        closeDate,
        lastTouchedDate,
        state,
        stateColor,
        numberOfAttachments,
        isReadOnly,
        entityType,
        url,
    }
}

export const propertyDefinitionsTransformer = ({
    id,
    active,
    dataType,
    displayName,
    isSystem,
    systemName,
    url,
}) => {
    return {
        id,
        active,
        dataType,
        displayName,
        isSystem,
        systemName,
        url,
    }
}

export const propertiesTransformer = ({
    id,
    entityType,
    properties,
}) => {
    if(Array.isArray(properties)){
        return properties.map( p => ({
            propertyDefinitionId: p.propertyDefinitionId,
            entityId: id,
            entityType,
            value: p.value,
        }))
    }
    return []
}

export const fetchFolderContentsRecursive = async (session, folderId='root') => {
    let cursor = ''
    const response = initialResponse()
    do{
        const res = await Apis.vault.getFolderContents({
            pathParams:{
                vaultId: session.vaultId,
                id: folderId,
            },
            params:{
                'option[searchSubFolders]': true,
                'option[includeItemEcoLinks]': true,
                'option[releasedFilesOnly]': false,
                'option[latestOnly]': true,
                'option[extendedModels]': true,
                'option[propDefIds]': 'all',
                // 'filter[Hidden]': 'all', 
                limit: 1000,
                ...(cursor?{cursorState: cursor} : {})
            },
            headers:{
                'authorization': session.token,
            },
        })
        invalidateCache()
        mergeResonse(response, res)
        cursor =''
        if(res.pagination?.nextUrl){
            const url = new URL(res.pagination?.nextUrl, location.origin)
            cursor = encodeURIComponent(url.searchParams.get('cursorState'))
        }

    }while(cursor)

    return response;
}

export const fetchCoContentsRecursive = async (session) => {
    let cursor = ''
    const response = initialResponse()
    do{
        const res = await Apis.vault.getChangeOrders({
            pathParams:{
                vaultId: session.vaultId,
            },
            params:{
                'option[fullModels]': true,
                limit: 1000,
                ...(cursor?{cursorState: cursor} : {})
            },
            headers:{
                'authorization': session.token,
            },
        })
        invalidateCache()
        mergeResonse(response, res)
        cursor =''
        if(res.pagination?.nextUrl){
            const url = new URL(res.pagination?.nextUrl, location.origin)
            cursor = encodeURIComponent(url.searchParams.get('cursorState'))
        }

    }while(cursor)

    return response;
}

export const fetchItemContentsRecursive = async (session) => {
    let cursor = ''
    const response = initialResponse()
    do{
        const res = await Apis.vault.getItemVersions({
            pathParams:{
                vaultId: session.vaultId,
            },
            params:{
                'option[fullModels]': true,
                limit: 1000,
                ...(cursor?{cursorState: cursor} : {})
            },
            headers:{
                'authorization': session.token,
            },
        })
        invalidateCache()
        mergeResonse(response, res)
        cursor =''
        if(res.pagination?.nextUrl){
            const url = new URL(res.pagination?.nextUrl, location.origin)
            cursor = encodeURIComponent(url.searchParams.get('cursorState'))
        }

    }while(cursor)

    return response;
}

export const fetchCurrentUserInfo = async (session) => {
    try {
        const user = await Apis.global.getUserById({
            pathParams:{
                id: session.userId
            },
            headers:{
                'authorization': session.token,
            },
        }).then(response => {
            if(response.statusCode && response.statusCode >= 400){
                return Promise.reject()
            }
            return response
        })
        return user
    }catch(e){
        return null
    }
}

export const fetchLifecycleDefinitions = async (session) => {
    if(!session?.vaultId || !session?.token){
        throw new Error('No active Vault session.')
    }
    let cursor = ''
    const results = []
    do{
        const response = await Apis.vault.getLifecycleDefinitions({
            pathParams: {
                vaultId: session.vaultId,
            },
            params: {
                'option[extendedModels]': true,
                limit: 1000,
                ...(cursor ? { cursorState: cursor } : {}),
            },
            headers: {
                'authorization': session.token,
            },
        })
        if(response?.statusCode && response.statusCode >= 400){
            throw buildVaultApiError(
                response,
                `Get lifecycle definitions failed (${response.statusCode})`
            )
        }
        results.push(...(Array.isArray(response?.results) ? response.results : []))
        cursor = ''
        if(response?.pagination?.nextUrl){
            const nextUrl = new URL(response.pagination.nextUrl, location.origin)
            const nextCursor = nextUrl.searchParams.get('cursorState')
            if(nextCursor){
                cursor = nextCursor
            }
        }
    }while(cursor)
    return results
}

export const updateFileLifecycleStates = async (session, payload = {}) => {
    if(!session?.vaultId || !session?.token){
        throw new Error('No active Vault session.')
    }
    const sanitizedRequests = (payload.updateLifecycleStateRequests || []).map((item) => {
        const request = {
            entityUrl: String(item?.entityUrl || '').trim(),
            lifecycleStateUrl: String(item?.lifecycleStateUrl || '').trim(),
        }
        if(item?.lifecycleDefinitionUrl){
            request.lifecycleDefinitionUrl = String(item.lifecycleDefinitionUrl).trim()
        }
        if(item?.revision){
            request.revision = String(item.revision)
        }
        if(!request.entityUrl || !request.lifecycleStateUrl){
            throw new Error('entityUrl and lifecycleStateUrl are required and must be full v2 URLs.')
        }
        return request
    })
    const requestBody = {
        updateLifecycleStateRequests: sanitizedRequests,
        ...(typeof payload.comment === 'string' && payload.comment.trim() ? { comment: payload.comment } : {}),
        ...(typeof payload.changeLifecycleDefinition === 'boolean' ? { changeLifecycleDefinition: payload.changeLifecycleDefinition } : {}),
        ...(typeof payload.specifyRevision === 'boolean' ? { specifyRevision: payload.specifyRevision } : {}),
    }
    if(!Array.isArray(requestBody.updateLifecycleStateRequests) || requestBody.updateLifecycleStateRequests.length === 0){
        throw new Error('updateLifecycleStateRequests must contain at least one request.')
    }
    const responsePayload = await Apis.vault.updateFileLifecycleStatesByMasterIds({
        pathParams: {
            vaultId: session.vaultId,
        },
        headers: {
            'content-type': 'application/json',
            'authorization': session.token,
        },
        data: requestBody,
    })
    if(responsePayload?.statusCode && responsePayload.statusCode >= 400){
        throw buildVaultApiError(
            responsePayload,
            `Update lifecycle states failed (${responsePayload.statusCode})`
        )
    }
    return responsePayload
}

export const loginUser = async ({ userName, password, vault}) => {
    try {
        const session = await Apis.global.createSession({
            data:{
                input: {
                    userName,
                    password,
                    vault,
                    appCode: 'TC'
                }
            }
        }).then(response => {
            if(response.statusCode && response.statusCode >= 400){
                return Promise.reject()
            }
            return response
        })
        return session
    }catch(e){
        return null
    }
}
  