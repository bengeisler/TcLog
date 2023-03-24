This readme is a copy of the [blog post introducing this framework](https://benediktgeisler.de/en/blog/tclog/).

*Logging in TwinCAT with the on-board means is limited to the output as ADS event. The TcLog library presented here enables flexible logging to the file system.*

## Logging in TwinCAT
From time to time it happens that I need a log function in TwinCAT to find sporadic errors or to record user interactions. TwinCAT provides a logging facility in the standard library: [AdsLogStr](https://infosys.beckhoff.com/index.php?content=../content/1031/tcplclibsystem/html/TcPlcLibSys_ADSLOGSTR.htm&id=). This function, which is available for the data types `LREAL`, `DINT` and `STRING`, allows ADS messages to be output as a text box on the screen and to the ADS console. A mask that is passed to the block can be used to configure which log level and which destination (console or text box) the message has. 

And that's pretty much all you can do in TwinCAT when it comes to logging.

Therefore I started the open source project **TcLog**. TcLog is a logging framework that can be integrated as a library in TwinCAT. It allows a flexible configuration of the logs as well as the specification of different log targets. 

The source code and the precompiled library is available at [GitHub](https://github.com/bengeisler/TcLog).


## TcLog: Flexible logging framework
In order not to reinvent the wheel, TcLog is based on existing logging solutions like [Serilog](https://github.com/serilog/serilog). Unlike Serilog, TcLog does *not* support [structured logging](https://messagetemplates.org). All log messages are converted directly to *strings*. 

TcLog provides a central static logger `TcLogCore` which can be configured via a *fluent interface*: 

```
VAR
	CoreLogger : TcLogCore;
END_VAR

CoreLogger
	.WriteToAds()
	.MinimumLevel(E_LogLevel.Warning)
	.RunLogger();
```

It is used via a second block 'TcLog' with which the messages are then triggered. 

```
VAR
	Logger : TcLog;
END_VAR

Logger.Debug('This is a debug message.');	
Logger.Error('This is an error message.');		
```

![Raising an error message](https://benediktgeisler.de/Error_message.png "Raising an error message")

The first message was triggered with log level *Debug*, but the minimum threshold was set to *Warning*, therefore only the second message is displayed. `TcLog` provides the following message levels: 
- `E_LogLevel.Debug`
- `E_LogLevel.Information`
- `E_LogLevel.Warning`
- `E_LogLevel.Error`
- `E_LogLevel.Fatal`

### Static binding of `TcLog` to `TcLogCore`
All instances of `TcLog` occurring in the program are statically bound to the one instance of `TcLogCore` which provides the configuration of the logger. This instance must be called cyclically. 

This behavior is known as the [Singleton](https://cidesi.repositorioinstitucional.mx/jspui/bitstream/1024/170/1/M-ARNC-2017.pdf) design pattern. Meanwhile it is seen rather critically, since it limits the testability of software. For the Singleton speaks however that it possesses a small overhead. Once the configuration of the logger in `MAIN` is set up, logging can be triggered anywhere in the PLC program by `TcLog`. Due to the Singleton the configuration of the central static logger will automatically be used. Since simplicity is the primary goal of this library, the advantages of the Singleton prevail. 

### Variable design of the message text

TcLog implementiert einen [StringBuilder](https://www.plccoder.com/fluent-code/) und daher lässt sich der Meldungstext flexibel zusammensetzen: 

```
VAR
	myInt : INT := 10;
	myVarInfo : __SYSTEM.VAR_INFO := __VARINFO(myInt);
END_VAR

Logger
	.AppendString('Let´s log some values: ')
	.AppendAny(myInt)
	.AppendString(' - or some symbols: ')
	.AppendVariable(myVarInfo, myInt)
	.Error('');	
```

![Using a StringBuilder to generate the message text](https://benediktgeisler.de/StringBuilder_in_message_text.png "Using a StringBuilder to generate the message text")

Thus any amount of information can be appended to the message without having to implement `TcLog` with a large number of input parameters, since TwinCAT does not allow optional input parameters. 

The use of a *fluent interface* brings another advantage: future changes to the block provide new functionality via new methods, not via new input parameters. This means that existing code does not have to be adapted.

### Including the instance path in the log message
TcLog offers with `.IncludeInstancePath()` the possibility to include the location where the message was triggered into the message text:

```
CoreLogger
	.WriteToAds()
	.IncludeInstancePath()
	.MinimumLevel(E_LogLevel.Warning)
	.RunLogger();
	
Logger.Error('This is an error message.');
```

![Including the instance path](https://benediktgeisler.de/InstancePath.png "Including the instance path")

### Conditional logging
The most common use of logging will be in the form `IF ... THEN log() END_IF`. Therefore this query is already integrated in TcLog:

```
VAR
	rTrigLog : R_TRIG;
	bLog : BOOL;
END_VAR

rTrigLog(CLK := bLog);
Logger
	.OnCondition(rTrigLog.Q)
	.Error('Only logs when OnCondition evaluates to TRUE.');	
```

### Logging on rising/falling edges
Since a log message is usually to be sent once in the event of a *status change*, TcLog also provides a block for this purpose: `TcLogTRIG`. In contrast to `TcLog`, a separate instance must be created for each use of this block, since the edge state is stored internally. The conditional execution can thus be further simplified:

```
VAR
	rTrigLogger : TcLogTRIG;
END_VAR

rTrigLogger
	.OnRisingEdge(bLog)
	.Error('rTrig Test');
```

Likewise, logging can be triggered on falling edges with `OnFallingEdge(cond)`. 

## Persist logs to the file system
The features shown so far are a flexible wrapper for `AdsLogStr`, but alone do not justify a new framework. TcLog therefore brings the option to store logs in the file system in the form of text files. This option can be applied to `TcLogCore` via the method `.WriteToFile(path, filename)`: 

```
fbCoreLogger
	.IncludeInstancePath()
	.MinimumLevel(E_LogLevel.Warning)
	.WriteToFile('c:\logs\', 'test.txt')
	.RunLogger();
	
rTrigLogger
	.OnRisingEdge(bLog)
	.Error('rTrig Test');
```

![Logging to the file system](https://benediktgeisler.de/LogMessageInFiileSystem.png "Logging to the file system")

The file name is additionally prefixed with the creation date of the log file. The format of the date can be defined arbitrarily by means of a format string. Example: 

*YYMMDD-hh:mm:ss:iii* 

**Important**: Upper and lower case must be maintained, furthermore the same letters must always be placed one after the other. Blocks of identical letters are not permitted: ~~*YYMMDD-YYYY*~~

This format is passed to `TcLogCore` via the method `.TimestampFormat('YYMMDD-hh:mm:ss:iii')`. 

Since [TwinCAT can only write to the local file system](https://alltwincat.com/2019/11/11/logging-of-files-to-a-network-drive/), this restriction also applies to TcLog. 

### Custom delimiters
`TcLogCore` can be configured to use an arbitrary delimiter between the components of the log entry with `.SetDelimiter('|')`. 

### Set the rolling interval
A *rolling interval* denotes the interval until a new log file is created. TcLog offers the possibility to create a new logfile in regular intervals. This *rolling interval* is specified to `TcLogCore` via `SetRollingInterval(..)`:
- `E_RollingInterval.None`: Do not create a new log file.
- `E_RollingInterval.Hourly`: Create a new log file every hour
- `E_RollingInterval.Daily`: Create a new log file daily
- E_RollingInterval.Monthly`: Create a new log file every month.

The log file is only created when a message is triggered. 

### Delete old log files
Um keine Schwemme an veralteten Logs zu erzeugen kann eine Lebensdauer von Logs festgelegt werden. Die Methode `DeleteLogsAfterDays(days)` von `TcLogCore` konfiguriert diese. Logdateien, deren Lebenszeit überschritten wurde, werden automatisch um Mitternacht gelöscht. 

## Customizing the logging
### Use of multiple loggers
Even though the logger was primarily designed as a singleton, it is possible to use multiple loggers. For example, sensor data can be collected cyclically and stored in a separate log file. To add another logger, an instance of 'TcLogCore' must be created. This is then bound to the desired `TcLog` instance:

```
VAR
	newLogger: TcLogCore;
	Logger : TcLog;
	myInt : INT := 10;
END_VAR

newLogger
	.MinimumLevel(E_LogLevel.Information)
	.SetRollingInterval(E_RollingInterval.Hourly)
	.WriteToFile('c:\logs\', 'sensor_data.txt')
	.DeleteLogFilesAfterDays(7)
	.RunLogger();
	
Logger.SetLogger(newLogger);

Logger
	.AppendString('Sensor xy: ')
	.AppendAny(myInt)
	.Information('');	
```

From now on `Logger` considers the configuration of `newLogger`.

### Custom logging templates
If one wants to record sensor data instead of the standard logs, for example, this is possible. The easiest way to do this is to program a wrapper around `TcLog` that enforces the specific template. 

#### Example: Logging of sensor data

Suppose we want to record sensor data in `REAL` format. The data is to be saved in a csv file that has the following format:

`hh:mm:ss;Betriebsmittelkennzeichen;Wert;Einheit`  
`10:33:15;+CC1-B31;35.1;°C`

#### Wrapper around `TcLog`

As wrapper we use an FB that encapsulates `TcLog` and enforces the data input with the help of the inputs. Furthermore it implements the interface `ILog` which establishes the link between logger and base logger. 

```
FUNCTION_BLOCK UserLog IMPLEMENTS ILog
VAR_INPUT
	condition: BOOL;
	identification: STRING;
	value: REAL;
	unit: STRING;
END_VAR
VAR
	GetTimeData: GenerateTimeData;
	timestamp: STRING;
END_VAR
VAR_STAT
	Logger: TcLog;	
END_VAR

GetTimeData();
timestamp := GetTimeData.ToString('hh:mm:ss');

Logger
	.OnCondition(condition)
	.AppendString(timestamp)
	.AppendString(';')
	.AppendString(identification)
	.AppendString(';')
	.AppendAny(value)
	.AppendString(';')
	.AppendString(unit)
	.ToCustomFormat('');
```

We can use the helper function `GenerateTimeData`, which returns the current date and time formatted via the `.ToString(Format)` method. With its help we generate the timestamp of the sensor data. 

The `.ToCustomFormat('')` method at the end of the chain causes the message to be logged unchanged. No additional information like further timestamps or instance path will be appended. 

#### The interface `ILog`

The interface is implemented by passing the logger reference to the `TcLog` instance:

```
METHOD SetLogger : BOOL
VAR_INPUT
	Ref2Core : REFERENCE TO TcLogCore;
END_VAR

Logger.SetLogger(Ref2Core);
```

#### Calling the wrapper

For example in `MAIN` `TcLogCore` is called cyclically. If there is more than one instance of it, we can tell our logger which instance we want via `.SetLogger(Instance)`. Otherwise the configuration of the logger singleton is used. 

```
VAR
	newLogger: TcLogCore;
	rTrigLog : R_TRIG;
	bLog : BOOL;
	myLog : UserLog;
	myValue: REAL := 1.0;
	myValue2: REAL := 2.0;
END_VAR

newLogger
	.MinimumLevel(E_LogLevel.Information)
	.SetRollingInterval(E_RollingInterval.Hourly)
	.WriteToFile('c:\logs\', 'sensor.csv')
	.DeleteLogFilesAfterDays(1)
	.RunLogger();
	
myLog.SetLogger(newLogger);
rTrigLog(CLK := bLog);

myLog(
	condition := rTrigLog.Q,
	identification := '+CC1-B31',
	value := myValue,
	unit := '°C');
	
myLog(
	condition := rTrigLog.Q,
	identification := '+CC1-B32',
	value := myValue2,
	unit := '°C');
```

As soon as logging is triggered via `bLog`, the csv file and the entries in it are created:

![Custom logging](https://benediktgeisler.de/CustomLogging.png "Custom logging")

### Use of custom loggers
`TcLogCore` implements the `ILogCore` interface which defines the `LogCustomFormat` and `LogStandardFormat` methods. 
A custom logger with for example other log sinks can be created in two ways:
1. create a new FB that inherits from `TcLogCore`. Thereby the new FB can be extended by additional functions and at the same time brings along all methods that `TcLogCore` has.
2. create a new FB that implements the `ILogCore` interface. This way the logger can be rewritten from scratch. The interface ensures that the existing instances of `TcLog` in the code can still be used. 

## Error messages
`TcCoreLog` provides information about internal error messages via the `Error` property. 

```
VAR
  error: ST_Error;
END_VAR

error := newLogger.Error;
```

![ST_Error](https://benediktgeisler.de/ST_Error.png "ST_Error")

## Unit- and integration tests
The project on Github contains both unit ([TcUnit](https://tcunit.org)) and integration tests ([xUnit](https://xunit.net)). 

## Further ways of logging in TwinCAT
With [log4TC](https://mbc-engineering.github.io/log4TC/index.html) there is another logging option for TwinCAT.  This enables structured logging, but an additional Windows service must be installed, which communicates with the PLC library. `TcLog` on the other hand comes as a pure PLC library. 
The code for log4TC has been published as open source on [GitHub](https://github.com/mbc-engineering/log4TC/releases).
