using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;

namespace SIMMETA;

public enum ClientAccesType
{
    Offline = 1,
    Online = 2
}

public static class LasCustomUtil
{
    public static System.Diagnostics.Stopwatch Start(string name = "")
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        if (!string.IsNullOrEmpty(name))
        {
            TraceUtil.WriteLine(name);
        }
        sw.Start();
        return sw;
    }

    public static void CheckTime(System.Diagnostics.Stopwatch sw,string name,bool restart = true)
    {
        if (restart) sw.Stop();
        TraceUtil.WriteLine($"{name} : {sw.ElapsedMilliseconds * 0.001f}");
        if (restart) sw.Restart();
    }

    public static void SaveFile(string path, byte[] contents)
    {
        using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write))
        {
            fs.Write(contents, 0, contents.Length);
        }
    }
    

    public static string AppName = string.Empty;

    private static ClientAccesType loginAccessType = ClientAccesType.Offline; // 1 : offline, 2 : online
    private static DateTime expiredTime;

    public static string GetAppPath()
    {
        var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var outputFolder = Path.Combine(localPath, AppName);
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        return outputFolder;
    }

    public static string GetAppPath(params string[] folderNames)
    {
        var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(localPath, AppName);

        var allFolders = new List<string> { appFolder };
        allFolders.AddRange(folderNames);
        var outputFolder = Path.Combine(allFolders.ToArray());

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        return outputFolder;
    }

    public static string GetAppDataPath()
    {
        var localPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var outputFolder = Path.Combine(localPath, AppName);
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        return outputFolder;
    }

    public static string MakeFolder(string folder, string subFolder)
    {
        int count = 1;

        while (true)
        {
            try
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
                break;
            }
            catch
            {
                string outputFolder = Path.GetDirectoryName(folder);
                folder = System.IO.Path.Combine(outputFolder, $"{subFolder}{count++}");
            }
        }

        Directory.CreateDirectory(folder);

        return folder;
    }

    public static string GetMacAddress()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        if (interfaces.Length == 0)
            return "";
        return interfaces[0].GetPhysicalAddress().ToString();
    }
}
