import Apis from '~/api';

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
