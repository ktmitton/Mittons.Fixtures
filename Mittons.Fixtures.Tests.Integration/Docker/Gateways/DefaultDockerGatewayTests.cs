using Xunit;
using Mittons.Fixtures.Docker.Gateways;
using System.Diagnostics;
using System.Text;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

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

            [Theory]
            [InlineData("alpine:3.15")]
            [InlineData("alpine:3.14")]
            public void ContainerRun_WhenCalledWithAnImage_ExpectContainerToBeForTheRequestedImage(string imageName)
            {
                // Arrange
                var gateway = new DefaultDockerGateway();

                // Act
                var containerId = gateway.ContainerRun(imageName, string.Empty);
                _containerIds.Add(containerId);

                // Assert
                var proc = new Process();
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
                var containerId = gateway.ContainerRun("alpine:3.15", string.Empty);
                _containerIds.Add(containerId);

                // Assert
                var proc = new Process();
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
                var containerId = gateway.ContainerRun("alpine:3.15", "/bin/bash");
                _containerIds.Add(containerId);

                // Assert
                var proc = new Process();
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

                var containerId = gateway.ContainerRun("alpine:3.15", string.Empty);
                _containerIds.Add(containerId);

                // Act
                gateway.ContainerRemove(containerId);

                // Assert
                var proc = new Process();
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

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest");
                _containerIds.Add(containerId);

                // Act
                var ipAddress = gateway.ContainerGetDefaultNetworkIpAddress(containerId);

                // Assert
                var proc = new Process();
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

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest");
                _containerIds.Add(containerId);

                // Act
                gateway.ContainerAddFile(containerId, temporaryFilename, containerFilename, default(string), default(string));

                // Assert
                var proc = new Process();
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

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest");
                _containerIds.Add(containerId);

                // Act
                gateway.ContainerAddFile(containerId, temporaryFilename, containerFilename, default(string), permissions);

                // Assert
                var proc = new Process();
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

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest tester:tester");
                _containerIds.Add(containerId);

                // Act
                gateway.ContainerAddFile(containerId, temporaryFilename, containerFilename, owner, default(string));

                // Assert
                var proc = new Process();
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

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest tester:tester");
                _containerIds.Add(containerId);

                gateway.ContainerAddFile(containerId, temporaryFilename, containerFilename, default(string), default(string));

                // Act
                gateway.ContainerRemoveFile(containerId, containerFilename);

                // Assert
                var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"exec {containerId} ls {containerFilename}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var output = proc.StandardOutput.ReadToEnd();

                Assert.Empty(output);
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
                var proc = new Process();
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
                var proc = new Process();
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

                var containerId = gateway.ContainerRun("atmoz/sftp:alpine", "guest:guest");
                _containerIds.Add(containerId);

                _networkNames.Add(uniqueName);

                gateway.NetworkCreate(uniqueName, new Dictionary<string, string>());

                // Act
                gateway.NetworkConnect(uniqueName, containerId, "test.example.com");

                // Assert
                var proc = new Process();
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