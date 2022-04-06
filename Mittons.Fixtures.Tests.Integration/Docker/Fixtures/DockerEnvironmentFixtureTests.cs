using System.Collections.Generic;
using System.Diagnostics;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Fixtures;
using Xunit;

namespace Mittons.Fixtures.Tests.Integration.Docker.Fixtures
{
    [Network("network1")]
    [Network("network2")]
    public class ExampleDockerEnvironmentFixture : DockerEnvironmentFixture
    {
        [Image("alpine:3.15")]
        [NetworkAlias("network1", "alpine.example.com")]
        public Container? AlpineContainer { get; set; }

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
        public void ExpectAlpineContainerToBeCreated()
        {
            Assert.NotNull(_dockerEnvironmentFixture.AlpineContainer);

            if (_dockerEnvironmentFixture.AlpineContainer is not null)
            {
                var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"ps -a --filter id={_dockerEnvironmentFixture.AlpineContainer.Id} --format '{{{{.ID}}}}'";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                Assert.NotEmpty(proc.StandardOutput.ReadToEnd());
            }
        }

        [Fact]
        public void ExpectAlpineContainerToBeConnectedToNetwork1()
        {
            Assert.NotNull(_dockerEnvironmentFixture.AlpineContainer);

            if (_dockerEnvironmentFixture.AlpineContainer is not null)
            {
                var proc = new Process();
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"inspect {_dockerEnvironmentFixture.AlpineContainer.Id} --format \"{{{{range $k, $v := .NetworkSettings.Networks}}}}{{{{printf \\\"%s\\n\\\" $k}}}}{{{{end}}}}\"";
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
        public void ExpectSftpContainerToBeCreated()
        {
            Assert.NotNull(_dockerEnvironmentFixture.SftpContainer);

            if (_dockerEnvironmentFixture.SftpContainer is not null)
            {
                var proc = new Process();
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
                var proc = new Process();
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
                var proc = new Process();
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
    }
}