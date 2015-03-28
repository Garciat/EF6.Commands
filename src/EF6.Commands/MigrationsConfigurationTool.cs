using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.Data.Entity.Migrations.Infrastructure;

namespace EF6.Commands
{
    public class MigrationsConfigurationTool
    {
        public static DbMigrationsConfiguration CreateConfiguration(Type configurationType, Assembly migrationsAssembly)
        {
            var configuration = Activator.CreateInstance(configurationType) as DbMigrationsConfiguration;
            configuration.MigrationsAssembly = migrationsAssembly;
            
            return configuration;
        }
        
        public static IEnumerable<Type> GetTypes(Assembly assembly) =>
            assembly.GetTypes().Where(
                t => !t.GetTypeInfo().IsAbstract
                     && !t.GetTypeInfo().IsGenericType
                     && typeof(DbMigrationsConfiguration).IsAssignableFrom(t));
        
        public static Type SelectType(IEnumerable<Type> types, Type contextType)
        {
            var configurationType = typeof(DbMigrationsConfiguration<>).MakeGenericType(contextType);
            
            var candidates = types.Where(t => configurationType.IsAssignableFrom(t)).ToArray();
            
            if (candidates.Length == 0)
            {
                throw new InvalidOperationException("No DbMigrationsConfiguration for: " + contextType.Name);
            }
            
            return candidates[0];
        }
    }
}