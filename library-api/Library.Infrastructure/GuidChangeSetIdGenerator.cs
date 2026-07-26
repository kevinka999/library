using Library.Application.Abstractions;

namespace Library.Infrastructure;

internal sealed class GuidChangeSetIdGenerator : IChangeSetIdGenerator
{
    public Guid NewId() => Guid.NewGuid();
}
