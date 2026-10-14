

export const propertyDefinitionsTransformer = ({
    id,
    active,
    dataType,
    displayName,
    isSystem,
    systemName,
    url,
}) => {
    return {
        id,
        active,
        dataType,
        displayName,
        isSystem,
        systemName,
        url,
    }
}

export const propertiesTransformer = ({
    id,
    entityType,
    properties,
}) => {
    if(Array.isArray(properties)){
        return properties.map( p => ({
            propertyDefinitionId: p.propertyDefinitionId,
            entityId: id,
            entityType,
            value: p.value,
        }))
    }
    return []
}
