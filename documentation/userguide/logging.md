# Logging
## Flexible logging
TcLog implements a [StringBuilder](https://www.plccoder.com/fluent-code/) which makes it easy to build your own message text: 

```
VAR
    _logger: TcLog;
  _myInt : INT := 10;
  _myVarInfo : __SYSTEM.VAR_INFO := __VARINFO(_myInt);
END_VAR

_logger
  .AppendString('Let´s log some values: ')
  .AppendAny(_myInt)
  .AppendString(' - or some symbols: ')
  .AppendVariable(_myVarInfo, _myInt)
  .Error(''); 
```
![Using a StringBuilder to generate the message text](https://benediktgeisler.de/StringBuilder_in_message_text.png "Using a StringBuilder to generate the message text")

Thus any amount of information can be appended to the message without having to implement `TcLog` with a large number of input parameters, since TwinCAT (at least in version before build 4026.0) does not allow optional input parameters. 

The methods `AppendString`, `AppendAny` and `AppendVariable` append the passed in data to the message text. 

The methods `Debug`, `Info`, `Warning`, `Error` and `Fatal` log the message with the respective log level. If you only want to log a simple string, you can pass it directly to the respective method, e.g. `_logger.Debug('This is a debug message.')`.

## Conditional logging
The most common use of logging will be in the form `IF ... THEN log() END_IF`. Therefore this query is already integrated in TcLog:

```
VAR
    _logger: TcLog;
  _triggerLogging : R_TRIG;
  _log : BOOL;
END_VAR

_triggerLogging(CLK := _log);
_logger
  .OnCondition(_triggerLogging.Q)
  .Error('Only logs when OnCondition evaluates to TRUE.');  
```

## Logging on rising/falling edges
Since a log message is usually to be sent once in the event of a *status change*, TcLog also provides a block for this purpose: `TcLogTrig`. In contrast to `TcLog`, a separate instance must be created for each use of this block, since the edge state is stored internally. The conditional execution can thus be further simplified:

```
VAR
  _loggerTrig : TcLogTRIG;
    _log : BOOL;
END_VAR

_loggerTrig
  .OnRisingEdge(_log)
  .Error('rTrig Test');
```

Likewise, logging can be triggered on falling edges with `OnFallingEdge(cond)`. 

## Use of multiple loggers
Even though the logger was primarily designed as a singleton, it is possible to use multiple loggers. For example, sensor data can be collected cyclically and stored in a separate log file. To add another logger, an instance of `TcLogCore` must be created. This is then bound to the desired `TcLog` instance:

```
VAR
  _newLogger: TcLogCore;
  _logger: TcLog;
  _myInt : INT := 10;
END_VAR

_newLogger
  .MinimumLevel(LogLevels.Information)
  .SetRollingInterval(RollingIntervals.Hourly)
  .WriteToFile('c:\logs\', 'sensor_data.txt')
  .DeleteLogFilesAfterDays(7)
  .RunLogger();
  
// Bind the new logger to the TcLog instance
_logger.SetLogger(_newLogger);

_logger.AppendString('Sensor xy: ')
  .AppendAny(_myInt)
  .Information(''); 
```

From now on `_logger` considers the configuration of `_newLogger`.