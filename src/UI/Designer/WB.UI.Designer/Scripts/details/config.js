﻿define('config',
    ['pnotify', 'ko', 'amplify'],
    function(toastr, ko, amplify) {

        var// properties
            //-----------------
            questionTypes = {
                "SingleOption": "Categorical: one answer",
                "MultyOption": "Categorical: multiple answers",
                "Numeric": "Numeric",
                "DateTime": "DateTime",
                "Text": "Text",
                "AutoPropagate": "AutoPropagate"
            },
            questionTypeOptions = [
                {
                    key: "SingleOption",
                    value: "Categorical: one answer"
                },
                {
                    key: "MultyOption",
                    value: "Categorical: multiple answers"
                },
                {
                    key: "Numeric",
                    value: "Numeric"
                },
                {
                    key: "DateTime",
                    value: "Date and time"
                },
                {
                    key: "Text",
                    value: "Text"
                },
                {
                    key: "AutoPropagate",
                    value: "Auto propagate"
                }
            ],
            questionScopes = [
                "Interviewer",
                "Supervisor",
                "Headquarter"
            ],
            answerOrders = [
                {
                    key: "AsIs",
                    value: "As is"
                },
                {
                    key: "AZ",
                    value: "Sort by label AZ"
                },
                {
                    key: "ZA",
                    value: "Sort by label ZA"
                },
                {
                    key: "MinMax",
                    value: "Sort by value, accending"
                },
                {
                    key: "MaxMin",
                    value: "Sort by value, discending"
                }
            ],
            groupTypes = [
                "None",
                "",
                "AutoPropagated"
            ],
            commands = {
                updateQuestionnaire: "UpdateQuestionnaire",
                createGroup: "AddGroup",
                cloneGroup: "CloneGroupWithoutChildren",
                updateGroup: "UpdateGroup",
                deleteGroup: "DeleteGroup",
                groupMove: "MoveGroup",
                createQuestion: "AddQuestion",
                cloneQuestion: "CloneQuestion",
                updateQuestion: "UpdateQuestion",
                deleteQuestion: "DeleteQuestion",
                questionMove: "MoveQuestion"
            },
            hashes = {
                details: '#/details',
                detailsGroup: '#/details/group',
                detailsQuestion: '#/details/question',
                detailsQuestionnaire: '#/details/questionnaire'
            },
            viewIds = {
                details: '#stacks'
            },
            messages = {
                viewModelActivated: 'viewmodel-activation'
            },
            stateKeys = {
                lastView: 'state.active-hash'
            },
            tips = {
                newGroup: {
                    title: "Save this group",
                    content: "",
                    placement: "top",
                    trigger: "hover"
                }
            },
            logger = $.pnotify, // use pnotify for the logger

            storeExpirationMs = (1000 * 60 * 60 * 24), // 1 day
            throttle = 400,
            loggerTmeout = 2000,
            warnings = {
                propagatedGroupCantBecomeChapter: {
                    title: 'Cant move',
                    text: "Auto propagate group can't become a chapter"
                },
                cantMoveQuestionOutsideGroup: {
                    title: 'Cant move',
                    text: "You can't move question outside any group"
                },
                cantMoveGroupIntoPropagatedGroup: {
                    title: 'Cant move',
                    text: "You can't move group into propagated group"
                },
                cantMoveUnsavedItem: {
                    title: 'Cant move',
                    text: "You can't move unsaved items"
                },
                saveParentFirst: {
                    title: 'Cant save',
                    text: "Save parent item first"
                },
                savedData: 'Data saved successfully'
            },
            // methods
            //-----------------

            init = function() {
                logger.defaults.delay = loggerTmeout;

                ko.validation.configure({
                    messagesOnModified: true,
                    parseInputAttributes: true,
                    insertMessages: true,
                    decorateElement: true,
                    errorElementClass: 'error',
                    errorMessageClass: "help-inline"
                });

                ko.bindingHandlers.sortable.options = { cursor: "move", handle: ".handler", axis: "y", placeholder: "ui-state-highlight" };
                ko.bindingHandlers.draggable.options = { cursor: "move", handle: ".handler", axis: "y" };
            };

        init();

        return {
            logger: logger,
            storeExpirationMs: storeExpirationMs,
            throttle: throttle,
            warnings: warnings,
            hashes: hashes,
            viewIds: viewIds,
            messages: messages,
            stateKeys: stateKeys,
            questionTypes: questionTypes,
            questionTypeOptions: questionTypeOptions,
            questionScopes: questionScopes,
            answerOrders: answerOrders,
            groupTypes: groupTypes,
            commands: commands,
            tips: tips
        };
    });
