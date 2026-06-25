using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System;
using System.Text;
using System.IO;


public class FileLogger : ILogger
{
    StreamWriter _writer;
    bool _hasError;

    const int SizeForAutoFlush = 4 * 1024 * 1024;
    const long CapacityForLogFileStorage = 100L * 1024 * 1024;
    int _accumSize = 0;
    float _lastFlushTime = 0;
    float _localTime = 0;
    string _currFileTitleName;
    int _reopenCnt = 0;

    public static List<FileLogger> list = new List<FileLogger>();

    string _logRootDir = "";

    public FileLogger(string logDir)
    {
        _logRootDir = logDir;

        list.Add(this);

        Directory.CreateDirectory(_logRootDir);

        DeleteOldLogFiles();

        DateTime currTime = DateTime.Now;
        string date = string.Format("{0}{1:00}{2:00}_{3:00}{4:00}{5:00}", currTime.Year, currTime.Month, currTime.Day, currTime.Hour, currTime.Minute, currTime.Second);

        _currFileTitleName = _logRootDir + "/log_" + date;

        try
        {
            _writer = File.CreateText(_currFileTitleName + ".txt");
            _writer.AutoFlush = true;
        }
        catch (Exception e)
        {
            SIMMETADebug.Error(e.Message);
        }
    }

    public void Close()
    {
        if (_writer != null)
            _writer.Close();
        _writer = null;
    }

    public void OnUpdate(float deltaTime)
    {
        _localTime += deltaTime;

        if (_lastFlushTime + 3f < _localTime)
            Flush();
    }

    public void Flush()
    {
        if (_writer != null)
            _writer.Flush();
        _accumSize = 0;
        _lastFlushTime = _localTime;
    }

    public static void FlushAllFileLoggers()
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null)
                list[i].Flush();
        }
    }

    public static void CloseAllFileLoggers()
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null)
                list[i].Close();
        }
    }

    public static void UpdateAllFileLoggers(float deltaTime)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null)
                list[i].OnUpdate(deltaTime);
        }
    }

    public void Log(LogType type, LogTag tag, string logString, int colorTag = -1)
    {
        if (_hasError)
            return;
        DateTime currTime = DateTime.Now;

        char logTypeC = (type == LogType.Log) ? 'I' : (type == LogType.Warning || type == LogType.Assert) ? 'W' : 'E';
        
        if (tag != null)
            logString = string.Format("{0:00}:{1:00}:{2:00} {3} [{4}] {5}\r\n", currTime.Hour, currTime.Minute, currTime.Second, logTypeC, tag.Name, logString);
        else
            logString = string.Format("{0:00}:{1:00}:{2:00} {3} {4}\r\n", currTime.Hour, currTime.Minute, currTime.Second, logTypeC, logString);

        if (_writer != null && _WriteText(logString))
            return;

        if (_writer != null)
        {
            _writer.Dispose();
            _writer = null;
        }     
        
        try
        {
            _writer = new StreamWriter(_currFileTitleName + (_reopenCnt == 0 ? string.Empty : "_" + _reopenCnt) + ".txt", true);
        }
        catch (Exception e2)
        {
            SIMMETADebug.Error(e2.Message);
        }


        if (_writer == null)
        {
            _reopenCnt++;
            try
            {
                _writer = File.CreateText(_currFileTitleName + (_reopenCnt == 0 ? string.Empty : "_" + _reopenCnt) + ".txt");
                _writer.AutoFlush = false;
            }
            catch (Exception e3)
            {
                SIMMETADebug.Error(e3.Message);
            }
        }
        

        if (_writer != null)
        {
            _WriteText(logString);
        }

        if (_writer == null)
            _hasError = true;
    }

    bool _WriteText(string text)
    {
        try
        {
            _writer.Write(text);

            _accumSize += text.Length;
            if (_accumSize > SizeForAutoFlush)
            {
                _accumSize = 0;
                _writer.Flush();
            }
        }
        catch (Exception e)
        {
            SIMMETADebug.Error(e.Message);
            return false;
        }
        return true;
    }

    void DeleteOldLogFiles()
    {
        DirectoryInfo dirInfo = new DirectoryInfo(_logRootDir);
        List<FileInfo> files = new List<FileInfo>(dirInfo.GetFiles());
        files.Sort((a, b) => { return a.CreationTime.CompareTo(b.CreationTime); });
        long totalSize = 0;
        for (int i = files.Count - 1; i >= 0; i--)
        {
            totalSize += files[i].Length;
            if (totalSize > CapacityForLogFileStorage)
                File.Delete(files[i].FullName);
        }
    }
}