namespace ArenaService.Models.Enums;

public enum PurchaseStatus
{
    PENDING,
    TRACKING,
    INSUFFICIENT_PAYMENT,
    INVALID_RECIPIENT,
    INVALID_CURRENCY,
    DUPLICATE_TRANSACTION,
    SUCCESS
}
