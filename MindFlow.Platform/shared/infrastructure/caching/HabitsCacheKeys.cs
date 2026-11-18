namespace Mindflow_backend.Shared.Infrastructure.Caching;

public static class HabitsCacheKeys
{
    public static string List(int userId) => $"habits:list:{userId}";
}
