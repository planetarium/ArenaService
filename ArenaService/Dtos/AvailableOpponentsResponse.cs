using ArenaService.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace ArenaService.Dtos;

public class AvailableOpponentsResponse
{
    [SwaggerSchema("재화를 소모해서 리스트를 초기화하고 있을 땐 빈 리스트로 리턴")]
    public required List<AvailableOpponentResponse> AvailableOpponents { get; set; }
    public RefreshTxTrackingStatus RefreshTxTrackingStatus { get; set; }
    public TxStatus? TxStatus { get; set; }
    public string? TxId { get; set; }
}
