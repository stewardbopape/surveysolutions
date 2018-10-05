using System.Threading.Tasks;
using WB.Services.Export.Questionnaire;
using WB.Services.Export.Tenant;

namespace WB.Services.Export.Ddi
{
    internal interface IDdiMetadataFactory
    {
        Task<string> CreateDDIMetadataFileForQuestionnaireInFolder(TenantInfo tenant, QuestionnaireId questionnaireId,
            string basePath);
    }
}
