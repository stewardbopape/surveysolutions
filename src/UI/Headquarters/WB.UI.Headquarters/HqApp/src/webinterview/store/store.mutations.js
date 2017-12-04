import { forEach } from "lodash"
import Vue from "vue"

export default {
    SET_ENTITIES_DETAILS(state, { entities, lastActivityTimestamp }) {
        state.lastActivityTimestamp = lastActivityTimestamp
        forEach(entities, entity => {
            if (entity != null) {
                entity.updatedAt = new Date()
                Vue.set(state.entityDetails, entity.id, entity)
            }
        })
    },
    SET_SECTION_DATA(state, sectionData) {
        state.entities = sectionData
    },
    CLEAR_ENTITIES(state, {ids}) {
        forEach(ids, id => {
             Vue.delete(state.entityDetails, id)
        })
    },
    SET_ANSWER_NOT_SAVED(state, { id, message }) {
        const validity = state.entityDetails[id].validity
        Vue.set(validity, "errorMessage", true)
        validity.messages = [message]
        validity.isValid = false
    },
    CLEAR_ANSWER_VALIDITY(state, { id }) {
        const validity = state.entityDetails[id].validity
        validity.isValid = true
        validity.messages = []
    },
    SET_BREADCRUMPS(state, crumps) {
        Vue.set(state, "breadcrumbs", crumps)
    },
    SET_LANGUAGE_INFO(state, languageInfo) {
        Vue.set(state, "originalLanguageName", languageInfo.originalLanguageName)
        Vue.set(state, "currentLanguage", languageInfo.currentLanguage)
        Vue.set(state, "languages", languageInfo.languages)
    },
    SET_INTERVIEW_INFO(state, interviewInfo) {
        state.questionnaireTitle = interviewInfo.questionnaireTitle
        state.firstSectionId = interviewInfo.firstSectionId
        state.interviewKey = interviewInfo.interviewKey
        state.receivedByInterviewer = interviewInfo.receivedByInterviewer
        state.interviewCannotBeChanged = interviewInfo.interviewCannotBeChanged
    },
    SET_COVER_INFO(state, coverInfo) {
        state.coverInfo = coverInfo
    },
    SET_COMPLETE_INFO(state, completeInfo) {
        Vue.set(state, "completeInfo", completeInfo)
    },
    SET_INTERVIEW_STATUS(state, interviewState) {
        Vue.set(state, "interviewState", interviewState)
    },
    SET_HAS_COVER_PAGE(state, hasCoverPage) {
        state.hasCoverPage = hasCoverPage
    },
    POSTING_COMMENT(state, {questionId}){
        const question = state.entityDetails[questionId]
        Vue.set(question, "postingComment", true)
    },


    SET_UPLOAD_PROGRESS(state, { id, now, total }) {
        const fetchState = {}

        Vue.set(fetchState, "uploaded", now)
        Vue.set(fetchState, "total", total)

        Vue.set(state.entityDetails[id], "fetchState", fetchState)
    },

    SET_FETCH(state, { id, ids, done }) {
        if (id && state.entityDetails[id]) {
            Vue.set(state.entityDetails[id], "fetching", !done)
        }

        if (ids) {
            ids.forEach(element => {
                if (state.entityDetails[element]) {
                    Vue.set(state.entityDetails[element], "fetching", !done)
                }
            })
        }
    },

    LOG_LAST_ACTIVITY(state){
        state.lastActivityTimestamp = new Date()
    },
    COMPLETE_INTERVIEW(state) {
        state.interviewCompleted = true;
    }
}
