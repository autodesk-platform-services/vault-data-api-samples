import { useEffect, useRef, useState } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { useSnapshot } from 'valtio'
import { filesStore, getFileAttachment } from '~/store/filesStore'
import { authStore } from '~/store/auth'
import Viewer from './-explorer/viewer'
import * as aq from 'arquero';

export const Route = createFileRoute('/_workspace/explorer/files/$id')({
  component: Component
})

const isViz = (name) => {
    return (name.endsWith('.dwf') || name.endsWith('.dwfx'))
}
function Component() {
    
    const { id } = Route.useParams()
    
    const { doneSync, files } = useSnapshot(filesStore)
    const { session } = useSnapshot(authStore)
    const [f1, setF1] = useState(null)
    const [urn, setUrn] = useState(null)
    const [err, setErr] = useState(false)
    
    useEffect(()=>{
        if(doneSync){
          const [f] = files.params({fileId: id})
          .filter((d) => d.id == fileId)
          .objects()
          setF1(f)
        }
    }, [doneSync, id, files])

    useEffect(() => {
        if(!f1){
            return
        }
        if(isViz(f1.name)){
            setUrn(`${f1.url}/svf/bubble.json`)
        }else if(f1.hasVisualizationAttachment){
            loadVizFileInfo()
        }else{
            setErr(true)
        }
    }, [f1])

    const  loadVizFileInfo = async () => {
        const f = await getFileAttachment(id)
        setF1(f)
    }

    if(err){
        return <div>
            Unable to view file.
            <br/>
            Generate a visualization file (DWF or DWFx) for the Viewer.
        </div>
    }
    if(urn){
        return <Viewer urn={urn} session={session}/>
    }
}