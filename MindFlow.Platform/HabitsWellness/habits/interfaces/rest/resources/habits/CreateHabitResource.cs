using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mindflow_backend.Habits.Domain.Model.ValueObjects;

namespace Mindflow_backend.Habits.Interfaces.Rest.Resources.Habits;

public record CreateHabitResource(
    [MaxLength(100)] [property: JsonPropertyName("name")] string Name,
    [MaxLength(50)] [property: JsonPropertyName("category")] string Category,
    [MaxLength(20)] [property: JsonPropertyName("frequency")] string Frequency,
    [property: JsonPropertyName("user_id")] int UserId
);
