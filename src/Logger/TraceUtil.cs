using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public static class TraceUtil
{
    public static bool isLogEnable { get; set; }

    public static void WriteLine(string message)
    {
        if (isLogEnable == false) return;
        Trace.WriteLine(message);
    }

    public static void Warning(string message)
    {
        if (isLogEnable == false) return;
        Trace.TraceWarning("warning : " + message);
    }

    public static void Assert(bool comparison, string? msg = null)
    {
        if (isLogEnable == false) return;
        Trace.Assert(comparison, msg);
    }

    public static void Error(string message)
    {
        if (isLogEnable == false) return;
        Trace.TraceError("error : " + message);
    }

    public static void Fail(Exception ex, string? message = null)
    {
        if (isLogEnable == false) return;
        if (!string.IsNullOrEmpty(message)) Trace.Fail(message);
        Trace.Fail(LogUtil.ExceptionDetails(ex));
    }
}