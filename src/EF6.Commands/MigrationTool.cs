using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.Data.Entity.Migrations.Infrastructure;

namespace EF6.Commands
{
    public class MigrationTool
    {
        private readonly Assembly _assembly;

        public MigrationTool(Assembly assembly)
        {
            _assembly = assembly;
        }
        
        public virtual void AddMigration(
            String migrationName,
            String contextTypeName,
            String rootNamespace,
            String projectDir)
        {
            var contextType = GetContextType(contextTypeName);
            var configurationType = GetConfigurationType(contextType);
            
            var configuration = CreateConfiguration(configurationType);
            var scaffolder = new MigrationScaffolder(configuration);
            
            var migration = scaffolder.Scaffold(migrationName);
            
            var migrationDirectory = Path.Combine(projectDir, migration.Directory);
            var migrationFile = Path.Combine(migrationDirectory, migration.MigrationId + "." + migration.Language);
            var migrationMetadataFile = Path.Combine(migrationDirectory, migration.MigrationId + ".Designer." + migration.Language);
            
            var designerCode =
                migration.DesignerCode
                .Replace("private readonly ResourceManager", "//private readonly ResourceManager");
            
            foreach (var replaceKey in new[] { "Source", "Target" })
            {
                if (migration.Resources.ContainsKey(replaceKey))
                {
                    var code = String.Format("Resources.GetString(\"{0}\")", replaceKey);
                    
                    var valueString = String.Format("\"{0}\"", migration.Resources[replaceKey]);
                    
                    designerCode = designerCode.Replace(code, valueString);
                }
            }
            
            Directory.CreateDirectory(migrationDirectory);
            File.WriteAllText(migrationFile, migration.UserCode);
            File.WriteAllText(migrationMetadataFile, designerCode);
        }

        public virtual IEnumerable<MigrationInfo> GetMigrations(String contextTypeName)
        {
            var contextType = GetContextType(contextTypeName);
            var configurationType = GetConfigurationType(contextType);
            
            var configuration = CreateConfiguration(configurationType);
            var migrator = new DbMigrator(configuration);
            
            var migrations = new Dictionary<String, MigrationInfo>();
            
            Func<String, MigrationInfo> getInfo = migrationId =>
            {
                MigrationInfo info;
                
                if (!migrations.TryGetValue(migrationId, out info))
                {
                    info = new MigrationInfo { Id = migrationId };
                    migrations.Add(migrationId, info);
                }
                
                return info;
            };
            
            foreach (var migrationId in migrator.GetDatabaseMigrations())
            {
                var info = getInfo.Invoke(migrationId);
                
                info.InDatabase = true;
            }
            
            foreach (var migrationId in migrator.GetLocalMigrations())
            {
                var info = getInfo.Invoke(migrationId);
                
                info.InProject = true;
            }
            
            return migrations.Values.OrderBy(i => i.Id);
        }

        public virtual String ScriptMigration(
            String fromMigrationName,
            String toMigrationName,
            bool idempotent,
            String contextTypeName)
        {
            if (idempotent)
            {
                throw new NotImplementedException();
            }
            
            var contextType = GetContextType(contextTypeName);
            var configurationType = GetConfigurationType(contextType);
            
            var configuration = CreateConfiguration(configurationType);
            var migrator = new DbMigrator(configuration);
            
            var scripting = new MigratorScriptingDecorator(migrator);
            
            var script = scripting.ScriptUpdate(fromMigrationName, toMigrationName);
            
            return script;
        }

        public virtual void ApplyMigration(String migrationName, String contextTypeName)
        {
            var contextType = GetContextType(contextTypeName);
            var configurationType = GetConfigurationType(contextType);
            
            var configuration = CreateConfiguration(configurationType);
            var migrator = new DbMigrator(configuration);
            
            if (migrationName == null)
            {
                migrator.Update();
            }
            else
            {
                migrator.Update(migrationName);
            }
        }

        public virtual void RemoveMigration(
            String contextTypeName,
            String rootNamespace,
            String projectDir)
        {
            throw new NotImplementedException();
        }

        public virtual Type GetContextType(String name)
        {
            var contextType = ContextTool.SelectType(GetContextTypes(), name);

            return contextType;
        }
        
        public Type GetConfigurationType(Type contextType)
        {
            var configurationType = MigrationsConfigurationTool.SelectType(GetConfigurationTypes(), contextType);
            
            return configurationType;
        }

        public virtual IEnumerable<Type> GetContextTypes() =>
            ContextTool.GetContextTypes(_assembly)
                .Distinct();
        
        public virtual IEnumerable<Type> GetConfigurationTypes() =>
            MigrationsConfigurationTool.GetTypes(_assembly)
                .Distinct();

        protected virtual DbMigrationsConfiguration CreateConfiguration(Type configurationType)
        {
            var configuration = MigrationsConfigurationTool.CreateConfiguration(configurationType, _assembly);
            
            return configuration;
        }

        protected virtual MigrationScaffolder CreateScaffolder(IServiceProvider services)
        {
            return null;
        }
    }
}