namespace Api14.CORE;

public class ApiResponse<T>
{
    public int Status { get; set; }
    public T Data { get; set; }
    public string? Message { get; set; }
}