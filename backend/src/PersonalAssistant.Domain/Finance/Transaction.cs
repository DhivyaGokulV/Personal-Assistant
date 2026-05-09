using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.Finance;

public class Transaction : EntityBase
{
    public DateOnly Date { get; set; }
    public TransactionType Type { get; set; }

    public Guid AccountId { get; set; }
    public Account? Account { get; set; }

    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public Guid? PaymentTypeId { get; set; }
    public PaymentType? PaymentType { get; set; }

    /// <summary>
    /// Set on both legs of a self-transfer to link the pair.
    /// Null for normal credits/debits.
    /// </summary>
    public Guid? TransferGroupId { get; set; }

    public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();
}
