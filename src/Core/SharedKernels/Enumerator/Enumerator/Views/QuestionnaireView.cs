﻿using SQLite;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;

namespace WB.Core.SharedKernels.Enumerator.Views
{
    public class QuestionnaireView : IPlainStorageEntity
    {
        [PrimaryKey]
        public string Id { get; set; }

        public QuestionnaireIdentity GetIdentity()
        {
            return QuestionnaireIdentity.Parse(Id);
        }

        public string Title { get; set; }
        public bool Census { get; set; }
        public int? WebModeAllowed { get; set; } // library does not support good way of handling default values and bools https://github.com/praeclarum/sqlite-net/issues/326
    }
}
