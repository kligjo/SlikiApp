using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace UmbracoCms.Web.Infrastructure.DatabaseMigrations;

/// <summary>
/// Base class for database migration components.
/// </summary>
public abstract class BaseDatabaseMigrationComponent : INotificationHandler<UnattendedInstallNotification>
{
    private readonly IRuntimeState _runtimeState;
    private readonly IKeyValueService _keyValueService;
    private readonly IMigrationPlanExecutor _migrationPlanExecutor;
    private readonly ICoreScopeProvider _scopeProvider;

    protected BaseDatabaseMigrationComponent(
        IRuntimeState runtimeState,
        IKeyValueService keyValueService,
        IMigrationPlanExecutor migrationPlanExecutor,
        ICoreScopeProvider scopeProvider)
    {
        _runtimeState = runtimeState;
        _keyValueService = keyValueService;
        _migrationPlanExecutor = migrationPlanExecutor;
        _scopeProvider = scopeProvider;
    }

    protected abstract RuntimeLevel[] SupportedRuntimeLevels { get; }

    protected abstract MigrationPlan BuildMigrationPlan();

    public void Initialize()
    {
        if (!SupportedRuntimeLevels.Contains(_runtimeState.Level))
        {
            return;
        }

        var plan = BuildMigrationPlan();
        var upgrader = new Upgrader(plan);
        upgrader.Execute(_migrationPlanExecutor, _scopeProvider, _keyValueService);
    }

    public void Handle(UnattendedInstallNotification notification)
    {
        Initialize();
    }
}
