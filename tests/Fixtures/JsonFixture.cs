using System.Text.Json.Nodes;
using Shouldly;

namespace ControlM.Fixtures;

internal static class JsonFixture
{
    public static string Load(string area, string feature, string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", area, feature, fileName));

    public static void ShouldEqualJson(this string? actual, string expected)
    {
        var actualNode = JsonNode.Parse(actual.ShouldNotBeNull());
        var expectedNode = JsonNode.Parse(expected);
        JsonNode.DeepEquals(actualNode, expectedNode).ShouldBeTrue(
            $"expected {expectedNode?.ToJsonString()} but was {actualNode?.ToJsonString()}");
    }
}
