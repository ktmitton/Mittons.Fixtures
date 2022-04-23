using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Exceptions;
using Mittons.Fixtures.Exceptions.Containers;
using Mittons.Fixtures.Models;
using Mittons.Fixtures.Resources;
using Xunit;

namespace Mittons.Fixtures.Tests.Integration.Docker.Gateways;

public class ContainerGatewayTests
{
    public class RemoveServiceTests : IDisposable
    {
        private readonly List<string> _containerIds = new List<string>();

        private readonly CancellationToken _cancellationToken;

        public RemoveServiceTests()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

            _cancellationToken = cancellationTokenSource.Token;
        }

        public void Dispose()
        {
            foreach (var containerId in _containerIds)
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
        public async Task RemoveServiceAsync_WhenCalled_ExpectTheServiceToBeRemoved()
        {
            // Arrange
            var attributes = new[] { new ImageAttribute("alpine:3.15") };

            var serviceGateway = new ContainerGateway();

            var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken);

            _containerIds.Add(service.ContainerId);

            // Act
            await serviceGateway.RemoveServiceAsync(service, _cancellationToken);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"ps -a --filter id={service.ContainerId} --format \"{{{{.ID}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                process.WaitForExit();

                var output = process.StandardOutput.ReadToEnd();
                Assert.Empty(output);
            }
        }
    }

    public class CreateServiceTests : IDisposable
    {
        private readonly List<string> _containerIds = new List<string>();

        private readonly CancellationToken _cancellationToken;

        public CreateServiceTests()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

            _cancellationToken = cancellationTokenSource.Token;
        }

        public void Dispose()
        {
            foreach (var containerId in _containerIds)
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
        public async Task CreateServiceAsync_WhenCalledWithoutAnImageName_ExpectAnErrorToBeThrown()
        {
            // Arrange
            var attributes = Enumerable.Empty<Attribute>();
            var serviceGateway = new ContainerGateway();

            // Act
            // Assert
            await Assert.ThrowsAsync<ImageNameMissingException>(() => serviceGateway.CreateServiceAsync(attributes, _cancellationToken));
        }

        [Fact]
        public async Task CreateServiceAsync_WhenCalledWitMultipleImageNames_ExpectAnErrorToBeThrown()
        {
            // Arrange
            var attributes = new[] { new ImageAttribute("alpine:3.15"), new ImageAttribute("alpine:3.14") };
            var serviceGateway = new ContainerGateway();

            // Act
            // Assert
            await Assert.ThrowsAsync<MultipleImageNamesProvidedException>(() => serviceGateway.CreateServiceAsync(attributes, _cancellationToken));
        }

        [Theory]
        [InlineData("alpine:3.15")]
        [InlineData("alpine:3.14")]
        public async Task InitializedServiceAsync_WhenCalledWithAnImageName_ExpectTheImageToBeCreated(string imageName)
        {
            // Arrange
            var attributes = new[] { new ImageAttribute(imageName) };
            var serviceGateway = new ContainerGateway();

            // Act
            var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken);
            _containerIds.Add(service.ContainerId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"inspect {service.ContainerId} --format \"{{{{.Config.Image}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                process.WaitForExit();

                var output = process.StandardOutput.ReadLine();

                Assert.Equal(imageName, output);
            }
        }

        [Theory]
        [InlineData("test")]
        [InlineData("test test2")]
        public async Task InitializedServiceAsync_WhenCalledWithACommand_ExpectTheContainerToApplyTheCommand(string command)
        {
            // Arrange
            var commandParts = command.Split(" ");

            var attributes = new List<Attribute> { new ImageAttribute("alpine:3.15") };
            attributes.AddRange(commandParts.Select(x => new CommandAttribute(x)));

            var serviceGateway = new ContainerGateway();

            // Act
            var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken);
            _containerIds.Add(service.ContainerId);

            // Assert
            using (var process = new Process())
            {
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = $"inspect {service.ContainerId} --format \"{{{{json .Config.Cmd}}}}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                process.WaitForExit();

                var output = JsonSerializer.Deserialize<string[]>(await process.StandardOutput.ReadToEndAsync()) ?? new string[0];

                Assert.Equal(commandParts.Length, output.Length);
                Assert.All(commandParts, x => Assert.Contains(x, output));
            }
        }

        [Theory]
        [InlineData("atmoz/sftp:alpine", "guest:guest", 22, "tcp")]
        [InlineData("redis:alpine", "", 6379, "tcp")]
        public async Task InitializedServiceAsync_WhenCalledForAnImageWithAnExposedPort_ExpectPortToBePublished(string imageName, string command, int port, string scheme)
        {
            // Arrange
            var attributes = new Attribute[] { new ImageAttribute(imageName), new CommandAttribute(command) };
            var serviceGateway = new ContainerGateway();

            var expectedUriBuilder = new UriBuilder();
            expectedUriBuilder.Scheme = scheme;

            // Act
            var service = await serviceGateway.CreateServiceAsync(attributes, _cancellationToken);
            _containerIds.Add(service.ContainerId);

            // Assert
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                expectedUriBuilder.Host = "localhost";

                using (var portProcess = new Process())
                {
                    portProcess.StartInfo.FileName = "docker";
                    portProcess.StartInfo.Arguments = $"port {service.ContainerId} {port}";
                    portProcess.StartInfo.UseShellExecute = false;
                    portProcess.StartInfo.RedirectStandardOutput = true;

                    portProcess.Start();
                    portProcess.WaitForExit();

                    var portDetails = (await portProcess.StandardOutput.ReadLineAsync() ?? string.Empty).Split(":");

                    Assert.Equal(2, portDetails.Length);

                    expectedUriBuilder.Port = int.Parse(portDetails[1]);
                }
            }
            else
            {
                using (var ipProcess = new Process())
                {
                    ipProcess.StartInfo.FileName = "docker";
                    ipProcess.StartInfo.Arguments = $"inspect {service.ContainerId} --format \"{{{{.NetworkSettings.IPAddress}}}}\"";
                    ipProcess.StartInfo.UseShellExecute = false;
                    ipProcess.StartInfo.RedirectStandardOutput = true;

                    ipProcess.Start();
                    ipProcess.WaitForExit();

                    expectedUriBuilder.Host = await ipProcess.StandardOutput.ReadLineAsync();
                }

                expectedUriBuilder.Port = port;
            }

            Assert.Equal(expectedUriBuilder.Uri, service.ServiceResources.Single(x => x.GuestUri.Scheme == scheme && x.GuestUri.Port == port).HostUri);
        }
    }

    public class RunTests : IDisposable
    {
        private readonly List<string> _containerIds = new List<string>();

        private readonly CancellationToken _cancellationToken;

        public RunTests()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

            _cancellationToken = cancellationTokenSource.Token;
        }

        public void Dispose()
        {
            foreach (var containerId in _containerIds)
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
        public async Task RunAsync_WhenCalledWithLabels_ExpectContainerToHaveTheLabelsApplied()
        {
            // Arrange
            var imageName = "alpine:3.15";
            var containerGateway = new ContainerGateway();

            // Act
            var containerId = await containerGateway.RunAsync(
                    imageName,
                    string.Empty,
                    new List<Option>
                    {
                        new Option
                        {
                            Name = "--label",
                            Value = "first=second"
                        },
                        new Option
                        {
                            Name = "--label",
                            Value = "third=fourth"
                        }
                    },
                    _cancellationToken
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

            Assert.Contains(actualLabels, x => x.Key == "first" && x.Value == "second");
            Assert.Contains(actualLabels, x => x.Key == "third" && x.Value == "fourth");
        }
    }

    // public class GetDefaultNetworkIpAddressTests : IDisposable
    // {
    //     private readonly List<string> _containerIds = new List<string>();

    //     private readonly CancellationToken _cancellationToken;

    //     public GetDefaultNetworkIpAddressTests()
    //     {
    //         var cancellationTokenSource = new CancellationTokenSource();
    //         cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

    //         _cancellationToken = cancellationTokenSource.Token;
    //     }

    //     public void Dispose()
    //     {
    //         foreach (var containerId in _containerIds)
    //         {
    //             using var proc = new Process();
    //             proc.StartInfo.FileName = "docker";
    //             proc.StartInfo.Arguments = $"rm --force {containerId}";
    //             proc.StartInfo.UseShellExecute = false;
    //             proc.StartInfo.RedirectStandardOutput = true;

    //             proc.Start();
    //             proc.WaitForExit();
    //         }
    //     }

    //     [Fact]
    //     public async Task GetDefaultNetworkIpAddress_WhenTheContainerIsOnOneNetwork_ReturnsTheIpAddressForTheNetwork()
    //     {
    //         // Arrange
    //         var containerGateway = new ContainerGateway();

    //         var containerId = await containerGateway.RunAsync("atmoz/sftp:alpine", "guest:guest", Enumerable.Empty<Option>(), _cancellationToken);
    //         _containerIds.Add(containerId);

    //         // Act
    //         var ipAddress = await containerGateway.GetDefaultNetworkIpAddressAsync(containerId, _cancellationToken);

    //         // Assert
    //         using var proc = new Process();
    //         proc.StartInfo.FileName = "docker";
    //         proc.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{range .NetworkSettings.Networks}}}}{{{{printf \\\"%s\\n\\\" .IPAddress}}}}{{{{end}}}}\"";
    //         proc.StartInfo.UseShellExecute = false;
    //         proc.StartInfo.RedirectStandardOutput = true;

    //         proc.Start();
    //         proc.WaitForExit();

    //         IPAddress.TryParse(proc.StandardOutput.ReadLine(), out var expectedIpAddress);

    //         Assert.Equal(expectedIpAddress, ipAddress);
    //     }
    // }

    // public class AddFile : IDisposable
    // {
    //     private readonly List<string> _containerIds = new List<string>();

    //     private readonly List<string> _filenames = new List<string>();

    //     private readonly CancellationToken _cancellationToken;

    //     public AddFile()
    //     {
    //         var cancellationTokenSource = new CancellationTokenSource();
    //         cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

    //         _cancellationToken = cancellationTokenSource.Token;
    //     }

    //     public void Dispose()
    //     {
    //         foreach (var containerId in _containerIds)
    //         {
    //             using var proc = new Process();
    //             proc.StartInfo.FileName = "docker";
    //             proc.StartInfo.Arguments = $"rm --force {containerId}";
    //             proc.StartInfo.UseShellExecute = false;
    //             proc.StartInfo.RedirectStandardOutput = true;

    //             proc.Start();
    //             proc.WaitForExit();
    //         }

    //         foreach (var filename in _filenames)
    //         {
    //             File.Delete(filename);
    //         }
    //     }

    //     [Fact]
    //     public async Task AddFile_WhenCalledForMissingDirectory_ExpectDirectoryToBeCreated()
    //     {
    //         // Arrange
    //         var files = new (string ContainerFilename, string TemporaryFilename)[]
    //         {
    //             ("/tmp2/test.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())),
    //             ("/tmp3/temp4/test2.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
    //         };

    //         _filenames.AddRange(files.Select(x => x.TemporaryFilename));

    //         var fileContents = "hello, world";

    //         foreach (var file in files)
    //         {
    //             File.WriteAllText(file.TemporaryFilename, fileContents);
    //         }

    //         var containerGateway = new ContainerGateway();

    //         var containerId = await containerGateway.RunAsync("atmoz/sftp:alpine", "guest:guest", Enumerable.Empty<Option>(), _cancellationToken);
    //         _containerIds.Add(containerId);

    //         // Act
    //         var tasks = files.Select(x => containerGateway.AddFileAsync(containerId, x.TemporaryFilename, x.ContainerFilename, default(string), default(string), _cancellationToken)).ToList();
    //         await Task.WhenAll(tasks);

    //         // Assert
    //         Assert.All(files, file =>
    //         {
    //             using (var process = new Process())
    //             {
    //                 process.StartInfo.FileName = "docker";
    //                 process.StartInfo.Arguments = $"exec {containerId} cat {file.ContainerFilename}";
    //                 process.StartInfo.UseShellExecute = false;
    //                 process.StartInfo.RedirectStandardOutput = true;

    //                 process.Start();
    //                 process.WaitForExit();

    //                 var output = process.StandardOutput.ReadToEnd();

    //                 Assert.Equal(fileContents, output);
    //             }
    //         });
    //     }

    //     [Fact]
    //     public async Task AddFile_WhenCalled_ExpectFileToBeCopiedToTheContainer()
    //     {
    //         // Arrange
    //         var files = new (string FileContents, string ContainerFilename, string TemporaryFilename)[]
    //         {
    //             ("test", "/tmp/test.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())),
    //             ("test\nfile", "/tmp/test2.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())),
    //             ("file\ntest", "/test.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
    //         };

    //         _filenames.AddRange(files.Select(x => x.TemporaryFilename));

    //         foreach (var file in files)
    //         {
    //             File.WriteAllText(file.TemporaryFilename, file.FileContents);
    //         }

    //         var containerGateway = new ContainerGateway();

    //         var containerId = await containerGateway.RunAsync("atmoz/sftp:alpine", "guest:guest", Enumerable.Empty<Option>(), _cancellationToken);
    //         _containerIds.Add(containerId);

    //         // Act
    //         var tasks = files.Select(x => containerGateway.AddFileAsync(containerId, x.TemporaryFilename, x.ContainerFilename, default(string), default(string), _cancellationToken)).ToList();
    //         await Task.WhenAll(tasks);

    //         // Assert
    //         Assert.All(files, file =>
    //         {
    //             using (var process = new Process())
    //             {
    //                 process.StartInfo.FileName = "docker";
    //                 process.StartInfo.Arguments = $"exec {containerId} cat {file.ContainerFilename}";
    //                 process.StartInfo.UseShellExecute = false;
    //                 process.StartInfo.RedirectStandardOutput = true;

    //                 process.Start();
    //                 process.WaitForExit();

    //                 var output = process.StandardOutput.ReadToEnd();

    //                 Assert.Equal(file.FileContents, output);
    //             }
    //         });
    //     }

    //     [Fact]
    //     public async Task AddFile_WhenCalledWithPermissions_ExpectThePermissionsToBeSet()
    //     {
    //         // Arrange
    //         var files = new (string Permissions, string ContainerFilename, string TemporaryFilename)[]
    //         {
    //             ("777", "/tmp/test.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())),
    //             ("757", "/tmp/test2.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())),
    //             ("557", "/test.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
    //         };

    //         _filenames.AddRange(files.Select(x => x.TemporaryFilename));

    //         foreach (var file in files)
    //         {
    //             File.WriteAllText(file.TemporaryFilename, "hello, world");
    //         }

    //         var containerGateway = new ContainerGateway();

    //         var containerId = await containerGateway.RunAsync("atmoz/sftp:alpine", "guest:guest", Enumerable.Empty<Option>(), _cancellationToken);
    //         _containerIds.Add(containerId);

    //         // Act
    //         var tasks = files.Select(x => containerGateway.AddFileAsync(containerId, x.TemporaryFilename, x.ContainerFilename, default(string), x.Permissions, _cancellationToken)).ToList();
    //         await Task.WhenAll(tasks);

    //         // Assert
    //         Assert.All(files, file =>
    //         {
    //             using (var process = new Process())
    //             {
    //                 process.StartInfo.FileName = "docker";
    //                 process.StartInfo.Arguments = $"exec {containerId} stat -c \"%a\" {file.ContainerFilename}";
    //                 process.StartInfo.UseShellExecute = false;
    //                 process.StartInfo.RedirectStandardOutput = true;

    //                 process.Start();
    //                 process.WaitForExit();

    //                 var output = process.StandardOutput.ReadLine();

    //                 Assert.Equal(file.Permissions, output);
    //             }
    //         });
    //     }

    //     [Fact]
    //     public async Task AddFile_WhenCalledWithAnOwner_ExpectThePermissionsToBeSet()
    //     {
    //         // Arrange
    //         var files = new (string Owner, string ContainerFilename, string TemporaryFilename)[]
    //         {
    //             ("guest", "/tmp/test.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())),
    //             ("tester", "/tmp/test2.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())),
    //             ("root", "/test.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
    //         };

    //         _filenames.AddRange(files.Select(x => x.TemporaryFilename));

    //         foreach (var file in files)
    //         {
    //             File.WriteAllText(file.TemporaryFilename, "hello, world");
    //         }

    //         var containerGateway = new ContainerGateway();

    //         var containerId = await containerGateway.RunAsync("atmoz/sftp:alpine", "guest:guest tester:tester", Enumerable.Empty<Option>(), _cancellationToken);
    //         _containerIds.Add(containerId);

    //         // Act
    //         var tasks = files.Select(x => containerGateway.AddFileAsync(containerId, x.TemporaryFilename, x.ContainerFilename, x.Owner, default(string), _cancellationToken)).ToList();
    //         await Task.WhenAll(tasks);

    //         // Assert
    //         Assert.All(files, file =>
    //         {
    //             using (var process = new Process())
    //             {
    //                 process.StartInfo.FileName = "docker";
    //                 process.StartInfo.Arguments = $"exec {containerId} stat -c \"%U\" {file.ContainerFilename}";
    //                 process.StartInfo.UseShellExecute = false;
    //                 process.StartInfo.RedirectStandardOutput = true;

    //                 process.Start();
    //                 process.WaitForExit();

    //                 var output = process.StandardOutput.ReadLine();

    //                 Assert.Equal(file.Owner, output);
    //             }
    //         });
    //     }
    // }

    // public class RemoveFile : IDisposable
    // {
    //     private readonly List<string> _containerIds = new List<string>();

    //     private readonly List<string> _filenames = new List<string>();

    //     private readonly CancellationToken _cancellationToken;

    //     public RemoveFile()
    //     {
    //         var cancellationTokenSource = new CancellationTokenSource();
    //         cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

    //         _cancellationToken = cancellationTokenSource.Token;
    //     }

    //     public void Dispose()
    //     {
    //         foreach (var containerId in _containerIds)
    //         {
    //             using var proc = new Process();
    //             proc.StartInfo.FileName = "docker";
    //             proc.StartInfo.Arguments = $"rm --force {containerId}";
    //             proc.StartInfo.UseShellExecute = false;
    //             proc.StartInfo.RedirectStandardOutput = true;

    //             proc.Start();
    //             proc.WaitForExit();
    //         }

    //         foreach (var filename in _filenames)
    //         {
    //             File.Delete(filename);
    //         }
    //     }

    //     [Fact]
    //     public async Task RemoveFile_WhenCalled_ExpectFileToBeRemoved()
    //     {
    //         // Arrange
    //         var files = new (string ContainerFilename, string TemporaryFilename)[]
    //         {
    //             ("/tmp/test.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())),
    //             ("/tmp/test2.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())),
    //             ("/test.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
    //         };

    //         _filenames.AddRange(files.Select(x => x.TemporaryFilename));

    //         var fileContents = "hello, world";

    //         foreach (var file in files)
    //         {
    //             File.WriteAllText(file.TemporaryFilename, fileContents);
    //         }

    //         var containerGateway = new ContainerGateway();

    //         var containerId = await containerGateway.RunAsync("atmoz/sftp:alpine", "guest:guest tester:tester", Enumerable.Empty<Option>(), _cancellationToken);
    //         _containerIds.Add(containerId);

    //         var addTasks = files.Select(x => containerGateway.AddFileAsync(containerId, x.TemporaryFilename, x.ContainerFilename, default(string), default(string), _cancellationToken)).ToList();
    //         await Task.WhenAll(addTasks);

    //         // Act
    //         var removeTasks = files.Select(x => containerGateway.RemoveFileAsync(containerId, x.ContainerFilename, _cancellationToken)).ToList();
    //         await Task.WhenAll(removeTasks);

    //         // Assert
    //         Assert.All(files, file =>
    //         {
    //             using (var process = new Process())
    //             {
    //                 process.StartInfo.FileName = "docker";
    //                 process.StartInfo.Arguments = $"exec {containerId} ls {file.ContainerFilename}";
    //                 process.StartInfo.UseShellExecute = false;
    //                 process.StartInfo.RedirectStandardOutput = true;

    //                 process.Start();
    //                 process.WaitForExit();

    //                 var output = process.StandardOutput.ReadToEnd();

    //                 Assert.Empty(output);
    //             }
    //         });
    //     }
    // }

    // public class EmptyDirectory : IDisposable
    // {
    //     private readonly List<string> _containerIds = new List<string>();

    //     private readonly List<string> _filenames = new List<string>();

    //     private readonly CancellationToken _cancellationToken;

    //     public EmptyDirectory()
    //     {
    //         var cancellationTokenSource = new CancellationTokenSource();
    //         cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

    //         _cancellationToken = cancellationTokenSource.Token;
    //     }

    //     public void Dispose()
    //     {
    //         foreach (var containerId in _containerIds)
    //         {
    //             using var proc = new Process();
    //             proc.StartInfo.FileName = "docker";
    //             proc.StartInfo.Arguments = $"rm --force {containerId}";
    //             proc.StartInfo.UseShellExecute = false;
    //             proc.StartInfo.RedirectStandardOutput = true;

    //             proc.Start();
    //             proc.WaitForExit();
    //         }

    //         foreach (var filename in _filenames)
    //         {
    //             File.Delete(filename);
    //         }
    //     }

    //     [Fact]
    //     public async Task EmptyDirectory_WhenCalled_ExpectDirectoryToBeEmptied()
    //     {
    //         // Arrange
    //         var files = new (string ContainerFilename, string TemporaryFilename)[]
    //         {
    //             ("/tmp/test.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())),
    //             ("/tmp/test/test2.txt", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
    //         };

    //         _filenames.AddRange(files.Select(x => x.TemporaryFilename));

    //         var fileContents = "hello, world";

    //         foreach (var file in files)
    //         {
    //             File.WriteAllText(file.TemporaryFilename, fileContents);
    //         }

    //         var containerGateway = new ContainerGateway();

    //         var containerId = await containerGateway.RunAsync("atmoz/sftp:alpine", "guest:guest tester:tester", Enumerable.Empty<Option>(), _cancellationToken);
    //         _containerIds.Add(containerId);

    //         var addTasks = files.Select(x => containerGateway.AddFileAsync(containerId, x.TemporaryFilename, x.ContainerFilename, default(string), default(string), _cancellationToken)).ToList();
    //         await Task.WhenAll(addTasks);

    //         // Act
    //         await containerGateway.EmptyDirectoryAsync(containerId, "/tmp", _cancellationToken);

    //         // Assert
    //         using var process = new Process();
    //         process.StartInfo.FileName = "docker";
    //         process.StartInfo.Arguments = $"exec {containerId} ls /tmp";
    //         process.StartInfo.UseShellExecute = false;
    //         process.StartInfo.RedirectStandardOutput = true;

    //         process.Start();
    //         process.WaitForExit();

    //         var output = process.StandardOutput.ReadToEnd();

    //         Assert.Empty(output);
    //     }
    // }

    // public class ExecuteCommandTests : IDisposable
    // {
    //     private readonly List<string> _containerIds = new List<string>();

    //     private readonly CancellationToken _cancellationToken;

    //     public ExecuteCommandTests()
    //     {
    //         var cancellationTokenSource = new CancellationTokenSource();
    //         cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

    //         _cancellationToken = cancellationTokenSource.Token;
    //     }

    //     public void Dispose()
    //     {
    //         foreach (var containerId in _containerIds)
    //         {
    //             using var proc = new Process();
    //             proc.StartInfo.FileName = "docker";
    //             proc.StartInfo.Arguments = $"rm --force {containerId}";
    //             proc.StartInfo.UseShellExecute = false;
    //             proc.StartInfo.RedirectStandardOutput = true;

    //             proc.Start();
    //             proc.WaitForExit();
    //         }
    //     }

    //     [Fact]
    //     public async Task ExecuteCommand_WhenCalled_ExpectResultsToBeReturned()
    //     {
    //         // Arrange
    //         var containerGateway = new ContainerGateway();

    //         var containerId = await containerGateway.RunAsync("atmoz/sftp:alpine", "guest:guest", Enumerable.Empty<Option>(), _cancellationToken);
    //         _containerIds.Add(containerId);

    //         for (var i = 0; i < 10; ++i)
    //         {
    //             var health = await containerGateway.ExecuteCommandAsync(containerId, "ps aux | grep -v grep | grep sshd || exit 1", _cancellationToken);

    //             if (health.Any())
    //             {
    //                 break;
    //             }

    //             await Task.Delay(1000);
    //         }

    //         // Act
    //         var results = await containerGateway.ExecuteCommandAsync(containerId, "ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_rsa_key.pub", _cancellationToken);

    //         // Assert
    //         using var proc = new Process();
    //         proc.StartInfo.FileName = "docker";
    //         proc.StartInfo.Arguments = $"exec {containerId} ssh-keygen -l -E md5 -f /etc/ssh/ssh_host_rsa_key.pub";
    //         proc.StartInfo.UseShellExecute = false;
    //         proc.StartInfo.RedirectStandardOutput = true;

    //         proc.Start();
    //         proc.WaitForExit();

    //         var output = proc.StandardOutput.ReadLine();

    //         Assert.Single(results);
    //         Assert.Equal(output, results.First());
    //     }
    // }

    // public class GetHostPortMapping : IDisposable
    // {
    //     private readonly List<string> _containerIds = new List<string>();

    //     private readonly CancellationToken _cancellationToken;

    //     public GetHostPortMapping()
    //     {
    //         var cancellationTokenSource = new CancellationTokenSource();
    //         cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

    //         _cancellationToken = cancellationTokenSource.Token;
    //     }

    //     public void Dispose()
    //     {
    //         foreach (var containerId in _containerIds)
    //         {
    //             using var proc = new Process();
    //             proc.StartInfo.FileName = "docker";
    //             proc.StartInfo.Arguments = $"rm --force {containerId}";
    //             proc.StartInfo.UseShellExecute = false;
    //             proc.StartInfo.RedirectStandardOutput = true;

    //             proc.Start();
    //             proc.WaitForExit();
    //         }
    //     }

    //     [Fact]
    //     public async Task GetHostPortMapping_WhenCalledForExposedPorts_ExpectHostPortToBeReturned()
    //     {
    //         // Arrange
    //         var containerGateway = new ContainerGateway();

    //         var containers = new (Task<string> Task, string Scheme, int Port)[]
    //         {
    //             (containerGateway.RunAsync("atmoz/sftp:alpine", "guest:guest", Enumerable.Empty<Option>(), _cancellationToken), "tcp", 22),
    //             (containerGateway.RunAsync("redis:alpine", string.Empty, Enumerable.Empty<Option>(), _cancellationToken), "tcp", 6379)
    //         };
    //         await Task.WhenAll(containers.Select(x => x.Task));

    //         _containerIds.AddRange(containers.Select(x => x.Task.Result));

    //         // Act
    //         var portMappings = containers.Select(x => (Task: containerGateway.GetHostPortMappingAsync(x.Task.Result, x.Scheme, x.Port, _cancellationToken), ContainerId: x.Task.Result, Scheme: x.Scheme, Port: x.Port));
    //         await Task.WhenAll(portMappings.Select(x => x.Task));

    //         // Assert
    //         Assert.All(portMappings, portMapping =>
    //         {
    //             using (var process = new Process())
    //             {
    //                 process.StartInfo.FileName = "docker";
    //                 process.StartInfo.Arguments = $"port {portMapping.ContainerId} {portMapping.Port}/{portMapping.Scheme}";
    //                 process.StartInfo.UseShellExecute = false;
    //                 process.StartInfo.RedirectStandardOutput = true;

    //                 process.Start();
    //                 process.WaitForExit();

    //                 int.TryParse(process.StandardOutput?.ReadLine()?.Split(':')?.Last(), out var expectedPort);

    //                 Assert.Equal(expectedPort, portMapping.Task.Result);
    //             }
    //         });
    //     }
    // }

    // public class GetHealthStatusAsync : IDisposable
    // {
    //     private readonly List<string> _containerIds = new List<string>();

    //     private readonly List<string> _filenames = new List<string>();

    //     private readonly CancellationToken _cancellationToken;

    //     public GetHealthStatusAsync()
    //     {
    //         var cancellationTokenSource = new CancellationTokenSource();
    //         cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

    //         _cancellationToken = cancellationTokenSource.Token;
    //     }

    //     public void Dispose()
    //     {
    //         foreach (var containerId in _containerIds)
    //         {
    //             using var proc = new Process();
    //             proc.StartInfo.FileName = "docker";
    //             proc.StartInfo.Arguments = $"rm --force {containerId}";
    //             proc.StartInfo.UseShellExecute = false;
    //             proc.StartInfo.RedirectStandardOutput = true;

    //             proc.Start();
    //             proc.WaitForExit();
    //         }
    //     }

    //     [Fact]
    //     public async Task GetHealthStatusAsync_WhenContainerIsRunning_ExpectRunningHealthStatus()
    //     {
    //         // Arrange
    //         var containerGateway = new ContainerGateway();

    //         var containerId = await containerGateway.RunAsync("redis:alpine", string.Empty, Enumerable.Empty<Option>(), _cancellationToken);
    //         _containerIds.Add(containerId);

    //         // Act
    //         var healthStatus = await containerGateway.GetHealthStatusAsync(containerId, _cancellationToken);

    //         // Assert
    //         Assert.Equal(HealthStatus.Running, healthStatus);
    //     }

    //     [Fact]
    //     public async Task GetHealthStatusAsync_WhenContainerIsHealthy_ExpectHealthyHealthStatus()
    //     {
    //         // Arrange
    //         var containerGateway = new ContainerGateway();

    //         var containerId = await containerGateway.RunAsync("--health-cmd=\"echo hello\" --health-interval=1s redis:alpine", string.Empty, Enumerable.Empty<Option>(), _cancellationToken);
    //         _containerIds.Add(containerId);

    //         // Act
    //         await Task.Delay(TimeSpan.FromSeconds(5));
    //         var healthStatus = await containerGateway.GetHealthStatusAsync(containerId, _cancellationToken);

    //         // Assert
    //         Assert.Equal(HealthStatus.Healthy, healthStatus);
    //     }

    //     [Fact]
    //     public async Task GetHealthStatusAsync_WhenContainerIsUnhealthy_ExpectUnhealthyHealthStatus()
    //     {
    //         // Arrange
    //         var containerGateway = new ContainerGateway();

    //         var containerId = await containerGateway.RunAsync("--health-cmd=\"exit 1\" --health-interval=1s --health-retries=1 --health-start-period=1s redis:alpine", string.Empty, Enumerable.Empty<Option>(), _cancellationToken);
    //         _containerIds.Add(containerId);

    //         // Act
    //         await Task.Delay(TimeSpan.FromSeconds(2));
    //         var healthStatus = await containerGateway.GetHealthStatusAsync(containerId, _cancellationToken);

    //         // Assert
    //         Assert.Equal(HealthStatus.Unhealthy, healthStatus);
    //     }

    //     [Fact]
    //     public async Task GetHealthStatusAsync_WhenContainerIsStarting_ExpectUnknownHealthStatus()
    //     {
    //         // Arrange
    //         var containerGateway = new ContainerGateway();

    //         var containerId = await containerGateway.RunAsync("--health-cmd=\"exit 1\" --health-interval=1s --health-retries=1 --health-start-period=1s redis:alpine", string.Empty, Enumerable.Empty<Option>(), _cancellationToken);
    //         _containerIds.Add(containerId);

    //         // Act
    //         var healthStatus = await containerGateway.GetHealthStatusAsync(containerId, _cancellationToken);

    //         // Assert
    //         Assert.Equal(HealthStatus.Unknown, healthStatus);
    //     }

    //     [Fact]
    //     public async Task GetHealthStatusAsync_WhenContainerIsExited_ExpectUnknownHealthStatus()
    //     {
    //         // Arrange
    //         var containerGateway = new ContainerGateway();

    //         var containerId = await containerGateway.RunAsync("alpine", string.Empty, Enumerable.Empty<Option>(), _cancellationToken);
    //         _containerIds.Add(containerId);

    //         for (var i = 0; i < 10; ++i)
    //         {
    //             using (var process = new Process())
    //             {
    //                 process.StartInfo.FileName = "docker";
    //                 process.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{.State.Running}}}}\"";
    //                 process.StartInfo.UseShellExecute = false;
    //                 process.StartInfo.RedirectStandardOutput = true;
    //                 process.Start();
    //                 process.WaitForExit();

    //                 var isRunning = JsonSerializer.Deserialize<bool>(process.StandardOutput.BaseStream);

    //                 if (!isRunning)
    //                 {
    //                     break;
    //                 }
    //             }

    //             await Task.Delay(TimeSpan.FromMilliseconds(250));
    //         }

    //         // Act
    //         var healthStatus = await containerGateway.GetHealthStatusAsync(containerId, _cancellationToken);

    //         // Assert
    //         Assert.Equal(HealthStatus.Unknown, healthStatus);
    //     }
    // }
}
