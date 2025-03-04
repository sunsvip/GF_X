using System;
using System.Collections.Generic;
using System.Linq;
using GameFramework.Event;
using UnityGameFramework.Runtime;
namespace GameFramework
{

    public class DataModelComponent : GameFrameworkComponent
    {
        private Dictionary<TypeIdPair, DataModelBase> m_DataModels;
        /// <summary>
        /// 获取DataModel数量。
        /// </summary>
        public int Count
        {
            get
            {
                return m_DataModels.Count;
            }
        }
        protected override void Awake()
        {
            base.Awake();
            m_DataModels = new Dictionary<TypeIdPair, DataModelBase>();
        }

        private void Start()
        {
            GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventCallback);
        }
        //private void OnDestroy()
        //{
        //    GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEventCallback);
        //}
        private void OnGFEventCallback(object sender, GameEventArgs e)
        {
            var args = e as GFEventArgs;
            if (args.EventType == GFEventType.ApplicationQuit)
            {
                ReleaseAll();
            }
        }
        /// <summary>
        /// 清除所有已存在的数据模型
        /// </summary>
        public void ReleaseAll()
        {
            var keys = m_DataModels.Keys.ToArray();
            for (int i = keys.Length - 1; i >= 0; i--)
            {
                this.InternalReleaseDataModel(keys[i]);
            }
        }
        /// <summary>
        /// 是否存在数据模型。
        /// </summary>
        /// <typeparam name="T">数据模型行的类型。</typeparam>
        /// <returns>是否存在数据模型。</returns>
        public bool HasDataModel<T>() where T : DataModelBase
        {
            return InternalHasDataModel(new TypeIdPair(typeof(T)));
        }

