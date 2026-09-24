namespace Mindflow_backend.Journal.Application.Services;

public interface ISearchTokenHasher
{
    string Hash(string token);
}
