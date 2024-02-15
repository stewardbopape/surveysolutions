import { defineStore } from 'pinia';
import { get, commandCall } from '../services/apiService';
import { newGuid } from '../helpers/guid';
import { i18n } from '../plugins/localization';
import emitter from '../services/emitter';
import {
    findIndex,
    forEach,
    isEmpty,
    map,
    filter,
    find,
    sortBy,
    without,
    cloneDeep,
    isEqual
} from 'lodash';

export const useQuestionnaireStore = defineStore('questionnaire', {
    state: () => ({
        info: {},
        edittingMetadata: {},
        edittingScenarios: [],
        edittingSharedInfo: {}
    }),
    getters: {
        getInfo: state => state.info,
        getEdittingMetadata: state => state.edittingMetadata,
        getIsDirtyMetadata: state =>
            !isEqual(state.edittingMetadata, state.info.metadata),
        getEdittingScenarios: state => state.edittingScenarios,
        getEdittingSharedInfo: state => state.edittingSharedInfo,
        getQuestionnaireEditDataDirty: state =>
            state.edittingSharedInfo.title != state.info.title ||
            state.edittingSharedInfo.variable != state.info.variable ||
            state.edittingSharedInfo.hideIfDisabled != state.info.hideIfDisabled
    },
    actions: {
        setupListeners() {
            emitter.on('macroAdded', this.macroAdded);
            emitter.on('macroDeleted', this.macroDeleted);
            emitter.on('macroUpdated', this.macroUpdated);

            emitter.on('categoriesUpdated', this.categoriesUpdated);
            emitter.on('categoriesDeleted', this.categoriesDeleted);

            emitter.on(
                'anonymousQuestionnaireSettingsUpdated',
                this.anonymousQuestionnaireSettingsUpdated
            );
            emitter.on(
                'questionnaireSettingsUpdated',
                this.questionnaireSettingsUpdated
            );
            emitter.on('ownershipPassed', this.ownershipPassed);

            emitter.on('sharedPersonAdded', this.sharedPersonAdded);
            emitter.on('sharedPersonRemoved', this.sharedPersonRemoved);

            emitter.on('translationUpdated', this.translationUpdated);
            emitter.on('translationDeleted', this.translationDeleted);
            emitter.on('defaultTranslationSet', this.defaultTranslationSet);

            emitter.on('metadataUpdated', this.metadataUpdated);

            emitter.on('groupDeleted', this.groupDeleted);
            emitter.on('groupUpdated', this.groupUpdated);

            emitter.on('rosterUpdated', this.rosterUpdated);

            emitter.on('scenarioUpdated', this.scenarioUpdated);
            emitter.on('scenarioDeleted', this.scenarioDeleted);

            emitter.on('lookupTableUpdated', this.lookupTableUpdated);
            emitter.on('lookupTableDeleted', this.lookupTableDeleted);

            emitter.on('attachmentDeleted', this.attachmentDeleted);
            emitter.on('attachmentUpdated', this.attachmentUpdated);

            emitter.on('itemPasted', this.itemPasted);
        },

        async fetchQuestionnaireInfo(questionnaireId) {
            try {
                const info = await get(
                    '/api/questionnaire/get/' + questionnaireId
                );
                this.setQuestionnaireInfo(info);
            } catch (error) {
                if (error.response.status === 401) {
                    window.location = '/';
                }
            }
        },

        resetSharedInfo() {
            this.edittingSharedInfo = this.getQuestionnaireEditData();
        },

        setQuestionnaireInfo(info) {
            this.info = info;

            this.edittingMetadata = cloneDeep(info.metadata);
            this.edittingScenarios = cloneDeep(info.scenarios);
            this.edittingSharedInfo = this.getQuestionnaireEditData();

            forEach(this.info.categories, categoriesItem => {
                var editCategories = cloneDeep(categoriesItem);
                categoriesItem.editCategories = editCategories;
            });

            forEach(this.info.lookupTables, lookupTable => {
                var editLookupTable = cloneDeep(lookupTable);
                lookupTable.editLookupTable = editLookupTable;
            });

            forEach(this.info.translations, translation => {
                var editTranslation = cloneDeep(translation);
                translation.editTranslation = editTranslation;
            });

            forEach(this.info.attachments, attachment => {
                var editAttachment = cloneDeep(attachment);
                attachment.editAttachment = editAttachment;
            });

            forEach(this.info.macros, macro => {
                this.prepareMacro(macro);
            });
        },

        getQuestionnaireEditData() {
            return {
                title: this.info.title,
                variable: this.info.variable,
                hideIfDisabled: this.info.hideIfDisabled,

                isAnonymouslyShared: this.info.isAnonymouslyShared,
                anonymousQuestionnaireId: this.info.anonymousQuestionnaireId,
                anonymouslySharedAtUtc: this.info.anonymouslySharedAtUtc
            };
        },

        macroAdded(payload) {
            this.prepareMacro(payload);
            this.info.macros.push(payload);
        },
        macroDeleted(payload) {
            const index = findIndex(this.info.macros, function(i) {
                return i.itemId === payload.itemId;
            });
            if (index !== -1) {
                this.info.macros.splice(index, 1);
            }
        },
        macroUpdated(payload) {
            const index = findIndex(this.info.macros, function(i) {
                return i.itemId === payload.itemId;
            });
            if (index !== -1) {
                this.prepareMacro(payload);
                Object.assign(this.info.macros[index], payload);
            }
        },
        anonymousQuestionnaireSettingsUpdated(payload) {
            this.info.isAnonymouslyShared = payload.isAnonymouslyShared;
            this.info.anonymousQuestionnaireId =
                payload.anonymousQuestionnaireId;
            this.info.anonymouslySharedAtUtc = payload.anonymouslySharedAtUtc;
        },
        questionnaireSettingsUpdated(payload) {
            this.info.title = payload.title;
            this.info.variable = payload.variable;
            this.info.hideIfDisabled = payload.hideIfDisabled;
            this.info.isPublic = payload.isPublic;
            this.info.defaultLanguageName = payload.defaultLanguageName;

            this.edittingSharedInfo = this.getQuestionnaireEditData();

            this.info.metadata.title = payload.title;
            this.edittingMetadata.title = payload.title;
        },
        ownershipPassed(payload) {
            forEach(this.info.sharedPersons, person => {
                if (person.email == payload.ownerEmail) {
                    person.isOwner = false;
                }

                if (person.email == payload.newOwnerEmail) {
                    person.isOwner = true;
                }
            });
        },
        sharedPersonRemoved(payload) {
            this.info.sharedPersons = filter(
                this.info.sharedPersons,
                person => {
                    return person.email !== payload.email;
                }
            );
        },
        sharedPersonAdded(paylaod) {
            if (
                filter(this.info.sharedPersons, {
                    email: paylaod.email
                }).length === 0
            ) {
                this.info.sharedPersons.push({
                    email: paylaod.email,
                    login: paylaod.name,
                    userId: paylaod.id,
                    shareType: paylaod.shareType
                });

                var owner = filter(this.info.sharedPersons, {
                    isOwner: true
                });
                var sharedPersons = sortBy(
                    without(this.info.sharedPersons, owner),
                    ['email']
                );

                this.info.sharedPersons.sharedPersons = [owner].concat(
                    sharedPersons
                );
            }
        },
        groupDeleted(event) {
            var index = findIndex(this.info.chapters, function(i) {
                return i.itemId === event.id.replaceAll('-', '');
            });

            if (index > -1) {
                this.info.chapters.splice(index, 1);
            }
        },
        groupUpdated(payload) {
            var index = findIndex(this.info.chapters, function(i) {
                return i.itemId === payload.group.id.replaceAll('-', '');
            });

            if (index > -1) {
                this.info.chapters[index].title = payload.group.title;
                this.info.chapters[index].hasCondition =
                    payload.group.enablementCondition !== null &&
                    /\S/.test(payload.group.enablementCondition);
                this.info.chapters[index].hideIfDisabled =
                    payload.group.hideIfDisabled;
            }
        },

        rosterUpdated(payload) {},
        attachmentDeleted(payload) {
            const index = findIndex(this.info.attachments, function(i) {
                return i.attachmentId === payload.id;
            });
            if (index !== -1) {
                this.info.attachments.splice(index, 1);
            }
        },
        attachmentUpdated(payload) {
            const newAttachment = cloneDeep(payload.attachment);
            newAttachment.file = null;
            newAttachment.editAttachment = cloneDeep(newAttachment);

            if (payload.attachment.oldAttachmentId) {
                const indexInit = findIndex(this.info.attachments, function(i) {
                    return (
                        i.attachmentId === payload.attachment.oldAttachmentId
                    );
                });
                if (indexInit !== -1) {
                    this.info.attachments[indexInit] = newAttachment;
                }
            } else {
                this.info.attachments.push(newAttachment);
            }
        },

        prepareMacro(macro) {
            macro.editMacro = cloneDeep(macro);
            macro.isDescriptionVisible = !isEmpty(macro.description);
        },

        addSection(callback) {
            const section = this.createEmptySection();

            var command = {
                questionnaireId: this.info.questionnaireId,
                groupId: section.itemId,
                title: section.title,
                condition: '',
                hideIfDisabled: false,
                isRoster: false,
                rosterSizeQuestionId: null,
                rosterSizeSource: 'Question',
                rosterFixedTitles: null,
                rosterTitleQuestionId: null,
                parentGroupId: null,
                variableName: null
            };

            return commandCall('AddGroup', command).then(result => {
                const index = this.info.chapters.length;
                this.info.chapters.splice(index, 0, section);
                callback(section);
                emitter.emit('chapterAdded');
            });
        },
        createEmptySection() {
            var newId = newGuid();
            var emptySection = {
                itemId: newId,
                title: i18n.t('QuestionnaireEditor.DefaultNewSection'),
                items: [],
                groupsCount: 0,
                hasCondition: false,
                hideIfDisabled: false,
                isCover: false,
                isReadOnly: false,
                itemType: 'Chapter',
                questionsCount: 0,
                rostersCount: 0
            };
            return emptySection;
        },

        deleteSection(chapterId) {
            var command = {
                questionnaireId: this.info.questionnaireId,
                groupId: chapterId
            };

            return commandCall('DeleteGroup', command).then(result => {
                const id = chapterId.replaceAll('-', '');

                var index = findIndex(this.info.chapters, function(i) {
                    return i.itemId === id;
                });

                this.info.chapters.splice(index, 1);

                emitter.emit('chapterDeleted', {
                    itemId: chapterId
                });
            });
        },

        categoriesUpdated(payload) {
            const newCategories = cloneDeep(payload.categories);
            newCategories.file = null;
            newCategories.editCategories = cloneDeep(newCategories);

            if (payload.categories.oldCategoriesId) {
                const indexInit = findIndex(this.info.categories, function(i) {
                    return (
                        i.categoriesId === payload.categories.oldCategoriesId
                    );
                });
                if (indexInit !== -1) {
                    this.info.categories[indexInit] = newCategories;
                }
            } else {
                this.info.categories.push(newCategories);
            }
        },

        categoriesDeleted(payload) {
            const index = findIndex(this.info.categories, function(i) {
                return i.categoriesId === payload.id;
            });
            if (index !== -1) {
                this.info.categories.splice(index, 1);
            }
        },

        async discardMetadataChanges() {
            this.edittingMetadata = cloneDeep(this.info.metadata);
        },

        metadataUpdated(event) {
            this.info.metadata = cloneDeep(event.metadata);
            this.edittingMetadata = cloneDeep(event.metadata);
            this.info.title = event.metadata.title;
        },

        translationDeleted(payload) {
            const index = findIndex(this.info.translations, function(i) {
                return i.translationId === payload.id;
            });
            if (index !== -1) {
                this.info.translations.splice(index, 1);
            }
        },
        translationUpdated(payload) {
            const newTranslation = cloneDeep(payload.translation);
            newTranslation.file = null;
            newTranslation.editTranslation = cloneDeep(newTranslation);

            if (payload.translation.oldTranslationId) {
                const index = findIndex(this.info.translations, function(i) {
                    return (
                        i.translationId === payload.translation.oldTranslationId
                    );
                });
                if (index !== -1) {
                    this.info.translations[index] = newTranslation;
                }
            } else {
                this.info.translations.push(newTranslation);
            }
        },

        defaultTranslationSet(event) {
            const translationId = event.translationId;

            forEach(this.info.translations, translation => {
                if (translation.translationId == translationId) {
                    translation.isDefault = true;
                    translation.editTranslation.isDefault = true;
                } else {
                    translation.isDefault = false;
                    translation.editTranslation.isDefault = false;
                }
            });
        },

        async scenarioUpdated(event) {
            const scenario = event.scenario;

            const item = find(
                this.info.scenarios,
                item => item.id == scenario.id
            );

            if (item) {
                item.title = scenario.title;
            }
        },

        async scenarioDeleted(event) {
            const scenarioId = event.scenarioId;

            this.info.scenarios = filter(this.info.scenarios, scenario => {
                return scenario.id !== scenarioId;
            });
            this.edittingScenarios = filter(
                this.edittingScenarios,
                scenario => {
                    return scenario.id !== scenarioId;
                }
            );
        },

        lookupTableUpdated(payload) {
            const newLookupTable = cloneDeep(payload.lookupTable);
            newLookupTable.file = null;
            newLookupTable.editLookupTable = cloneDeep(newLookupTable);

            if (payload.lookupTable.oldLookupTableId) {
                const indexInit = findIndex(this.info.lookupTables, function(
                    i
                ) {
                    return i.itemId === payload.lookupTable.oldLookupTableId;
                });
                if (indexInit !== -1) {
                    this.info.lookupTables[indexInit] = newLookupTable;
                }
            } else {
                this.info.lookupTables.push(newLookupTable);
            }
        },

        async lookupTableDeleted(payload) {
            const indexInit = findIndex(this.info.lookupTables, function(i) {
                return i.itemId === payload.id;
            });
            if (indexInit !== -1) {
                this.info.lookupTables.splice(indexInit, 1);
            }
        },

        itemPasted(event) {
            //if (event.parentId == null) {
            this.fetchQuestionnaireInfo(this.info.questionnaireId);
            //}
        }
    }
});
