using System;
using FluentAssertions;
using Moq;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.Enumerator.ViewModels;
using WB.Tests.Abc;


namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.NavigationStateTests
{
    internal class when_navigating_to_disabled_group
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            var interview = Mock.Of<IStatefulInterview>(_
                => _.HasGroup(disabledGroup) == true
                   && _.IsEnabled(disabledGroup) == false);

            navigationState = Create.Other.NavigationState(
                interviewRepository: Setup.StatefulInterviewRepository(interview));

            navigationState.ScreenChanged += eventArgs => navigatedTo = eventArgs.TargetGroup;
            BecauseOf();
        }

        public void BecauseOf() =>
            navigationState.NavigateTo(NavigationIdentity.CreateForGroup(disabledGroup));

        [NUnit.Framework.Test] public void should_not_navigate () =>
            navigatedTo.Should().BeNull();

        private static NavigationState navigationState;
        private static Identity disabledGroup = Create.Entity.Identity(Guid.Parse("11111111111111111111111111111111"), Empty.RosterVector);
        private static Identity navigatedTo;
    }
}
