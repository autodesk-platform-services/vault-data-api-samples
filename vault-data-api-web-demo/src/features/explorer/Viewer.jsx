import { useState, useRef } from 'react';
import { useRetriableRequest } from 'alova/client'
import { alovaInstance } from '~/api'
import { connectionStore, toVaultProxyUrl } from '~/features/connection/connection'

const frameStyles = {
    width: 'calc(100% + 48px)',
    display: 'block',
    border: 'none',
    height: '100%',
    margin: '0 -24px',
}

export default function Viewer({urn, session}){
    
    const frame = useRef(null)
    const [retryCount, setRetryCount] = useState(0)
    const onFrameLoad = () => {
        if(frame.current && urn){
            const w = frame.current.contentWindow
            const msg = {
                documentId: toVaultProxyUrl(urn),
                proxyPrefix: `/__vault/${connectionStore.connection.connectionId}`,
                serverAddress: connectionStore.connection.serverAddress,
                accessToken: session.token.split(' ')[1],
                type: 'viewer',
            }
            w.postMessage(msg, window.location.origin)
        }
    }

    const {
        loading,
        // data,
        error,
        // onError,
        onRetry,
        // onFail,
        // onSuccess,
        // onComplete
      } = useRetriableRequest(
        alovaInstance.Get(toVaultProxyUrl(urn).replace(alovaInstance.options.baseURL,''), {
            headers:{
                'authorization': session.token,
            }
        }),{
        retry: 10,
        backoff: {
            delay: 2000,
            multiplier: 1.5
          }
      });
      onRetry((event)=>{
        setRetryCount(event.retryTimes)
      })
    if(loading){
        return <div style={{textAlign: 'center', paddingTop: '200px'}}>
                Loading...
                <br/>
                {
                    retryCount > 0 ? `Retry ${retryCount}/10` : ''
                }
        </div>
    }
    if(error) {
        return error.message
    }
    return <iframe ref={frame} src='/viewer.html' style={frameStyles} onLoad={onFrameLoad}/>
}
