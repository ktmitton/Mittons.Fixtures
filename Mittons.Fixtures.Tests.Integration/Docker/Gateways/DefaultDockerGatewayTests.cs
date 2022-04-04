using Xunit;
using Mittons.Fixtures.Docker.Gateways;
using System.Diagnostics;
using System.Text;
using System;
using System.Collections.Generic;

namespace Mittons.Fixtures.Tests.Integration.Docker.Gateways
{
    public class DefaultDockerGatewayTests : IDisposable
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
        public void Run_WhenCalledWithAnImage_ExpectContainerToBeForTheRequestedImage(string imageName)
        {
            // Arrange
            var gateway = new DefaultDockerGateway();

            // Act
            var containerId = gateway.Run(imageName, string.Empty);
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
        public void Run_WhenCalledForAlpineWithNoCommand_ExpectContainerToHaveStartedWithTheDefaultCommand()
        {
            // Arrange
            var gateway = new DefaultDockerGateway();

            // Act
            var containerId = gateway.Run("alpine:3.15", string.Empty);
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
        public void Run_WhenCalledForAlpineWithACommand_ExpectContainerToHaveStartedWithTheCommand()
        {
            // Arrange
            var gateway = new DefaultDockerGateway();

            // Act
            var containerId = gateway.Run("alpine:3.15", "/bin/bash");
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
        public void Remove_WhenTheContainerDoesNotExist_ExpectSuccessfulReturn()
        {
            // Arrange
            var gateway = new DefaultDockerGateway();

            // Act
            // Assert
            gateway.Remove("cd898788786795df83dbf414bbcc9e6c6be9d4bc932e96a6542c03d033e1cc72");
        }

        [Fact]
        public void Remove_WhenTheContainerExists_ExpectTheContainerToBeRemoved()
        {
            // Arrange
            var gateway = new DefaultDockerGateway();

            var containerId = gateway.Run("alpine:3.15", string.Empty);
            containerIds.Add(containerId);

            // Act
            gateway.Remove(containerId);

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
    }
}