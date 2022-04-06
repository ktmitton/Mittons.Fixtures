using System.Diagnostics;
using System.Net;

namespace Mittons.Fixtures.Docker.Gateways
{
    public class DefaultDockerGateway : IDockerGateway
    {
        public string ContainerRun(string imageName, string command)
        {
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"run -d {imageName} {command}";
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

        public void NetworkCreate(string networkName)
        {
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"network create {networkName}";
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