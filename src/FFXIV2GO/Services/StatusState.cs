namespace FFXIV2GO.Services;

/// <summary>
/// 状态指示图标三态：Ok=绿对号，Fail=红错号，Neutral=灰点点。
/// </summary>
public enum StatusState
{
    /// <summary>正常（绿色对号）。</summary>
    Ok,

    /// <summary>异常（红色错号）。</summary>
    Fail,

    /// <summary>中性 / 待处理（灰色点点点）。</summary>
    Neutral
}
