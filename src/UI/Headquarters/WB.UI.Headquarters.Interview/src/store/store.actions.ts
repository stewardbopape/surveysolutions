import { apiCaller } from "../api"
import router from "./../router"

export default {
    async loadQuestionnaire({commit}, questionnaireId) {
        const questionnaireInfo =
            await apiCaller<IQuestionnaireInfo>(api => api.questionnaireDetails(questionnaireId))
        commit("SET_QUESTIONNAIRE_INFO", questionnaireInfo);
    },
    async startInterview({commit}, questionnaireId: string) {
        const interviewId = await apiCaller(api => api.createInterview(questionnaireId)) as string;
        const loc = { name: "prefilled", params: { id: interviewId } };
        router.push(loc)
    },
    async fetchTextQuestion({commit}, entity) {
        const entityDetails = await apiCaller(api => api.getTextQuestion(entity.identity))
        commit("SET_TEXTQUESTION_DETAILS", entityDetails);
    },
    async fetchSingleOptionQuestion({ commit }, entity) {
        const entityDetails = await apiCaller(api => api.getSingleOptionQuestion(entity.identity))
        commit("SET_SINGLEOPTION_DETAILS", entityDetails);
    },
    async getPrefilledQuestions({ commit }, interviewId) {
        await apiCaller(api => api.startInterview(interviewId))
        const data = await apiCaller(api => api.getPrefilledQuestions())
        commit("SET_PREFILLED_QUESTIONS", data)
    },
    async answerSingleOptionQuestion({commit}, answerInfo) {
        await apiCaller(api => api.answerSingleOptionQuestion(answerInfo.answer, answerInfo.questionId))
    },
    async answerTextQuestion({ commit }, entity) {
        await apiCaller(api => api.answerTextQuestion(entity.identity, entity.text))
    },
    InterviewMount({ commit }, { id }) {
        commit("SET_INTERVIEW", id)
    }
}
