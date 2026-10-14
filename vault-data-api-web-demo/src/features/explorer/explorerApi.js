import Apis from '~/api';
import { initialResponse, mergeResonse } from '~/api/response';
import { connectionStore } from '~/features/connection/connection';

export const fetchFolderContents = async (session, folderId='root', onProgress = () => {}) => {
    let cursor = ''
    const response = initialResponse()
    do{
        if (session.connectionId && session.connectionId !== connectionStore.connection?.connectionId) {
            throw new Error('The server connection changed.')
        }
        const res = await Apis.vault.getFolderContents({
            cacheFor: 0,
            pathParams:{
                vaultId: session.vaultId,
                id: folderId,
            },
            params:{
                'option[searchSubFolders]': false,
                'option[includeItemEcoLinks]': true,
                'option[releasedFilesOnly]': false,
                'option[latestOnly]': true,
                'option[extendedModels]': true,
                // 'filter[Hidden]': 'all', 
                limit: 1000,
                ...(cursor?{cursorState: cursor} : {})
            },
            headers:{
                'authorization': session.token,
            },
        })
        mergeResonse(response, res)
        onProgress(response.results.length)
        cursor =''
        if(res.pagination?.nextUrl){
            const url = new URL(res.pagination?.nextUrl, location.origin)
            cursor = encodeURIComponent(url.searchParams.get('cursorState'))
        }

    }while(cursor)

    return response;
}
