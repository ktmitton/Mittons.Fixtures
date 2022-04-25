using System;

namespace Mittons.Fixtures.Adapters
{
    public interface IUriAdapter
    {
        bool CanSupportUri(Uri uri);
    }
}
