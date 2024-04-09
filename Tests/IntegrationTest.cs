using NUnit.Framework;
using System.Data;
using MySql.Data.MySqlClient;

[TestFixture]
public class DataAcquisitionAppIntegrationTests
{
    private string _connectionString;
    private MySqlConnection _connection;

    [SetUp]
    public void Setup()
    {
        DotNetEnv.Env.Load(@"..\server.env");
        string DB_SERVER = DotNetEnv.Env.GetString("DB_SERVER");
        string DB_NAME = DotNetEnv.Env.GetString("DB_NAME");
        string DB_USER = DotNetEnv.Env.GetString("DB_USER");
        string DB_PASSWORD = DotNetEnv.Env.GetString("DB_PASSWORD");
        // Setup database connection
        _connectionString = $"server={DB_SERVER};database={DB_NAME};uid={DB_USER};pwd={DB_PASSWORD}";
        _connection = new MySqlConnection(_connectionString);
        _connection.Open();
    }

    [TearDown]
    public void TearDown()
    {
        // Close database connection
        _connection.Close();
    }

    [Test]
    public void InsertSystemData_Should_Insert_Data_Into_Database()
    {
        // Arrange
        var dataAcquisitionApp = new DataAcquisitionApp();
        var systemData = new SystemData
        {
            SerialNumber = 1,
            Timestamp = DateTime.Now,
            DeviceName = "Test Device",
            BatteryPercentage = 80,
            CPUUsage = 50,
            MemoryUsage = 2048,
            DriveUsages = new Dictionary<char, DriveUsage>
            {
                { 'C', new DriveUsage { UsedSpaceGB = 100, FreeSpaceGB = 200 } },
                { 'D', new DriveUsage { UsedSpaceGB = 150, FreeSpaceGB = 250 } }
            },
            NetworkSentGB = 10,
            NetworkReceivedGB = 20
        };

        // Act
        dataAcquisitionApp.InsertSystemData(systemData, _connection);

        // Assert
        string query = "SELECT COUNT(*) FROM system_data";
        var command = new MySqlCommand(query, _connection);
        int rowCount = Convert.ToInt32(command.ExecuteScalar());
        Assert.AreEqual(1, rowCount); // Ensure that one row is inserted
    }
}
