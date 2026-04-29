using System.Collections.Frozen;
using System.Collections.Generic;
using FluentIcons.Common;

namespace CC98.Services.Helpers; 

/// <summary>
///     为版面图标关联提供扩展方法。该类型为静态类型。
/// </summary>
public static class BoardIconHelper
{
    /// <summary>
    ///     包含所有版面编号和对应的图标的字典。
    /// </summary>
    private static FrozenDictionary<int, Symbol> Icons { get; } = new Dictionary<int, Symbol>
    {
        { 758, Symbol.LeafOne },
        { 182, Symbol.Heart },
        { 184, Symbol.ChatHelp },
        { 68, Symbol.Library },
        { 581, Symbol.BeakerEdit },
        { 102, Symbol.HatGraduation },
        { 304, Symbol.Translate },
        { 263, Symbol.TaskList },
        { 105, Symbol.Code },
        { 749, Symbol.ReadingList },
        { 100, Symbol.InfoSparkle },
        { 777, Symbol.Shield },
        { 357, Symbol.BoardSplit },
        { 459, Symbol.CalendarWorkWeek },
        { 515, Symbol.BuildingTownhouse },
        { 235, Symbol.Agents },
        { 782, Symbol.ArrowTrending },
        { 180, Symbol.PhoneDesktop },
        { 30, Symbol.AnimalPawPrint },
        { 760, Symbol.Bookmark },
        { 26, Symbol.BookmarkMultiple },
        { 25, Symbol.MusicNote2 },
        { 91, Symbol.Games },
        { 115, Symbol.LeafTwo },
        { 744, Symbol.MoviesAndTv },
        { 788, Symbol.DriveTrain },
        { 43, Symbol.ShoppingBag },
        { 562, Symbol.ShoppingBagAdd },
        { 569, Symbol.AgentsAdd },
        { 764, Symbol.ShoppingBagArrowLeft },
        { 114, Symbol.HeartBroken },
        { 81, Symbol.WeatherMoon },
        { 152, Symbol.HeartPulse },
        { 135, Symbol.WeatherSunny },
        { 15, Symbol.Sport },
        { 226, Symbol.Toolbox },
        { 258, Symbol.AnimalCat },
        { 173, Symbol.CameraSparkles },
        { 353, Symbol.Sparkle },
        { 229, Symbol.FoodPizza },
        { 261, Symbol.LeafThree },
        { 315, Symbol.Album }
    }.ToFrozenDictionary();

    /// <summary>
    ///     尝试根据版面的编号和名称获取一个合适的图标。
    /// </summary>
    /// <param name="id">版面的编号。</param>
    /// <param name="name">版面的名称。</param>
    /// <returns>和版面对应的图标。</returns>
    public static Symbol GetSymbol(int id, string name)
    {
        if (Icons.TryGetValue(id, out var symbol)) return symbol;

        if (string.IsNullOrEmpty(name)) return Symbol.Tag;

        if (name.Contains("答疑")) return Symbol.Teaching;

        if (name.Contains("院")) return Symbol.ChartPerson;

        return Symbol.Tag;
    }
}