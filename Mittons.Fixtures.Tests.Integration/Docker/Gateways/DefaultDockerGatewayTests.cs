using Xunit;
using Mittons.Fixtures.Docker.Gateways;
using System.Diagnostics;
using System.Text;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Mittons.Fixtures.Tests.Integration.Docker.Gateways
{
    public class DefaultDockerGatewayTests
    {
        public class ContainerTests : IDisposable
        {
            private List<string> _containerIds = new List<string>();

            private List<string> _filenames = new List<string>();

            public void Dispose()
            {
                foreach(var containerId in _containerIds)
                {
                    using var proc = new Process();
                    proc.StartInfo.FileName = "docker";
                    proc.StartInfo.Arguments = $"rm --force {containerId}";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;

                    proc.Start();
                    proc.WaitForExit();
                }

                foreach(var filename in _filenames)
                {
                    File.Delete(filename);
                }
            }

            [Fact]
            public void ContainerRun_WhenCalledWithLabels_ExpectContainerToHaveTheLabelsApplied()
            {
                // Arrange
                var imageName = "alpine:3.15";
                var gateway = new DefaultDockerGateway();

                // Act
                var containerId = gateway.ContainerRun(
                        imageName,
                        string.Empty,
                        new Dictionary<string, string>
                        {
                            { "first", "second" },
                            { "third", "fourth" }
                        }
                    );

                _containerIds.Add(containerId);

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{json .Config.Labels}}}}\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var output = proc.StandardOutput.ReadToEnd();

                var actualLabels = JsonSerializer.Deserialize<Dictionary<string, string>>(output) ?? new Dictionary<string, string>();

                Assert.True(actualLabels.ContainsKey("first"));
                Assert.True(actualLabels.ContainsKey("third"));
                Assert.Equal("second", actualLabels["first"]);
                Assert.Equal("fourth", actualLabels["third"]);
            }

            [Theory]
            [InlineData("alpine:3.15")]
            [InlineData("alpine:3.14")]
            public void ContainerRun_WhenCalledWithAnImage_ExpectContainerToBeForTheRequestedImage(string imageName)
            {
                // Arrange
                var gateway = new DefaultDockerGateway();

                // Act
                var containerId = gateway.ContainerRun(imageName, string.Empty, new Dictionary<string, string>());
                _containerIds.Add(containerId);

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"inspect {containerId} --format '{{{{.Config.Image}}}}'";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var outputBuilder = new StringBuilder();

                while (!proc.StandardOutput.EndOfStream) {
                    outputBuilder.Append(proc.StandardOutput.ReadLine());
                }

                var output = outputBuilder.ToString();

                Assert.Equal($"'{imageName}'", output);
            }

            [Fact]
            public void ContainerRun_WhenCalledForAlpineWithNoCommand_ExpectContainerToHaveStartedWithTheDefaultCommand()
            {
                // Arrange
                var gateway = new DefaultDockerGateway();

                // Act
                var containerId = gateway.ContainerRun("alpine:3.15", string.Empty, new Dictionary<string, string>());
                _containerIds.Add(containerId);

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"inspect {containerId} --format '{{{{.Config.Cmd}}}}'";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var outputBuilder = new StringBuilder();

                while (!proc.StandardOutput.EndOfStream) {
                    outputBuilder.Append(proc.StandardOutput.ReadLine());
                }

                var output = outputBuilder.ToString();

                Assert.Equal("'[/bin/sh]'", output);
            }

            [Fact]
            public void ContainerRun_WhenCalledForAlpineWithACommand_ExpectContainerToHaveStartedWithTheCommand()
            {
                // Arrange
                var gateway = new DefaultDockerGateway();

                // Act
                var containerId = gateway.ContainerRun("alpine:3.15", "/bin/bash", new Dictionary<string, string>());
                _containerIds.Add(containerId);

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"inspect {containerId} --format '{{{{.Config.Cmd}}}}'";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var outputBuilder = new StringBuilder();

                while (!proc.StandardOutput.EndOfStream) {
                    outputBuilder.Append(proc.StandardOutput.ReadLine());
                }

                var output = outputBuilder.ToString();

                Assert.Equal("'[/bin/bash]'", output);
            }

            [Fact]
            public void ContainerRemove_WhenTheContainerDoesNotExist_ExpectSuccessfulReturn()
            {
                // Arrange
                var gateway = new DefaultDockerGateway();

                // Act
                // Assert
                gateway.ContainerRemove("cd898788786795df83dbf414bbcc9e6c6be9d4bc932e96a6542c03d033e1cc72");
            }

            [Fact]
            public void ContainerRemove_WhenTheContainerExists_ExpectTheContainerToBeRemoved()
            {
                // Arrange
                var gateway = new DefaultDockerGateway();

                var containerId = gateway.ContainerRun("alpine:3.15", string.Empty, new Dictionary<string, string>());
                _containerIds.Add(containerId);

                // Act
                gateway.ContainerRemove(containerId);

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"ps -a --filter id={containerId} --format '{{{{.ID}}}}'";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var output = proc.StandardOutput.ReadToEnd();

                Assert.Empty(output);
            }

            [Fact]
            public void ContainerGetDefaultNetworkIpAddress_WhenTheContainerIsOnOneNetwork_ReturnsTheIpAddressForTheNetwork()
            {
                // Arrange
                var gateway = new DefaultDockerGateway();

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest", new Dictionary<string, string>());
                _containerIds.Add(containerId);

                // Act
                var ipAddress = gateway.ContainerGetDefaultNetworkIpAddress(containerId);

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{range .NetworkSettings.Networks}}}}{{{{printf \\\"%s\\n\\\" .IPAddress}}}}{{{{end}}}}\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                IPAddress.TryParse(proc.StandardOutput.ReadLine(), out var expectedIpAddress);

                Assert.Equal(expectedIpAddress, ipAddress);
            }

            [Theory]
            [InlineData("test", "/tmp/test.txt")]
            [InlineData("test\nfile", "/tmp/test2.txt")]
            [InlineData("file\ntest", "/test.txt")]
            public void ContainerAddFile_WhenCalled_ExpectFileToBeCopiedToTheContainer(string fileContents, string containerFilename)
            {
                // Arrange
                var temporaryFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                _filenames.Add(temporaryFilename);

                File.WriteAllText(temporaryFilename, fileContents);

                var gateway = new DefaultDockerGateway();

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest", new Dictionary<string, string>());
                _containerIds.Add(containerId);

                // Act
                gateway.ContainerAddFile(containerId, temporaryFilename, containerFilename, default(string), default(string));

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"exec {containerId} cat {containerFilename}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var output = proc.StandardOutput.ReadToEnd();

                Assert.Equal(fileContents, output);
            }

            [Theory]
            [InlineData("777", "/tmp/test.txt")]
            [InlineData("757", "/tmp/test2.txt")]
            [InlineData("557", "/test.txt")]
            public void ContainerAddFile_WhenCalledWithPermissions_ExpectThePermissionsToBeSet(string permissions, string containerFilename)
            {
                // Arrange
                var temporaryFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                _filenames.Add(temporaryFilename);

                File.WriteAllText(temporaryFilename, "hello, world");

                var gateway = new DefaultDockerGateway();

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest", new Dictionary<string, string>());
                _containerIds.Add(containerId);

                // Act
                gateway.ContainerAddFile(containerId, temporaryFilename, containerFilename, default(string), permissions);

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"exec {containerId} stat -c \"%a\" {containerFilename}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var output = proc.StandardOutput.ReadLine();

                Assert.Equal(permissions, output);
            }

            [Theory]
            [InlineData("guest", "/tmp/test.txt")]
            [InlineData("tester", "/tmp/test2.txt")]
            [InlineData("root", "/test.txt")]
            public void ContainerAddFile_WhenCalledWithAnOwner_ExpectThePermissionsToBeSet(string owner, string containerFilename)
            {
                // Arrange
                var temporaryFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                _filenames.Add(temporaryFilename);

                File.WriteAllText(temporaryFilename, "hello, world");

                var gateway = new DefaultDockerGateway();

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest tester:tester", new Dictionary<string, string>());
                _containerIds.Add(containerId);

                // Act
                gateway.ContainerAddFile(containerId, temporaryFilename, containerFilename, owner, default(string));

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"exec {containerId} stat -c \"%U\" {containerFilename}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var output = proc.StandardOutput.ReadLine();

                Assert.Equal(owner, output);
            }

            [Theory]
            [InlineData("/tmp/test.txt")]
            [InlineData("/tmp/test2.txt")]
            [InlineData("/test.txt")]
            public void ContainerRemoveFile_WhenCalled_ExpectFileToBeRemoved(string containerFilename)
            {
                // Arrange
                var temporaryFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                _filenames.Add(temporaryFilename);

                File.WriteAllText(temporaryFilename, "hello, world");

                var gateway = new DefaultDockerGateway();

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest tester:tester", new Dictionary<string, string>());
                _containerIds.Add(containerId);

                gateway.ContainerAddFile(containerId, temporaryFilename, containerFilename, default(string), default(string));

                // Act
                gateway.ContainerRemoveFile(containerId, containerFilename);

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"exec {containerId} ls {containerFilename}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var output = proc.StandardOutput.ReadToEnd();

                Assert.Empty(output);
            }

            [Fact]
            public void ContainerExecuteCommand_WhenCalled_ExpectResultsToBeReturned()
            {
                // Arrange
                var gateway = new DefaultDockerGateway();

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest", new Dictionary<string, string>());
                _containerIds.Add(containerId);

                for (var i = 0; i < 10; ++i)
                {
                    var health = gateway.ContainerExecuteCommand(containerId, "ps aux | grep -v grep | grep sshd || exit 1");

                    if (health.Any())
                    {
                        break;
                    }

                    Task.Delay(1000).GetAwaiter().GetResult();
                }

                // Act
                var results = gateway.ContainerExecuteCommand(containerId, "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_rsa_key.pub");

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"exec {containerId} ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_rsa_key.pub";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var output = proc.StandardOutput.ReadLine();

                Assert.Single(results);
                Assert.Equal(output, results.First());
            }

            [Fact]
            public void ContainerGetHostPortMapping_WhenCalledForSftp_ExpectHostPortToBeReturnedForContainerPort22()
            {
                // Arrange
                var gateway = new DefaultDockerGateway();

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest", new Dictionary<string, string>());
                _containerIds.Add(containerId);

                // Act
                var actualPort = gateway.ContainerGetHostPortMapping(containerId, "tcp", 22);

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"port {containerId} 22/tcp";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                int.TryParse(proc.StandardOutput?.ReadLine()?.Split(':')?.Last(), out var expectedPort);

                Assert.Equal(expectedPort, actualPort);
            }

            [Fact]
            public void ContainerGetHostPortMapping_WhenCalledForRedis_ExpectHostPortToBeReturnedForContainerPort6379()
            {
                // Arrange
                var gateway = new DefaultDockerGateway();

                var containerId = gateway.ContainerRun("redis:alpine", string.Empty, new Dictionary<string, string>());
                _containerIds.Add(containerId);

                // Act
                var actualPort = gateway.ContainerGetHostPortMapping(containerId, "tcp", 6379);

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"port {containerId} 6379/tcp";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                int.TryParse(proc.StandardOutput?.ReadLine()?.Split(':')?.Last(), out var expectedPort);

                Assert.Equal(expectedPort, actualPort);
            }

            public class GetHealthStatusAsync : IDisposable
            {
                private List<string> _containerIds = new List<string>();

                private List<string> _filenames = new List<string>();

                public void Dispose()
                {
                    foreach(var containerId in _containerIds)
                    {
                        using var proc = new Process();
                        proc.StartInfo.FileName = "docker";
                        proc.StartInfo.Arguments = $"rm --force {containerId}";
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.RedirectStandardOutput = true;

                        proc.Start();
                        proc.WaitForExit();
                    }
                }

                [Fact]
                public async Task GetHealthStatusAsync_WhenContainerIsRunning_ExpectRunningHealthStatus()
                {
                    // Arrange
                    var gateway = new DefaultDockerGateway();

                    var containerId = gateway.ContainerRun("redis:alpine", string.Empty, new Dictionary<string, string>());
                    _containerIds.Add(containerId);

                    // Act
                    var healthStatus = await gateway.ContainerGetHealthStatusAsync(containerId, CancellationToken.None);

                    // Assert
                    Assert.Equal(HealthStatus.Running, healthStatus);
                }

                [Fact]
                public async Task GetHealthStatusAsync_WhenContainerIsHealthy_ExpectHealthyHealthStatus()
                {
                    // Arrange
                    var gateway = new DefaultDockerGateway();

                    var containerId = gateway.ContainerRun("--health-cmd=\"echo hello\" --health-interval=1s redis:alpine", string.Empty, new Dictionary<string, string>());
                    _containerIds.Add(containerId);

                    // Act
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    var healthStatus = await gateway.ContainerGetHealthStatusAsync(containerId, CancellationToken.None);

                    // Assert
                    Assert.Equal(HealthStatus.Healthy, healthStatus);
                }

                [Fact]
                public async Task GetHealthStatusAsync_WhenContainerIsUnhealthy_ExpectUnhealthyHealthStatus()
                {
                    // Arrange
                    var gateway = new DefaultDockerGateway();

                    var containerId = gateway.ContainerRun("--health-cmd=\"exit 1\" --health-interval=1s --health-retries=1 --health-start-period=1s redis:alpine", string.Empty, new Dictionary<string, string>());
                    _containerIds.Add(containerId);

                    // Act
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    var healthStatus = await gateway.ContainerGetHealthStatusAsync(containerId, CancellationToken.None);

                    // Assert
                    Assert.Equal(HealthStatus.Unhealthy, healthStatus);
                }

                [Fact]
                public async Task GetHealthStatusAsync_WhenContainerIsStarting_ExpectUnknownHealthStatus()
                {
                    // Arrange
                    var gateway = new DefaultDockerGateway();

                    var containerId = gateway.ContainerRun("--health-cmd=\"exit 1\" --health-interval=1s --health-retries=1 --health-start-period=1s redis:alpine", string.Empty, new Dictionary<string, string>());
                    _containerIds.Add(containerId);

                    // Act
                    var healthStatus = await gateway.ContainerGetHealthStatusAsync(containerId, CancellationToken.None);

                    // Assert
                    Assert.Equal(HealthStatus.Unknown, healthStatus);
                }

                [Fact]
                public async Task GetHealthStatusAsync_WhenContainerIsExited_ExpectUnknownHealthStatus()
                {
                    // Arrange
                    var gateway = new DefaultDockerGateway();

                    var containerId = gateway.ContainerRun("alpine", string.Empty, new Dictionary<string, string>());
                    _containerIds.Add(containerId);

                    // Act
                    var healthStatus = await gateway.ContainerGetHealthStatusAsync(containerId, CancellationToken.None);

                    // Assert
                    Assert.Equal(HealthStatus.Unknown, healthStatus);
                }
            }
        }

        public class NetworkTests : IDisposable
        {
            private List<string> _networkNames = new List<string>();

            private List<string> _containerIds = new List<string>();

            public void Dispose()
            {
                foreach(var containerId in _containerIds)
                {
                    using var proc = new Process();
                    proc.StartInfo.FileName = "docker";
                    proc.StartInfo.Arguments = $"rm --force {containerId}";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;

                    proc.Start();
                    proc.WaitForExit();
                }

                foreach(var networkName in _networkNames)
                {
                    using var proc = new Process();
                    proc.StartInfo.FileName = "docker";
                    proc.StartInfo.Arguments = $"network rm {networkName}";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;

                    proc.Start();
                    proc.WaitForExit();
                }
            }

            [Fact]
            public void NetworkCreate_WhenCalled_ExpectNetworkToBeCreatedWithTheProvidedName()
            {
                // Arrange
                var networkName = "test";
                var uniqueName = $"{networkName}-{Guid.NewGuid()}";

                var gateway = new DefaultDockerGateway();

                _networkNames.Add(uniqueName);

                // Act
                gateway.NetworkCreate(uniqueName, new Dictionary<string, string>());

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"network ls -f name={uniqueName} -q";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var output = proc.StandardOutput.ReadToEnd();

                Assert.False(string.IsNullOrWhiteSpace(output));
            }

            [Fact]
            public void NetworkCreate_WhenCalledWithLabels_ExpectLabelsToBeAttachedToTheNetwork()
            {
                // Arrange
                var networkName = "test";
                var uniqueName = $"{networkName}-{Guid.NewGuid()}";

                var expectedLabels = new Dictionary<string, string>
                {
                    { "first", "second" },
                    { "third", "fourth" }
                };

                var gateway = new DefaultDockerGateway();

                _networkNames.Add(uniqueName);

                // Act
                gateway.NetworkCreate(uniqueName, expectedLabels);

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"network inspect {uniqueName} --format \"{{{{json .Labels}}}}\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var output = proc.StandardOutput.ReadToEnd();

                var actualLabels = JsonSerializer.Deserialize<Dictionary<string, string>>(output) ?? new Dictionary<string, string>();

                Assert.True(actualLabels.ContainsKey("first"));
                Assert.True(actualLabels.ContainsKey("third"));
                Assert.Equal(expectedLabels["first"], actualLabels["first"]);
                Assert.Equal(expectedLabels["third"], actualLabels["third"]);
            }

            [Fact]
            public void NetworkRemove_WhenCalled_ExpectNetworkToBeRemoved()
            {
                // Arrange
                var networkName = "test";
                var uniqueName = $"{networkName}-{Guid.NewGuid()}";

                var gateway = new DefaultDockerGateway();

                _networkNames.Add(uniqueName);

                gateway.NetworkCreate(uniqueName, new Dictionary<string, string>());

                // Act
                gateway.NetworkRemove(uniqueName);

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"network ls -f name={uniqueName} -q";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var output = proc.StandardOutput.ReadToEnd();

                Assert.True(string.IsNullOrWhiteSpace(output));
            }

            [Fact]
            public void NetworkConnect_WhenCalled_ExpectContainerToBeConnectedToNetwork()
            {
                // Arrange
                var networkName = "test";
                var uniqueName = $"{networkName}-{Guid.NewGuid()}";

                var gateway = new DefaultDockerGateway();

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest", new Dictionary<string, string>());
                _containerIds.Add(containerId);

                _networkNames.Add(uniqueName);

                gateway.NetworkCreate(uniqueName, new Dictionary<string, string>());

                // Act
                gateway.NetworkConnect(uniqueName, containerId, "test.example.com");

                // Assert
                using var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{range $k, $v := .NetworkSettings.Networks}}}}{{{{printf \\\"%s\\n\\\" $k}}}}{{{{end}}}}\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                List<string> connectedNetworks = new List<string>();

                while (!proc.StandardOutput.EndOfStream)
                {
                    var network = proc.StandardOutput.ReadLine();

                    if (!string.IsNullOrWhiteSpace(network))
                    {
                        connectedNetworks.Add(network);
                    }
                }

                Assert.Contains(uniqueName, connectedNetworks);
            }
        }
    }
}