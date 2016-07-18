using System;
using Main.Core.Documents;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Repositories
{
    internal class PlainQuestionnaireRepositoryWithCache : IPlainQuestionnaireRepository
    {
        private readonly IPlainKeyValueStorage<QuestionnaireDocument> repository;
        private readonly ConcurrentDictionary<string, QuestionnaireDocument> cache = new ConcurrentDictionary<string, QuestionnaireDocument>();
        private readonly Dictionary<QuestionnaireIdentity, PlainQuestionnaire> plainQuestionnaireCache = new Dictionary<QuestionnaireIdentity, PlainQuestionnaire>();
        
        public PlainQuestionnaireRepositoryWithCache(IPlainKeyValueStorage<QuestionnaireDocument> repository)
        {
            this.repository = repository;
        }

        public IQuestionnaire GetQuestionnaire(QuestionnaireIdentity identity, string language)
        {
            if (!this.plainQuestionnaireCache.ContainsKey(identity))
            {
                QuestionnaireDocument questionnaireDocument = this.GetQuestionnaireDocument(identity.QuestionnaireId, identity.Version);
                if (questionnaireDocument == null || questionnaireDocument.IsDeleted)
                    return null;

                var plainQuestionnaire = new PlainQuestionnaire(questionnaireDocument, identity.Version);
                plainQuestionnaire.WarmUpPriorityCaches();

                this.plainQuestionnaireCache[identity] = plainQuestionnaire;
            }

            return this.plainQuestionnaireCache[identity];
        }

        public void StoreQuestionnaire(Guid id, long version, QuestionnaireDocument questionnaireDocument)
        {
            string repositoryId = GetRepositoryId(id, version);
            this.repository.Store(questionnaireDocument, repositoryId);
            this.cache[repositoryId] = questionnaireDocument.Clone();
            this.plainQuestionnaireCache.Remove(new QuestionnaireIdentity(id, version));
        }

        public QuestionnaireDocument GetQuestionnaireDocument(Guid id, long version)
        {
            string repositoryId = GetRepositoryId(id, version);

            if (!this.cache.ContainsKey(repositoryId))
            {
                this.cache[repositoryId] = this.repository.GetById(repositoryId);
            }

            return this.cache[repositoryId];
        }

        public QuestionnaireDocument GetQuestionnaireDocument(QuestionnaireIdentity identity)
        {
            return this.GetQuestionnaireDocument(identity.QuestionnaireId, identity.Version);
        }

        public void DeleteQuestionnaireDocument(Guid id, long version)
        {
            string repositoryId = GetRepositoryId(id, version);
            var document = this.repository.GetById(repositoryId);

            if (document == null)
                return;

            document.IsDeleted = true;
            StoreQuestionnaire(id, version, document);

            this.cache[repositoryId] = null;
            this.plainQuestionnaireCache.Remove(new QuestionnaireIdentity(id, version));
        }

        private static string GetRepositoryId(Guid id, long version)
        {
            return $"{id.FormatGuid()}${version}";
        }
    }
}