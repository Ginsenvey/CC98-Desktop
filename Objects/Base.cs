using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Windows.System.Profile;

/// <summary>
/// 存放基础对象模型
/// </summary> 

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

public class ApiResponse
{
    public bool IsSuccess { get; set; }
    public string Content {  get; set; }= string.Empty;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }

    public static ApiResponse Success(string content, string message = "") => new ApiResponse
    {
        IsSuccess = true,
        Content=content,
        Message = message,
        StatusCode = (int)HttpStatusCode.OK
    };

    public static ApiResponse Fail(string message, int statusCode = 0) => new ApiResponse
    {
        IsSuccess = false,
        Message = message,
        StatusCode = (int)statusCode
    };
}


/// <summary>
/// 增量更新模型
/// </summary>
public class Increment
{
    public int pageSize;
    public int currentPage;
    public bool hasMore;
    public int startIndex => currentPage * pageSize;
    public Increment(int pageSize = 10,int currentPage = 0, bool hasMore = true)
    {
        this.pageSize = pageSize;
        this.currentPage = currentPage;
        this.hasMore = hasMore;
    }

    public void Clear()
    {
        currentPage = 0;
        hasMore = true;
    }
    public async Task LoadMore(int currentIndex, Func<Task<bool>> load)
    {
        if (hasMore&&(currentIndex + 1) % pageSize == 0)
        {
            var success = await load();
            //成功则翻页
            if (success)
            {
                currentPage++;
            }
        }

    }
}

public class AppLog 
{ 
    public string message;

}
