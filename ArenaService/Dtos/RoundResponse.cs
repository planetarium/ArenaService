namespace ArenaService.Dtos;

public class RoundResponse
{
    public int Id { get; set; }
    public long StartBlockIndex { get; set; }
    public long EndBlockIndex { get; set; }
}
