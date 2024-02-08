import { get, commandCall } from './apiService';
import emitter from './emitter';

export async function getRoster(questionnaireId, entityId) {
    const data = await get('/api/questionnaire/editRoster/' + questionnaireId, {
        rosterId: entityId
    });
    return data;
}

export function updateRoster(questionnaireId, roster) {
    var command = {
        questionnaireId: questionnaireId,
        groupId: roster.itemId,
        title: roster.title,
        description: roster.description,
        condition: roster.enablementCondition,
        hideIfDisabled: roster.hideIfDisabled,
        variableName: roster.variableName,
        displayMode: roster.displayMode,
        isRoster: true
    };

    switch (roster.type) {
        case 'Fixed':
            command.rosterSizeSource = 'FixedTitles';
            command.fixedRosterTitles = roster.fixedRosterTitles;
            break;
        case 'Numeric':
            command.rosterSizeQuestionId = roster.rosterSizeNumericQuestionId;
            command.rosterTitleQuestionId = roster.rosterTitleQuestionId;
            break;
        case 'List':
            command.rosterSizeQuestionId = roster.rosterSizeListQuestionId;
            break;
        case 'Multi':
            command.rosterSizeQuestionId = roster.rosterSizeMultiQuestionId;
            break;
    }

    return commandCall('UpdateGroup', command).then(response => {
        emitter.emit('rosterUpdated', { roster: roster });
    });
}

export function deleteRoster(questionnaireId, entityId) {
    var command = {
        questionnaireId: questionnaireId,
        groupId: entityId
    };

    return commandCall('DeleteGroup', command).then(result => {
        emitter.emit('rosterDeleted', {
            id: entityId
        });
    });
}
