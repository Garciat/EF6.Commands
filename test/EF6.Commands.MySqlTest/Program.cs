using System;
using System.IO;
using System.Linq;

using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.Entity.Infrastructure; 
using System.ComponentModel.DataAnnotations.Schema;

using MySql.Data.Entity;

using System.Resources;
using System.Configuration;

namespace Example
{
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
    
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class TestContext : DbContext
    {
        public DbSet<Person> Person { get; set; }
        
        public TestContext()
            : base("Server=localhost;Port=3306;Database=testdb;Uid=garciat;Pwd=hola;")
        {
            //Database.Log = Console.Error.WriteLine;
        }
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
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
