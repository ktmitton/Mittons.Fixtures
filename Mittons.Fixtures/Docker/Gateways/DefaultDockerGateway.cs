using System.Diagnostics;

namespace Mittons.Fixtures.Docker.Gateways
{
    public class DefaultDockerGateway : IDockerGateway
    {
        public void Remove(string containerName)
        {
            throw new System.NotImplementedException();
        }

        public string Run(string imageName, string command)
        {
            var proc = new Process();
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
}