namespace ArenaService.Dtos;

public class SeasonResponse
{
    public int Id { get; set; }
    public long StartBlockIndex { get; set; }
    public long EndBlockIndex { get; set; }
    public int Interval { get; set; }
}