        /// <summary>
        /// 是否存在数据模型。
        /// </summary>
        /// <param name="dataRowType">数据模型行的类型。</param>
        /// <returns>是否存在数据模型。</returns>
        public bool HasDataModel(Type dataRowType)
        {
            if (dataRowType == null)
            {
                throw new GameFrameworkException("DataModel type is invalid.");
            }

            if (!typeof(DataModelBase).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("DataModel type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalHasDataModel(new TypeIdPair(dataRowType));
        }

        /// <summary>
        /// 是否存在数据模型。
        /// </summary>
        /// <typeparam name="T">数据模型行的类型。</typeparam>
        /// <param name="name">数据模型名称。</param>
        /// <returns>是否存在数据模型。</returns>
        public bool HasDataModel<T>(int id) where T : DataModelBase
        {
            return InternalHasDataModel(new TypeIdPair(typeof(T), id));
        }

        /// <summary>
        /// 是否存在数据模型。
        /// </summary>
        /// <param name="dataRowType">数据模型行的类型。</param>
        /// <param name="id">数据模型名称。</param>
        /// <returns>是否存在数据模型。</returns>
        public bool HasDataModel(Type dataRowType, int id)
        {
            if (dataRowType == null)
            {
                throw new GameFrameworkException("DataModel type is invalid.");
            }

            if (!typeof(DataModelBase).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("DataModel type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalHasDataModel(new TypeIdPair(dataRowType, id));
        }

        /// <summary>
        /// 获取数据模型。
        /// </summary>
        /// <typeparam name="T">数据模型行的类型。</typeparam>
        /// <returns>要获取的数据模型。</returns>
        public T GetDataModel<T>() where T : DataModelBase
        {
            return InternalGetDataModel(new TypeIdPair(typeof(T))) as T;
        }
        /// <summary>
        /// 获取或创建数据模型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetOrCreate<T>() where T : DataModelBase, new()
        {
            T result = GetDataModel<T>();
            if (result == null)
            {
                result = CreateDataModel<T>();
            }
            return result;
        }
        /// <summary>
        /// 获取数据模型。
        /// </summary>
        /// <param name="dataRowType">数据模型行的类型。</param>
        /// <returns>要获取的数据模型。</returns>
        public DataModelBase GetDataModel(Type dataRowType)
        {
            if (dataRowType == null)
            {
                throw new GameFrameworkException("DataModel type is invalid.");
            }

            if (!typeof(DataModelBase).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("DataModel type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalGetDataModel(new TypeIdPair(dataRowType));
        }
        /// <summary>
        /// 获取或创建数据模型
        /// </summary>
        /// <param name="dataRowType"></param>
        /// <returns></returns>
        public DataModelBase GetOrCreate(Type dataRowType)
        {
            var result = GetDataModel(dataRowType);
            if (result == null)
            {
                result = CreateDataModel(dataRowType);
            }
            return result;
        }
        /// <summary>
        /// 获取数据模型。
        /// </summary>
        /// <typeparam name="T">数据模型行的类型。</typeparam>
        /// <param name="id">数据模型名称。</param>
        /// <returns>要获取的数据模型。</returns>
        public T GetDataModel<T>(int id) where T : DataModelBase
        {
            return InternalGetDataModel(new TypeIdPair(typeof(T), id)) as T;
        }
        /// <summary>
        /// 获取或创建数据模型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public T GetOrCreate<T>(int id) where T : DataModelBase, new()
        {
            T result = GetDataModel<T>(id);
            if (result == null)
            {
                result = CreateDataModel<T>(id);
            }
            return result;
        }
        /// <summary>
        /// 获取数据模型。
        /// </summary>
        /// <param name="dataRowType">数据模型行的类型。</param>
        /// <param name="id">数据模型名称。</param>
        /// <returns>要获取的数据模型。</returns>
        public DataModelBase GetDataModel(Type dataRowType, int id)
        {
            if (dataRowType == null)
            {
                throw new GameFrameworkException("DataModel type is invalid.");
            }

            if (!typeof(DataModelBase).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("DataModel type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalGetDataModel(new TypeIdPair(dataRowType, id));
        }
        /// <summary>
        /// 获取或创建数据模型
        /// </summary>
        /// <param name="dataRowType"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataModelBase GetOrCreate(Type dataRowType, int id)
        {
            var result = GetDataModel(dataRowType, id);
            if (result == null)
            {
                result = CreateDataModel(dataRowType, id);
            }
            return result;
        }
        /// <summary>
        /// 获取所有数据模型。
        /// </summary>
        /// <returns>所有数据模型。</returns>
        public DataModelBase[] GetAllDataModels()
        {
            int index = 0;
            DataModelBase[] results = new DataModelBase[m_DataModels.Count];
            foreach (KeyValuePair<TypeIdPair, DataModelBase> DataModel in m_DataModels)
            {
                results[index++] = DataModel.Value;
            }

            return results;
        }
        /// <summary>
        /// 获取某种类型的全部DataModel
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        public void GetAllDataModels<T>(List<T> results) where T : DataModelBase
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<TypeIdPair, DataModelBase> DataModel in m_DataModels)
            {
                if (DataModel.Key.Type == typeof(T))
                {
                    results.Add(DataModel.Value as T);
                }
            }
        }
        /// <summary>
        /// 获取某种类型的全部DataModel
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetAllDataModels<T>() where T : DataModelBase
        {
            var list = new List<T>();
            GetAllDataModels<T>(list);
            return list;
        }
        /// <summary>
        /// 获取所有数据模型。
        /// </summary>
        /// <param name="results">所有数据模型。</param>
        public void GetAllDataModels(List<DataModelBase> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<TypeIdPair, DataModelBase> DataModel in m_DataModels)
            {
                results.Add(DataModel.Value);
            }
        }

        /// <summary>
        /// 创建数据模型。
        /// </summary>
        /// <typeparam name="T">数据模型的类型。</typeparam>
        /// <returns>要创建的数据模型。</returns>
        public T CreateDataModel<T>(RefParams userdata = null) where T : DataModelBase, new()
        {

            return CreateDataModel<T>(0, userdata);
        }

        /// <summary>
        /// 创建数据模型。
        /// </summary>
        /// <param name="dataRowType">数据模型行的类型。</param>
        /// <returns>要创建的数据模型。</returns>
        public DataModelBase CreateDataModel(Type dataRowType)
        {
            return CreateDataModel(dataRowType, 0);
        }

        /// <summary>
        /// 创建数据模型。
        /// </summary>
        /// <typeparam name="T">数据模型行的类型。</typeparam>
        /// <param name="id">数据模型名称。</param>
        /// <returns>要创建的数据模型。</returns>
        public T CreateDataModel<T>(int id, RefParams userdata = null) where T : DataModelBase, new()
        {
            TypeIdPair TypeIdPair = new TypeIdPair(typeof(T), id);
            if (HasDataModel<T>(id))
            {
                throw new GameFrameworkException(Utility.Text.Format("Already exist data model '{0}'.", TypeIdPair));
            }

            T DataModel = ReferencePool.Acquire<T>();
            DataModel.Init(id, userdata);
            m_DataModels.Add(TypeIdPair, DataModel);
            return DataModel;
        }

        /// <summary>
        /// 创建数据模型。
        /// </summary>
        /// <param name="dataRowType">数据模型行的类型。</param>
        /// <param name="id">数据模型名称。</param>
        /// <returns>要创建的数据模型。</returns>
        public DataModelBase CreateDataModel(Type dataRowType, int id, RefParams userdata = null)
        {
            if (dataRowType == null)
            {
                throw new GameFrameworkException("DataModel type is invalid.");
            }

            if (!typeof(DataModelBase).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("DataModel type '{0}' is invalid.", dataRowType.FullName));
            }

            TypeIdPair TypeIdPair = new TypeIdPair(dataRowType, id);
            if (HasDataModel(dataRowType, id))
            {
                throw new GameFrameworkException(Utility.Text.Format("Already exist data model '{0}'.", TypeIdPair));
            }

            DataModelBase DataModel = ReferencePool.Acquire(dataRowType) as DataModelBase;
            DataModel.Init(id, userdata);
            m_DataModels.Add(TypeIdPair, DataModel);
            return DataModel;
        }

        /// <summary>
        /// 销毁数据模型。
        /// </summary>
        /// <typeparam name="T">数据模型行的类型。</typeparam>
        public bool ReleaseDataModel<T>() where T : DataModelBase
        {
            return InternalReleaseDataModel(new TypeIdPair(typeof(T)));
        }

        /// <summary>
        /// 销毁数据模型。
        /// </summary>
        /// <param name="dataRowType">数据模型行的类型。</param>
        /// <returns>是否销毁数据模型成功。</returns>
        public bool ReleaseDataModel(Type dataRowType)
        {
            if (dataRowType == null)
            {
                throw new GameFrameworkException("Data row type is invalid.");
            }

            if (!typeof(DataModelBase).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalReleaseDataModel(new TypeIdPair(dataRowType));
        }

        /// <summary>
        /// 销毁数据模型。
        /// </summary>
        /// <typeparam name="T">数据模型行的类型。</typeparam>
        /// <param name="id">数据模型名称。</param>
        public bool ReleaseDataModel<T>(int id) where T : DataModelBase
        {
            return InternalReleaseDataModel(new TypeIdPair(typeof(T), id));
        }

        /// <summary>
        /// 销毁数据模型。
        /// </summary>
        /// <param name="dataRowType">数据模型行的类型。</param>
        /// <param name="name">数据模型名称。</param>
        /// <returns>是否销毁数据模型成功。</returns>
        public bool ReleaseDataModel(Type dataRowType, int id)
        {
            if (dataRowType == null)
            {
                throw new GameFrameworkException("DataModel type is invalid.");
            }

            if (!typeof(DataModelBase).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("DataModel type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalReleaseDataModel(new TypeIdPair(dataRowType, id));
        }

        /// <summary>
        /// 销毁数据模型。
        /// </summary>
        /// <typeparam name="T">数据模型行的类型。</typeparam>
        /// <param name="DataModel">要销毁的数据模型。</param>
        /// <returns>是否销毁数据模型成功。</returns>
        public bool ReleaseDataModel<T>(DataModelBase DataModel) where T : DataModelBase
        {
            if (DataModel == null)
            {
                throw new GameFrameworkException("DataModel is invalid.");
            }

            return InternalReleaseDataModel(new TypeIdPair(typeof(T), DataModel.Id));
        }

        /// <summary>
        /// 销毁数据模型。
        /// </summary>
        /// <param name="DataModel">要销毁的数据模型。</param>
        /// <returns>是否销毁数据模型成功。</returns>
        public bool ReleaseDataModel(DataModelBase DataModel)
        {
            if (DataModel == null)
            {
                throw new GameFrameworkException("DataModel is invalid.");
            }

            return InternalReleaseDataModel(new TypeIdPair(DataModel.GetType(), DataModel.Id));
        }

        private bool InternalHasDataModel(TypeIdPair TypeIdPair)
        {
            return m_DataModels.ContainsKey(TypeIdPair);
        }

        private DataModelBase InternalGetDataModel(TypeIdPair TypeIdPair)
        {
            DataModelBase DataModel = null;
            if (m_DataModels.TryGetValue(TypeIdPair, out DataModel))
            {
                return DataModel;
            }

            return null;
        }
        private bool InternalReleaseDataModel(TypeIdPair TypeIdPair)
        {
            DataModelBase DataModel = null;
            if (m_DataModels.TryGetValue(TypeIdPair, out DataModel))
            {
                DataModel.Shutdown();
                return m_DataModels.Remove(TypeIdPair);
            }

            return false;
        }
    }
}