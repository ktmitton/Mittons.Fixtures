namespace Mittons.Fixtures.Containers
{
    public interface IContainerService : IService
    {
        string ContainerId { get; }
    }
}