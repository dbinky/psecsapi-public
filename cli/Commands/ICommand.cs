using System.CommandLine;

namespace psecsapi.Console.Commands
{
    /// <summary>
    /// Interface for CLI commands that can be auto-discovered and registered.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Builds and returns the System.CommandLine Command instance.
        /// </summary>
        Command Build();
    }
}
