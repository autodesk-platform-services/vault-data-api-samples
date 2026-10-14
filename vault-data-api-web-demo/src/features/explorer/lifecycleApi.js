import Apis from '~/api';

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
                cursor = encodeURIComponent(nextCursor)
            }
        }
    }while(cursor)
    return results
}

export const updateFileLifecycleStates = async (session, payload = {}) => {
    if(!session?.vaultId || !session?.token){
        throw new Error('No active Vault session.')
    }
    const rawRequests = payload.updateLifecycleStateRequests
    const requestsArray =
        rawRequests == null ? [] :
        Array.isArray(rawRequests) ? rawRequests :
        null
    if(requestsArray === null){
        throw new Error('updateLifecycleStateRequests must be an array.')
    }
    const sanitizedRequests = requestsArray.map((item) => {
        const request = {
            entityUrl: String(item?.entityUrl || '').trim(),
            lifecycleStateUrl: String(item?.lifecycleStateUrl || '').trim(),
        }
        if(item?.revision){
            request.revision = String(item.revision)
        }
        if(!request.entityUrl || !request.lifecycleStateUrl){
            throw new Error('entityUrl and lifecycleStateUrl are required and must be non-empty strings.')
        }
        return request
    })
    const requestBody = {
        updateLifecycleStateRequests: sanitizedRequests,
        ...(typeof payload.comment === 'string' && payload.comment.trim() ? { comment: payload.comment } : {}),
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

export const updateFileLifecycleDefinitions = async (session, payload = {}) => {
    if(!session?.vaultId || !session?.token){
        throw new Error('No active Vault session.')
    }
    const rawRequests = payload.updateLifecycleDefinitionRequests
    const requestsArray =
        rawRequests == null ? [] :
        Array.isArray(rawRequests) ? rawRequests :
        null
    if(requestsArray === null){
        throw new Error('updateLifecycleDefinitionRequests must be an array.')
    }
    const sanitizedRequests = requestsArray.map((item) => {
        const request = {
            entityUrl: String(item?.entityUrl || '').trim(),
            lifecycleDefinitionUrl: String(item?.lifecycleDefinitionUrl || '').trim(),
            lifecycleStateUrl: String(item?.lifecycleStateUrl || '').trim(),
        }
        if(!request.entityUrl || !request.lifecycleDefinitionUrl || !request.lifecycleStateUrl){
            throw new Error(
                'entityUrl, lifecycleDefinitionUrl and lifecycleStateUrl are required and must be non-empty strings.'
            )
        }
        return request
    })
    const requestBody = {
        updateLifecycleDefinitionRequests: sanitizedRequests,
        ...(typeof payload.comment === 'string' && payload.comment.trim() ? { comment: payload.comment } : {}),
    }
    if(
        !Array.isArray(requestBody.updateLifecycleDefinitionRequests) ||
        requestBody.updateLifecycleDefinitionRequests.length === 0
    ){
        throw new Error('updateLifecycleDefinitionRequests must contain at least one request.')
    }
    const responsePayload = await Apis.vault.updateFileLifecycleDefinitions({
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
            `Update lifecycle definitions failed (${responsePayload.statusCode})`
        )
    }
    return responsePayload
}
