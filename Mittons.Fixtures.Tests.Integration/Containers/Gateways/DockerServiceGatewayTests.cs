// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.Linq;
// using System.Runtime.InteropServices;
// using System.Text.Json;
// using System.Threading;
// using System.Threading.Tasks;
// using Mittons.Fixtures.Attributes;
// using Mittons.Fixtures.Containers.Attributes;
// using Mittons.Fixtures.Containers.Gateways;
// using Mittons.Fixtures.Exceptions.Containers;
// using Xunit;

// namespace Mittons.Fixtures.Tests.Integration.Containers.Gateways;

// public class DockerServiceGatewayTests
// {
//     public class RemoveServiceTests : Xunit.IAsyncLifetime
//     {
//         private readonly List<string> _containerIds = new List<string>();

//         private readonly CancellationToken _cancellationToken;

//         public RemoveServiceTests()
//         {
//             var cancellationTokenSource = new CancellationTokenSource();
//             cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

//             _cancellationToken = cancellationTokenSource.Token;
//         }

//         public async Task DisposeAsync()
//         {
//             foreach (var containerId in _containerIds)
//             {
//                 using var process = new Process();
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"rm --force {containerId}";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);
//             }
//         }

//         public Task InitializeAsync()
//             => Task.CompletedTask;

//         [Fact]
//         public async Task RemoveServiceAsync_WhenCalled_ExpectTheServiceToBeRemoved()
//         {
//             // Arrange
//             var attributes = new Attribute[] { new ImageAttribute("alpine:3.15"), new RunAttribute() };

//             var serviceGateway = new DockerServiceGateway();

//             var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken);

//             _containerIds.Add(service.ServiceId);

//             // Act
//             await serviceGateway.RemoveServiceAsync(service, _cancellationToken).ConfigureAwait(false);

//             // Assert
//             using (var process = new Process())
//             {
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"ps -a --filter id={service.ServiceId} --format \"{{{{.ID}}}}\"";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);

//                 var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
//                 Assert.Empty(output);
//             }
//         }
//     }

//     public class OptionTests : Xunit.IAsyncLifetime
//     {
//         private readonly List<string> _containerIds = new List<string>();

//         private readonly CancellationToken _cancellationToken;

//         public OptionTests()
//         {
//             var cancellationTokenSource = new CancellationTokenSource();
//             cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

//             _cancellationToken = cancellationTokenSource.Token;
//         }

//         public async Task DisposeAsync()
//         {
//             foreach (var containerId in _containerIds)
//             {
//                 using var process = new Process();
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"rm --force {containerId}";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);
//             }
//         }

//         public Task InitializeAsync()
//             => Task.CompletedTask;

//         [Fact]
//         public async Task InitializedServiceAsync_WhenCalledWithNoHealthCheck_ExpectNoHealthSettingsToBeSet()
//         {
//             // Arrange
//             var attributes = new Attribute[] { new ImageAttribute("alpine:3.15"), new RunAttribute() };
//             var serviceGateway = new DockerServiceGateway();

//             // Act
//             var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken).ConfigureAwait(false);
//             _containerIds.Add(service.ServiceId);

//             // Assert
//             using (var process = new Process())
//             {
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"inspect {service.ServiceId} --format \"{{{{json .Config.Healthcheck}}}}\"";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);

//                 var healthCheck = JsonSerializer.Deserialize<HealthCheck>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false));

//                 Assert.Null(healthCheck);
//             }
//         }

//         [Fact]
//         public async Task InitializedServiceAsync_WhenCalledWithADisabledHealthCheck_ExpectHealthChecksToBeDisabled()
//         {
//             // Arrange
//             var attributes = new Attribute[] { new ImageAttribute("alpine:3.15"), new HealthCheckAttribute { Disabled = true }, new RunAttribute() };
//             var serviceGateway = new DockerServiceGateway();

//             // Act
//             var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken).ConfigureAwait(false);
//             _containerIds.Add(service.ServiceId);

//             // Assert
//             using (var process = new Process())
//             {
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"inspect {service.ServiceId} --format \"{{{{json .Config.Healthcheck}}}}\"";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);

//                 var healthCheck = JsonSerializer.Deserialize<HealthCheck>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false));

//                 Assert.Equal("NONE", string.Join(" ", healthCheck?.Test ?? new string[0]));
//             }
//         }

//         [Fact]
//         public async Task InitializedServiceAsync_WhenCalled_ExpectRunOptionsToBeApplied()
//         {
//             // Arrange
//             var attributes = new Attribute[] { new ImageAttribute("alpine:3.15"), new HealthCheckAttribute { Disabled = true }, new RunAttribute() };
//             var serviceGateway = new DockerServiceGateway();

