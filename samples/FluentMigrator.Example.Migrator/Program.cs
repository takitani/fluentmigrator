#region License
//
// Copyright (c) 2018, Fluent Migrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.IO;

using FluentMigrator.Example.Migrations;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Announcers;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.SQLite;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace FluentMigrator.Example.Migrator
{
    static class Program
    {
        static void Main()
        {
            // Configure the DB connection
            var dbFileName = Path.Combine(AppContext.BaseDirectory, "test.db");
            var csb = new SqliteConnectionStringBuilder
            {
                DataSource = dbFileName,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            var serviceProvider = ConfigureServices(new ServiceCollection(), csb.ConnectionString);
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

            // Run the migrations
            runner.MigrateUp();
        }

        private static IServiceProvider ConfigureServices(IServiceCollection services, string connectionString)
        {
            services
                // Create the announcer to output the migration messages
                .AddSingleton<IAnnouncer>(sp => new ConsoleAnnouncer() { ShowSql = true })

                // Processor specific options (usually none are needed)
                .AddSingleton<IMigrationProcessorOptions, ProcessorOptions>()

                // Initialize the DB-specific processor
                .AddSingleton<IMigrationProcessorFactory, SQLiteProcessorFactory>()
                .AddScoped(sp =>
                {
                    var processorFactory = sp.GetRequiredService<IMigrationProcessorFactory>();
                    var announcer = sp.GetRequiredService<IAnnouncer>();
                    var options = sp.GetRequiredService<IMigrationProcessorOptions>();
                    return processorFactory.Create(connectionString, announcer, options);
                })

                // Configure the loader for migrations that should be executed during maintenance steps
                .AddScoped<IMaintenanceLoader, MaintenanceLoader>()

                // Configure the loader for migrations tagged for a profile
                .AddScoped<IProfileLoader, ProfileLoader>()

                // Configure the migration information loader
                .AddScoped<IMigrationInformationLoader, DefaultMigrationInformationLoader>()

                // Configure the runner context
                .AddScoped<IRunnerContext>(sp =>
                {
                    var announcer = sp.GetRequiredService<IAnnouncer>();
                    return new RunnerContext(announcer)
                    {
                        AllowBreakingChange = true,
                    };
                })

                // Configure the runner conventions
                .AddScoped<IMigrationRunnerConventions, MigrationRunnerConventions>()

                // Configure the runner
                .AddScoped<IMigrationRunner, MigrationRunner>();

            // Configure the new and shiny stuff
            ConfigureNewServices(services);

#pragma warning disable 612
            // Configure compatibility stuff
            ConfigureLegacyServices(services);
#pragma warning restore 612

            return services.BuildServiceProvider();
        }

        private static void ConfigureNewServices(IServiceCollection services)
        {
            // Add the default embedded resource provider
            services
                .AddScoped<IEmbeddedResourceProvider, DefaultEmbeddedResourceProvider>();

            // Add migrations
            services.Scan(source =>
            {
                source
                    .FromAssemblyOf<AddGTDTables>()
                    .AddClasses(filter => filter.AssignableTo<IMigration>())
                    .As<IMigration>()
                    .WithScopedLifetime();
            });
        }

        [Obsolete]
        private static void ConfigureLegacyServices(IServiceCollection services)
        {
            services
                .AddScoped<IAssemblyCollection, AssemblyCollectionService>();
        }
    }
}
