import { proxy, subscribe } from 'valtio'
import { fetchCurrentUserInfo, loginUser } from './helper'
const sessionKey = 'vault:session'

const readSessionInfo = () => {
    return JSON.parse(localStorage.getItem(sessionKey) || 'null')
}

const saveSessionInfo = (session) => {
    if(typeof session === 'object'){
        return localStorage.setItem(sessionKey, JSON.stringify(session))
    }
    localStorage.removeItem(sessionKey)
}

export const AuthStatus = {
    PENDING: 1,
    YES: 2,
    NO: 3
}

export const authStore = proxy({
    authStatus: AuthStatus.PENDING,
    session: null,
    user: null,
})

export const login = async (data) => {
    const session = await loginUser(data)
    if(session){
        authStore.session = {
            token: `Bearer ${session.accessToken}`,
            userId: session.userInformation.id,
            userName: session.userInformation.systemName,
            vaultId: session.vaultInformation.id,
            vaultName: session.vaultInformation.name,
        }
    }
}

export const logout = async () => {
    authStore.session = null
    authStore.user = null
    authStore.authStatus = AuthStatus.NO
}

subscribe(authStore, async () => {
    if(authStore.session){
        const user =  await fetchCurrentUserInfo(authStore.session)
        if(user){
            authStore.user = user
            authStore.authStatus = AuthStatus.YES
        }else{
            authStore.session = null
            authStore.user = null
            authStore.authStatus = AuthStatus.NO
        }
    }
    saveSessionInfo(authStore.session)
})
// trigger user info fetch if session exist
authStore.session = readSessionInfo()