//             // Act
//             var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken).ConfigureAwait(false);
//             _containerIds.Add(service.ServiceId);

//             // Assert
//             using (var process = new Process())
//             {
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"inspect {service.ServiceId} --format \"{{{{json .Config.Labels}}}}\"";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);

//                 var labels = JsonSerializer.Deserialize<Dictionary<string, string>>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false));

//                 Assert.Contains(labels, x => x.Key == "mittons.fixtures.run.id" && x.Value == RunAttribute.DefaultId);
//             }
//         }

//         [Theory]
//         [InlineData("test command", 1, 1, 1, 1)]
//         [InlineData("test2", 0, 0, 0, 0)]
//         [InlineData("test2", 2, 3, 4, 5)]
//         public async Task InitializedServiceAsync_WhenCalledWithAHealthCheck_ExpectHealthChecksToBeSet(string expectedCommand, byte expectedInterval, byte expectedTimeout, byte expectedStartPeriod, byte expectedRetries)
//         {
//             // Arrange
//             long nanosecondModifier = 1000000000;

//             var attributes = new Attribute[]
//             {
//                 new ImageAttribute("alpine:3.15"),
//                 new HealthCheckAttribute
//                 {
//                     Command = expectedCommand,
//                     Interval = expectedInterval,
//                     Timeout = expectedTimeout,
//                     StartPeriod = expectedStartPeriod,
//                     Retries = expectedRetries
//                 },
//                 new RunAttribute()
//             };
//             var serviceGateway = new DockerServiceGateway();

//             // Act
//             var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken).ConfigureAwait(false);
//             _containerIds.Add(service.ServiceId);

//             // Assert
//             using (var process = new Process())
//             {
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"inspect {service.ServiceId} --format \"{{{{json .Config.Healthcheck}}}}\"";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);

//                 var healthCheck = JsonSerializer.Deserialize<HealthCheck>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false));

//                 Assert.Equal($"CMD-SHELL {expectedCommand}", string.Join(" ", healthCheck?.Test ?? new string[0]));
//                 Assert.Equal(expectedInterval * nanosecondModifier, healthCheck?.Interval);
//                 Assert.Equal(expectedTimeout * nanosecondModifier, healthCheck?.Timeout);
//                 Assert.Equal(expectedStartPeriod * nanosecondModifier, healthCheck?.StartPeriod);
//                 Assert.Equal(expectedRetries, healthCheck?.Retries);
//             }
//         }

//         private class HealthCheck
//         {
//             public string[] Test { get; set; } = Array.Empty<string>();

//             public long Interval { get; set; }

//             public long Timeout { get; set; }

//             public long StartPeriod { get; set; }

//             public byte Retries { get; set; }
//         }
//     }

//     public class CreateServiceTests : Xunit.IAsyncLifetime
//     {
//         private readonly List<string> _containerIds = new List<string>();

//         private readonly CancellationToken _cancellationToken;

//         public CreateServiceTests()
//         {
//             var cancellationTokenSource = new CancellationTokenSource();
//             cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

//             _cancellationToken = cancellationTokenSource.Token;
//         }

//         public async Task DisposeAsync()
//         {
//             foreach (var containerId in _containerIds)
//             {
//                 using var process = new Process();
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"rm --force {containerId}";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);
//             }
//         }

//         public Task InitializeAsync()
//             => Task.CompletedTask;

//         [Fact]
//         public async Task CreateServiceAsync_WhenCalledWithoutAnImageName_ExpectAnErrorToBeThrown()
//         {
//             // Arrange
//             var attributes = Enumerable.Empty<Attribute>();
//             var serviceGateway = new DockerServiceGateway();

//             // Act
//             // Assert
//             await Assert.ThrowsAsync<ImageNameMissingException>(() => serviceGateway.CreateServiceAsync(attributes, _cancellationToken)).ConfigureAwait(false);
//         }

//         [Fact]
//         public async Task CreateServiceAsync_WhenCalledWitMultipleImageNames_ExpectAnErrorToBeThrown()
//         {
//             // Arrange
//             var attributes = new[] { new ImageAttribute("alpine:3.15"), new ImageAttribute("alpine:3.14") };
//             var serviceGateway = new DockerServiceGateway();

//             // Act
//             // Assert
//             await Assert.ThrowsAsync<MultipleImageNamesProvidedException>(() => serviceGateway.CreateServiceAsync(attributes, _cancellationToken)).ConfigureAwait(false);
//         }

