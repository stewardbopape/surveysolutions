﻿using System;
using Machine.Specifications;

namespace WB.Tests.Integration.EsentTests
{
    internal class when_esent_store_is_cleared : with_esent_store<TestStoredEntity>
    {
        Establish context = () =>
        {
            transientEntity = new TestStoredEntity
            {
                Id = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"),
                IntegerProperty = 5,
                StringProperty = "Some test string"
            };
            storage.Store(transientEntity, itemId);
        };

        Because of = () => storage.Clear();

        It should_clear_stored_data = () => storage.GetById(itemId).ShouldBeNull();

        const string itemId = "id";
        static TestStoredEntity transientEntity;
    }
}

