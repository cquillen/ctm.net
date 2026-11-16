using ControlM.Simulator.Authentication;
using Shouldly;

namespace ControlM.Simulator.Tests.Authentication;

public sealed class ApiKeySequenceTests
{
    [Test]
    public void Keys_are_numbered_from_one_in_each_sequence()
    {
        var first = new ApiKeySequence();
        var second = new ApiKeySequence();

        string[] keys = [first.Next(), first.Next(), second.Next()];

        keys.ShouldBe(["SIMKEY1", "SIMKEY2", "SIMKEY1"]);
    }
}
