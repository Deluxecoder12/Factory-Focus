using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using MySql.Data.MySqlClient;

class DataAcquisitionApp
{
    static void Main(string[] args)
    {
        DotNetEnv.Env.Load(@"..\server.env");
        string insertQuery = "INSERT INTO system_data (SN, Timestamp, Device_Name, `CPU_Usage (%)`, `Memory_Usage (MB)`, " +
                          "`Drive C:\\ Used Space (GB)`, `Drive C:\\ Free Space (GB)`, `Drive D:\\ Used Space (GB)`, `Drive D:\\ Free Space (GB)`, " +
                          "`Network Sent (GB)`, `Network Received (GB)`) " +
                          "VALUES (@sn, @timestamp, @deviceName, @cpuUsage, @memoryUsage, @driveCUsedSpace, @driveCFreeSpace, " +
                          "@driveDUsedSpace, @driveDFreeSpace, @networkSentGB, @networkReceivedGB)";

        Console.WriteLine("Data Acquisition Application");
        Console.WriteLine("------------------------");

        int snum = 1;

        // Create a MySqlCommand object
        MySqlCommand insertCommand = new MySqlCommand(insertQuery, GetConnection());

        while (true)
        {
            // Current value of PK won't collide with past values
            insertCommand.Parameters.Clear();

            // Get Serial Number
            insertCommand.Parameters.AddWithValue("@sn", snum);
            snum++;

            // Get TimeStamp
            DateTime timestamp = DateTime.Now;
            insertCommand.Parameters.AddWithValue("@timestamp", timestamp);

            // Get Device Name
            string computerName = Environment.MachineName;
            Console.WriteLine($"Device Name: {computerName}");

            // Get CPU Usage
            float cpuUsage = GetCpuUsage();
            Console.WriteLine($"CPU Usage: {cpuUsage}%");

            // Get Memory Consumption
            long memoryUsage = GetMemoryUsage();
            Console.WriteLine($"Memory Usage: {memoryUsage} MB");

            insertCommand.Parameters.AddWithValue("@deviceName", computerName);
            insertCommand.Parameters.AddWithValue("@cpuUsage", cpuUsage);
            insertCommand.Parameters.AddWithValue("@memoryUsage", memoryUsage);

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
                    insertCommand.Parameters.AddWithValue($"@drive{drive.Name[0]}UsedSpace", usedSpace);
                    insertCommand.Parameters.AddWithValue($"@drive{drive.Name[0]}FreeSpace", freeSpace);
                }
            }

            // Get Network Activity
            double sentGB = GetNetworkSentGB();
            double receivedGB = GetNetworkReceivedGB();
            Console.WriteLine($"Network Activity: Sent - {sentGB} GB, Received - {receivedGB} GB");

            Console.WriteLine("------------------------");
            Console.WriteLine("Press 'q' to exit, any other key to continue...");

            

            // Add parameters with collected values
            insertCommand.Parameters.AddWithValue("@networkSentGB", sentGB);
            insertCommand.Parameters.AddWithValue("@networkReceivedGB", receivedGB);

            // Console.WriteLine($"SQL Query: {insertQuery}"); Query Check

            // Execute the insert statement
            insertCommand.ExecuteNonQuery();

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

    static double GetNetworkSentGB()
    {
        // More robust solutions might involve monitoring specific network adapters.
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT BytesSentPerSec FROM Win32_PerfRawData_Tcpip_NetworkInterface");
        double bytesSent = 0;
        foreach (ManagementObject obj in searcher.Get())
        {
            bytesSent = Math.Round(Convert.ToDouble(obj["BytesSentPerSec"]) / (1024 * 1024 * 1024), 2);
            
        }
        return bytesSent;
    }

    static double GetNetworkReceivedGB()
    {
        // Similar approach as GetNetworkSentBytes for received bytes
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT BytesReceivedPerSec FROM Win32_PerfRawData_Tcpip_NetworkInterface");
        double bytesReceived = 0;
        foreach (ManagementObject obj in searcher.Get())
        {
            bytesReceived = Math.Round(Convert.ToDouble(obj["BytesReceivedPerSec"]) / (1024 * 1024 * 1024), 2);
        }
        return bytesReceived;
    }

    static MySqlConnection GetConnection()
    {
        string DB_SERVER = DotNetEnv.Env.GetString("DB_SERVER");
        string DB_NAME = DotNetEnv.Env.GetString("DB_NAME");
        string DB_USER = DotNetEnv.Env.GetString("DB_USER");
        string DB_PASSWORD = DotNetEnv.Env.GetString("DB_PASSWORD");

        string connectionString = $"server={DB_SERVER};database={DB_NAME};uid={DB_USER};pwd={DB_PASSWORD}";
        MySqlConnection connection = new MySqlConnection(connectionString);

        try
        {
            connection.Open();
        }
        catch(Exception ex)
        {
            Console.WriteLine("Error Connecting");
            throw;
        }

        return connection;
    }
}

