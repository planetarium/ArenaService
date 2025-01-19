using ArenaService.Constants;

namespace ArenaService.Dtos;

public class RefreshRequestResponse
{
    public RefreshStatus RefreshStatus { get; set; }
    public List<string>? SpecifiedOpponentAvatarAddresses { get; set; }
    public TxStatus? TxStatus { get; set; }
    public string? TxId { get; set; }
}
