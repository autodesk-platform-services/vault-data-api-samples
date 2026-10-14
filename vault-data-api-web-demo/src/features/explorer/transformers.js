import * as aq from 'arquero';

export const folderTransformer1 = (data = []) => {
    const foldersData = data.reduce((acc, o)=>{
        acc['name'].push(o['name']);
        acc['id'].push(o['id']);
        acc['parentId'].push(o['parentFolderId']);
        acc['fullName'].push(o['fullName']);
        acc['category'].push(o['category']);
        acc['categoryColor'].push(o['categoryColor']);
        acc['stateColor'].push(o['stateColor']);
        acc['subfolderCount'].push(o['subfolderCount']);
        acc['isLibrary'].push(o['isLibrary']);
        acc['isCloaked'].push(o['isCloaked']);
        acc['isReadOnly'].push(o['isReadOnly']);
        acc['createDate'].push(o['createDate']);
        acc['createUserName'].push(o['createUserName']);
        // acc['properties'].push(o['properties']);
        acc['entityType'].push(o['entityType']);
        acc['url'].push(o['url']);
        return acc
    }, {
        name: [],
        id: [],
        parentId: [],
        fullName: [],
        category: [],
        categoryColor: [],
        stateColor: [],
        subfolderCount: [],
        isLibrary: [],
        isCloaked: [],
        isReadOnly: [],
        createDate: [],
        createUserName: [],
        entityType: [],
        url: [],
    })
    return aq.table(foldersData)
}

export const folderTransformer = ({
    name,
    id,
    parentFolderId,
    fullName,
    category,
    categoryColor,
    stateColor,
    subfolderCount,
    isLibrary,
    isCloaked,
    isReadOnly,
    createDate,
    createUserName,
    entityType,
    url,
}) => {
    return {
        name,
        id,
        parentId: parentFolderId,
        fullName,
        category,
        categoryColor,
        stateColor,
        subfolderCount,
        isLibrary,
        isCloaked,
        isReadOnly,
        createDate,
        createUserName,
        entityType,
        url,
    }
}

export const fileTransformer1 = (data = []) => {
    const filesData = data.reduce((acc, o)=>{
        acc['name'].push(o['name']);
        acc['id'].push(o['id']);
        acc['state'].push(o['state']);
        acc['stateColor'].push(o['stateColor']);
        acc['revision'].push(o['revision']);
        acc['category'].push(o['category']);
        acc['categoryColor'].push(o['categoryColor']);
        acc['classification'].push(o['classification']);
        acc['lastModifiedDate'].push(o['lastModifiedDate']);
        acc['isCheckedOut'].push(o['isCheckedOut']);
        acc['hasVisualizationAttachment'].push(o['hasVisualizationAttachment']);
        acc['visualizationAttachmentStatus'].push(o['visualizationAttachmentStatus']);
        acc['size'].push(o['size']);
        acc['isCloaked'].push(o['isCloaked']);
        acc['checkinDate'].push(o['checkinDate']);
        acc['checkoutDate'].push(o['checkoutDate']);
        acc['isHidden'].push(o['isHidden']);
        acc['isReadOnly'].push(o['isReadOnly']);
        acc['parentId'].push(o['parentFolderId']);
        acc['masterId'].push(o['file'].id);
        acc['lifecycleDefinitionUrl'].push(
            o['lifecycleState']?.lifecycleDefinition?.url || o['lifecycleDefinition']?.url || ''
        );
        acc['lifecycleDefinitionName'].push(
            o['lifecycleState']?.lifecycleDefinition?.displayName ||
            o['lifecycleState']?.lifecycleDefinition?.name ||
            o['lifecycleDefinition']?.displayName ||
            o['lifecycleDefinition']?.name ||
            ''
        );
        acc['lifecycleStateUrl'].push(o['lifecycleState']?.url || '');
        acc['lifecycleStateName'].push(
            o['lifecycleState']?.displayName || o['lifecycleState']?.name || o['state'] || ''
        );
        acc['createDate'].push(o['createDate']);
        acc['createUserName'].push(o['createUserName']);
        // acc['properties'].push(o['properties']);
        acc['version'].push(o['version']);
        acc['entityType'].push(o['entityType']);
        acc['url'].push(o['url']);
        return acc
    }, {
        name: [],
        id: [],
        state: [],
        stateColor: [],
        revision: [],
        category: [],
        categoryColor: [],
        classification: [],
        lastModifiedDate: [],
        isCheckedOut: [],
        hasVisualizationAttachment: [],
        visualizationAttachmentStatus: [],
        size: [],
        isCloaked: [],
        checkinDate: [],
        checkoutDate: [],
        isHidden: [],
        isReadOnly: [],
        parentId: [],
        masterId: [],
        lifecycleDefinitionUrl: [],
        lifecycleDefinitionName: [],
        lifecycleStateUrl: [],
        lifecycleStateName: [],
        createDate: [],
        createUserName: [],
        version: [],
        entityType: [],
        url: [],
    })
    return aq.table(filesData)
}

