﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.SharedKernels.DataCollection.Views.Questionnaire;

namespace WB.Core.SharedKernels.DataCollection.Tests.Views.QuestionnaireRosterStructureTests
{
    internal class when_creating_roster_structure_for_questionnarie_which_has_capital_question_inside_autopropagate_style_roster : QuestionnaireRosterStructureTestContext
    {
        Establish context = () =>
        {
            capitalQuestionId = new Guid("CBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
            autoPropagatedQuestionId = new Guid("EBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
            rosterGroupId = new Guid("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");

            questionnarie = CreateQuestionnaireDocumentWithOneChapter(
                new AutoPropagateQuestion()
                {
                    PublicKey = autoPropagatedQuestionId,
                    Triggers = new List<Guid> { rosterGroupId },
                    QuestionType = QuestionType.AutoPropagate
                },
                new Group("Roster")
                {
                    Propagated = Propagate.AutoPropagated,
                    PublicKey = rosterGroupId,
                    Children = new List<IComposite>
                    {
                        new NumericQuestion() { PublicKey = capitalQuestionId, Capital = true }
                    }
                });
        };

        Because of = () =>
            questionnaireRosterStructure = new QuestionnaireRosterStructure(questionnarie, 1);

        It should_contain_1_roster_scope = () =>
            questionnaireRosterStructure.RosterScopes.Count().ShouldEqual(1);

        It should_specify_autoPropagated_question_id_as_id_of_roster_scope = () =>
            questionnaireRosterStructure.RosterScopes.Single().Key.ShouldEqual(autoPropagatedQuestionId);

        It should_specify_id_of_capital_question_id_as_roster_title_question_for_roster_id_in_roster_scope = () =>
            questionnaireRosterStructure.RosterScopes.Single().Value
                .RosterIdToRosterTitleQuestionIdMap[rosterGroupId].ShouldEqual(capitalQuestionId);

        private static QuestionnaireDocument questionnarie;
        private static QuestionnaireRosterStructure questionnaireRosterStructure;
        private static Guid capitalQuestionId;
        private static Guid rosterGroupId;
        private static Guid autoPropagatedQuestionId;
    }
}
