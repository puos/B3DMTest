using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace SIMMETA;

public class BugReporter
{
    static string GetComputerName()
    {
        return Environment.MachineName;
    }

    static string GetIpAddress()
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://ifconfig.me");
        request.Method = "GET";
        request.UserAgent = "curl"; 

        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        using (Stream stream = response.GetResponseStream())
        using (StreamReader reader = new StreamReader(stream))
        {
            string ip = reader.ReadToEnd().Trim();
            return ip;
        }
    }

    static string GetCpuInfo()
    {
        StringBuilder cpuInfo = new StringBuilder();
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor");
        foreach (ManagementObject obj in searcher.Get())
        {
            cpuInfo.AppendLine($"Name: {obj["Name"]}");
            cpuInfo.AppendLine($"Manufacturer: {obj["Manufacturer"]}");
            cpuInfo.AppendLine($"Description: {obj["Description"]}");
        }
        return cpuInfo.ToString();
    }

    static string GetMemoryInfo()
    {
        StringBuilder memoryInfo = new StringBuilder();
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");
        foreach (ManagementObject obj in searcher.Get())
        {
            memoryInfo.AppendLine($"Capacity: {obj["Capacity"]} bytes");
            memoryInfo.AppendLine($"Speed: {obj["Speed"]} MHz");
            memoryInfo.AppendLine($"Manufacturer: {obj["Manufacturer"]}");
        }
        return memoryInfo.ToString();
    }

    static string GetDiskInfo()
    {
        StringBuilder diskInfo = new StringBuilder();
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_DiskDrive");
        foreach (ManagementObject obj in searcher.Get())
        {
            diskInfo.AppendLine($"Model: {obj["Model"]}");
            diskInfo.AppendLine($"InterfaceType: {obj["InterfaceType"]}");
            diskInfo.AppendLine($"Size: {obj["Size"]} bytes");
        }
        return diskInfo.ToString();
    }

    static string GetGpuInfo()
    {
        StringBuilder gpuInfo = new StringBuilder();
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
        foreach (ManagementObject obj in searcher.Get())
        {
            gpuInfo.AppendLine($"Name: {obj["Name"]}");
            gpuInfo.AppendLine($"AdapterRAM: {obj["AdapterRAM"]} bytes");
            gpuInfo.AppendLine($"DriverVersion: {obj["DriverVersion"]}");
        }
        return gpuInfo.ToString();
    }

    static string GetOsInfo()
    {
        StringBuilder osInfo = new StringBuilder();
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
        foreach (ManagementObject obj in searcher.Get())
        {
            osInfo.AppendLine($"Name: {obj["Name"]}");
            osInfo.AppendLine($"Version: {obj["Version"]}");
            osInfo.AppendLine($"Manufacturer: {obj["Manufacturer"]}");
        }
        return osInfo.ToString();
    }

    static string GetSystemInfo(string ipAddress)
    {
        StringBuilder systemInfo = new StringBuilder();
        systemInfo.AppendLine("Computer Name: " + GetComputerName());
        systemInfo.AppendLine("IP Address: " + ipAddress);
        systemInfo.AppendLine("Mac Address: " + LasCustomUtil.GetMacAddress());
        systemInfo.AppendLine("CPU Info: " + GetCpuInfo());
        systemInfo.AppendLine("Memory Info: " + GetMemoryInfo());
        systemInfo.AppendLine("Disk Info: " + GetDiskInfo());
        systemInfo.AppendLine("GPU Info: " + GetGpuInfo());
        systemInfo.AppendLine("OS Info: " + GetOsInfo());
        return systemInfo.ToString();
    }

    public static void Send(string zipPath , string reason)
    {
        try
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            SmtpClient client = new SmtpClient("groupware62.hanbiro.net", 587);
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Credentials = new NetworkCredential("puos033@neospectra.co.kr", "neo1234!");
            client.Timeout = 30000;

            MailAddress from = new MailAddress("puos033@neospectra.co.kr");
            MailAddress to = new MailAddress("puos033@neospectra.co.kr");
            MailMessage mailMessage = new MailMessage(from, to);
            mailMessage.To.Add("leeyoonju@neospectra.co.kr");
            var ip = GetIpAddress();
            var date = DateTime.Now.ToString("yy-MM-dd-HH-mm");
            ip = ip.Replace("\r\n", "");

            mailMessage.Subject = $"Lidar Recon Bug Report {ip} , {date}";
            mailMessage.Body = $"reason : {reason} \r\n\n {GetSystemInfo(ip)}";

            Attachment attachment = new Attachment(zipPath);
            mailMessage.Attachments.Add(attachment);

            client.Send(mailMessage);
            mailMessage.Dispose();
        }
        catch (Exception ex)
        {
        }
    }
}
