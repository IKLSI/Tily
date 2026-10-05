using Xunit;

namespace Tily.Core.Tests.Terminal;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TerminalCollection
{
    public const string Name = "Terminal";
}
