import { GET_SETTINGS, SET_DISABLED_CLIENT } from "../actions/MusicClientsActions";


export default function MusicClientsReducer(state = {}, action) {
    if (action.type === GET_SETTINGS) {
        return {
            ...state,
            client: action.payload.client,
            restrictions: action.payload.restrictions || "None",
            lidarr: {
                hostname: action.payload.lidarr.hostname,
                baseUrl: action.payload.lidarr.baseUrl,
                port: action.payload.lidarr.port,
                apiKey: action.payload.lidarr.apiKey,
                useSSL: action.payload.lidarr.useSSL,
                categories: action.payload.lidarr.categories.map(x => ({
                    ...x,
                    primaryTypes: x.primaryTypes && x.primaryTypes.length > 0
                        ? x.primaryTypes
                        : (x.releaseTypes && x.releaseTypes.length > 0 ? x.releaseTypes : ["Album", "EP", "Single"]),
                    secondaryTypes: x.secondaryTypes && x.secondaryTypes.length > 0
                        ? x.secondaryTypes
                        : ["Studio", "Soundtrack", "Remix", "DJ-mix", "Compilation"],
                    releaseStatuses: x.releaseStatuses && x.releaseStatuses.length > 0
                        ? x.releaseStatuses
                        : ["Official"]
                })),
                searchNewRequests: action.payload.lidarr.searchNewRequests,
                monitorNewRequests: action.payload.lidarr.monitorNewRequests,
                version: action.payload.lidarr.version,
                isLoadingPaths: false,
                hasLoadedPaths: false,
                arePathsValid: false,
                paths: [],
                isLoadingProfiles: false,
                hasLoadedProfiles: false,
                areProfilesValid: false,
                profiles: [],
                isLoadingMetadataProfiles: false,
                hasLoadedMetadataProfiles: false,
                areMetadataProfilesValid: false,
                metadataProfiles: [],
                isLoadingTags: false,
                hasLoadedTags: false,
                areTagsValid: false,
                tags: []
            },
            otherCategories: action.payload.otherCategories
        };
    } else if (action.type === SET_DISABLED_CLIENT) {
        return {
            ...state,
            client: "Disabled"
        };
    }

    return { ...state };
}
