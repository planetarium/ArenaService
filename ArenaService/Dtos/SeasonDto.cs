namespace ArenaService.Dtos;

public class SeasonDto
{
    public int Id { get; set; }
    public long StartBlockIndex { get; set; }
    public long EndBlockIndex { get; set; }
    public int TicketRefillInterval { get; set; }
}
