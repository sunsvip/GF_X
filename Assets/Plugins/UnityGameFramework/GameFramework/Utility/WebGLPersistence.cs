using System;
using System.Runtime.InteropServices;

namespace GameFramework
{
    /// <summary>
    /// WebGL / 微信小游戏持久化辅助。
    /// </summary>
    public static class WebGLPersistence
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void GF_SyncFs(string path);

        [DllImport("__Internal")]
        private static extern void GF_InitFsSync();

        [DllImport("__Internal")]
        private static extern int GF_IsFsSyncReady();
#endif

        /// <summary>
        /// 请求初始化持久化文件系统，将已有持久化内容恢复到运行时文件系统。
        /// 非 WebGL 平台下为空操作。
        /// </summary>
        public static void Initialize()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                GF_InitFsSync();
            }
            catch (Exception)
            {
                // 某些运行时或宿主未提供 JS 桥时，保持静默失败，避免影响正常资源流程。
            }
#endif
        }

        /// <summary>
        /// 持久化文件系统是否已完成初始化。
        /// 非 WebGL 平台始终返回 true。
        /// </summary>
        public static bool IsReady
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                try
                {
                    return GF_IsFsSyncReady() != 0;
                }
                catch (Exception)
                {
                    return true;
                }
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// 请求将读写区文件系统内容同步到持久化存储。
        /// 非 WebGL 平台下为空操作。
        /// </summary>
        public static void Sync(string path = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                GF_SyncFs(path);
            }
            catch (Exception)
            {
                // 某些运行时或宿主未提供 JS 桥时，保持静默失败，避免影响正常资源流程。
            }
#endif
        }
    }
}