export const fileTransformer = ({
    name,
    id,
    state,
    stateColor,
    revision,
    category,
    categoryColor,
    classification,
    lastModifiedDate,
    isCheckedOut,
    hasVisualizationAttachment,
    visualizationAttachmentStatus,
    size,
    isCloaked,
    checkinDate,
    checkoutDate,
    isHidden,
    isReadOnly,
    parentFolderId,
    file,
    lifecycleState,
    lifecycleDefinition,
    createDate,
    createUserName,
    version,
    entityType,
    url,
}) => {
    return {
        name,
        id,
        state,
        stateColor,
        revision,
        category,
        categoryColor,
        classification,
        lastModifiedDate,
        isCheckedOut,
        hasVisualizationAttachment,
        visualizationAttachmentStatus,
        size,
        isCloaked,
        checkinDate,
        checkoutDate,
        isHidden,
        isReadOnly,
        parentId: parentFolderId,
        masterId: file.id,
        lifecycleDefinitionUrl:
            lifecycleState?.lifecycleDefinition?.url || lifecycleDefinition?.url || '',
        lifecycleDefinitionName:
            lifecycleState?.lifecycleDefinition?.displayName ||
            lifecycleState?.lifecycleDefinition?.name ||
            lifecycleDefinition?.displayName ||
            lifecycleDefinition?.name ||
            '',
        lifecycleStateUrl: lifecycleState?.url || '',
        lifecycleStateName: lifecycleState?.displayName || lifecycleState?.name || state || '',
        createDate,
        createUserName,
        version,
        entityType,
        url,
    }
}

export const linkTransformer1 = (data = []) => {
    const linksData = data.reduce((acc, o)=>{
        acc['name'].push(o['name']);
        acc['id'].push(o['id']);
        acc['fromEntityId'].push(o['fromEntity'].id);
        acc['toEntityId'].push(o['toEntity'].id);
        acc['createDate'].push(o['createDate']);
        acc['createUserName'].push(o['createUserName']);
        acc['entityType'].push(`${o['toEntity']['entityType']}${o['entityType']}`);
        acc['url'].push(o['url']);
        return acc
    }, {
        name: [],
        id: [],
        fromEntityId: [],
        toEntityId: [],
        createDate: [],
        createUserName: [],
        entityType: [],
        url: [],
    })
    return aq.table(linksData)
}

export const linkTransformer = ({
    name,
    id,
    fromEntity,
    toEntity,
    createDate,
    entityType,
    createUserName,
    url
}) => {
    return {
        name,
        id,
        fromEntityId: fromEntity.id,
        toEntityId: toEntity.id,
        createDate,
        createUserName,
        entityType: `${toEntity.entityType}${entityType}`,
        url,
    }
}
