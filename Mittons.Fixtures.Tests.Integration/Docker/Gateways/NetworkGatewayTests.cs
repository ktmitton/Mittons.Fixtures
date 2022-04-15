using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Extensions;
using Mittons.Fixtures.Models;
using Xunit;

namespace Mittons.Fixtures.Tests.Integration.Docker.Gateways;

public class NetworkGatewayTests
{
    private readonly List<string> _networkNames = new List<string>();

    private readonly List<string> _containerIds = new List<string>();

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

        foreach (var networkName in _networkNames)
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
    public async Task CreateAsync_WhenCalled_ExpectNetworkToBeCreatedWithTheProvidedName()
    {
        // Arrange
        var networkName = "test";
        var uniqueName = $"{networkName}-{Guid.NewGuid()}";

        var networkGateway = new NetworkGateway();

        _networkNames.Add(uniqueName);

        // Act
        await networkGateway.CreateAsync(uniqueName, Enumerable.Empty<Option>(), (new CancellationToken()).CreateLinkedTimeoutToken(TimeSpan.FromSeconds(5)));

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
    public async Task CreateAsync_WhenCalledWithLabels_ExpectLabelsToBeAttachedToTheNetwork()
    {
        // Arrange
        var networkName = "test";
        var uniqueName = $"{networkName}-{Guid.NewGuid()}";

        var expectedOptions = new Option[]
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
        };

        var networkGateway = new NetworkGateway();

        _networkNames.Add(uniqueName);

        // Act
        await networkGateway.CreateAsync(uniqueName, expectedOptions, (new CancellationToken()).CreateLinkedTimeoutToken(TimeSpan.FromSeconds(5)));

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

        Assert.Contains(actualLabels, x => x.Key == "first" && x.Value == "second");
        Assert.Contains(actualLabels, x => x.Key == "third" && x.Value == "fourth");
    }

    [Fact]
    public async Task RemoveAsync_WhenCalled_ExpectNetworkToBeRemoved()
    {
        // Arrange
        var networkName = "test";
        var uniqueName = $"{networkName}-{Guid.NewGuid()}";

        var networkGateway = new NetworkGateway();

        _networkNames.Add(uniqueName);

        await networkGateway.CreateAsync(uniqueName, Enumerable.Empty<Option>(), (new CancellationToken()).CreateLinkedTimeoutToken(TimeSpan.FromSeconds(5)));

        // Act
        await networkGateway.RemoveAsync(uniqueName, CancellationToken.None);

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
    public async Task ConnectAsync_WhenCalled_ExpectContainerToBeConnectedToNetwork()
    {
        // Arrange
        var networkName = "test";
        var uniqueName = $"{networkName}-{Guid.NewGuid()}";

        var networkGateway = new NetworkGateway();
        var containerGateway = new ContainerGateway();

        var containerId = await containerGateway.RunAsync("atmoz/sftp:alpine", "guest:guest", Enumerable.Empty<Option>(), CancellationToken.None);
        _containerIds.Add(containerId);

        _networkNames.Add(uniqueName);

        await networkGateway.CreateAsync(uniqueName, Enumerable.Empty<Option>(), (new CancellationToken()).CreateLinkedTimeoutToken(TimeSpan.FromSeconds(5)));

        // Act
        await networkGateway.ConnectAsync(uniqueName, containerId, "test.example.com", CancellationToken.None);

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
