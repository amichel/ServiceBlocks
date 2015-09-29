using ServiceBlocks.Engines.CommandProcessor;

namespace ServiceBlocks.CommandProcessor.Tests
{
    public class MockCommandProcessor : DefaultCommandProcessor<bool, int, MockCommand>
    {
    }
}