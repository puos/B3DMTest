using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;


/// <summary>
/// LogService class
/// </summary>
public class LogService
{
	public static bool enableLog;
	
	private static List<ILogger> loggerList = new List<ILogger>();

	static bool _hasFileLogger = false;

    public static string logPath = AppContext.BaseDirectory + "/logs";
	
	public static void AddFileLogger()
	{
		if(!_hasFileLogger)
        {
            loggerList.Add(new FileLogger(logPath));
            _hasFileLogger = true;
        } 
	}

	public static int LogTagMask { get; set; }

	public static bool HasLogTagMask(LogTag logTag)
	{
		return (LogTagMask & logTag.Flag) != 0;
	}

	// 사용예) 
	// LogService.GetLogger(System.Type.GetType("SomeLogger"))
	//		.Log("TAG", logString);
	//-----------------------------------------------------------------------------------------
	public static ILogger GetLogger(System.Type type)
	{
		foreach (var logger in loggerList)
		{
			if (logger.GetType() == type)
				return logger;
		}
		return null;
	}

	public static void AddLogger(ILogger logger)
	{
		if (logger == null)
			return;

		foreach (var l in loggerList)
		{
			if (l.GetType() == logger.GetType())
				return;
		}

		loggerList.Add(logger);
	}

	public static void RemoveLogger(ILogger logger)
	{
		if (logger == null)
			return;

		foreach (var l in loggerList)
		{
			if (l.GetType() == logger.GetType())
			{
				loggerList.Remove(logger);
				return;
			}
		}
	}

	public static void Log(LogTag tag, string logString, int colorTag = -1)
	{
		if (!enableLog)
			return;

		if (loggerList == null)
			return;

		foreach (var logger in loggerList)
		{
			logger.Log(LogType.Log, tag, logString, colorTag);
		}
	}

	public static void LogWarning(LogTag tag, string logString, int colorTag = -1)
	{
		if (!enableLog)
			return;

		if (loggerList == null)
			return;

		foreach (var logger in loggerList)
		{
			logger.Log(LogType.Warning, tag, logString, colorTag);
		}
	}

	public static void LogError(LogTag tag, string logString, int colorTag = -1)
	{
		if (!enableLog)
			return;

		if (loggerList == null)
			return;

		foreach (var logger in loggerList)
		{
			logger.Log(LogType.Error, tag, logString, colorTag);
		}
	}

	public static void DumpLog(LogType type, string msg)
	{
		int year = DateTime.Now.Year;
		int month = DateTime.Now.Month;
		int day = DateTime.Now.Day;

		string logFilePath = AppContext.BaseDirectory + "/dump/" + year.ToString() + "_" + month.ToString() + "_" + day.ToString();
		if (Directory.Exists(logFilePath) == false)
			Directory.CreateDirectory(logFilePath);

		logFilePath += "/dump.txt";
		using (StreamWriter w = File.AppendText(logFilePath))
		{
			w.WriteLine("\r\nDump : {0}", DateTime.Now.ToLongTimeString());
			switch (type)
			{
				case LogType.Log:
					w.WriteLine(" i : {0}", msg); break;
				case LogType.Warning:
					w.WriteLine(" w : {0}", msg); break;
				case LogType.Error:
				case LogType.Exception:
				case LogType.Assert:
					w.WriteLine(" e : {0}", msg);
					w.WriteLine("  : {0}", GetStackTrace());
					w.WriteLine("-------------------------------");
					break;
			}
		}
	}

	public static string GetStackTrace()
	{
        string stackTrace = Environment.StackTrace;
		return stackTrace.ToString();
	}
}
