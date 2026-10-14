

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
