using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoHPMA.Models;

/// <summary>
/// 鼠标动作类型枚举
/// </summary>
public enum MouseActionType
{
    Click,
    Drag,
    LongPress
}

/// <summary>
/// 统一的鼠标动作模型，支持点击、拖拽、长按三种操作
/// </summary>
public partial class MouseActionModel : ObservableObject
{
    [ObservableProperty]
    private MouseActionType _actionType = MouseActionType.Click;

    [ObservableProperty]
    private string _description = "动作";

    /// <summary>
    /// X 坐标（点击/长按位置，拖拽起点）
    /// </summary>
    [ObservableProperty]
    private int _x = 200;

    /// <summary>
    /// Y 坐标（点击/长按位置，拖拽起点）
    /// </summary>
    [ObservableProperty]
    private int _y = 200;

    /// <summary>
    /// 拖拽终点 X 坐标（仅拖拽有效）
    /// </summary>
    [ObservableProperty]
    private int _endX = 400;

    /// <summary>
    /// 拖拽终点 Y 坐标（仅拖拽有效）
    /// </summary>
    [ObservableProperty]
    private int _endY = 400;

    /// <summary>
    /// 持续时间（毫秒），拖拽时为拖拽耗时，长按时为按压时长
    /// </summary>
    [ObservableProperty]
    private int _duration = 500;

    /// <summary>
    /// 动作间隔（毫秒），执行完本动作后等待的时间
    /// </summary>
    [ObservableProperty]
    private int _interval = 500;

    /// <summary>
    /// 重复次数（所有类型都有效）
    /// </summary>
    [ObservableProperty]
    private int _times = 1;

    public MouseActionModel() { }

    public MouseActionModel(MouseActionType actionType, string description = "动作")
    {
        ActionType = actionType;
        Description = description;

        // 根据类型设置默认描述
        if (description == "动作")
        {
            Description = actionType switch
            {
                MouseActionType.Click => "点击动作",
                MouseActionType.Drag => "拖拽动作",
                MouseActionType.LongPress => "长按动作",
                _ => "动作"
            };
        }
    }

    /// <summary>
    /// 获取动作类型的显示名称
    /// </summary>
    [JsonIgnore]
    public string ActionTypeName => ActionType switch
    {
        MouseActionType.Click => "点击",
        MouseActionType.Drag => "拖拽",
        MouseActionType.LongPress => "长按",
        _ => "未知"
    };

    /// <summary>
    /// 获取参数的简短描述（格式：坐标 → 终点 → 持续时间 → 间隔时间 → 重复次数）
    /// </summary>
    [JsonIgnore]
    public string ParameterSummary => ActionType switch
    {
        MouseActionType.Click => $"({X},{Y}) @{Interval}ms ×{Times}",
        MouseActionType.Drag => $"({X},{Y})→({EndX},{EndY}) {Duration}ms @{Interval}ms ×{Times}",
        MouseActionType.LongPress => $"({X},{Y}) {Duration}ms @{Interval}ms ×{Times}",
        _ => ""
    };

    /// <summary>
    /// 创建点击动作
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="interval">动作间隔（毫秒）</param>
    /// <param name="times">重复次数</param>
    public static MouseActionModel CreateClick(int x, int y, int interval = 500, int times = 1)
    {
        return new MouseActionModel(MouseActionType.Click)
        {
            X = x,
            Y = y,
            Interval = interval,
            Times = times
        };
    }

    /// <summary>
    /// 创建拖拽动作
    /// </summary>
    /// <param name="startX">起点 X 坐标</param>
    /// <param name="startY">起点 Y 坐标</param>
    /// <param name="endX">终点 X 坐标</param>
    /// <param name="endY">终点 Y 坐标</param>
    /// <param name="duration">拖拽持续时间（毫秒）</param>
    /// <param name="interval">动作间隔（毫秒）</param>
    /// <param name="times">重复次数</param>
    public static MouseActionModel CreateDrag(int startX, int startY, int endX, int endY, int duration = 500, int interval = 500, int times = 1)
    {
        return new MouseActionModel(MouseActionType.Drag)
        {
            X = startX,
            Y = startY,
            EndX = endX,
            EndY = endY,
            Duration = duration,
            Interval = interval,
            Times = times
        };
    }

    /// <summary>
    /// 创建长按动作
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="duration">长按持续时间（毫秒）</param>
    /// <param name="interval">动作间隔（毫秒）</param>
    /// <param name="times">重复次数</param>
    public static MouseActionModel CreateLongPress(int x, int y, int duration = 1000, int interval = 500, int times = 1)
    {
        return new MouseActionModel(MouseActionType.LongPress)
        {
            X = x,
            Y = y,
            Duration = duration,
            Interval = interval,
            Times = times
        };
    }
}

/// <summary>
/// 用于导入导出的动作列表包装类
/// </summary>
public class MouseActionList
{
    public string Version { get; set; } = "1.0";
    public List<MouseActionModel> Actions { get; set; } = new();
}
