# Getting started
`TcLog` has two main building blocks:
- `TcLog`, which is used to log messages.  
- `TcLogCore`, which is the central static logger that takes care of processing the logged messages, such as sending them to ADS ouput or persisting them to the file system.

You would typically call `TcLogCore` once in your project and configure the logger behaviour. Then, you would use `TcLog` to log messages. Each call of `TcLog` then uses the same configuration specified with `TcLogCore`. While there is normally only one instance of `TcLogCore` in your project, you can create as many instances of `TcLog` as you like, typically one in each POU. Both `TcLog` and `TcLogCore` provide fluent interfaces to make configuration and log message creation as easy as possible.

## Example usage
Configure the core logger in your project:
```
VAR
	_coreLogger : TcLogCore(bufferSize := 100 * SIZEOF(BYTE) * MAX_STRINGLENGTH);
END_VAR

_coreLogger
	.WriteToAds()
	.WriteToFile('c:\logs\', 'sensor_data.txt')
	.MinimumLevel(LogLevels.Debug)
	.RunLogger();
```
Then, maybe in a different POU, use `TcLog` to log messages:
```
VAR
	_logger : TcLog;
END_VAR

_logger.Debug('This is a debug message.');	
_logger.Error('This is an error message.');		
```
This will log both messages to both the ADS output and the file system.

Next, see how to [configure TcLog](configuration.md) and how to [use TcLog in detail](logging.md).