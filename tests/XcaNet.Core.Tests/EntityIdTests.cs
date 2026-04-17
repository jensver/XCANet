using XcaNet.Core.Abstractions;

namespace XcaNet.Core.Tests;

public sealed class EntityIdTests
{
    [Fact]
    public void New_ShouldCreateNonEmptyIdentifier()
    {
        Assert.NotEqual(Guid.Empty, EntityId.New().Value);
    }
}
