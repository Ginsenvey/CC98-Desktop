using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using CC98.Kernel;
using CC98.Objects;

namespace CC98.Services;

public class BoardSectionManager
{
    private const string CacheFileName = "board_sections.json";

    // 数据模型
    /// <summary>
    ///     私有构造方法。
    /// </summary>
    private BoardSectionManager()
    {
        // 私有构造函数
    }

    /// <summary>
    ///     对象的唯一实例。
    /// </summary>
    public static BoardSectionManager Instance { get; } = new();

    /// <summary>
    ///     从API刷新分区数据并更新缓存。
    /// </summary>
    public async Task<bool> RefreshFromApiAsync(string apiUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await LoginService.Vpn.GetAsync(apiUrl);

            var jsonResponse = await res.Content.ReadAsStringAsync(cancellationToken);

            // 2. 解析并提取所需数据
            var sections = ParseSections(jsonResponse);

            // 3. 保存到缓存
            return await SaveToCacheAsync(sections);
        }
        catch (Exception ex)
        {
            await App.Logger.WriteAsync("SectionInfoManager", "刷新全部版面信息失败", ex.Message);
            return false;
        }
    }

    /// <summary>
    ///     从缓存读取分区数据
    /// </summary>
    public async Task<IEnumerable<SectionInfo>> LoadFromCacheAsync()
    {
        try
        {
            var folder = ApplicationData.Current.LocalCacheFolder;
            var filePath = Path.Combine(folder.Path, CacheFileName);

            var cache = await LocalCache.CreateAsync(filePath);

            if (cache.IsAvailable && !string.IsNullOrWhiteSpace(cache.Content))
                return JsonSerialize.Deserialize<List<SectionInfo>>(cache.Content)!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"读取分区缓存失败: {ex.Message}");
        }

        return new List<SectionInfo>();
    }

    /// <summary>
    ///     检查缓存是否存在且有效
    /// </summary>
    public bool HasValidCacheAsync()
    {
        try
        {
            var folder = ApplicationData.Current.LocalCacheFolder;
            var filePath = Path.Combine(folder.Path, CacheFileName);

            return File.Exists(filePath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     清理缓存
    /// </summary>
    public void ClearCacheAsync()
    {
        try
        {
            var folder = ApplicationData.Current.LocalCacheFolder;
            var filePath = Path.Combine(folder.Path, CacheFileName);

            if (File.Exists(filePath)) File.Delete(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清理分区缓存失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     按名称搜索分区（可选功能）
    /// </summary>
    public async Task<SectionInfo?> FindSectionByNameAsync(string sectionName)
    {
        var sections = await LoadFromCacheAsync();
        return sections.FirstOrDefault(s =>
            s.Name.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     按ID搜索版面（可选功能）
    /// </summary>
    public async Task<BoardInfo?> FindBoardByIdAsync(int boardId)
    {
        var sections = await LoadFromCacheAsync();

        foreach (var section in sections)
        {
            var board = section.Boards.FirstOrDefault(b => b.Id == boardId);
            if (board != null)
                return board;
        }

        return null;
    }

    #region 私有方法

    /// <summary>
    ///     解析JSON并提取所需的分区数据
    /// </summary>
    private List<SectionInfo> ParseSections(string jsonResponse)
    {
        var sections = new List<SectionInfo>();

        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement;

        // 假设API返回的是分区数组
        if (root.ValueKind == JsonValueKind.Array)
            foreach (var sectionElement in root.EnumerateArray())
            {
                var section = new SectionInfo();

                // 提取分区名称
                if (sectionElement.TryGetProperty("name", out var nameProp))
                    section.Name = nameProp.GetString() ?? string.Empty;

                // 提取版主列表
                if (sectionElement.TryGetProperty("masters", out var mastersProp) &&
                    mastersProp.ValueKind == JsonValueKind.Array)
                    foreach (var master in mastersProp.EnumerateArray())
                        section.Masters.Add(master.GetString() ?? string.Empty);

                // 提取版面列表
                if (sectionElement.TryGetProperty("boards", out var boardsProp) &&
                    boardsProp.ValueKind == JsonValueKind.Array)
                    foreach (var boardElement in boardsProp.EnumerateArray())
                    {
                        var board = new BoardInfo();

                        if (boardElement.TryGetProperty("id", out var idProp))
                            board.Id = idProp.GetInt32();

                        if (boardElement.TryGetProperty("name", out var boardNameProp))
                            board.Name = boardNameProp.GetString() ?? string.Empty;

                        section.Boards.Add(board);
                    }

                sections.Add(section);
            }

        return sections;
    }

    /// <summary>
    ///     保存分区数据到缓存
    /// </summary>
    private async Task<bool> SaveToCacheAsync(List<SectionInfo> sections)
    {
        try
        {
            var json = JsonSerialize.Serialize(sections);

            var folder = ApplicationData.Current.LocalCacheFolder;
            var filePath = Path.Combine(folder.Path, CacheFileName);

            var result = await LocalCache.SaveJsonAsync(
                filePath,
                json,
                false);

            return result.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存分区缓存失败: {ex.Message}");
            return false;
        }
    }

    #endregion
}