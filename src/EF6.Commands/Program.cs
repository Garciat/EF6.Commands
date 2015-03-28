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
using Microsoft.Framework.Runtime.Common.CommandLine;

namespace EF6.Commands
{
    public class Program
    {
        private readonly String _projectDir;
        private readonly String _rootNamespace;
        private readonly ILibraryManager _libraryManager;
        private readonly MigrationTool _migrationTool;
        
        private CommandLineApplication _app;
        
        public Program(IApplicationEnvironment appEnv, ILibraryManager libraryManager)
        {
            _projectDir = appEnv.ApplicationBasePath;
            _rootNamespace = appEnv.ApplicationName;
            
            var assemblyName = new AssemblyName(appEnv.ApplicationName);
            var assembly = Assembly.Load(assemblyName);
            
            _migrationTool = new MigrationTool(assembly);
            _libraryManager = libraryManager;
        }
        
        public virtual int Main(String[] args)
        {
            _app = new CommandLineApplication { Name = "ef" };
            _app.VersionOption(
                "-v|--version",
                typeof(Program).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion);
            _app.HelpOption("-h|--help");
            _app.Command(
                "context",
                context =>
                {
                    context.Description = "Commands to manage your DbContext";
                    context.HelpOption("-h|--help");
                    context.Command(
                        "list",
                        list =>
                        {
                            list.Description = "List the contexts";
                            list.HelpOption("-h|--help");
                            list.OnExecute(() => ListContexts());
                        },
                        addHelpCommand: false);
                    context.OnExecute(() => ShowHelp(context.Name));
                },
                addHelpCommand: false);
            _app.Command(
                "migration",
                migration =>
                {
                    migration.Description = "Commands to manage your Code First Migrations";
                    migration.HelpOption("-h|--help");
                    migration.Command(
                        "add",
                        add =>
                        {
                            add.Description = "Add a new migration";
                            var name = add.Argument("[name]", "The name of the migration");
                            var context = add.Option(
                                "-c|--context <context>",
                                "The context class to use",
                                CommandOptionType.SingleValue);
                            var startupProject = add.Option(
                                "-s|--startupProjectName <projectName>",
                                "The name of the project to use as the startup project",
                                CommandOptionType.SingleValue);
                            add.HelpOption("-h|--help");
                            add.OnExecute(() => AddMigration(name.Value, context.Value(), startupProject.Value()));
                        },
                        addHelpCommand: false);
                    migration.Command(
                        "apply",
                        apply =>
                        {
                            apply.Description = "Apply migrations to the database";
                            var migrationName = apply.Argument("[migration]", "The migration to apply");
                            var context = apply.Option(
                                "-c|--context <context>",
                                "The context class to use",
                                CommandOptionType.SingleValue);
                            var startupProject = apply.Option(
                                "-s|--startupProjectName <projectName>",
                                "The name of the project to use as the startup project",
                                CommandOptionType.SingleValue);
                            apply.HelpOption("-h|--help");
                            apply.OnExecute(() => ApplyMigration(migrationName.Value, context.Value(), startupProject.Value()));
                        },
                        addHelpCommand: false);
                    migration.Command(
                        "list",
                        list =>
                        {
                            list.Description = "List the migrations";
                            var context = list.Option(
                                "-c|--context <context>",
                                "The context class to use",
                                CommandOptionType.SingleValue);
                            list.HelpOption("-h|--help");
                            list.OnExecute(() => ListMigrations(context.Value()));
                        },
                        addHelpCommand: false);
                    migration.Command(
                        "script",
                        script =>
                        {
                            script.Description = "Generate a SQL script from migrations";
                            var from = script.Argument("[from]", "The starting migration");
                            var to = script.Argument("[to]", "The ending migration");
                            var output = script.Option(
                                "-o|--output <file>",
                                "The file to write the script to instead of stdout",
                                CommandOptionType.SingleValue);
                            var idempotent = script.Option(
                                "-i|--idempotent",
                                "Generate an idempotent script",
                                CommandOptionType.NoValue);
                            var context = script.Option(
                                "-c|--context <context>",
                                "The context class to use",
                                CommandOptionType.SingleValue);
                            var startupProject = script.Option(
                                "-s|--startupProjectName <projectName>",
                                "The name of the project to use as the startup project",
                                CommandOptionType.SingleValue);
                            script.HelpOption("-h|--help");
                            script.OnExecute(() => ScriptMigration(from.Value, to.Value, output.Value(), idempotent.HasValue(), context.Value(), startupProject.Value()));
                        },
                        addHelpCommand: false);
                    migration.Command(
                        "remove",
                        remove =>
                        {
                            remove.Description = "Remove the last migration";
                            var context = remove.Option(
                                "-c|--context <context>",
                                "The context class to use",
                                CommandOptionType.SingleValue);
                            remove.HelpOption("-h|--help");
                            remove.OnExecute(() => RemoveMigration(context.Value()));
                        },
                        addHelpCommand: false);
                    migration.OnExecute(() => ShowHelp(migration.Name));
                },
                addHelpCommand: false);
            _app.Command(
                "help",
                help =>
                {
                    help.Description = "Show help information";
                    var command = help.Argument("[command]", "Command that help information explains");
                    help.OnExecute(() => ShowHelp(command.Value));
                },
                addHelpCommand: false);
            _app.OnExecute(() => ShowHelp(command: null));

            return _app.Execute(args);
        }
        
