using Mindflow_backend.Habits.Domain.Model.ValueObjects;

namespace Mindflow_backend.Habits.Interfaces.Rest.Assemblers;

/// <summary>
///     Acepta los valores del enum (<c>Daily/Weekly/Monthly</c>) y sus equivalentes en español
///     que usan las apps móviles (<c>Diario/Semanal/Mensual</c>).
/// </summary>
public static class HabitFrequencyParser
{
    public static HabitFrequency Parse(string? value)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "daily": case "diario": return HabitFrequency.Daily;
            case "weekly": case "semanal": return HabitFrequency.Weekly;
            case "monthly": case "mensual": return HabitFrequency.Monthly;
            default:
                throw new ArgumentException(
                    $"Frecuencia inválida: '{value}'. Valores permitidos: Daily, Weekly, Monthly.");
        }
    }
}
