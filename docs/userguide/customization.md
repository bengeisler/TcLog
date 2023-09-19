# Customization
TcLog is design to be easily customizable. This page shows how to customize the logger to your needs.

## Custom logging templates
If one wants to record sensor data instead of the standard logs, for example, this is possible. The easiest way to do this is to program a wrapper around `TcLog` that enforces the specific template. 

### Example: Logging of sensor data

Suppose we want to record sensor data in `REAL` format. The data is to be saved in a csv file that has the following format:

`hh:mm:ss;device designation;value;unit`  

And the output should look like this:

`10:33:15;+CC1-B31;35.1;°C`

### Wrapper around `TcLog`

As wrapper we use an function block that encapsulates `TcLog` and enforces the data input with the help of the inputs. Furthermore it implements the interface `ILog` which establishes the link between logger and base logger. 

```st
FUNCTION_BLOCK UserLog IMPLEMENTS ILog
VAR_INPUT
  Condition: BOOL;
  Identification: STRING;
  Value: REAL;
  Unit: STRING;
END_VAR
VAR
  _getTimeData: DateTime;
  _timestamp: STRING;
END_VAR
VAR_STAT
  _logger: TcLog; 
END_VAR

_getTimeData();
_timestamp := _getTimeData.ToString('hh:mm:ss');

_logger
  .OnCondition(Condition)
  .AppendString(_timestamp)
  .AppendString(';')
  .AppendString(Identification)
  .AppendString(';')
  .AppendAny(Value)
  .AppendString(';')
  .AppendString(Unit)
  .ToCustomFormat('');
```

We can use the helper function `GenerateTimeData`, which returns the current date and time formatted via the `.ToString(Format)` method. With its help we generate the timestamp of the sensor data. 

The `.ToCustomFormat('')` method at the end of the chain causes the message to be logged unchanged. No additional information like further timestamps or instance path will be appended. 

### The interface `ILog`

The interface is implemented by passing the logger reference to the `TcLog` instance:

```st
METHOD SetLogger : BOOL
VAR_INPUT
  ref2Core : REFERENCE TO TcLogCore;
END_VAR

_logger.SetLogger(ref2Core);
```

### Calling the wrapper

Somewhere in our program `TcLogCore` is called cyclically. If there is more than one instance of it, we can tell our logger which instance we want via `.SetLogger(Instance)`. Otherwise the configuration of the logger singleton is used. 

```st
VAR
  _newLogger: TcLogCore;
  _rTrigLog : R_TRIG;
  _log : BOOL;
  _myLog : UserLog;
  _myValue: REAL := 1.0;
  _myValue2: REAL := 2.0;
END_VAR

_newLogger
  .MinimumLevel(LogLevels.Information)
  .SetRollingInterval(RollingIntervals.Hourly)
  .WriteToFile('c:\logs\', 'sensor.csv')
  .DeleteLogFilesAfterDays(1)
  .RunLogger();
  
_myLog.SetLogger(_newLogger);
_rTrigLog(CLK := _log);

_myLog(
  Condition := _rTrigLog.Q,
    Identification := '+CC1-B31',
  Value := _myValue,
  Unit := '°C');
  
_myLog(
  Condition := _rTrigLog.Q,
  Identification := '+CC1-B32',
  Value := _myValue2,
  Unit := '°C');
```

As soon as logging is triggered via `_log`, the csv file and the entries in it are created:

![Custom logging](https://benediktgeisler.de/CustomLogging.png "Custom logging")

## Use of custom loggers
`TcLogCore` implements the `ILogCore` interface which defines the `LogCustomFormat` and `LogStandardFormat` methods. 
A custom logger with for example other log sinks can be created in two ways:
1. create a new FB that inherits from `TcLogCore`. Thereby the new FB can be extended by additional functions and at the same time brings along all methods that `TcLogCore` has.
2. create a new FB that implements the `ILogCore` interface. This way the logger can be rewritten from scratch. The interface ensures that the existing instances of `TcLog` in the code can still be used. 