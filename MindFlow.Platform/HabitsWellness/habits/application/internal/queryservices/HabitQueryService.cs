using Mindflow_backend.Habits.Application.Internal.CommandServices;
using Mindflow_backend.Habits.Application.Queries.Habits;
using Mindflow_backend.Habits.Domain.Model.Aggregates;
using Mindflow_backend.Habits.Domain.Model.Entities;
using Mindflow_backend.Habits.Domain.Repositories;

namespace Mindflow_backend.Habits.Application.Internal.QueryServices;

public class HabitQueryService(
    IHabitRepository habitRepository,
    IHabitCompletionLogRepository logRepository) : IHabitQueryService
{
    public async Task<Habit?> Handle(GetHabitByIdQuery query, CancellationToken cancellationToken = default)
    {
        var habit = await habitRepository.FindByIdAndUserIdAsync(query.Id, query.UserId, cancellationToken);
        if (habit is not null)
            ApplyStreak(habit, await logRepository.FindByHabitIdAsync(habit.Id, cancellationToken));
        return habit;
    }

    public async Task<IEnumerable<Habit>> Handle(GetAllHabitsByUserIdQuery query, CancellationToken cancellationToken = default)
    {
        var habits = (await habitRepository.FindByUserIdAsync(query.UserId, cancellationToken)).ToList();
        if (habits.Count == 0)
            return habits;

        // Una sola consulta para los logs de todos los hábitos (antes: una por hábito)
        var logsByHabit = (await logRepository.FindByUserIdAsync(query.UserId, cancellationToken))
            .ToLookup(l => l.HabitId);

        foreach (var habit in habits)
            ApplyStreak(habit, logsByHabit[habit.Id]);
        return habits;
    }

    private static void ApplyStreak(Habit habit, IEnumerable<HabitCompletionLog> logs)
    {
        var completedDates = logs
            .Where(l => l.Completed)
            .Select(l => l.Date.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        habit.SetStreak(HabitLogCommandService.ComputeStreak(completedDates, habit.Frequency));
    }
}
