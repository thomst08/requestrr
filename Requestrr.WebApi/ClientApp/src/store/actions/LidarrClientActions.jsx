export const LIDARR_SET_CLIENT = "musicClients:set_lidarr_client";
export const LIDARR_LOAD_PATHS = "musicClients:load_lidarr_paths";
export const LIDARR_SET_PATHS= "musicClients:set_lidarr_paths";
export const LIDARR_LOAD_PROFILES = "musicClients:load_lidarr_profiles";
export const LIDARR_SET_PROFILES = "musicClients:set_lidarr_profiles";
export const LIDARR_LOAD_METADATA_PROFILES = "musicClients:load_lidarr_metadata_profiles";
export const LIDARR_SET_METADATA_PROFILES = "musicClients:set_lidarr_metadata_profiles";
export const LIDARR_LOAD_TAGS = "musicClients:load_lidarr_tags";
export const LIDARR_SET_TAGS = "musicClients:set_lidarr_tags";


export function setLidarrClient(settings) {
    return {
        type: LIDARR_SET_CLIENT,
        payload: settings
    };
};


export function isLoadingLidarrPaths(isLoading) {
    return {
        type: LIDARR_LOAD_PATHS,
        payload: isLoading
    };
};


export function setLidarrPaths(lidarrPaths) {
    return {
        type: LIDARR_SET_PATHS,
        payload: lidarrPaths
    };
};


export function isLoadingLidarrProfiles(isLoading) {
    return {
        type: LIDARR_LOAD_PROFILES,
        payload: isLoading
    };
};


export function setLidarrProfiles(lidarrProfiles) {
    return {
        type: LIDARR_SET_PROFILES,
        payload: lidarrProfiles
    };
};


export function isLoadingLidarrMetadataProfiles(isLoading) {
    return {
        type: LIDARR_LOAD_METADATA_PROFILES,
        payload: isLoading
    };
};


export function setLidarrMetadataProfiles(lidarrMetadataProfiles) {
    return {
        type: LIDARR_SET_METADATA_PROFILES,
        payload: lidarrMetadataProfiles
    };
};


export function isLoadingLidarrTags(isLoading) {
    return {
        type: LIDARR_LOAD_TAGS,
        payload: isLoading
    };
};


export function setLidarrTags(lidarrTags) {
    return {
        type: LIDARR_SET_TAGS,
        payload: lidarrTags
    };
};


export function setLidarrConnectionSettings(connectionSettings) {
    return (dispatch, getState) => {
        const state = getState();

        let lidarr = {
            ...state.music.lidarr,
            hostname: connectionSettings.hostname,
            baseUrl: connectionSettings.baseUrl,
            port: connectionSettings.port,
            apiKey: connectionSettings.apiKey,
            useSSL: connectionSettings.useSSL,
            version: connectionSettings.version
        };

        dispatch(setLidarrClient({
            lidarr: lidarr
        }));

        return new Promise((resolve, reject) => {
            return { ok: false };
        });
    }
}


export function addLidarrCategory(category) {
    return (dispatch, getState) => {
        const state = getState();

        let categories = [...state.music.lidarr.categories];
        categories.push(category);

        let lidarr = {
            ...state.music.lidarr,
            categories: categories
        };

        dispatch(setLidarrClient({
            lidarr: lidarr
        }));

        return new Promise((resolve, reject) => {
            return { ok: false };
        });
    }
};


export function removeLidarrCategory(categoryId) {
    return (dispatch, getState) => {
        const state = getState();

        let categories = [...state.music.lidarr.categories];
        categories = categories.filter(x => x.id !== categoryId);

        let lidarr = {
            ...state.music.lidarr,
            categories: categories
        };

        dispatch(setLidarrClient({
            lidarr: lidarr
        }));

        return new Promise((resolve, reject) => {
            return { ok: false };
        });
    }
};


