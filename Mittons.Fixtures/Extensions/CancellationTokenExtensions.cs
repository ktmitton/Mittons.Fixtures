using System;
using System.Threading;

namespace Mittons.Fixtures.Extensions
{
    public static class CancellationTokenExtensions
    {
        public static CancellationToken CreateLinkedTimeoutToken(this CancellationToken cancellationToken, TimeSpan timeout)
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource();
            timeoutCancellationTokenSource.CancelAfter(timeout);

            return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token).Token;
        }
    }
}