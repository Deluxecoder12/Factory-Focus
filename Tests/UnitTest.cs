using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Mail;
using MySql.Data.MySqlClient;

namespace Tests
{
    public class UnitTests
    {
        // Unit tests for individual methods
            private TestContext testContextInstance;
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        [Test]
        public void Test_GetCpuUsage()
        {
            // Act
            float cpuUsage = DataAcquisitionApp.GetCpuUsage();

            // Assert
            ClassicAssert.GreaterOrEqual(cpuUsage, 0); // CPU usage should be non-negative
            ClassicAssert.LessOrEqual(cpuUsage, 100);  // CPU usage should be less than or equal to 100%
        }

        [Test]
        public void Test_GetMemoryUsage()
        {
            // Act
            long memoryUsage = DataAcquisitionApp.GetMemoryUsage();

            // Assert
            ClassicAssert.GreaterOrEqual(memoryUsage, 0); // Memory usage should be non-negative
        }

        [Test]
        public void Test_GetNetworkSentGB()
        {
            // Act
            double networkSentGB = DataAcquisitionApp.GetNetworkSentGB();

            // Assert
            ClassicAssert.GreaterOrEqual(networkSentGB, 0); // Network sent should be non-negative
        }

        [Test]
        public void Test_GetNetworkReceivedGB()
        {
            // Act
            double networkReceivedGB = DataAcquisitionApp.GetNetworkReceivedGB();

            // Assert
            ClassicAssert.GreaterOrEqual(networkReceivedGB, 0); // Network received should be non-negative
        }

        // Integration test

        [Test]
        public void Test_CheckAndTriggerAlert()
        {
            // Arrange
            List<float> testData = new List<float> { 30, 100, 100, 1 }; // Test data for battery percentage, CPU usage, memory usage, disk free space
            string[] metrics = { "Battery Percentage", "CPU Usage", "Memory Usage", "Disk Free Space"};

            // Redirect console output to a StringWriter
            using (StringWriter sw = new StringWriter())
            {
                Console.SetOut(sw);

                // Act
                DataAcquisitionApp.CheckAlert(testData);

                 // Capture the console output
                string consoleOutput = sw.ToString();
                // TestContext.WriteLine(consoleOutput);

                // Assert
                string expectedMessage = "ALERT: Battery Percentage threshold exceeded!\r\n" +
                                        "Failed to send email: Value cannot be null. (Parameter 'from')\r\n" +
                                        "ALERT: CPU Usage threshold exceeded!\r\n" +
                                        "Failed to send email: Value cannot be null. (Parameter 'from')\r\n" +
                                        "ALERT: Memory Usage threshold exceeded!\r\n" +
                                        "Failed to send email: Value cannot be null. (Parameter 'from')\r\n" +
                                        "ALERT: Disk Free Space threshold exceeded!\r\n" +
                                        "Failed to send email: Value cannot be null. (Parameter 'from')" ;
                ClassicAssert.IsTrue(consoleOutput.Contains(expectedMessage));
            }
        }


    }
}
