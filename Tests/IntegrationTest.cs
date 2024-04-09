using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.Mail;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MySql.Data.MySqlClient;

[TestClass]
public class DataAcquisitionIntegrationTests
{
    [TestMethod]
    public void GetNextSerialNumber_ReturnsNextAvailableSerialNumber()
    {
        // Arrange
        var connection = new Mock<MySqlConnection>();
        connection.Setup(c => c.Open()).Verifiable();
        connection.Setup(c => c.ExecuteScalar(It.IsAny<string>())).Returns(5);

        // Act
        var nextSerialNumber = DataAcquisitionApp.GetNextSerialNumber(connection.Object);

        // Assert
        Assert.AreEqual(6, nextSerialNumber);
        connection.Verify(c => c.Open(), Times.Once);
    }

    [TestMethod]
    public void GetCpuUsage_ReturnsValidCpuUsage()
    {
        // Arrange
        var cpuCounter = new Mock<PerformanceCounter>("Processor", "% Processor Time", "_Total");
        cpuCounter.Setup(c => c.NextValue()).Returns(50f);

        // Act
        var cpuUsage = DataAcquisitionApp.GetCpuUsage();

        // Assert
        Assert.AreEqual(50f, cpuUsage);
    }

    [TestMethod]
    public void GetMemoryUsage_ReturnsValidMemoryUsage()
    {
        // Arrange
        var process = new Mock<Process>();
        process.Setup(p => p.WorkingSet64).Returns(1024 * 1024 * 1024);

        // Act
        var memoryUsage = DataAcquisitionApp.GetMemoryUsage();

        // Assert
        Assert.AreEqual(1024, memoryUsage);
    }

    [TestMethod]
    public void GetNetworkSentGB_ReturnsValidNetworkSentBytes()
    {
        // Arrange
        var searcher = new Mock<ManagementObjectSearcher>("SELECT BytesSentPerSec FROM Win32_PerfRawData_Tcpip_NetworkInterface");
        var managementObject = new Mock<ManagementObject>();
        managementObject.Setup(o => o["BytesSentPerSec"]).Returns(1024 * 1024 * 1024); // Mocking BytesSentPerSec value
        var managementObjectCollection = new ManagementObjectCollection { managementObject.Object };
        searcher.Setup(s => s.Get()).Returns(managementObjectCollection);

        // Act
        var networkSentGB = DataAcquisitionApp.GetNetworkSentGB();

        // Assert
        Assert.AreEqual(1, networkSentGB);
    }

    [TestMethod]
    public void GetNetworkReceivedGB_ReturnsValidNetworkReceivedBytes()
    {
        // Arrange
        var searcher = new Mock<ManagementObjectSearcher>("SELECT BytesReceivedPerSec FROM Win32_PerfRawData_Tcpip_NetworkInterface");
        var managementObject = new Mock<ManagementObject>();
        managementObject.Setup(o => o["BytesReceivedPerSec"]).Returns(1024 * 1024 * 1024); // Mocking BytesReceivedPerSec value
        var managementObjectCollection = new ManagementObjectCollection { managementObject.Object };
        searcher.Setup(s => s.Get()).Returns(managementObjectCollection);

        // Act
        var networkReceivedGB = DataAcquisitionApp.GetNetworkReceivedGB();

        // Assert
        Assert.AreEqual(1, networkReceivedGB);
    }

    [TestMethod]
    public void SendEmail_SendsEmailSuccessfully()
    {
        // Arrange
        var smtpClient = new Mock<SmtpClient>("smtp.gmail.com");
        smtpClient.Setup(c => c.Send(It.IsAny<MailMessage>())).Verifiable();

        // Act
        DataAcquisitionApp.SendEmail("CPU Usage");

        // Assert
        smtpClient.Verify(c => c.Send(It.IsAny<MailMessage>()), Times.Once);
    }
}