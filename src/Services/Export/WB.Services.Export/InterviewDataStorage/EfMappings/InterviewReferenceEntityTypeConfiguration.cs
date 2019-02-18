﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WB.Services.Export.InterviewDataStorage.EfMappings
{
    public class InterviewReferenceEntityTypeConfiguration : IEntityTypeConfiguration<InterviewReference>
    {
        public void Configure(EntityTypeBuilder<InterviewReference> builder)
        {
            builder.ToTable("interview__references");
            builder.HasKey(x => x.InterviewId);
        }
    }

    public class DeletedQuestionnaireReferenceTypeConfiguration : IEntityTypeConfiguration<DeletedQuestionnaireReference>
    {
        public void Configure(EntityTypeBuilder<DeletedQuestionnaireReference> builder)
        {
            builder.ToTable("__deleted_questionnaire_reference");
            builder.HasKey(x => x.Id);
        }
    }
}
