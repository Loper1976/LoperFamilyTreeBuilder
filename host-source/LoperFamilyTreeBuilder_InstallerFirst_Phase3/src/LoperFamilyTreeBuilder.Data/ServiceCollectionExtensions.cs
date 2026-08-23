using LoperFamilyTreeBuilder.Data.Services;
using LoperFamilyTreeBuilder.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResearchAgent.Core.Integration;
using ResearchAgent.Core.Persistence;
using ResearchAgent.Core.Review;
using ResearchAgent.Core.UI;
using ResearchAgent.Core.Research;
using LoperFamilyTreeBuilder.ImportExport.Gedcom;

namespace LoperFamilyTreeBuilder.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFamilyTreeData(
        this IServiceCollection services)
    {
        services.AddSingleton<ApplicationPaths>();
        services.AddSingleton<LocalDbConnectionStringFactory>();

        services.AddDbContextFactory<FamilyTreeDbContext>((provider, options) =>
        {
            var connectionString =
                provider.GetRequiredService<LocalDbConnectionStringFactory>()
                    .Create();

            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(
                    typeof(FamilyTreeDbContext).Assembly.FullName));
        });

        services.AddScoped<PeopleQueryService>();
        services.AddScoped<PersonCommandService>();
        services.AddScoped<PersonProfileQueryService>();
        services.AddScoped<FamilyBranchQueryService>();
        services.AddScoped<DashboardQueryService>();
        services.AddScoped<CoreDataInitializationService>();
        services.AddSingleton<JsonResearchStore>(provider =>
        {
            var paths = provider.GetRequiredService<ApplicationPaths>();
            paths.EnsureLocalDirectories();
            return new JsonResearchStore(paths.ResearchDirectory);
        });
        services.AddSingleton<IResearchStore>(provider => provider.GetRequiredService<JsonResearchStore>());
        services.AddSingleton<IResearchCenterQueryStore>(provider => provider.GetRequiredService<JsonResearchStore>());
        services.AddScoped<IHostPersonReader, HostPersonReader>();
        services.AddScoped<IApprovalStore, HostApprovalStore>();
        services.AddScoped<HostDatabaseBackupService>();
        services.AddScoped<IHostBackupGate>(provider => provider.GetRequiredService<HostDatabaseBackupService>());
        services.AddScoped<IAcceptedTreePromotionService, AcceptedTreePromotionService>();
        services.AddScoped<IResearchCenterService, ResearchCenterService>();
        services.AddScoped<ResearchExecutor>();
        services.AddScoped<ResearchCommandService>();
        services.AddSingleton<GedcomParser>();
        services.AddSingleton<GedcomDuplicateAnalyzer>();
        services.AddScoped<GedcomImportPreviewService>();

        return services;
    }

    public static async Task InitializeFamilyTreeDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var factory =
            scope.ServiceProvider.GetRequiredService<
                IDbContextFactory<FamilyTreeDbContext>>();

        await using (var db =
            await factory.CreateDbContextAsync(cancellationToken))
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        var initializer =
            scope.ServiceProvider.GetRequiredService<
                CoreDataInitializationService>();

        await initializer.InitializeAsync(cancellationToken);
    }
}
