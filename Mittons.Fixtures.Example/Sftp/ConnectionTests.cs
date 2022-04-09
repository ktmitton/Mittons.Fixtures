using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Example.Fixtures;
using Renci.SshNet;
using Xunit;

namespace Mittons.Fixtures.Example.Sftp
{
    public class ConnectionSettingsTests : IClassFixture<ReportingEnvironmentFixture>
    {
        private readonly ReportingEnvironmentFixture _reportingEnvironment;

        public ConnectionSettingsTests(ReportingEnvironmentFixture reportingEnvironment)
        {
            _reportingEnvironment = reportingEnvironment;
        }

        [Fact]
        public void Connect_WhenUsingTheAdminCredentials_ExpectASuccessfulConnection()
        {
            // Arrange
            var connectionSettings = _reportingEnvironment.SftpContainer.SftpConnectionSettings.Single(x => x.Key == "admin").Value;

            var connectionInfo = new ConnectionInfo(
                    connectionSettings.Host,
                    connectionSettings.Port,
                    connectionSettings.Username,
                    new PasswordAuthenticationMethod(connectionSettings.Username, connectionSettings.Password)
                );

            // Act
            using var client = new SftpClient(connectionInfo);

            client.Connect();

            // Assert
            Assert.True(client.IsConnected);
        }

        [Theory]
        [InlineData("/home/admin/testfile.txt", "testfile.txt", "hello, world")]
        [InlineData("/home/admin/inbox/other.txt", "inbox/other.txt", "goodbye, world")]
        public async Task DownloadFile_WhenTheFileExists_ExpectTheFileToBeDownloaded(string filename, string sftpFilename, string fileContents)
        {
            // Arrange
            var connectionSettings = _reportingEnvironment.SftpContainer.SftpConnectionSettings.Single(x => x.Key == "admin").Value;

            await _reportingEnvironment.SftpContainer.CreateUserFileAsync(connectionSettings.Username, fileContents, filename, default, default, CancellationToken.None);

            var connectionInfo = new ConnectionInfo(
                    connectionSettings.Host,
                    connectionSettings.Port,
                    connectionSettings.Username,
                    new PasswordAuthenticationMethod(connectionSettings.Username, connectionSettings.Password)
                );

            using var client = new SftpClient(connectionInfo);
            client.Connect();

            var outputStream = new MemoryStream();

            var taskCompletionSource = new TaskCompletionSource<string>();

            // Act
            client.BeginDownloadFile(
                    sftpFilename,
                    outputStream,
                    x =>
                    {
                        outputStream.Seek(0, SeekOrigin.Begin);

                        using var reader = new StreamReader(outputStream);

                        taskCompletionSource.SetResult(reader.ReadToEnd());
                    }
                );

            var result = await taskCompletionSource.Task;

            // Assert
            Assert.Equal(fileContents, result);
        }
    }
}