export function setLidarrCategory(categoryId, field, data) {
    return (dispatch, getState) => {
        const state = getState();

        let categories = [...state.music.lidarr.categories];
        
        for (let index = 0; index < categories.length; index++) {
            if (categories[index].id === categoryId) {
                let category = { ...categories[index] };

                if (field === "name") {
                    category.name = data;
                } else if (field === "profileId") {
                    category.profileId = data;
                } else if (field === "metadataProfileId") {
                    category.metadataProfileId = data;
                } else if (field === "rootFolder") {
                    category.rootFolder = data;
                } else if (field === "tags") {
                    category.tags = state.music.lidarr.tags.map(x => x.id).filter(x => data.includes(x));
                } else if (field === "primaryTypes") {
                    category.primaryTypes = ["Album", "Broadcast", "EP", "Other", "Single"].filter(x => data.includes(x));
                } else if (field === "secondaryTypes") {
                    category.secondaryTypes = ["Studio", "Spokenword", "Soundtrack", "Remix", "Mixtape/Street", "Live", "Interview", "DJ-mix", "Demo", "Compilation", "Audio drama"].filter(x => data.includes(x));
                } else if (field === "releaseStatuses") {
                    category.releaseStatuses = ["Pseudo-Release", "Promotion", "Official", "Bootleg"].filter(x => data.includes(x));
                }

                categories[index] = category;
            }
        }

        let lidarr = {
            ...state.music.lidarr,
            categories: categories
        };
        console.log(lidarr)

        dispatch(setLidarrClient({
            lidarr: lidarr
        }));

        return new Promise((resolve, reject) => {
            return { ok: false };
        });
    }
};


export function setLidarrCategories(categoies) {
    return (dispatch, getState) => {
        const state = getState();

        let lidarr = {
            ...state.music.lidarr,
            categoies: [...categoies]
        };

        dispatch(setLidarrClient({
            lidarr: lidarr
        }));

        return new Promise((resolve, reject) => {
            return { ok: false };
        });
    }
};


export function loadLidarrRootPaths(forceReload) {
    return (dispatch, getState) => {
        const state = getState();

        var lidarr = state.music.lidarr;

        if ((!lidarr.hasLoadedPaths && !lidarr.isLoadingPaths) || forceReload) {
            dispatch(isLoadingLidarrPaths(true));

            return fetch("../api/music/lidarr/rootpath", {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${state.user.token}`
                },
                body: JSON.stringify({
                    "Hostname": lidarr.hostname,
                    'BaseUrl': lidarr.baseUrl,
                    "Port": Number(lidarr.port),
                    "ApiKey": lidarr.apiKey,
                    "UseSSL": lidarr.useSSL,
                    "Version": lidarr.version,
                })
            })
                .then(data => {
                    if (data.status !== 200) {
                        throw new Error("Bad request.");
                    }

                    return data;
                })
                .then(data => data.json())
                .then(data => {
                    dispatch(setLidarrPaths(data));

                    return {
                        ok: true,
                        paths: data
                    }
                })
                .catch(err => {
                    dispatch(setLidarrPaths([]));
                    return { ok: false };
                })
        }
        else {
            return new Promise((resolve, reject) => {
                return { ok: false };
            });
        }
    };
};


export function loadLidarrProfiles(forceReload) {
    return (dispatch, getState) => {
        const state = getState();
        var lidarr = state.music.lidarr;

        if ((!lidarr.hasLoadedProfiles && !lidarr.isLoadingProfiles) || forceReload) {
            dispatch(isLoadingLidarrProfiles(true));

            return fetch("../api/music/lidarr/profile", {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${state.user.token}`
                },
                body: JSON.stringify({
                    "Hostname": lidarr.hostname,
                    'BaseUrl': lidarr.baseUrl,
                    "Port": Number(lidarr.port),
                    "ApiKey": lidarr.apiKey,
                    "UseSSL": lidarr.useSSL,
                    "Version": lidarr.version,
                })
            })
                .then(data => {
                    if (data.status !== 200) {
                        throw new Error("Bad request.");
                    }

                    return data;
                })
                .then(data => data.json())
                .then(data => {
                    dispatch(setLidarrProfiles(data));

                    return {
                        ok: true,
                        profiles: data
                    }
                })
                .catch(err => {
                    dispatch(setLidarrProfiles([]));
                    return { ok: false };
                })
        } else {
            return new Promise((resolve, reject) => {
                return { ok: false };
            });
        }
    };
};


