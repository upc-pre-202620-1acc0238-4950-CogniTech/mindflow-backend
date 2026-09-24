using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mindflow_backend.Habits.Infrastructure.Persistence.Ef.Configuration;
using Mindflow_backend.iam.domain.model.aggregates;
using Mindflow_backend.iam.domain.model.entities;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Encryption;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
using Mindflow_backend.AiFeedback.Domain.Model.Entities;
using Mindflow_backend.AiIntegration.Domain.Model.Entities;
using Mindflow_backend.Habits.Domain.Model.Entities;
using Mindflow_backend.Chat.Domain.Entities;
using Mindflow_backend.WellnessContent.Domain.Entities;
using JournalEntry = Mindflow_backend.Journal.Domain.Entities.JournalEntry;
using EntryTag = Mindflow_backend.Journal.Domain.Entities.EntryTag;
using Tag = Mindflow_backend.Journal.Domain.Entities.Tag;
using Media = Mindflow_backend.Journal.Domain.Entities.Media;
using JournalSearchToken = Mindflow_backend.Journal.Domain.Entities.JournalSearchToken;

namespace Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class AppDbContext(DbContextOptions options, AesEncryptionService encryptionService) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<EntryTag> EntryTags => Set<EntryTag>();
    public DbSet<Media> Media => Set<Media>();
    public DbSet<JournalSearchToken> JournalSearchTokens => Set<JournalSearchToken>();
    public DbSet<AiFeedbackRating> AiFeedbackRatings => Set<AiFeedbackRating>();
    public DbSet<AiMetricLog> AiMetricLogs => Set<AiMetricLog>();
    public DbSet<CachedHabitSuggestion> CachedHabitSuggestions => Set<CachedHabitSuggestion>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<WellnessExercise> WellnessExercises => Set<WellnessExercise>();

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddInterceptors(new AuditableEntityInterceptor());
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>().HasKey(u => u.Id);
        builder.Entity<User>().Property(u => u.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<User>().Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.Entity<User>().Property(u => u.PasswordHash).IsRequired();
        builder.Entity<User>().Property(u => u.Name).HasMaxLength(100);
        builder.Entity<User>().Property(u => u.Occupation).HasMaxLength(100);
        builder.Entity<User>().Property(u => u.GoogleId).HasMaxLength(255);
        builder.Entity<User>().Property(u => u.PinHash).HasMaxLength(255);
        builder.Entity<User>().Property(u => u.Role).IsRequired().HasMaxLength(20).HasDefaultValue(User.DefaultRole);
        builder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        builder.Entity<User>().HasIndex(u => u.GoogleId).IsUnique();

        var encryptedConverter = new EncryptedStringConverter(encryptionService);

        builder.Entity<JournalEntry>(entity =>
        {
            entity.Property(e => e.Content).HasColumnType("LONGTEXT").HasConversion(encryptedConverter);
            entity.Property(e => e.AiResponse).HasColumnType("TEXT");

            entity.HasMany(e => e.EntryTags)
                  .WithOne(et => et.Entry)
                  .HasForeignKey(et => et.EntryId);

            entity.HasMany(e => e.Media)
                  .WithOne(m => m.Entry)
                  .HasForeignKey(m => m.EntryId);

            entity.HasQueryFilter(e => e.DeletedAt == null);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.Date });
            entity.Property(e => e.ClientId).HasMaxLength(64);
            entity.HasIndex(e => new { e.UserId, e.ClientId }).IsUnique();
        });

        builder.Entity<Tag>(entity =>
        {
            entity.HasMany(t => t.EntryTags)
                  .WithOne(et => et.Tag)
                  .HasForeignKey(et => et.TagId);
        });

        builder.Entity<EntryTag>(entity =>
        {
            entity.HasIndex(et => new { et.EntryId, et.TagId }).IsUnique();
            entity.HasQueryFilter(et => et.Entry.DeletedAt == null);
        });

        builder.Entity<Media>(entity =>
        {
            entity.HasQueryFilter(m => m.Entry.DeletedAt == null);
        });

        builder.Entity<JournalSearchToken>(entity =>
        {
            entity.HasOne(t => t.Entry)
                  .WithMany()
                  .HasForeignKey(t => t.EntryId);

            entity.HasIndex(t => new { t.UserId, t.TokenHash });
            entity.HasIndex(t => t.EntryId);
            entity.HasQueryFilter(t => t.Entry.DeletedAt == null);
        });

        builder.Entity<WellnessExercise>(entity =>
        {
            entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.AudioUrl).HasMaxLength(500);
            entity.HasIndex(e => new { e.Type, e.IsActive });
        });

        builder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Token).IsRequired().HasMaxLength(64);
            entity.Property(t => t.ExpiresAt).IsRequired();
            entity.HasIndex(t => t.Token).IsUnique();
            entity.HasIndex(t => t.UserId);
        });

        builder.ApplyHabitsConfiguration();

        builder.Entity<AiFeedbackRating>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.ContentType).IsRequired().HasMaxLength(20);
            entity.Property(f => f.Rating).IsRequired();
            entity.Property(f => f.Comment).HasMaxLength(500);
            entity.HasIndex(f => f.UserId);
            entity.HasIndex(f => new { f.UserId, f.ContentId, f.ContentType }).IsUnique();
        });

        builder.Entity<AiMetricLog>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Operation).IsRequired().HasMaxLength(50);
            entity.Property(m => m.ErrorMessage).HasMaxLength(500);
            entity.HasIndex(m => m.CreatedAt);
        });

        builder.Entity<CachedHabitSuggestion>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.SuggestionsJson).IsRequired().HasColumnType("TEXT");
            entity.HasIndex(c => c.UserId).IsUnique();
        });

        builder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(255);
            entity.Property(c => c.Category).IsRequired().HasMaxLength(50);
            entity.HasIndex(c => c.UserId);
            entity.HasMany(c => c.Messages)
                  .WithOne(m => m.Conversation)
                  .HasForeignKey(m => m.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Role).IsRequired().HasMaxLength(20);
            entity.Property(m => m.Content).IsRequired().HasColumnType("LONGTEXT")
                  .HasConversion(encryptedConverter);
            entity.HasIndex(m => m.ConversationId);
        });

        builder.UseSnakeCaseNamingConvention();

        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            d => DateOnly.FromDateTime(d));

        var nullableDateOnlyConverter = new ValueConverter<DateOnly?, DateTime?>(
            d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : null,
            d => d.HasValue ? DateOnly.FromDateTime(d.Value) : null);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateOnly))
                    property.SetValueConverter(dateOnlyConverter);
                else if (property.ClrType == typeof(DateOnly?))
                    property.SetValueConverter(nullableDateOnlyConverter);
            }
        }
    }
}
