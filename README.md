# EF6.Commands

EF6 code-first migrations on ASP.NET vNext!

### Install

https://www.nuget.org/packages/EF6.Commands/

```
{
  "dependencies": {
    "EF6.Commands": "1.1.0-*"
  },
  "commands": {
    "ef": "EF6.Commands"
  }
}
```

You will have to create a `DbMigrationConfiguration` class yourself. See the [test](test) directory for reference.

### Usage

Dump of CLI help info:

```
$ dnx . ef
 v1.1.0-beta

Usage: ef [options] [command]

Options:
  -v|--version  Show version information
  -h|--help     Show help information

Commands:
  context    Commands to manage your DbContext
  migration  Commands to manage your Code First Migrations
  help       Show help information

Use "ef help [command]" for more information about a command.
```

```
$ dnx . ef context
 v1.1.0-beta

Usage: ef context [options] [command]

Options:
  -h|--help  Show help information

Commands:
  list  List the contexts

Use "ef help [command]" for more information about a command.
```

```
$ dnx . ef migration
 v1.1.0-beta

Usage: ef migration [options] [command]

Options:
  -h|--help  Show help information

Commands:
  add     Add a new migration
  apply   Apply migrations to the database
  list    List the migrations
  script  Generate a SQL script from migrations
  remove  Remove the last migration (not implemented)

Use "ef help [command]" for more information about a command.
```
