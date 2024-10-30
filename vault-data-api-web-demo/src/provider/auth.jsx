import { createContext, useState, useContext, useCallback, useEffect } from 'react'
import { useLocalStorage } from "@uidotdev/usehooks";
import Apis from '~/api'

const AuthContext = createContext(null)
const sessionKey = 'vault:session'

export const AuthProvider = ({ children }) => {
    /*
  const [session, saveSession] = useLocalStorage(sessionKey);
  const [user, setUser] = useState(null)

  const logout = useCallback(async () => {
    // saveSession(null)
  }, [saveSession])

  const login = useCallback(, [saveSession])

  useEffect(() => {
    const fn = async ()=>{
        if(!user && session){
            try {
                const user = await Apis.global.get_user_with_id({
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
                setUser(user)
            }catch(e){
                console.log(e)
                // saveSession(null)
            }
        }
    }
    fn()
  }, [session, saveSession, user])
*/
  return (
    <AuthContext.Provider value={{}}>
      {children}
    </AuthContext.Provider>
  )
}

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
