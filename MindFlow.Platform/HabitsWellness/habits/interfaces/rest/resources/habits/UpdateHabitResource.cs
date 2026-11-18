using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mindflow_backend.Habits.Interfaces.Rest.Resources.Habits;

public record UpdateHabitResource(
    [MaxLength(100)] [property: JsonPropertyName("name")] string Name,
    [MaxLength(50)] [property: JsonPropertyName("category")] string Category,
    [MaxLength(20)] [property: JsonPropertyName("frequency")] string Frequency
);