//         [Theory]
//         [InlineData("alpine:3.15")]
//         [InlineData("alpine:3.14")]
//         public async Task InitializedServiceAsync_WhenCalledWithAnImageName_ExpectTheImageToBeCreated(string imageName)
//         {
//             // Arrange
//             var attributes = new Attribute[] { new ImageAttribute(imageName), new RunAttribute() };
//             var serviceGateway = new DockerServiceGateway();

//             // Act
//             var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken).ConfigureAwait(false);
//             _containerIds.Add(service.ServiceId);

//             // Assert
//             using (var process = new Process())
//             {
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"inspect {service.ServiceId} --format \"{{{{.Config.Image}}}}\"";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);

//                 var output = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);

//                 Assert.Equal(imageName, output);
//             }
//         }

//         [Theory]
//         [InlineData("test")]
//         [InlineData("test test2")]
//         public async Task InitializedServiceAsync_WhenCalledWithACommand_ExpectTheContainerToApplyTheCommand(string command)
//         {
//             // Arrange
//             var commandParts = command.Split(" ");

//             var attributes = new List<Attribute> { new ImageAttribute("alpine:3.15"), new RunAttribute() };
//             attributes.AddRange(commandParts.Select(x => new CommandAttribute(x)));

//             var serviceGateway = new DockerServiceGateway();

//             // Act
//             var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken).ConfigureAwait(false);
//             _containerIds.Add(service.ServiceId);

//             // Assert
//             using (var process = new Process())
//             {
//                 process.StartInfo.FileName = "docker";
//                 process.StartInfo.Arguments = $"inspect {service.ServiceId} --format \"{{{{json .Config.Cmd}}}}\"";
//                 process.StartInfo.UseShellExecute = false;
//                 process.StartInfo.RedirectStandardOutput = true;

//                 process.Start();
//                 await process.WaitForExitAsync().ConfigureAwait(false);

//                 var output = JsonSerializer.Deserialize<string[]>(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false)) ?? new string[0];

//                 Assert.Equal(commandParts.Length, output.Length);
//                 Assert.All(commandParts, x => Assert.Contains(x, output));
//             }
//         }

//         [Theory]
//         [InlineData("atmoz/sftp:alpine", "guest:guest", 22, "tcp")]
//         [InlineData("redis:alpine", "", 6379, "tcp")]
//         public async Task InitializedServiceAsync_WhenCalledForAnImageWithAnExposedPort_ExpectPortToBePublished(string imageName, string command, int port, string scheme)
//         {
//             // Arrange
//             var attributes = new Attribute[] { new ImageAttribute(imageName), new CommandAttribute(command), new RunAttribute() };
//             var serviceGateway = new DockerServiceGateway();

//             var expectedUriBuilder = new UriBuilder();
//             expectedUriBuilder.Scheme = scheme;

//             // Act
//             var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken).ConfigureAwait(false);
//             _containerIds.Add(service.ServiceId);

//             // Assert
//             if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//             {
//                 expectedUriBuilder.Host = "localhost";

//                 using (var portProcess = new Process())
//                 {
//                     portProcess.StartInfo.FileName = "docker";
//                     portProcess.StartInfo.Arguments = $"port {service.ServiceId} {port}";
//                     portProcess.StartInfo.UseShellExecute = false;
//                     portProcess.StartInfo.RedirectStandardOutput = true;

//                     portProcess.Start();
//                     await portProcess.WaitForExitAsync().ConfigureAwait(false);

//                     var portDetails = (await portProcess.StandardOutput.ReadLineAsync().ConfigureAwait(false) ?? string.Empty).Split(":");

//                     Assert.Equal(2, portDetails.Length);

//                     expectedUriBuilder.Port = int.Parse(portDetails[1]);
//                 }
//             }
//             else
//             {
//                 using (var ipProcess = new Process())
//                 {
//                     ipProcess.StartInfo.FileName = "docker";
//                     ipProcess.StartInfo.Arguments = $"inspect {service.ServiceId} --format \"{{{{.NetworkSettings.IPAddress}}}}\"";
//                     ipProcess.StartInfo.UseShellExecute = false;
//                     ipProcess.StartInfo.RedirectStandardOutput = true;

//                     ipProcess.Start();
//                     await ipProcess.WaitForExitAsync().ConfigureAwait(false);

//                     expectedUriBuilder.Host = await ipProcess.StandardOutput.ReadLineAsync().ConfigureAwait(false);
//                 }

//                 expectedUriBuilder.Port = port;
//             }

//             Assert.Equal(expectedUriBuilder.Uri, service.Resources.Single(x => x.GuestUri.Scheme == scheme && x.GuestUri.Port == port).HostUri);
//         }
//     }
// }
