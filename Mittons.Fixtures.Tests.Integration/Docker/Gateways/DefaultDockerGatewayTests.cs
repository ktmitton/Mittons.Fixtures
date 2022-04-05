using Xunit;
using Mittons.Fixtures.Docker.Gateways;
using System.Diagnostics;
using System.Text;
using System;
using System.Collections.Generic;
using System.Net;

namespace Mittons.Fixtures.Tests.Integration.Docker.Gateways
{
    public class DefaultDockerGatewayTests
    {
        public class ContainerTests : IDisposable
        {
            private List<string> containerIds = new List<string>();

            public void Dispose()
            {
                foreach(var containerId in containerIds)
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

            [Theory]
            [InlineData("alpine:3.15")]
            [InlineData("alpine:3.14")]
            public void ContainerRun_WhenCalledWithAnImage_ExpectContainerToBeForTheRequestedImage(string imageName)
            {
                // Arrange
                var gateway = new DefaultDockerGateway();

                // Act
                var containerId = gateway.ContainerRun(imageName, string.Empty);
                containerIds.Add(containerId);

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
                containerIds.Add(containerId);

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
                containerIds.Add(containerId);

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
                containerIds.Add(containerId);

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

                var containerId = gateway.ContainerRun("atmoz/sftp", "guest:guest");
                containerIds.Add(containerId);

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
        }

        public class NetworkTests : IDisposable
        {
            private List<string> _networkNames = new List<string>();

            public void Dispose()
            {
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
                gateway.NetworkCreate(uniqueName);

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

                gateway.NetworkCreate(uniqueName);

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
        }
    }
}