export function loadLidarrMetadataProfiles(forceReload) {
    return (dispatch, getState) => {
        const state = getState();
        var lidarr = state.music.lidarr;

        if ((!lidarr.hasLoadedMetadataProfiles && !lidarr.isLoadingMetadataProfiles) || forceReload) {
            dispatch(isLoadingLidarrMetadataProfiles(true));

            return fetch("../api/music/lidarr/metadataprofile", {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${state.user.token}`
                },
                body: JSON.stringify({
                    "Hostname": lidarr.hostname,
                    'BaseUrl': lidarr.baseUrl,
                    "Port": Number(lidarr.port),
                    "ApiKey": lidarr.apiKey,
                    "UseSSL": lidarr.useSSL,
                    "Version": lidarr.version,
                })
            })
                .then(data => {
                    if (data.status !== 200) {
                        throw new Error("Bad request.");
                    }

                    return data;
                })
                .then(data => data.json())
                .then(data => {
                    dispatch(setLidarrMetadataProfiles(data));

                    return {
                        ok: true,
                        metadataProfiles: data
                    }
                })
                .catch(err => {
                    dispatch(setLidarrMetadataProfiles([]));
                    return { ok: false };
                })
        } else {
            return new Promise((resolve, reject) => {
                return { ok: false };
            });
        }
    };
};


export function loadLidarrTags(forceReload) {
    return (dispatch, getState) => {
        const state = getState();

        var lidarr = state.music.lidarr;

        if ((!lidarr.hasLoadedTags && !lidarr.isLoadingTags) || forceReload) {
            dispatch(isLoadingLidarrTags(true));

            return fetch("../api/music/lidarr/tag", {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${state.user.token}`
                },
                body: JSON.stringify({
                    "Hostname": lidarr.hostname,
                    'BaseUrl': lidarr.baseUrl,
                    "Port": Number(lidarr.port),
                    "ApiKey": lidarr.apiKey,
                    "UseSSL": lidarr.useSSL,
                    "Version": lidarr.version,
                })
            })
                .then(data => {
                    if (data.status !== 200) {
                        throw new Error("Bad request.");
                    }

                    return data;
                })
                .then(data => data.json())
                .then(data => {
                    dispatch(setLidarrTags({ ok: true, data: data }));

                    return {
                        ok: true,
                        tags: data
                    }
                })
                .catch(err => {
                    dispatch(setLidarrTags({ ok: false, data: [] }));
                    return { ok: false };
                })
        }
        else {
            return new Promise((resolve, reject) => {
                return { ok: false };
            });
        }
    };
};


export function testLidarrSettings(settings) {
    return (dispatch, getState) => {
        const state = getState();

        return fetch("../api/music/lidarr/test", {
            method: "POST",
            headers: {
                "Accept": "application/json",
                "Content-Type": "application/json",
                "Authorization": `Bearer ${state.user.token}`
            },
            body: JSON.stringify({
                "Hostname": settings.hostname,
                "BaseUrl": settings.baseUrl,
                "Port": Number(settings.port),
                "ApiKey": settings.apiKey,
                "UseSSL": settings.useSSL,
                "Version": settings.version
            })
        })
            .then(data => data.json())
            .then(data => {
                dispatch(loadLidarrProfiles(true));
                dispatch(loadLidarrMetadataProfiles(true));
                dispatch(loadLidarrRootPaths(true));
                dispatch(loadLidarrTags(true));

                if (data.ok)
                    return { ok: true };
                return { ok: false, error: data };
            });
    }
}


export function saveLidarrClient(saveModel) {
    return (dispatch, getState) => {
        const state = getState();

        return fetch("../api/music/lidarr", {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${state.user.token}`
            },
            body: JSON.stringify({
                'Hostname': saveModel.lidarr.hostname,
                'BaseUrl': saveModel.lidarr.baseUrl,
                'Port': Number(saveModel.lidarr.port),
                'ApiKey': saveModel.lidarr.apiKey,
                'UseSSL': saveModel.lidarr.useSSL,
                'Categories': state.music.lidarr.categories,
                "Version": saveModel.lidarr.version,
                'SearchNewRequests': saveModel.lidarr.searchNewRequests,
                'MonitorNewRequests': saveModel.lidarr.monitorNewRequests,
                'Restrictions': saveModel.restrictions
            })
        })
            .then(data => data.json())
            .then(data => {
                if (data.ok) {
                    let newLidarr = {
                        ...state.music.lidarr,
                        hostname: saveModel.lidarr.hostname,
                        baseUrl: saveModel.lidarr.baseUrl,
                        port: saveModel.lidarr.port,
                        apiKey: saveModel.lidarr.apiKey,
                        useSSL: saveModel.lidarr.useSSL,
                        categories: state.music.lidarr.categories,
                        searchNewRequests: saveModel.lidarr.searchNewRequests,
                        monitorNewRequests: saveModel.lidarr.monitorNewRequests,
                        version: saveModel.lidarr.version
                    };

                    dispatch(setLidarrClient({
                        lidarr: newLidarr
                    }));
                    return { ok: true };
                }

                return { ok: false, error: data };
            });
    }
}
