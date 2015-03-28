using System;
using System.IO;
using System.Linq;

using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.Data.Entity.Migrations.History;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.Entity.Infrastructure; 
using System.ComponentModel.DataAnnotations.Schema;

using Npgsql;

using System.Resources;
using System.Configuration;

namespace Example
{
    public class NpgsqlEFConfiguration : DbConfiguration
    {
        public NpgsqlEFConfiguration()
        {
            SetProviderServices("Npgsql", NpgsqlServices.Instance);
            SetProviderFactory("Npgsql", NpgsqlFactory.Instance);
            SetDefaultConnectionFactory(new NpgsqlConnectionFactory());
            SetMigrationSqlGenerator("Npgsql", () => new NpgsqlMigrationSqlGenerator());
            SetHistoryContext("Npgsql", (conn, schema) => CreateHistoryContext(conn, schema));
        }
        
        private HistoryContext CreateHistoryContext(DbConnection connection, String defaultSchema)
        {
            return new HistoryContext(connection, "public");
        }
    }
    
    public class MigrationsConfiguration : DbMigrationsConfiguration<TestContext>
    {
        public MigrationsConfiguration()
        {
            AutomaticMigrationsEnabled = false;
        }
        
        protected override void Seed(TestContext db)
        {
            var p1 = new Person()
            {
                Id = 1,
                Name = "Gabriel Garcia",
                Age = 22
            };
            
            db.Person.AddOrUpdate(p1);
            
            db.SaveChanges();
        }
    }
    
    [DbConfigurationType(typeof(NpgsqlEFConfiguration))]
    public class TestContext : DbContext
    {
        public DbSet<Person> Person { get; set; }
        
        public TestContext()
            : base("Server=localhost;Port=5432;Database=testdb;User Id=garciat;")
        {
            //Database.Log = Console.Error.WriteLine;
        }
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("public");
            
            base.OnModelCreating(modelBuilder);
        }
    }
    
    [Table("Person")]
    public class Person
    {
        public Int32 Id { get; set; }
        
        public String Name { get; set; }
        
        public Int32 Age { get; set; }
    }
    
    public class Program
    {
        public static void Main(String[] args)
        {
            using (var db = new TestContext())
            {
                var people = db.Person.ToArray();
                
                foreach (var p in people)
                {
                    Console.WriteLine(p.Name);
                }
            }
        }
    }
}
