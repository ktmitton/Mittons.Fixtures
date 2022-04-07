using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Mittons.Fixtures.Docker.Gateways
{
    public class DefaultDockerGateway : IDockerGateway
    {
        public string ContainerRun(string imageName, string command, Dictionary<string, string> labels)
        {
            var labelStrings = labels.Select(x => $"--label \"{x.Key}={x.Value}\"");

            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"run -P -d {string.Join(" ", labelStrings)} {imageName} {command}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var containerId = default(string);

                while (!proc.StandardOutput.EndOfStream) {
                    containerId = proc.StandardOutput.ReadLine();
                }

                return containerId ?? string.Empty;
            }
        }

        public void ContainerRemove(string containerId)
        {
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"rm --force {containerId}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();
            }
        }

        public IPAddress ContainerGetDefaultNetworkIpAddress(string containerId)
        {
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"inspect {containerId} --format \"{{{{range .NetworkSettings.Networks}}}}{{{{printf \\\"%s\\n\\\" .IPAddress}}}}{{{{end}}}}\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                IPAddress.TryParse(proc.StandardOutput.ReadLine(), out var expectedIpAddress);

                return expectedIpAddress;
            }
        }

        public void ContainerAddFile(string containerId, string hostFilename, string containerFilename, string owner = null, string permissions = null)
        {
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"cp \"{hostFilename}\" \"{containerId}:{containerFilename}\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();
            }

            if (!string.IsNullOrWhiteSpace(owner))
            {
                using (var proc = new Process())
                {
                    proc.StartInfo.FileName = "docker";
                    proc.StartInfo.Arguments = $"exec {containerId} chown {owner} \"{containerFilename}\"";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;

                    proc.Start();
                    proc.WaitForExit();
                }
            }

            if (!string.IsNullOrWhiteSpace(permissions))
            {
                using (var proc = new Process())
                {
                    proc.StartInfo.FileName = "docker";
                    proc.StartInfo.Arguments = $"exec {containerId} chmod {permissions} \"{containerFilename}\"";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;

                    proc.Start();
                    proc.WaitForExit();
                }
            }
        }

        public void ContainerRemoveFile(string containerId, string containerFilename)
        {
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"exec {containerId} rm \"{containerFilename}\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();
            }
        }

        public IEnumerable<string> ContainerExecuteCommand(string containerId, string command)
        {
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"exec {containerId} {command}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                var lines = new List<string>();

                while (!proc.StandardOutput.EndOfStream) {
                    lines.Add(proc.StandardOutput.ReadLine());
                }

                return lines;
            }
        }

        public int ContainerGetHostPortMapping(string containerId, string protocol, int containerPort)
        {
            using(var proc = new Process())
            {
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"port {containerId} {containerPort}/{protocol}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();

                int.TryParse(proc.StandardOutput?.ReadLine()?.Split(':')?.Last(), out var port);

                return port;
            }
        }

        public void NetworkCreate(string networkName, Dictionary<string, string> labels)
        {
            var labelStrings = labels.Select(x => $"--label \"{x.Key}={x.Value}\"");

            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"network create {string.Join(" ", labelStrings)} {networkName}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();
            }
        }

        public void NetworkRemove(string networkName)
        {
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"network rm {networkName}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();
            }
        }

        public void NetworkConnect(string networkName, string containerId, string alias)
        {
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"network connect --alias {alias} {networkName} {containerId}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                proc.WaitForExit();
            }
        }
    }
}