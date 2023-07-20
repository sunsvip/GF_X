using System;
using GameFramework;

/// <summary>
/// DownLoad 结果
/// </summary>
public class DownloadResult : IReference
{
    /// <summary>
    /// 是否有错误
    /// </summary>
    public bool IsError { get; private set; }
    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrorMessage { get; private set; }
    /// <summary>
    /// 自定义数据
    /// </summary>
    public object UserData { get; private set; }

    public static DownloadResult Create(bool isError, string errorMessage, object userData)
    {
        DownloadResult downLoadResult = ReferencePool.Acquire<DownloadResult>();
        downLoadResult.IsError = isError;
        downLoadResult.ErrorMessage = errorMessage;
        downLoadResult.UserData = userData;
        return downLoadResult;
    }

    public void Clear()
    {
        IsError = false;
        ErrorMessage = string.Empty;
        UserData = null;
    }
}