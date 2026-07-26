namespace Library.Application.Abstractions;

public interface IChangeSetIdGenerator
{
    Guid NewId();
}
