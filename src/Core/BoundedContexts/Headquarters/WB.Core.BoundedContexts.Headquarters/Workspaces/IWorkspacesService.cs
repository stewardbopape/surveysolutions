#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Infrastructure.Native.Storage.Postgre;
using WB.Infrastructure.Native.Workspaces;

namespace WB.Core.BoundedContexts.Headquarters.Workspaces
{
    public interface IWorkspacesService
    {
        public Task Generate(string name, DbUpgradeSettings upgradeSettings);
        void AddUserToWorkspace(HqUser user, string workspace);
        List<WorkspaceContext> GetEnabledWorkspaces();
        List<WorkspaceContext> GetAllWorkspaces();
        void AssignWorkspaces(HqUser user, List<Workspace> workspacesList);
        Task DeleteAsync(WorkspaceContext workspace, CancellationToken token = default);
    }
}
