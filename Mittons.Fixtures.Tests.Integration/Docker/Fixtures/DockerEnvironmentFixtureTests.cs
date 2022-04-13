using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Fixtures;
using Xunit;

namespace Mittons.Fixtures.Tests.Integration.Docker.Fixtures;

[Network("network1")]
[Network("network2")]
public class ExampleDockerEnvironmentFixture : DockerEnvironmentFixture, Xunit.IAsyncLifetime
{
    [Image("redis:alpine")]
    [NetworkAlias("network1", "alpine.example.com")]
    public Container? RedisContainer { get; set; }

    [NetworkAlias("network1", "sftp.example.com")]
    [NetworkAlias("network2", "sftp-other.example.com")]
    public SftpContainer? SftpContainer { get; set; }
}

public class DockerEnvironmentFixtureTests : IClassFixture<ExampleDockerEnvironmentFixture>
{
    private readonly ExampleDockerEnvironmentFixture _dockerEnvironmentFixture;

    public DockerEnvironmentFixtureTests(ExampleDockerEnvironmentFixture dockerEnvironmentFixture)
    {
        _dockerEnvironmentFixture = dockerEnvironmentFixture;
    }

    [Fact]
    public void ExpectNetwork1ToHaveRunLabels()
    {
        using var proc = new Process();
        proc.StartInfo.FileName = "docker";
        proc.StartInfo.Arguments = $"network inspect network1-{_dockerEnvironmentFixture.InstanceId} --format \"{{{{json .Labels}}}}\"";
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;

        proc.Start();
        proc.WaitForExit();

        var output = proc.StandardOutput.ReadToEnd();

        var labels = JsonSerializer.Deserialize<Dictionary<string, string>>(output) ?? new Dictionary<string, string>();

        Assert.True(labels.ContainsKey("mittons.fixtures.run.id"));
        Assert.Equal(Run.DefaultId, labels["mittons.fixtures.run.id"]);
    }

    [Fact]
    public void ExpectNetwork2ToHaveRunLabels()
    {
        using var proc = new Process();
        proc.StartInfo.FileName = "docker";
        proc.StartInfo.Arguments = $"network inspect network2-{_dockerEnvironmentFixture.InstanceId} --format \"{{{{json .Labels}}}}\"";
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;

        proc.Start();
        proc.WaitForExit();

        var output = proc.StandardOutput.ReadToEnd();

        var labels = JsonSerializer.Deserialize<Dictionary<string, string>>(output) ?? new Dictionary<string, string>();

        Assert.True(labels.ContainsKey("mittons.fixtures.run.id"));
        Assert.Equal(Run.DefaultId, labels["mittons.fixtures.run.id"]);
    }

    [Fact]
    public void ExpectRedisContainerToBeCreated()
    {
        Assert.NotNull(_dockerEnvironmentFixture.RedisContainer);

        if (_dockerEnvironmentFixture.RedisContainer is not null)
        {
            using var proc = new Process();
            proc.StartInfo.FileName = "docker";
            proc.StartInfo.Arguments = $"ps -a --filter id={_dockerEnvironmentFixture.RedisContainer.Id} --format '{{{{.ID}}}}'";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;

            proc.Start();
            proc.WaitForExit();

            Assert.NotEmpty(proc.StandardOutput.ReadToEnd());
        }
    }

    [Fact]
    public void ExpectRedisContainerToBeConnectedToNetwork1()
    {
        Assert.NotNull(_dockerEnvironmentFixture.RedisContainer);

        if (_dockerEnvironmentFixture.RedisContainer is not null)
        {
            using var proc = new Process();
            proc.StartInfo.FileName = "docker";
            proc.StartInfo.Arguments = $"inspect {_dockerEnvironmentFixture.RedisContainer.Id} --format \"{{{{range $k, $v := .NetworkSettings.Networks}}}}{{{{printf \\\"%s\\n\\\" $k}}}}{{{{end}}}}\"";
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

            Assert.Contains(connectedNetworks, x => x.StartsWith($"network1-"));
        }
    }

