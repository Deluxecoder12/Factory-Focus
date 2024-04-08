using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Mail;
using MySql.Data.MySqlClient;

class DataAcquisitionApp
{
    static void Main(string[] args)
    {
        DotNetEnv.Env.Load(@"..\server.env");
        string insertQuery = "INSERT INTO system_data (SN, Timestamp, Device_Name, `Battery (%)`, `CPU_Usage (%)`, `Memory_Usage (MB)`, " +
                          "`Drive C:\\ Used Space (GB)`, `Drive C:\\ Free Space (GB)`, `Drive D:\\ Used Space (GB)`, `Drive D:\\ Free Space (GB)`, " +
                          "`Network Sent (GB)`, `Network Received (GB)`) " +
                          "VALUES (@sn, @timestamp, @deviceName, @battery, @cpuUsage, @memoryUsage, @driveCUsedSpace, @driveCFreeSpace, " +
                          "@driveDUsedSpace, @driveDFreeSpace, @networkSentGB, @networkReceivedGB)";

        Console.WriteLine("Data Acquisition Application");
        Console.WriteLine("------------------------");
        
        MySqlConnection connection = GetConnection();
        int snum = GetNextSerialNumber(connection); // To track data entry num

        // Create a MySqlCommand object
        MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection);

        while (true)
        {
            List<float> performanceData = new List<float>(); // For storing data to check for trigger
            
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
            insertCommand.Parameters.AddWithValue("@deviceName", computerName);

            // Retrieve battery information
            ManagementObjectSearcher batterySearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
            foreach (ManagementObject obj in batterySearcher.Get())
            {
                string name = obj["Name"].ToString();
                float estimatedChargeRemaining = Convert.ToSingle(obj["EstimatedChargeRemaining"]);
                performanceData.Add(estimatedChargeRemaining);
                Console.WriteLine($"Battery: {name}, Charge Remaining: {estimatedChargeRemaining}%");
                insertCommand.Parameters.AddWithValue("@battery", estimatedChargeRemaining);
            }

            // Get CPU Usage
            float cpuUsage = GetCpuUsage();
            performanceData.Add(cpuUsage);
            Console.WriteLine($"CPU Usage: {cpuUsage}%");
            insertCommand.Parameters.AddWithValue("@cpuUsage", cpuUsage);

            // Get Memory Consumption
            long memoryUsage = GetMemoryUsage();
            performanceData.Add(memoryUsage);
            Console.WriteLine($"Memory Usage: {memoryUsage} MB");
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
                    performanceData.Add(freeSpace);
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

            // Check KPI thresholds and trigger alerts
            CheckAndTriggerAlert(performanceData);

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            if (keyInfo.KeyChar == 'q')
            {
                break;
            }

            Console.Clear(); // Clear the console for a cleaner output
        }
    }

    static int GetNextSerialNumber(MySqlConnection connection)
    {
        int SerialNumber = 1; // Default value if no records exist yet

        string query = "SELECT MAX(SN) FROM system_data";
        MySqlCommand command = new MySqlCommand(query, connection);

        object result = command.ExecuteScalar();
        if (result != DBNull.Value)
        {
            SerialNumber = Convert.ToInt32(result) + 1;
        }

        return SerialNumber;
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

    static void CheckAndTriggerAlert(List<float> performanceData)
    {
        const float MinBatteryPercentage = 40; // in percentage (Arbitrary value for checking)
        const float MaxCpuUtilization = 80; // in percentage
        const float MaxMemoryUtilization = 90; // in percentage
        const float MinDiskSpace = 2; // in GB (minimum 2 GB free space)

        string[] metrics = { "Battery Percentage", "CPU Usage", "Memory Usage", "Disk Free Space"};

        // Check KPI thresholds and trigger alerts
        for(int i = 0; i < performanceData.Count; i++)
        {
            switch(i)
            {
                case 0:
                    if (performanceData[i] < MinBatteryPercentage)
                        TriggerAlert(metrics[i]);
                    break;
                case 1:
                    if (performanceData[i] > MaxCpuUtilization)
                        TriggerAlert(metrics[i]);
                    break;
                case 2:
                    if (performanceData[i] > MaxMemoryUtilization)
                        TriggerAlert(metrics[i]);
                    break;
                default:
                    if (i > 2)
                    {
                        if (performanceData[i] < MinDiskSpace)
                            TriggerAlert(metrics[metrics.Length - 1]);
                    }
                    break; 
            }
        }
    }

    static void TriggerAlert(string metric)
    {
        // Trigger alert
        Console.WriteLine($"ALERT: {metric} threshold exceeded!");
        SendEmail(metric);
    }

    static void SendEmail(string metric)
    {
        try
        {
            // Sender's email address and password
            string SENDER_EMAIL = DotNetEnv.Env.GetString("SENDER_EMAIL");
            string SENDER_PASSWORD = DotNetEnv.Env.GetString("SENDER_PASSWORD");
            
            // Recipient's email address
            string RECEIVER_EMAIL = DotNetEnv.Env.GetString("RECEIVER_EMAIL");

            // SMTP server details (replace with your SMTP server and port)
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(SENDER_EMAIL, SENDER_PASSWORD),
                EnableSsl = true,
            };

            // Create a new MailMessage instance
            MailMessage message = new MailMessage(SENDER_EMAIL, RECEIVER_EMAIL)
            {
                // Set email subject and body
                Subject = "Alert: Threshold Exceeded",
                Body = $"ALERT: {metric} threshold exceeded!"
            };

            // Send the email
            smtpClient.Send(message);

            Console.WriteLine("Email sent successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }
    }
}

