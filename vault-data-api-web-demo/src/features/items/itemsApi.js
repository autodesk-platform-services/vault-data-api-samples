import { invalidateCache } from 'alova';
import Apis from '~/api';
import { initialResponse, mergeResonse } from '~/api/response';

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
