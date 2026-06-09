using Microsoft.Extensions.Options;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using Umbraco.Cms.Core.Sync;

namespace UmbracoCms.Web.Infrastructure.Configuration;

public class ConfigurationServerRoleAccessor : IServerRoleAccessor
{
    private readonly IOptionsMonitor<ApplicationOptions> _applicationOptions;

    public ConfigurationServerRoleAccessor(IOptionsMonitor<ApplicationOptions> applicationOptions)
    {
        _applicationOptions = applicationOptions;
    }

    public ServerRole CurrentServerRole => _applicationOptions.CurrentValue.ServerRole;
}
