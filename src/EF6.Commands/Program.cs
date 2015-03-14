using System;
using System.IO;
using System.Linq;
using System.Reflection;

using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.Entity.Infrastructure; 
using System.ComponentModel.DataAnnotations.Schema;

using System.Resources;
using System.Configuration;

using Microsoft.Framework.Runtime;

namespace EF6.Commands
{
    public class Program
    {
        private readonly String _appBasePath;
        private readonly Assembly _appAssembly;
        
        public Program(IApplicationEnvironment appEnv)
        {
            _appBasePath = appEnv.ApplicationBasePath;
            
            var assemblyName = new AssemblyName(appEnv.ApplicationName);
            
            _appAssembly = Assembly.Load(assemblyName);
        }
        
        private DbMigrationsConfiguration GetMigrationsConfigurationInstance()
        {
            var search =
                from t in _appAssembly.GetTypes()
                where typeof(DbMigrationsConfiguration).IsAssignableFrom(t.BaseType)
                select t;
            
            var configType = search.FirstOrDefault();
            
            if (configType == null)
            {
                throw new Exception("nope");
            }
            
            var config = (DbMigrationsConfiguration)Activator.CreateInstance(configType);
            config.MigrationsAssembly = _appAssembly;
            
            return config;
        }
        
        private void CreateMigration(String directory, String name)
        {
            var config      = GetMigrationsConfigurationInstance();
            var scaffolder  = new MigrationScaffolder(config);
            var migration   = scaffolder.Scaffold(name);
            
            var userCodePath        = Path.Combine(_appBasePath, directory, name + ".cs");
            var designerCodePath    = Path.Combine(_appBasePath, directory, name + ".Designer.cs");
            var resourcePath        = Path.Combine(_appBasePath, directory, name + ".resx");
            
            File.WriteAllText(userCodePath, migration.UserCode);
            File.WriteAllText(designerCodePath, migration.DesignerCode);
            
            using (var writer = new ResXResourceWriter(resourcePath))
            {
                foreach (var resource in migration.Resources)
                {
                    writer.AddResource(resource.Key, resource.Value);
                }
            }
        }
        
        private void UpdateDatabase()
        {
            var config      = GetMigrationsConfigurationInstance();
            var migrator    = new DbMigrator(config);
            
            //var all = String.Join(", ", migrator.GetLocalMigrations().ToArray());
            
            migrator.Update();
        }
        
        public virtual int Main(String[] args)
        {
            switch (args[0])
            {
            case "add-migration":
                CreateMigration(args[1], args[2]);
                break;
            case "update-db":
                UpdateDatabase();
                break;
            }
            return 0;
        }
    }
}