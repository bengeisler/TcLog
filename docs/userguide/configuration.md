# Configuration
`TcLogCore`  is used to build the logging configuration and to run the persistence mechanism. 

## Message format
The message format can be adapted with several methods of `TcLogCore` that are described in the following.

### Delimiter
`TcLogCore` can be configured to use an arbitrary delimiter between the components of the log entry with `.SetDelimiter('|')`. 

### Including the instance path in the log message
TcLog offers with `.IncludeInstancePath()` the possibility to include the location where the message was triggered into the message text:

```st
_coreLogger
  .WriteToAds()
  .IncludeInstancePath()
  .MinimumLevel(LogLevels.Warning)
  .RunLogger();
  
_logger.Error('This is an error message.');
```

![Including the instance path](https://benediktgeisler.de/InstancePath.png "Including the instance path")

## Log to ADS output
When adding the method `.WriteToAds()` to `TcLogCore`, the log messages are sent to the ADS output:

```st
_coreLogger
    .WriteToAds()
    .RunLogger();
```

## Log to file system
TcLog brings the option to store logs in the file system in the form of text files. This option can be applied to `TcLogCore` via the method `.WriteToFile(path, filename)`: 

```st
_coreLogger
  .IncludeInstancePath()
  .MinimumLevel(LogLevels.Warning)
  .WriteToFile('c:\logs\', 'test.txt')
  .RunLogger();
  
_loggerTrig
  .OnRisingEdge(_log)
  .Error('rTrig Test');
```

![Logging to the file system](https://benediktgeisler.de/LogMessageInFiileSystem.png "Logging to the file system")

### Timestamp
The file name is additionally prefixed with the creation date of the log file. The format of the date can be defined arbitrarily by means of a format string. Example: 

*YYMMDD-hh:mm:ss:iii* 

> [!IMPORTANT] 
> Upper and lower case must be maintained, furthermore the same letters must always be placed one after the other. Blocks of identical letters are not permitted: ~~*YYMMDD-YYYY*~~

This format is passed to `TcLogCore` via the method `.TimestampFormat('YYMMDD-hh:mm:ss:iii')`. 

## Minimum log level
With the method `.MinimumLevel(level)` of `TcLogCore` the minimum log level can be specified. All messages with a lower log level are ignored.

TcLog supports the following log levels:
- `LogLevels.Debug`
- `LogLevels.Information`
- `LogLevels.Warning`
- `LogLevels.Error`
- `LogLevels.Fatal`


## Rolling interval
A *rolling interval* denotes the interval until a new log file is created. TcLog offers the possibility to create a new logfile in regular intervals. This *rolling interval* is specified to `TcLogCore` via `SetRollingInterval(..)`:
- `RollingIntervals.None`: Do not create a new log file.
- `RollingIntervals.Hourly`: Create a new log file every hour
- `RollingIntervals.Daily`: Create a new log file daily
- `RollingIntervals.Monthly`: Create a new log file every month.

The log file is only created when a message is triggered. 

## Delete old log files
To get rid of old log files, a lifespan of logs can be set with help of the method `DeleteLogsAfterDays(days)` of `TcLogCore`. Log files whose lifespan exceed the specified limit will automatically be deleted at midnight.

## Starting the logger
After the configuration is complete, the logger is started with the method `RunLogger()` of `TcLogCore`. 

## Using different verbosity levels
Different scenarios may require different logging strategies. For example, you may want to log all messages with a log level of `Error` or higher in production, but all messages with a log level of `Debug` or higher in development. You can achieve this like this:

```st
VAR
    _coreLogger : TcLogCore(bufferSize := 100 * SIZEOF(BYTE) * MAX_STRINGLENGTH);
    _logger : TcLog;
    _isDevelopment : BOOL := TRUE;
END_VAR

_coreLogger
    .WriteToAds()
    .WriteToFile('c:\logs\', 'sensor_data.txt');

IF _isDevelopment THEN
    _coreLogger.MinimumLevel(LogLevels.Debug);
ELSE
    _coreLogger.MinimumLevel(LogLevels.Error);
END_IF

_coreLogger.RunLogger();

_logger.Debug('This is a debug message.');
_logger.Error('This is an error message.');
```
