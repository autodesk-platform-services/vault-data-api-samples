import { invalidateCache } from 'alova';
import Apis from '~/api';
import { initialResponse, mergeResonse } from '~/api/response';

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
