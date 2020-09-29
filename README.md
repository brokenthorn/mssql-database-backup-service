# bt_sql_backup_service

**bt_sql_backup_service** is a Windows system service for running scheduled database backup
jobs on editions of SQL Server that do not include a SQL Server Agent.

## Installation

1. Install the dotnet core SDK.
1. Build and publish. For example:

   ```sh
   $dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true
   ```

1. Move the generate EXE to your folder of preference.

## Usage

1. Run `bt_sql_backup_service.exe install` as admin to install the service.
1. Run `bt_sql_backup_service.exe start` as admin to start the service now.
1. Check `C:\temp` for logs.

More command line arguments are available by reading the
[Topshelf](http://topshelf-project.com/) documentation.

## TODO

- [ ] Better runtime configuration.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.

## License

[MIT](https://choosealicense.com/licenses/mit/)
