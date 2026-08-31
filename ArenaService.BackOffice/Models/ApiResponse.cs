namespace ArenaService.BackOffice.Models;

/// <summary>Common response envelope shared by every back office API controller.</summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new()
        {
            Success = true,
            Message = message,
            Data = data
        };

    public static ApiResponse<T> Error(string message) =>
        new()
        {
            Success = false,
            Message = message,
            Data = default
        };
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static ApiResponse Ok(string? message = null) => new() { Success = true, Message = message };

    public static ApiResponse Error(string message) => new() { Success = false, Message = message };
}
