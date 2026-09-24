using Cortex.Mediator.Commands;
using Mindflow_backend.Journal.Application.Dtos;
using Mindflow_backend.Shared.Application.Model;

namespace Mindflow_backend.Journal.Application.Commands;

public class SyncJournalEntriesCommand : ICommand<Result<List<SyncJournalEntryResultDto>>>
{
    public int UserId { get; set; }
    public List<SyncJournalEntryItemRequest> Items { get; set; } = [];
}
