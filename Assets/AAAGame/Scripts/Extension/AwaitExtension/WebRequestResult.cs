using GameFramework;

public class WebRequestResult : IReference
{
    /// <summary>
    /// web请求 返回数据
    /// </summary>
    public byte[] Bytes { get; private set; }
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


    public static WebRequestResult Create(byte[] bytes, bool isError, string errorMessage, object userData)
    {
        WebRequestResult webResult = ReferencePool.Acquire<WebRequestResult>();
        webResult.Bytes = bytes;
        webResult.IsError = isError;
        webResult.ErrorMessage = errorMessage;
        webResult.UserData = userData;
        return webResult;
    }

    public WebRequestResult Init(byte[] bytes, bool isError, string errorMessage, object userData)
    {
        this.Bytes = bytes;
        this.IsError = isError;
        this.ErrorMessage = errorMessage;
        this.UserData = userData;
        return this;
    }
    public void Clear()
    {
        Bytes = null;
        IsError = false;
        ErrorMessage = string.Empty;
        UserData = null;
    }
}