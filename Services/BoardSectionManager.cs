using CC98.Kernel.Authorize;
using CC98.Objects;
using CC98.Services.Extensions;

using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Windows.Storage;
using CC98.Kernel;
using System.Net.Http;
using CC98.Kernel.Network;

namespace CC98.Services;

/// <summary>
/// 提供分区数据的管理，包括从API刷新、缓存读取和搜索功能。
/// </summary>
public class BoardSectionManager
{
    public ApiService ApiService=App.Current.GetService<ApiService>();
    /// <summary>
    /// 本地缓存文件名。
    /// </summary>
    private const string CacheFileName = "board_sections.json";

    /// <summary>
    /// 本地缓存文件的完整路径。
    /// </summary>
    private static string CacheFilePath { get; } =Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, CacheFileName);

    /// <summary>
    ///     私有构造方法。
    /// </summary>
    private BoardSectionManager()
    {
    }

    /// <summary>
    ///     对象的唯一实例。
    /// </summary>
    public static BoardSectionManager Instance { get; } = new();

    /// <summary>
    /// 加载分区数据。
    /// </summary>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。操作结果包含所有分区数据。</returns>
    /// <remarks>本方法首先判断是否存在本地缓存，如本地有缓存则使用本地缓存数据；如没有则从官方 API 地址获取数据并写入缓存。</remarks>
    public async Task<SectionInfo[]> GetSectionDataAsync(CancellationToken cancellationToken = default)
    {
        return await JsonFileCache.GetDataAsync(cancellationToken) ?? await RefreshFromApiAsync(cancellationToken);
    }

    /// <summary>
    /// 强制从 API 立即刷新最新的分区数据。
    /// </summary>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。操作结果包含刷新后的所有分区数据。</returns>
    public Task<SectionInfo[]> ForceUpdateFromApiAsync(CancellationToken cancellationToken = default)
    {
        return RefreshFromApiAsync(cancellationToken);
    }

    /// <summary>
    /// 本地缓存对象。
    /// </summary>
    private JsonFileCache<SectionInfo[]> JsonFileCache { get; } = new(CacheFilePath);

    /// <summary>
    ///     强制从官方 API 刷新分区数据并更新缓存。
    /// </summary>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。操作结果包含从 API 中获得的分区数据。</returns>
    private async Task<SectionInfo[]> RefreshFromApiAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await ApiService.Fetch<SectionInfo[]>(ApiEndpoints.Forum.AllBoards,cancellationToken);

            var data =res.Data?? throw new InvalidOperationException("无法从 API 中提取数据。");

            await SaveToLocalCacheAsync(data, cancellationToken);
            return data;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("SectionInfoManager", ex.Message);
            throw;
        }
    }

    /// <summary>
    ///     清理缓存。
    /// </summary>
    public void ClearCacheAsync()
    {
        try
        {
            JsonFileCache.Delete();
        }
        catch (Exception ex)
        {
            Trace.TraceError("清理分区缓存失败, 错误 = {0}", ex.Message);
        }
    }

    /// <summary>
    ///     按名称搜索分区（可选功能）
    /// </summary>
    public async Task<SectionInfo?> FindSectionByNameAsync(string sectionName)
    {
        var sections = await LoadFromLocalCacheAsync();
        return sections.FirstOrDefault(s =>
            s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     按ID搜索版面（可选功能）
    /// </summary>
    public async Task<BoardInfo?> FindBoardByIdAsync(int boardId)
    {
        var sections = await LoadFromLocalCacheAsync();

        return sections.Select(section => section.Boards.FirstOrDefault(b => b.Id == boardId)).OfType<BoardInfo>().FirstOrDefault();
    }

    #region 私有方法


    /// <summary>
    ///     从本地缓存加载分区数据。
    /// </summary>
    private async Task<SectionInfo[]> LoadFromLocalCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await JsonFileCache.GetDataAsync(cancellationToken);

        }
        catch (Exception ex)
        {
            Trace.TraceError("读取分区缓存失败, 错误 = {0}", ex.Message);
            return [];
        }
    }

    /// <summary>
    ///     保存分区数据到本地缓存。
    /// </summary>
    private async Task SaveToLocalCacheAsync(SectionInfo[] data, CancellationToken cancellationToken = default)
    {
        try
        {
            await JsonFileCache.UpdateDataAsync(data, cancellationToken);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"保存分区缓存失败: {ex.Message}");
            throw;
        }
    }

    #endregion
}