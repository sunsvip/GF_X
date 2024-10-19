
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using DG.Tweening;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
[Serializable]
public class SerializeFieldData
{
    public string VarName;      //变量名
    public GameObject[] Targets;//关联的GameObject
    public string VarType;      //变量类型FullName,带有名字空间
    public int VarPrefix;//变量private/protect/public
    public SerializeFieldData(string varName, GameObject[] targets = null)
    {
        VarName = varName;
        Targets = targets ?? new GameObject[1];
    }
    public T GetComponent<T>(int idx) where T : Component
    {
        return Targets[idx].GetComponent<T>();
    }
    public T[] GetComponents<T>() where T : Component
    {
        T[] result = new T[Targets.Length];
        for (int i = 0; i < Targets.Length; i++)
        {
            result[i] = Targets[i].GetComponent<T>();
        }
        return result;
    }
}

public class UIFormBase : UIFormLogic
{
    [HideInInspector][SerializeField] SerializeFieldData[] _fields = new SerializeFieldData[0];
    [SerializeField] protected RectTransform topBar = null;
    /// <summary>
    /// UI打开动画
    /// </summary>
    [HideInInspector][SerializeField] DOTweenSequence m_OpenAnimation = null;
    /// <summary>    /// UI关闭动画, 若为空,默认使用UI打开动画倒放
    /// </summary>
    [HideInInspector][SerializeField] DOTweenSequence m_CloseAnimation = null;
    public UIParams Params { get; private set; }
    public int Id => this.UIForm.SerialId;
    public bool Interactable
    {
        get
        {
            return canvasGroup.interactable;
        }
        set
        {
            canvasGroup.interactable = value;
        }
    }
    private CanvasGroup canvasGroup = null;
    protected Canvas UICanvas { get; private set; }
    private bool isOnEscape;
    IList<KeyValuePair<Type, string>> m_ItemPools = null;
    /// <summary>
    /// 子UI界面, 会随着父界面关闭而关闭
    /// </summary>
    IList<int> m_SubUIForms = null;
    bool m_Covering = false;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        Array.Clear(_fields, 0, _fields.Length);
        UICanvas = gameObject.GetOrAddComponent<Canvas>();
        canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
        RectTransform transform = GetComponent<RectTransform>();
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.anchoredPosition = Vector2.zero;
        transform.sizeDelta = Vector2.zero;
        transform.localPosition = Vector3.zero;
        gameObject.GetOrAddComponent<GraphicRaycaster>();
        InitLocalization();
        FitHoleScreen();
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_Covering = false;
        Params = userData as UIParams;
        var cvs = GetComponent<Canvas>();
        cvs.overrideSorting = true;
        cvs.sortingOrder = Params.SortOrder ?? 0;
        Interactable = false;
        isOnEscape = Params.AllowEscapeClose ?? false;
        PlayUIAnimation(true, OnOpenAnimationComplete);
        Params.OpenCallback?.Invoke(this);
    }
    protected override void OnCover()
    {
        base.OnCover();
        m_Covering = true;
    }
    protected override void OnReveal()
    {
        base.OnReveal();
        if (m_Covering)
            PlayUIAnimation(true, null);
        m_Covering = false;
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        if (isOnEscape && Input.GetKeyDown(KeyCode.Escape) && GF.UI.GetTopUIFormId() == this.UIForm.SerialId)
        {
            this.OnClickClose();
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (!isShutdown)
        {
            Params.CloseCallback?.Invoke(this);
            ReferencePool.Release(Params);
            CloseAllSubUIForms();
        }
        UnspawnAllItemObjects();
        base.OnClose(isShutdown, userData);
    }

    private void OnDestroy()
    {
        DestroyAllItemPool();
    }
    /// <summary>
    /// 打开子UI Form
    /// </summary>
    /// <param name="viewName"></param>
    /// <param name="params"></param>
    /// <returns></returns>
    public int OpenSubUIForm(UIViews viewName, UIParams @params = null)
    {
        if (m_SubUIForms == null) m_SubUIForms = new List<int>(2);
        if (@params == null)
        {
            @params = UIParams.Create();
        }
        @params.SortOrder = Params.SortOrder + m_SubUIForms.Count + 1;
        var uiformId = GF.UI.OpenUIForm(viewName, @params);
        m_SubUIForms.Add(uiformId);
        return uiformId;
    }
    /// <summary>
    /// 关闭子UI Form
    /// </summary>
    /// <param name="uiformId"></param>
    public void CloseSubUIForm(int uiformId)
    {
        if (!m_SubUIForms.Contains(uiformId)) return;
        m_SubUIForms.Remove(uiformId);
        if (GF.UI.HasUIForm(uiformId))
            GF.UI.CloseUIForm(uiformId);
    }
    private void CloseAllSubUIForms()
    {
        if (m_SubUIForms != null)
        {
            for (int i = m_SubUIForms.Count - 1; i >= 0; i--)
            {
                CloseSubUIForm(m_SubUIForms[i]);
            }
        }
    }
    public SerializeFieldData[] GetFieldsProperties()
    {
        return _fields;
    }
    public void ModifyFieldsProperties(SerializeFieldData[] modified)
    {
        this._fields = modified;
    }
    private void UnspawnAllItemObjects()
    {
        if (m_ItemPools == null) return;
        const string FUNC_NAME = "UnspawnAll";
        foreach (var item in m_ItemPools)
        {
            var pool = GF.ObjectPool.GetObjectPool(item.Key, item.Value);
            var unspawnAllFunc = pool.GetType().GetMethod(FUNC_NAME, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            unspawnAllFunc.Invoke(pool, null);
        }
    }
    private void DestroyAllItemPool()
    {
        if (m_ItemPools == null) return;

        foreach (var item in m_ItemPools)
        {
            GF.ObjectPool.DestroyObjectPool(item.Key, item.Value);
        }
    }

    /// <summary>
    /// 从对象池获取一个Item (界面关闭时会自动Unspawn)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="itemTemple">Item实例化模板</param>
    /// <param name="instanceRoot">Item实例化到根节点</param>
    /// <param name="capacity">对象池容量</param>
    /// <param name="expireTime">对象过期时间(过期后自动销毁)</param>
    /// <returns></returns>
    protected T SpawnItem<T>(GameObject itemTemple, Transform instanceRoot, float autoReleaseInterval = 5f, int capacity = 50, float expireTime = 50) where T : UIItemObject, new()
    {
        var itemTempleId = itemTemple.GetHashCode().ToString();
        GameFramework.ObjectPool.IObjectPool<T> pool;
        if (GF.ObjectPool.HasObjectPool<T>(itemTempleId))
        {
            pool = GF.ObjectPool.GetObjectPool<T>(itemTempleId);
        }
        else
        {
            pool = GF.ObjectPool.CreateSingleSpawnObjectPool<T>(itemTempleId, autoReleaseInterval, capacity, expireTime, 0);
            if (m_ItemPools == null) m_ItemPools = new List<KeyValuePair<Type, string>>();
            m_ItemPools.Add(KeyValuePair.Create(typeof(T), itemTempleId));
        }

        var spawn = pool.Spawn();
        if (spawn == null)
        {
            var itemInstance = Instantiate(itemTemple, instanceRoot);
            itemInstance.transform.localPosition = Vector3.zero;
            itemInstance.transform.localRotation = Quaternion.identity;
            if (!itemInstance.activeSelf) itemInstance.SetActive(true);
            spawn = UIItemObject.Create<T>(itemInstance);
            pool.Register(spawn, true);
        }
        return spawn;
    }
    /// <summary>
    /// 从对象池回收Item
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="itemTemple">Item实例化模板</param>
    /// <param name="itemObject">要回收的Item实例</param>
    protected void UnspawnItem<T>(GameObject itemTemple, T itemObject) where T : UIItemObject, new()
    {
        UnspawnItem<T>(itemTemple, itemObject.gameObject);
    }
    /// <summary>
    /// 从对象池回收Item
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="itemTemple">Item实例化模板</param>
    /// <param name="itemInstance">要回收的Item实例</param>
    protected void UnspawnItem<T>(GameObject itemTemple, GameObject itemInstance) where T : UIItemObject, new()
    {
        var itemTempleId = itemTemple.GetHashCode().ToString();
        if (!GF.ObjectPool.HasObjectPool<T>(itemTempleId)) return;

        var pool = GF.ObjectPool.GetObjectPool<T>(itemTempleId);
        pool.Unspawn(itemInstance);
    }
    /// <summary>
    /// 更新界面中静态文本的多语言文字
    /// </summary>
    public virtual void InitLocalization()
    {
        UIStringKey[] texts = GetComponentsInChildren<UIStringKey>(true);
        foreach (var t in texts)
        {
            if (t.TryGetComponent<TMPro.TextMeshProUGUI>(out var textMeshCom))
            {
                textMeshCom.text = GF.Localization.GetString(t.Key);
            }
            else if (t.TryGetComponent<Text>(out var textCom))
            {
                textCom.text = GF.Localization.GetString(t.Key);
            }
        }
    }
    /// <summary>
    /// 适配挖空屏/刘海屏
    /// </summary>
    private void FitHoleScreen()
    {
        if (topBar == null)
        {
            return;
        }
        float topSpace = Screen.height - Screen.safeArea.height;
        if (topSpace < 1f)
        {
            return;
        }
#if UNITY_IOS
        topSpace = 80;
#endif
        var pos = topBar.anchoredPosition;
        pos.y = -topSpace;
        topBar.anchoredPosition = pos;
    }
    private void PlayUIAnimation(bool isOpen, GameFrameworkAction onAnimComplete)
    {
        if (isOpen)
        {
            if (m_OpenAnimation != null)
            {
                var anim = m_OpenAnimation.DOPlay();
                if (onAnimComplete != null)
                    anim.OnComplete(onAnimComplete.Invoke);
            }
            else
            {
                onAnimComplete?.Invoke();
            }
        }
        else
        {
            //如果关闭动画未配置, 默认将打开动画倒放作为关闭动画
            if (m_CloseAnimation != null)
            {
                var anim = m_CloseAnimation.DOPlay();
                if (onAnimComplete != null)
                    anim.OnComplete(onAnimComplete.Invoke);
            }
            else
            {
                if (m_OpenAnimation != null)
                {
                    var anim = m_OpenAnimation.DORewind();
                    if (onAnimComplete != null)
                        anim.OnComplete(onAnimComplete.Invoke);
                }
                else
                {
                    onAnimComplete?.Invoke();
                }
            }
        }
    }
    public void CloseWithAnimation()
    {
        Interactable = false;
        PlayUIAnimation(false, OnCloseAnimationComplete);
    }


    public virtual void OnClickClose()
    {
        GF.Sound.PlayEffect("ui/ui_click.wav");
        GF.UI.Close(this.UIForm);
    }

    public void ClickUIButton(string bt_tag)
    {
        GF.Sound.PlayEffect("ui/ui_click.wav");
        OnButtonClick(this, bt_tag);
    }
    public void ClickUIButton(Button btSelf)
    {
        GF.Sound.PlayEffect("ui/ui_click.wav");
        OnButtonClick(this, btSelf);
    }
    protected virtual void OnButtonClick(object sender, string btId)
    {
        Params.ButtonClickCallback?.Invoke(sender, btId);
    }
    protected virtual void OnButtonClick(object sender, UnityEngine.UI.Button btSelf)
    {
    }
    /// <summary>
    /// UI打开动画完成时回调
    /// </summary>
    protected virtual void OnOpenAnimationComplete()
    {
        Interactable = true;
    }
    /// <summary>
    /// UI关闭动画完成时回调
    /// </summary>
    protected virtual void OnCloseAnimationComplete()
    {
        GF.UI.CloseUIForm(this.UIForm);
    }
}