        private int ListContexts()
        {
            var contexts = _migrationTool.GetContextTypes();
            var any = false;
            foreach (var context in contexts)
            {
                // TODO: Show simple names
                Console.WriteLine(context.FullName);
                any = true;
            }

            if (!any)
            {
                Console.WriteLine("No DbContext was found.");
            }

            return 0;
        }
        
        public virtual int AddMigration(String name, String context, String startupProject)
        {
            if (string.IsNullOrEmpty(name))
            {
                _app.Commands.Single(c => c.Name == "migration").ShowHelp("add");

                return 1;
            }

            return ExecuteInDirectory(
                startupProject,
                () =>
                {
                    _migrationTool.AddMigration(name, context, _rootNamespace, _projectDir);

                    return 0;
                });
        }

        public virtual int ApplyMigration(String migration, String context, String startupProject)
        {
            return ExecuteInDirectory(
                startupProject,
                () =>
                {
                    _migrationTool.ApplyMigration(migration, context);

                    return 0;
                });
        }

        public virtual int ListMigrations(String context)
        {
            var migrations = _migrationTool.GetMigrations(context);
            var any = false;
            foreach (var migrationInfo in migrations)
            {
                Console.Write(migrationInfo.Id);
                
                if (migrationInfo.InDatabase)
                {
                    Console.Write(" [Database]");
                }
                
                if (migrationInfo.InProject)
                {
                    Console.Write(" [Project]");
                }
                
                Console.WriteLine();
                
                any = true;
            }

            if (!any)
            {
                Console.WriteLine("No migrations were found.");
            }

            return 0;
        }

        public virtual int ScriptMigration(
            String from,
            String to,
            String output,
            bool idempotent,
            String context,
            String startupProject)
        {
            return ExecuteInDirectory(
                startupProject,
                () =>
                {
                    var sql = _migrationTool.ScriptMigration(from, to, idempotent, context);

                    if (string.IsNullOrEmpty(output))
                    {
                        Console.WriteLine(sql);
                    }
                    else
                    {
                        File.WriteAllText(output, sql);
                    }

                    return 0;
                });
        }

        public virtual int RemoveMigration(String context)
        {
            _migrationTool.RemoveMigration(context, _rootNamespace, _projectDir);

            return 0;
        }
        
        private int ShowHelp(String command)
        {
            _app.ShowHelp(command);
            
            return 0;
        }
        
        private int ExecuteInDirectory(string startupProject, Func<int> invoke)
        {
            var returnDirectory = Directory.GetCurrentDirectory();
            try
            {
                var startupProjectDir = GetProjectPath(startupProject);
                if (startupProjectDir != null)
                {
                    Console.WriteLine("Executing in startup Directory: {0}", startupProjectDir);
                    Directory.SetCurrentDirectory(startupProjectDir);
                }

                return invoke.Invoke();
            }
            finally
            {
                Directory.SetCurrentDirectory(returnDirectory);
            }
        }

        private string GetProjectPath(string projectName)
        {
            if (projectName == null)
            {
                return null;
            }

            string projectDir = null;
            var library = _libraryManager.GetLibraryInformation(projectName);
            var libraryPath = library.Path;
            if (library.Type == "Project")
            {
                projectDir = Path.GetDirectoryName(libraryPath);
            }

            return projectDir;
        }
    }
}