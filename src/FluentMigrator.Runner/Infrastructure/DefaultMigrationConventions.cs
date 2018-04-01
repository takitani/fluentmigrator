#region License
//
// Copyright (c) 2007-2018, Sean Chambers <schambers80@gmail.com>
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FluentMigrator.Infrastructure;
using FluentMigrator.Infrastructure.Extensions;
using FluentMigrator.Model;
using FluentMigrator.Runner.VersionTableInfo;

namespace FluentMigrator.Runner.Infrastructure
{
    public class DefaultMigrationConventions : IMigrationConventions
    {
        private DefaultMigrationConventions()
        {
        }

        public static DefaultMigrationConventions Instance { get; } = new DefaultMigrationConventions();

        public Func<ForeignKeyDefinition, string> GetForeignKeyName => GetForeignKeyNameImpl;
        public Func<IndexDefinition, string> GetIndexName => GetIndexNameImpl;
        public Func<string, string> GetPrimaryKeyName => GetPrimaryKeyNameImpl;
        public Func<Type, bool> TypeIsMigration => TypeIsMigrationImpl;
        public Func<Type, bool> TypeIsProfile => TypeIsProfileImpl;
        public Func<Type, MigrationStage?> GetMaintenanceStage => GetMaintenanceStageImpl;
        public Func<Type, bool> TypeIsVersionTableMetaData => TypeIsVersionTableMetaDataImpl;
        public Func<string> GetWorkingDirectory => () => Environment.CurrentDirectory;
        public Func<Type, IMigrationInfo> GetMigrationInfo => GetMigrationInfoForImpl;
        public Func<ConstraintDefinition, string> GetConstraintName => GetConstraintNameImpl;
        public Func<Type, bool> TypeHasTags => TypeHasTagsImpl;
        public Func<Type, IEnumerable<string>, bool> TypeHasMatchingTags => TypeHasMatchingTagsImpl;
        public Func<Type, string, string> GetAutoScriptUpName => GetAutoScriptUpNameImpl;
        public Func<Type, string, string> GetAutoScriptDownName => GetAutoScriptDownNameImpl;
        public Func<string> GetDefaultSchema => () => null;

        private static string GetPrimaryKeyNameImpl(string tableName)
        {
            return "PK_" + tableName;
        }

        private static string GetForeignKeyNameImpl(ForeignKeyDefinition foreignKey)
        {
            var sb = new StringBuilder();

            sb.Append("FK_");
            sb.Append(foreignKey.ForeignTable);

            foreach (string foreignColumn in foreignKey.ForeignColumns)
            {
                sb.Append("_");
                sb.Append(foreignColumn);
            }

            sb.Append("_");
            sb.Append(foreignKey.PrimaryTable);

            foreach (string primaryColumn in foreignKey.PrimaryColumns)
            {
                sb.Append("_");
                sb.Append(primaryColumn);
            }

            return sb.ToString();
        }

        private static string GetIndexNameImpl(IndexDefinition index)
        {
            var sb = new StringBuilder();

            sb.Append("IX_");
            sb.Append(index.TableName);

            foreach (IndexColumnDefinition column in index.Columns)
            {
                sb.Append("_");
                sb.Append(column.Name);
            }

            return sb.ToString();
        }

        private static bool TypeIsMigrationImpl(Type type)
        {
            return typeof(IMigration).IsAssignableFrom(type) && type.HasAttribute<MigrationAttribute>();
        }

        private static MigrationStage? GetMaintenanceStageImpl(Type type)
        {
            if (!typeof(IMigration).IsAssignableFrom(type))
                return null;

            var attribute = type.GetOneAttribute<MaintenanceAttribute>();
            return attribute?.Stage;
        }

        private static bool TypeIsProfileImpl(Type type)
        {
            return typeof(IMigration).IsAssignableFrom(type) && type.HasAttribute<ProfileAttribute>();
        }

        private static bool TypeIsVersionTableMetaDataImpl(Type type)
        {
            return typeof(IVersionTableMetaData).IsAssignableFrom(type) && type.HasAttribute<VersionTableMetaDataAttribute>();
        }

        private static IMigrationInfo GetMigrationInfoForImpl(Type migrationType)
        {
            IMigration CreateMigration()
            {
                return (IMigration) Activator.CreateInstance(migrationType);
            }

            var migrationAttribute = migrationType.GetOneAttribute<MigrationAttribute>();
            var migrationInfo = new MigrationInfo(migrationAttribute.Version, migrationAttribute.Description, migrationAttribute.TransactionBehavior, CreateMigration);

            foreach (MigrationTraitAttribute traitAttribute in migrationType.GetAllAttributes<MigrationTraitAttribute>())
                migrationInfo.AddTrait(traitAttribute.Name, traitAttribute.Value);

            return migrationInfo;
        }

        private static string GetConstraintNameImpl(ConstraintDefinition expression)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(expression.IsPrimaryKeyConstraint ? "PK_" : "UC_");

            sb.Append(expression.TableName);
            foreach (var column in expression.Columns)
            {
                sb.Append("_" + column);
            }
            return sb.ToString();
        }

        private static bool TypeHasTagsImpl(Type type)
        {
            return type.GetOneAttribute<TagsAttribute>(true) != null;
        }

        private static bool TypeHasMatchingTagsImpl(Type type, IEnumerable<string> tagsToMatch)
        {
            var tags = type.GetAllAttributes<TagsAttribute>(true);

            if (tags.Any() && !tagsToMatch.Any())
                return false;

            var tagNamesForAllBehavior = tags.Where(t => t.Behavior == TagBehavior.RequireAll).SelectMany(t => t.TagNames).ToArray();
            if (tagNamesForAllBehavior.Any() && tagsToMatch.All(t => tagNamesForAllBehavior.Any(t.Equals)))
            {
                return true;
            }

            var tagNamesForAnyBehavior = tags.Where(t => t.Behavior == TagBehavior.RequireAny).SelectMany(t => t.TagNames).ToArray();
            if (tagNamesForAnyBehavior.Any() && tagsToMatch.Any(t => tagNamesForAnyBehavior.Any(t.Equals)))
            {
                return true;
            }

            return false;
        }

        private static string GetAutoScriptUpNameImpl(Type type, string databaseType)
        {
            if (TypeIsMigrationImpl(type))
            {
                var version = type.GetOneAttribute<MigrationAttribute>().Version;
                return string.Format("Scripts.Up.{0}_{1}_{2}.sql"
                        , version
                        , type.Name
                        , databaseType);
            }
            return string.Empty;
        }

        private static string GetAutoScriptDownNameImpl(Type type, string databaseType)
        {
            if (TypeIsMigrationImpl(type))
            {
                var version = type.GetOneAttribute<MigrationAttribute>().Version;
                return string.Format("Scripts.Down.{0}_{1}_{2}.sql"
                        , version
                        , type.Name
                        , databaseType);
            }
            return string.Empty;
        }
    }
}
