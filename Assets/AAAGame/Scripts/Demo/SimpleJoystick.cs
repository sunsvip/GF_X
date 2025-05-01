using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SimpleJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    [SerializeField] private float handleRange = 50f;
    [SerializeField] private bool showOnTouch = true;
    [SerializeField][Range(0, 0.5f)] private float deadZone = 0.2f;
    [Header("References")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    public UnityAction OnPointerUpCallback = null;
    private bool m_Enable;
    public bool Enable
    {
        get => m_Enable;
        set
        {
            m_Enable = value;
            canvasGroup.alpha = m_Enable ? 1 : 0;
        }
    }
    // 输入值属性
    public Vector2 Direction => new Vector2(Horizontal, Vertical);
    public float Horizontal => clampedInput.x;
    public float Vertical => clampedInput.y;
    public float Distance => Direction.magnitude;
    private CanvasGroup canvasGroup;
    private RectTransform baseRect;
    private Vector2 originalBackgroundPos;
    private Vector2 clampedInput;

    private void Awake()
    {
        baseRect = GetComponent<RectTransform>();
        originalBackgroundPos = background.anchoredPosition;
        canvasGroup = GetComponent<CanvasGroup>();
        if(canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        canvasGroup.alpha = 1;
        CalculateInput(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        CalculateInput(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 重置所有状态
        clampedInput = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        background.anchoredPosition = originalBackgroundPos;
        canvasGroup.alpha = 0.5f;
        OnPointerUpCallback?.Invoke();
    }

    private void CalculateInput(PointerEventData eventData)
    {
        // 坐标转换
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint);

        // 计算输入向量
        Vector2 inputVector = localPoint / (background.sizeDelta * 0.5f);

        // 应用死区和限制范围
        float inputMagnitude = inputVector.magnitude;
        if (inputMagnitude < deadZone)
        {
            clampedInput = Vector2.zero;
        }
        else
        {
            float normalizedMagnitude = (inputMagnitude - deadZone) / (1 - deadZone);
            clampedInput = inputVector.normalized * Mathf.Clamp01(normalizedMagnitude);
        }

        // 更新手柄位置
        handle.anchoredPosition = clampedInput * background.sizeDelta * 0.5f * handleRange * 0.01f;
    }

    // 在编辑器模式下实时预览摇杆范围
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!background || !handle) return;

        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.DrawWireDisc(background.position, Vector3.forward,
            background.sizeDelta.x * 0.5f * handleRange * 0.01f);
    }
#endif
}