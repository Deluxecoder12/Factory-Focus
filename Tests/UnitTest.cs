using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System.Management;

[TestFixture]
public class DataAcquisitionAppTests
{
    [Test]
    public void CheckAndTriggerAlert_Should_Trigger_Alert_When_Battery_Percentage_Low()
    {
        // Arrange
        var mockDataAcquisitionApp = new Mock<DataAcquisitionApp>();
        var performanceData = new List<float> { 30 }; // Low battery percentage
        string expectedMetric = "Battery Percentage";

        // Act
        mockDataAcquisitionApp.Object.CheckAndTriggerAlert(performanceData);

        // Assert
        mockDataAcquisitionApp.Verify(x => x.TriggerAlert(expectedMetric), Times.Once);
    }

    [Test]
    public void CheckAndTriggerAlert_Should_Trigger_Alert_When_CPU_Utilization_High()
    {
        // Arrange
        var mockDataAcquisitionApp = new Mock<DataAcquisitionApp>();
        var performanceData = new List<float> { 85 }; // High CPU utilization
        string expectedMetric = "CPU Usage";

        // Act
        mockDataAcquisitionApp.Object.CheckAndTriggerAlert(performanceData);

        // Assert
        mockDataAcquisitionApp.Verify(x => x.TriggerAlert(expectedMetric), Times.Once);
    }

    [Test]
    public void CheckAndTriggerAlert_Should_Not_Trigger_Alert_When_Memory_Utilization_High()
    {
        // Arrange
        var mockDataAcquisitionApp = new Mock<DataAcquisitionApp>();
        var performanceData = new List<float> { 60 }; 
        string expectedMetric = "Memory Usage";

        // Act
        mockDataAcquisitionApp.Object.CheckAndTriggerAlert(performanceData);

        // Assert
        mockDataAcquisitionApp.Verify(x => x.TriggerAlert(expectedMetric), Times.Never);
    }
}
