# bt_sql_backup_service

A quick and dirty Windows Service replacement for [SQL Server Agent](https://en.wikipedia.org/wiki/SQL_Server_Agent).

I built this to monitor and automate database backups to local disk and cloud
storage for on-premise servers running SQL Server Express edition, which doesn't have SQL Server Agent.

There are
[other](https://www.mssqltips.com/sqlservertip/5830/how-to-schedule-sql-scripts-on-sql-server-express-edition/)
solutions out there but they seem very
[limited](https://stackoverflow.com/questions/7201061/how-to-create-jobs-in-sql-server-express-edition)
(no reporting, no notification, no auditing, no integration and not to mention,
very hard to maintain).

I plan to gradually add new functionality to this service over time. Check the TODO list below for more info.

## How to Build, Install and Use

1. Clone this repository using Git or download a zipped version from GitHub.
1. Install the [.Net Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) if you don't have it already.
1. Open a command line and navigate to the folder where the source code is and run:

   ```sh
   $dotnet publish -r win-x64 -c Release
   ```

   If you don't like publishing the app with so many DLLs and other files in the
   same folder, you can try publishing as a single file, which will zip all the
   dependencies together in the EXE and unzip it to a temp folder at runtime
   (initial startup time will be slower):

   ```sh
   $dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true
   ```

1. Move the generated EXE (and optional dependencies) to your folder of preference. There is no installer yet.
1. Configure the SQL jobs by editing `sql_commands.json`.
1. Install and start the service by running

   ```sh
   $bt_sql_backup_service.exe install
   $bt_sql_backup_service.exe start
   ```

1. Check Windows Event Viewer for log messages from the service or any errors.
   You can also run the service executable from the command line and see log
   messages outputted directly to the console by just calling the executable
   with no arguments.

More command line arguments are available. Read the
[Topshelf](http://topshelf-project.com/) project documentation.

## The `sql_commands.json` file

This file contains a list of `SchedulableSqlCommands`. The service will read this file on startup and load any commands defined there as jobs to schedule and execute.

An example file is provided which looks like this:

```json
[
  {
    "name": "Full backup of user databases",
    "description": "Performs full backups to local disk, at 3:00 AM, for all user databases, using Ola Hallengren's DatabaseBackup procedure. After it's finished, it deletes backups older than 24h.",
    "connectionString": "Server=127.0.0.1,1443;Connection Timeout=10;Database=master;User Id=USERNAME;Password=PASSWORD",
    "sqlCommand": "EXECUTE [master].[dbo].[DatabaseBackup] @Databases = 'USER_DATABASES', @Directory = N'D:\\sqlbackups', @BackupType = 'FULL', @CleanupTime = 24, @CopyOnly = 'N', @MaxTransferSize = 4194304, @BufferCount = 25, @Compress = 'N', @CheckSum = 'Y', @Verify = 'N', @LogToTable = 'Y';",
    "commandTimeout": 10800,
    "cron": "0 0 3 ? * * *"
  },
  {
    "name": "Diff backup of user databases",
    "description": "Performs diff backups to local disk, every 2 hours from 10 AM to 10 PM, for all user databases, using Ola Hallengren's DatabaseBackup procedure. After it's finished, it deletes backups older than 24h.",
    "connectionString": "Server=127.0.0.1,1443;Connection Timeout=10;Database=master;User Id=USERNAME;Password=PASSWORD",
    "sqlCommand": "EXECUTE [master].[dbo].[DatabaseBackup] @Databases = 'USER_DATABASES', @Directory = N'D:\\sqlbackups', @BackupType = 'DIFF', @CleanupTime = 24, @CopyOnly = 'N', @MaxTransferSize = 4194304, @BufferCount = 25, @Compress = 'N', @CheckSum = 'Y', @Verify = 'N', @LogToTable = 'Y';",
    "commandTimeout": 10800,
    "cron": "0 0 10,12,14,16,18,20 ? * * *"
  }
]
```

This file assumes you have [Ola's `DatabaseBackup`](https://ola.hallengren.com/sql-server-backup.html) script installed on the `master` database and a SQL server installed and running on the same machine as the service itself. It assumes a custom SQL Server TCP port value of 1443.

What this configuration essentially does is schedule a job for a full backup of all user databases everyday at 3 AM and a job for differential backups of all user databases every 2 hours starting from 10 AM up until 8 PM including (assuming those are working hours, when the database gets new data added).

## TODO

- [x] Run SQL scripts on a CRON schedule.
- [ ] Email notifications.
- [ ] Option to backup to Azure Blob Storage directly (as opposed to SQL Server doing that).
- [ ] Option to sync local backups to Azure Blob Storage or Azure File Shares in
      order to preserve backup LSN chain and not have to run the same backup
      twice.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to
discuss what you would like to change.

Please make sure to update tests as appropriate.

## License

[MIT](https://choosealicense.com/licenses/mit/)
