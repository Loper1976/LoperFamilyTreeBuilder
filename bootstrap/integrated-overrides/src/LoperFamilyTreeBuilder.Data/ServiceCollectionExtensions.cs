using LoperFamilyTreeBuilder.Core.Genealogy;
using LoperFamilyTreeBuilder.Data.Services;
using LoperFamilyTreeBuilder.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoperFamilyTreeBuilder.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFamilyTreeData(this IServiceCollection services)
    {
        services.AddSingleton<ApplicationPaths>();
        services.AddSingleton<LocalDbConnectionStringFactory>();
        services.AddSingleton<FamilyTreeGraphBuilder>();

        services.AddDbContextFactory<FamilyTreeDbContext>((provider, options) =>
        {
            var connectionString = provider.GetRequiredService<LocalDbConnectionStringFactory>().Create();
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(FamilyTreeDbContext).Assembly.FullName));
        });

        services.AddScoped<PeopleQueryService>();
        services.AddScoped<PersonCommandService>();
        services.AddScoped<FamilyBranchQueryService>();
        services.AddScoped<DashboardQueryService>();
        services.AddScoped<TreeIntegrityQueryService>();
        services.AddScoped<FamilyTreeQueryService>();
        services.AddScoped<MedicalHealthQueryService>();
        services.AddScoped<CoreDataInitializationService>();
        return services;
    }

    public static async Task InitializeFamilyTreeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FamilyTreeDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(cancellationToken))
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        var initializer = scope.ServiceProvider.GetRequiredService<CoreDataInitializationService>();
        await initializer.InitializeAsync(cancellationToken);
    }
}
