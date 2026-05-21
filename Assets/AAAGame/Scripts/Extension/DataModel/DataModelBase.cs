
namespace GameFramework
{
    public abstract class DataModelBase : IReference
    {
        [Newtonsoft.Json.JsonIgnore]
        public int Id { get; private set; } = 0;
        [Newtonsoft.Json.JsonIgnore]
        public RefParams Userdata { get; private set; } = null;

        /// <summary>
        /// 每次取用时自动调用
        /// </summary>
        /// <param name="userdata"></param>
        protected virtual void OnCreate(RefParams userdata) { }

        /// <summary>
        /// 当对象回收时自动调用
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
            OnRelease();
            this.Id = 0;
            ReleaseUserdata();
        }

        internal void Shutdown()
        {
            ReferencePool.Release(this);
        }

        protected void ReleaseUserdata()
        {
            if (Userdata != null)
            {
                ReferencePool.Release(Userdata);
                Userdata = null;
            }
        }
    }

}
