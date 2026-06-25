using System.Collections;
using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SIMMETA;

public class SIMMETALogUtil : TraceListener
{
    public static bool IsConsoleLog { get; set; } = false;

    public static void Init(string appPath)
    {
         var logFolder = System.IO.Path.Combine(appPath, "logfolder");

        LogService.enableLog = true;
        LogService.logPath = logFolder;
        TraceUtil.isLogEnable = true;
        LogService.AddFileLogger();
        Trace.Listeners.Add(new SIMMETALogUtil());
    }

    public static void Close()
    {
        Trace.Flush();
        Trace.Close();
    }

    public override void Write(string message)
    {
        SIMMETADebug.Log(message);
        if (IsConsoleLog) Console.Write(message);
    }

    public override void WriteLine(string message)
    {
        SIMMETADebug.Log(message);
        if (IsConsoleLog) Console.WriteLine(message);
    }

    public static void HandleLog(string msg, string stackTrace, LogType type)
	{
        string value = msg;

        if (type == LogType.Exception)
        {
            value = msg + "_" + stackTrace;
         }

        SIMMETADebug.Log(value);
    }
}