    [Fact]
    public void ExpectRedisContainerToHaveRunLabel()
    {
        Assert.NotNull(_dockerEnvironmentFixture.RedisContainer);

        if (_dockerEnvironmentFixture.RedisContainer is not null)
        {
            using var proc = new Process();
            proc.StartInfo.FileName = "docker";
            proc.StartInfo.Arguments = $"inspect {_dockerEnvironmentFixture.RedisContainer.Id} --format \"{{{{json .Config.Labels}}}}\"";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;

            proc.Start();
            proc.WaitForExit();

            var output = proc.StandardOutput.ReadToEnd();

            var labels = JsonSerializer.Deserialize<Dictionary<string, string>>(output) ?? new Dictionary<string, string>();

            Assert.True(labels.ContainsKey("mittons.fixtures.run.id"));
            Assert.Equal(Run.DefaultId, labels["mittons.fixtures.run.id"]);
        }
    }

    [Fact]
    public void ExpectSftpContainerToBeCreated()
    {
        Assert.NotNull(_dockerEnvironmentFixture.SftpContainer);

        if (_dockerEnvironmentFixture.SftpContainer is not null)
        {
            using var proc = new Process();
            proc.StartInfo.FileName = "docker";
            proc.StartInfo.Arguments = $"ps -a --filter id={_dockerEnvironmentFixture.SftpContainer.Id} --format '{{{{.ID}}}}'";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;

            proc.Start();
            proc.WaitForExit();

            Assert.NotEmpty(proc.StandardOutput.ReadToEnd());
        }
    }

    [Fact]
    public void ExpectSftpContainerToBeConnectedToNetwork1()
    {
        Assert.NotNull(_dockerEnvironmentFixture.SftpContainer);

        if (_dockerEnvironmentFixture.SftpContainer is not null)
        {
            using var proc = new Process();
            proc.StartInfo.FileName = "docker";
            proc.StartInfo.Arguments = $"inspect {_dockerEnvironmentFixture.SftpContainer.Id} --format \"{{{{range $k, $v := .NetworkSettings.Networks}}}}{{{{printf \\\"%s\\n\\\" $k}}}}{{{{end}}}}\"";
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

            Assert.Contains(connectedNetworks, x => x.StartsWith($"network1-"));
        }
    }

    [Fact]
    public void ExpectSftpContainerToBeConnectedToNetwork2()
    {
        Assert.NotNull(_dockerEnvironmentFixture.SftpContainer);

        if (_dockerEnvironmentFixture.SftpContainer is not null)
        {
            using var proc = new Process();
            proc.StartInfo.FileName = "docker";
            proc.StartInfo.Arguments = $"inspect {_dockerEnvironmentFixture.SftpContainer.Id} --format \"{{{{range $k, $v := .NetworkSettings.Networks}}}}{{{{printf \\\"%s\\n\\\" $k}}}}{{{{end}}}}\"";
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

            Assert.Contains(connectedNetworks, x => x.StartsWith($"network2-"));
        }
    }

    [Fact]
    public void ExpectSftpContainerToHaveRunLabel()
    {
        Assert.NotNull(_dockerEnvironmentFixture.SftpContainer);

        if (_dockerEnvironmentFixture.SftpContainer is not null)
        {
            using var proc = new Process();
            proc.StartInfo.FileName = "docker";
            proc.StartInfo.Arguments = $"inspect {_dockerEnvironmentFixture.SftpContainer.Id} --format \"{{{{json .Config.Labels}}}}\"";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;

            proc.Start();
            proc.WaitForExit();

            var output = proc.StandardOutput.ReadToEnd();

            var labels = JsonSerializer.Deserialize<Dictionary<string, string>>(output) ?? new Dictionary<string, string>();

            Assert.True(labels.ContainsKey("mittons.fixtures.run.id"));
            Assert.Equal(Run.DefaultId, labels["mittons.fixtures.run.id"]);
        }
    }
}
