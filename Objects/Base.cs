using System.Net;
using Windows.System.Profile;

namespace CC98.Objects;


public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }

    public static ApiResponse<T> Success(T data, string message = "") =>new ApiResponse<T>
    {
        IsSuccess = true,
        Data = data,
        Message = message,
        StatusCode =(int)HttpStatusCode.OK
    };

    public static ApiResponse<T> Fail(string message, int statusCode=0) =>new ApiResponse<T>
    {
        IsSuccess = false,
        Message = message,
        StatusCode = (int)statusCode
    };
    public bool IsNotValid => !IsSuccess || Data == null;
}
