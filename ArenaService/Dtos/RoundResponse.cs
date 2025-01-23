namespace ArenaService.Dtos;

public class RoundResponse
{
    public required int Id { get; set; }
    public required long StartBlockIndex { get; set; }
    public required long EndBlockIndex { get; set; }
}
