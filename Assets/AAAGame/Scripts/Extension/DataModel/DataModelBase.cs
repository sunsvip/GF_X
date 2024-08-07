
namespace GameFramework
{

    public abstract class DataModelBase : IReference
    {
        [Newtonsoft.Json.JsonIgnore]
        public int Id { get; private set; } = 0;
        [Newtonsoft.Json.JsonIgnore]
        public RefParams Userdata { get; private set; } = null;

        /// <summary>
        /// 首次获取时
        /// </summary>
        /// <param name="userdata"></param>
        protected virtual void OnCreate(RefParams userdata) { }

        /// <summary>
        /// 当对象回收时自动调用OnClear,常用于重置变量属性,避免复用对象时带有默认数值(脏数据)
        /// </summary>
        protected virtual void OnRelease() { }
        internal void Init(int id, RefParams userdata)
        {
            this.Id = id;
            this.Userdata = userdata;
            OnCreate(userdata);
        }
        public void Clear()
        {
            this.Id = 0;
            if (Userdata != null)
            {
                ReferencePool.Release(Userdata);
            }
        }

        internal void Shutdown()
        {
            OnRelease();
            ReferencePool.Release(this);
        }
    }

}