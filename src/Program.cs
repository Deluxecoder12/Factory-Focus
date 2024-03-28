using System;
using System.Diagnostics;
using System.IO;
using System.Management;

class DataAcquisitionApp
{
    static void Main(string[] args)
    {
        Console.WriteLine("Data Acquisition Application");
        Console.WriteLine("------------------------");

        while (true)
        {
            // Get CPU Usage
            float cpuUsage = GetCpuUsage();
            Console.WriteLine($"CPU Usage: {cpuUsage}%");

            // Get Memory Consumption
            long memoryUsage = GetMemoryUsage();
            Console.WriteLine($"Memory Usage: {memoryUsage} MB");

            // Get Disk Space
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                if (drive.IsReady)
                {
                    long totalSpace = drive.TotalSize / (1024 * 1024 * 1024);
                    long freeSpace = drive.TotalFreeSpace / (1024 * 1024 * 1024);
                    long usedSpace = totalSpace - freeSpace;
                    Console.WriteLine($"Drive {drive.Name}: Used Space - {usedSpace} GB, Free Space - {freeSpace} GB");
                }
            }

            // Get Network Activity (basic example)
            long bytesSent = GetNetworkSentBytes();
            long bytesReceived = GetNetworkReceivedBytes();
            Console.WriteLine($"Network Activity: Sent - {bytesSent} Bytes, Received - {bytesReceived} Bytes");

            Console.WriteLine("------------------------");
            Console.WriteLine("Press 'q' to exit, any other key to continue...");
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            if (keyInfo.KeyChar == 'q')
            {
                break;
            }

            Console.Clear(); // Clear the console for a cleaner output
        }
    }

    static float GetCpuUsage()
    {
        PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        return cpuCounter.NextValue();
    }

    static long GetMemoryUsage()
    {
        Process currentProcess = Process.GetCurrentProcess();
        return currentProcess.WorkingSet64 / (1024 * 1024);
    }

    static long GetNetworkSentBytes()
    {
        // Basic example using ManagementObjectSearcher. 
        // More robust solutions might involve monitoring specific network adapters.
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT BytesSentPerSec FROM Win32_PerfRawData_Tcpip_NetworkInterface");
        long bytesSent = 0;
        foreach (ManagementObject obj in searcher.Get())
        {
            bytesSent = Convert.ToInt64(obj["BytesSentPerSec"]);
        }
        return bytesSent;
    }

    static long GetNetworkReceivedBytes()
    {
        // Similar approach as GetNetworkSentBytes for received bytes
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT BytesReceivedPerSec FROM Win32_PerfRawData_Tcpip_NetworkInterface");
        long bytesReceived = 0;
        foreach (ManagementObject obj in searcher.Get())
        {
            bytesReceived = Convert.ToInt64(obj["BytesReceivedPerSec"]);
        }
        return bytesReceived;
    }
}

