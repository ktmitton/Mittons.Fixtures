using System.Threading.Tasks;

namespace Mittons.Fixtures
{
	/// <summary>
	/// Used to provide asynchronous lifetime functionality for test classes.
	/// </summary>
	/// <remarks>
	/// Implemenations of this must be adapted to the testing framework you leverage.
	/// </remarks>
	public interface IAsyncLifetime
	{
		/// <summary>
		/// Called immediately after the class has been created, before it is used.
		/// </summary>
		Task InitializeAsync();

		/// <summary>
		/// Called when an object is no longer needed. Called just before <see cref="System.IDisposable.Dispose"/> 
		/// if the class also implements that.
		/// </summary>
		Task DisposeAsync();
	}
}
