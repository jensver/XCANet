namespace XcaNet.Core.Abstractions;

public readonly record struct EntityId(Guid Value)
{
    public static EntityId New() => new(Guid.NewGuid());
}
