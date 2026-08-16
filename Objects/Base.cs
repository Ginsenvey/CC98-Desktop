using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;

/// <summary>
/// 存放基础对象模型
/// </summary> 

namespace CC98.Objects;

public class ApiResponse<T>
{
    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess { get; set; }
    
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }

    public bool IsNotValid => !IsSuccess || Data == null;

    public static ApiResponse<T> Success(T data, string message = "")
    {
        return new()
        {
            IsSuccess = true,
            Data = data,
            Message = message,
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    public static ApiResponse<T> Fail(string message, int statusCode = 0)
    {
        return new()
        {
            IsSuccess = false,
            Message = message,
            StatusCode = statusCode
        };
    }
}

public class ApiResponse
{
    public bool IsSuccess { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }

    public static ApiResponse Success(string content, string message = "")
    {
        return new()
        {
            IsSuccess = true,
            Content = content,
            Message = message,
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    public static ApiResponse Fail(string message, int statusCode = 0)
    {
        return new()
        {
            IsSuccess = false,
            Message = message,
            StatusCode = statusCode
        };
    }
}

/// <summary>
///     增量更新模型
/// </summary>
public class Increment
{
    public int CurrentPage;

    //应假定没有更多项，由返回项项数决定是否还有更多
    public bool HasMore;
    public int PageSize;

    public Increment(int pageSize = 10, int currentPage = 0, bool hasMore = false)
    {
        PageSize = pageSize;
        CurrentPage = currentPage;
        HasMore = hasMore;
    }

    public int StartIndex => CurrentPage * PageSize;

    public void Clear()
    {
        CurrentPage = 0;
        HasMore = false;
    }

    public async Task LoadMore(int currentIndex, Func<Task<bool>> load)
    {
        // 防重入:增量加载期间忽略重复触发(ItemsRepeater 回收/重复 Prepare 会重复调用)
        if (_loading) return;
        if (HasMore && (currentIndex + 1) % PageSize == 0)
        {
            _loading = true;
            CurrentPage++;
            try
            {
                var success = await load();
                //如果没有成功，则回退页码，等待下一次尝试
                if (!success) CurrentPage--;
            }
            finally
            {
                _loading = false;
            }
        }
    }

    //强制加载下一页
    public async Task LoadNextPage(Func<Task<bool>> load)
    {
        if (_loading) return;
        _loading = true;
        CurrentPage++;
        try
        {
            var success = await load();
            //如果没有成功，则回退页码，等待下一次尝试
            if (!success) CurrentPage--;
        }
        finally
        {
            _loading = false;
        }
    }

    public async Task LoadLastPage(Func<Task<bool>> load)
    {
        if (_loading) return;
        _loading = true;
        CurrentPage--;
        try
        {
            var success = await load();
            //如果没有成功，则回退页码，等待下一次尝试
            if (!success) CurrentPage++;
        }
        finally
        {
            _loading = false;
        }
    }

    private bool _loading;
}