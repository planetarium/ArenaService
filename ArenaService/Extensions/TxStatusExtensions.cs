namespace ArenaService.Extensions;

using ArenaService.Models;

public static class TxStatusExtensions
{
    public static TxStatus ToModelTxStatus(this Client.TxStatus txStatus)
    {
        return txStatus switch
        {
            Client.TxStatus.Invalid => TxStatus.INVALID,
            Client.TxStatus.Staging => TxStatus.STAGING,
            Client.TxStatus.Success => TxStatus.SUCCESS,
            Client.TxStatus.Failure => TxStatus.FAILURE,
            Client.TxStatus.Included => TxStatus.INCLUDED,
            _ => throw new ArgumentException()
        };
    }
}
