using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System;
using System.Text;
using System.IO;

public enum LogType
{
    Error,
    Assert,
    Warning,
    Log,
    Exception
}

/// <summary>
/// ILogger interface
/// </summary>
public interface ILogger
{
    void Log(LogType type, LogTag tag, string logString, int colorTag = -1);

    void OnUpdate(float deltaTime);
}

public class LogTag
{
    public int Flag;
    public string Name;
    public LogTag(int flag, string name)
    {
        this.Flag = flag;
        this.Name = name;
    }
}
