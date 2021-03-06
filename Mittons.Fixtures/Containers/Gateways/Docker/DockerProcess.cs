using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mittons.Fixtures.Containers.Gateways.Docker
{
    internal class DockerProcess : Process
    {
        public DockerProcess(string arguments)
            : base()
        {
            StartInfo.FileName = "docker";
            StartInfo.Arguments = arguments;
            StartInfo.UseShellExecute = false;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.RedirectStandardError = true;
            EnableRaisingEvents = true;
        }

        public Task<int> RunProcessAsync(CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();

            Exited += (s, a) =>
            {
                taskCompletionSource.SetResult(ExitCode);
            };

            Start();

            cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken));

            return taskCompletionSource.Task;
        }
    }
}
