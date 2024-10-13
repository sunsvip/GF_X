using DG.Tweening;
using System;
using UnityEngine;
using static DOTweenSequence;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using DG.DOTweenEditor;
using UnityEditorInternal;
using UnityEditor;

#region Editor Inspector
[CanEditMultipleObjects]
[CustomEditor(typeof(DOTweenSequence))]
public class DOTweeSequenceInspector : Editor
{
    SerializedProperty m_Sequence;
    ReorderableList m_SequenceList;

    GUIContent m_PlayBtnContent;
    GUIContent m_RewindBtnContent;
    GUIContent m_ResetBtnContent;
    private GUILayoutOption m_btnHeight;

    private void OnEnable()
    {
        m_PlayBtnContent = EditorGUIUtility.TrIconContent("d_PlayButton@2x", "播放");
        m_RewindBtnContent = EditorGUIUtility.TrIconContent("d_preAudioAutoPlayOff@2x", "倒放");
        m_ResetBtnContent = EditorGUIUtility.TrIconContent("d_preAudioLoopOff@2x", "重置");
        m_btnHeight = GUILayout.Height(35);
        m_Sequence = serializedObject.FindProperty("m_Sequence");
        m_SequenceList = new ReorderableList(serializedObject, m_Sequence);
        m_SequenceList.drawElementCallback = OnDrawSequenceItem;
        m_SequenceList.elementHeightCallback = index =>
        {
            var item = m_Sequence.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(item);
        };
        m_SequenceList.drawHeaderCallback = OnDrawSequenceHeader;
    }

    public override void OnInspectorGUI()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(m_PlayBtnContent, m_btnHeight))
                {
                    if (DOTweenEditorPreview.isPreviewing)
                    {
                        DOTweenEditorPreview.Stop(true, true);
                        (target as DOTweenSequence).DOKill();
                    }
                    DOTweenEditorPreview.PrepareTweenForPreview((target as DOTweenSequence).DOPlay());
                    DOTweenEditorPreview.Start();
                }
                if (GUILayout.Button(m_RewindBtnContent, m_btnHeight))
                {
                    if (DOTweenEditorPreview.isPreviewing)
                    {
                        DOTweenEditorPreview.Stop(true, true);
                        (target as DOTweenSequence).DOKill();
                    }
                    DOTweenEditorPreview.PrepareTweenForPreview((target as DOTweenSequence).DORewind());
                    DOTweenEditorPreview.Start();
                }
                if (GUILayout.Button(m_ResetBtnContent, m_btnHeight))
                {
                    DOTweenEditorPreview.Stop(true, true);
                    (target as DOTweenSequence).DOKill();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        serializedObject.Update();
        m_SequenceList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
        base.OnInspectorGUI();
    }

    private void OnDrawSequenceHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Animation Sequences");
    }
    private void OnDrawSequenceItem(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = m_Sequence.GetArrayElementAtIndex(index);
        EditorGUI.PropertyField(rect, element, true);
    }
}

[CustomPropertyDrawer(typeof(SequenceAnimation))]
public class SequenceTweenMoveDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var onPlay = property.FindPropertyRelative("OnPlay");
        var onUpdate = property.FindPropertyRelative("OnUpdate");
        var onComplete = property.FindPropertyRelative("OnComplete");
        return EditorGUIUtility.singleLineHeight * 11 + (property.isExpanded ? (EditorGUI.GetPropertyHeight(onPlay) + EditorGUI.GetPropertyHeight(onUpdate) + EditorGUI.GetPropertyHeight(onComplete)) : 0);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.indentLevel++;
        var target = property.FindPropertyRelative("Target");
        var addType = property.FindPropertyRelative("AddType");
        var tweenType = property.FindPropertyRelative("AnimationType");
        var toValue = property.FindPropertyRelative("ToValue");
        var useToTarget = property.FindPropertyRelative("UseToTarget");
        var toTarget = property.FindPropertyRelative("ToTarget");
        var useFromValue = property.FindPropertyRelative("UseFromValue");
        var fromValue = property.FindPropertyRelative("FromValue");
        var duration = property.FindPropertyRelative("DurationOrSpeed");
        var speedBased = property.FindPropertyRelative("SpeedBased");
        var delay = property.FindPropertyRelative("Delay");
        var customEase = property.FindPropertyRelative("CustomEase");
        var ease = property.FindPropertyRelative("Ease");
        var easeCurve = property.FindPropertyRelative("EaseCurve");
        var loops = property.FindPropertyRelative("Loops");
        var loopType = property.FindPropertyRelative("LoopType");
        var updateType = property.FindPropertyRelative("UpdateType");
        var snapping = property.FindPropertyRelative("Snapping");
        var onPlay = property.FindPropertyRelative("OnPlay");
        var onUpdate = property.FindPropertyRelative("OnUpdate");
        var onComplete = property.FindPropertyRelative("OnComplete");

        var lastRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(lastRect, addType);

        EditorGUI.BeginChangeCheck();
        lastRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(lastRect, target);
        lastRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(lastRect, tweenType);

        if (EditorGUI.EndChangeCheck())
        {
            var fixedComType = GetFixedComponentType(target.objectReferenceValue as Component, (DOTweenType)tweenType.enumValueIndex);
            if (fixedComType != null)
            {
                target.objectReferenceValue = fixedComType;
            }
        }

        if (target.objectReferenceValue != null && null == GetFixedComponentType(target.objectReferenceValue as Component, (DOTweenType)tweenType.enumValueIndex))
        {
            lastRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.HelpBox(lastRect, string.Format("{0}不支持{1}", target.objectReferenceValue == null ? "Target" : target.objectReferenceValue.GetType().Name, tweenType.enumDisplayNames[tweenType.enumValueIndex]), MessageType.Error);
        }
        const float itemWidth = 110;
        const float setBtnWidth = 30;
        //Delay, Snapping
        lastRect.y += EditorGUIUtility.singleLineHeight;
        var horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;
        EditorGUI.PropertyField(horizontalRect, delay);
        horizontalRect.x += setBtnWidth + horizontalRect.width;
        horizontalRect.width = itemWidth;
        snapping.boolValue = EditorGUI.ToggleLeft(horizontalRect, "Snapping", snapping.boolValue);

        //From Value
        lastRect.y += EditorGUIUtility.singleLineHeight;
        horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;



        //ToTarget
        lastRect.y += EditorGUIUtility.singleLineHeight;
        var toRect = lastRect;
        toRect.width -= setBtnWidth + itemWidth;

        //To Value
        var dotweenTp = (DOTweenType)tweenType.enumValueIndex;
        switch (dotweenTp)
        {
            case DOTweenType.DOMoveX:
            case DOTweenType.DOMoveY:
            case DOTweenType.DOMoveZ:
            case DOTweenType.DOLocalMoveX:
            case DOTweenType.DOLocalMoveY:
            case DOTweenType.DOLocalMoveZ:
            case DOTweenType.DOAnchorPosX:
            case DOTweenType.DOAnchorPosY:
            case DOTweenType.DOAnchorPosZ:
            case DOTweenType.DOFade:
            case DOTweenType.DOCanvasGroupFade:
            case DOTweenType.DOFillAmount:
            case DOTweenType.DOValue:
            case DOTweenType.DOScaleX:
            case DOTweenType.DOScaleY:
            case DOTweenType.DOScaleZ:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    var value = fromValue.vector4Value;
                    value.x = EditorGUI.FloatField(horizontalRect, "From", value.x);
                    fromValue.vector4Value = value;
                    EditorGUI.EndDisabledGroup();

                    if (!useToTarget.boolValue)
                    {
                        value = toValue.vector4Value;
                        value.x = EditorGUI.FloatField(toRect, "To", value.x);
                        toValue.vector4Value = value;
                    }
                }
                break;
            case DOTweenType.DOAnchorPos:
            case DOTweenType.DOFlexibleSize:
            case DOTweenType.DOMinSize:
            case DOTweenType.DOPreferredSize:
            case DOTweenType.DOSizeDelta:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    fromValue.vector4Value = EditorGUI.Vector2Field(horizontalRect, "From", fromValue.vector4Value);
                    EditorGUI.EndDisabledGroup();
                    if (!useToTarget.boolValue)
                        toValue.vector4Value = EditorGUI.Vector2Field(toRect, "To", toValue.vector4Value);
                }
                break;
            case DOTweenType.DOMove:
            case DOTweenType.DOLocalMove:
            case DOTweenType.DOAnchorPos3D:
            case DOTweenType.DOScale:
            case DOTweenType.DORotate:
            case DOTweenType.DOLocalRotate:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    fromValue.vector4Value = EditorGUI.Vector3Field(horizontalRect, "From", fromValue.vector4Value);
                    EditorGUI.EndDisabledGroup();
                    if (!useToTarget.boolValue)
                        toValue.vector4Value = EditorGUI.Vector3Field(toRect, "To", toValue.vector4Value);
                }
                break;
            case DOTweenType.DOColor:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    fromValue.vector4Value = EditorGUI.ColorField(horizontalRect, "From", fromValue.vector4Value);
                    EditorGUI.EndDisabledGroup();
                    if (!useToTarget.boolValue)
                        toValue.vector4Value = EditorGUI.ColorField(toRect, "To", toValue.vector4Value);
                }
                break;
        }
        if (useToTarget.boolValue)
        {
            toTarget.objectReferenceValue = EditorGUI.ObjectField(toRect, "To", toTarget.objectReferenceValue, target.objectReferenceValue != null ? target.objectReferenceValue.GetType() : typeof(Component), true);

            if (toTarget.objectReferenceValue == null)
            {
                lastRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.HelpBox(lastRect, "To target cannot be null.", MessageType.Error);
            }
        }
        horizontalRect.x += horizontalRect.width;
        horizontalRect.width = setBtnWidth;
        if (useFromValue.boolValue && GUI.Button(horizontalRect, "Set"))
        {
            SetValueFromTarget(dotweenTp, target, fromValue);
        }
        horizontalRect.x += setBtnWidth;
        horizontalRect.width = itemWidth;
        useFromValue.boolValue = EditorGUI.ToggleLeft(horizontalRect, "Enable", useFromValue.boolValue);

        toRect.x += toRect.width;
        toRect.width = setBtnWidth;
        if (!useToTarget.boolValue && GUI.Button(toRect, "Set"))
        {
            SetValueFromTarget(dotweenTp, target, toValue);
        }
        toRect.x += setBtnWidth;
        toRect.width = itemWidth;
        useToTarget.boolValue = EditorGUI.ToggleLeft(toRect, "ToTarget", useToTarget.boolValue);

        //Duration
        lastRect.y += EditorGUIUtility.singleLineHeight;
        horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;
        EditorGUI.PropertyField(horizontalRect, duration);
        horizontalRect.x += setBtnWidth + horizontalRect.width;
        horizontalRect.width = itemWidth;
        speedBased.boolValue = EditorGUI.ToggleLeft(horizontalRect, "Use Speed", speedBased.boolValue);

        //Ease
        lastRect.y += EditorGUIUtility.singleLineHeight;
        horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;
        if (customEase.boolValue)
            EditorGUI.PropertyField(horizontalRect, easeCurve);
        else
            EditorGUI.PropertyField(horizontalRect, ease);
        horizontalRect.x += setBtnWidth + horizontalRect.width;
        horizontalRect.width = itemWidth;
        customEase.boolValue = EditorGUI.ToggleLeft(horizontalRect, "Use Curve", customEase.boolValue);

        //Loops
        lastRect.y += EditorGUIUtility.singleLineHeight;
        horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;
        EditorGUI.PropertyField(horizontalRect, loops);
        horizontalRect.x += setBtnWidth + horizontalRect.width;
        horizontalRect.width = itemWidth;
        EditorGUI.BeginDisabledGroup(loops.intValue == 1);
        loopType.enumValueIndex = (int)(LoopType)EditorGUI.EnumPopup(horizontalRect, (LoopType)loopType.enumValueIndex);
        EditorGUI.EndDisabledGroup();
        //UpdateType
        lastRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(lastRect, updateType);

        //Events
        lastRect.y += EditorGUIUtility.singleLineHeight;
        property.isExpanded = EditorGUI.Foldout(lastRect, property.isExpanded, "Animation Events");
        if (property.isExpanded)
        {
            //OnPlay
            lastRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(lastRect, onPlay);

            //OnUpdate
            lastRect.y += EditorGUI.GetPropertyHeight(onPlay);
            EditorGUI.PropertyField(lastRect, onUpdate);

            //OnComplete
            lastRect.y += EditorGUI.GetPropertyHeight(onUpdate);
            EditorGUI.PropertyField(lastRect, onComplete);
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    private void SetValueFromTarget(DOTweenType tweenType, SerializedProperty target, SerializedProperty value)
    {
        if (target.objectReferenceValue == null) return;
        var targetCom = target.objectReferenceValue;
        switch (tweenType)
        {
            case DOTweenType.DOMove:
                {
                    value.vector4Value = (targetCom as Transform).position;
                    break;
                }
            case DOTweenType.DOMoveX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).position.x;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOMoveY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).position.y;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOMoveZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).position.z;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOLocalMove:
                {
                    value.vector4Value = (targetCom as Transform).localPosition;
                    break;
                }
            case DOTweenType.DOLocalMoveX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localPosition.x;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOLocalMoveY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localPosition.y;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOLocalMoveZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localPosition.z;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOAnchorPos:
                {
                    value.vector4Value = (targetCom as RectTransform).anchoredPosition;
                    break;
                }
            case DOTweenType.DOAnchorPosX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as RectTransform).anchoredPosition.x;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOAnchorPosY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as RectTransform).anchoredPosition.y;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOAnchorPosZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as RectTransform).anchoredPosition3D.z;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOAnchorPos3D:
                {
                    value.vector4Value = (targetCom as RectTransform).anchoredPosition3D;
                    break;
                }
            case DOTweenType.DOColor:
                {
                    value.vector4Value = (targetCom as UnityEngine.UI.Graphic).color;
                    break;
                }
            case DOTweenType.DOFade:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.UI.Graphic).color.a;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOCanvasGroupFade:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.CanvasGroup).alpha;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOValue:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.UI.Slider).value;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOSizeDelta:
                {
                    value.vector4Value = (targetCom as RectTransform).sizeDelta;
                    break;
                }
            case DOTweenType.DOFillAmount:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.UI.Image).fillAmount;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOFlexibleSize:
                {
                    value.vector4Value = (targetCom as LayoutElement).GetFlexibleSize();
                    break;
                }
            case DOTweenType.DOMinSize:
                {
                    value.vector4Value = (targetCom as LayoutElement).GetMinSize();
                    break;
                }
            case DOTweenType.DOPreferredSize:
                {
                    value.vector4Value = (targetCom as LayoutElement).GetPreferredSize();
                    break;
                }
            case DOTweenType.DOScale:
                {
                    value.vector4Value = (targetCom as Transform).localScale;
                    break;
                }
            case DOTweenType.DOScaleX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localScale.x;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOScaleY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localScale.y;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOScaleZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localScale.z;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DORotate:
                {
                    value.vector4Value = (targetCom as Transform).eulerAngles;
                    break;
                }
            case DOTweenType.DOLocalRotate:
                {
                    value.vector4Value = (targetCom as Transform).localEulerAngles;
                    break;
                }
        }
    }

    private static Component GetFixedComponentType(Component com, DOTweenType tweenType)
    {
        if (com == null) return null;
        switch (tweenType)
        {
            case DOTweenType.DOMove:
            case DOTweenType.DOMoveX:
            case DOTweenType.DOMoveY:
            case DOTweenType.DOMoveZ:
            case DOTweenType.DOLocalMove:
            case DOTweenType.DOLocalMoveX:
            case DOTweenType.DOLocalMoveY:
            case DOTweenType.DOLocalMoveZ:
            case DOTweenType.DOScale:
            case DOTweenType.DOScaleX:
            case DOTweenType.DOScaleY:
            case DOTweenType.DOScaleZ:
                return com.gameObject.GetComponent<Transform>();
            case DOTweenType.DOAnchorPos:
            case DOTweenType.DOAnchorPosX:
            case DOTweenType.DOAnchorPosY:
            case DOTweenType.DOAnchorPosZ:
            case DOTweenType.DOAnchorPos3D:
            case DOTweenType.DOSizeDelta:
                return com.gameObject.GetComponent<RectTransform>();
            case DOTweenType.DOColor:
            case DOTweenType.DOFade:
                return com.gameObject.GetComponent<UnityEngine.UI.Graphic>();
            case DOTweenType.DOCanvasGroupFade:
                return com.gameObject.GetComponent<UnityEngine.CanvasGroup>();
            case DOTweenType.DOFillAmount:
                return com.gameObject.GetComponent<UnityEngine.UI.Image>();
            case DOTweenType.DOFlexibleSize:
            case DOTweenType.DOMinSize:
            case DOTweenType.DOPreferredSize:
                return com.gameObject.GetComponent<UnityEngine.UI.LayoutElement>();
            case DOTweenType.DOValue:
                return com.gameObject.GetComponent<UnityEngine.UI.Slider>();

        }
        return null;
    }
}
#endregion
#endif
public class DOTweenSequence : MonoBehaviour
{
    [HideInInspector][SerializeField] SequenceAnimation[] m_Sequence;
    [SerializeField] bool m_PlayOnAwake = false;
    [SerializeField] float m_Delay = 0;
    [SerializeField] Ease m_Ease = Ease.OutQuad;
    [SerializeField] int m_Loops = 1;
    [SerializeField] LoopType m_LoopType = LoopType.Restart;
    [SerializeField] UpdateType m_UpdateType = UpdateType.Normal;

    [SerializeField] bool m_IgnoreTimeScale = false;
    [SerializeField] UnityEvent m_OnPlay = null;
    [SerializeField] UnityEvent m_OnUpdate = null;
    [SerializeField] UnityEvent m_OnComplete = null;

    private Tween m_Tween;
    private void Awake()
    {
        InitTween();
        if (m_PlayOnAwake) DOPlay();
    }

    private void InitTween()
    {
        foreach (var item in m_Sequence)
        {
            var useFromValue = item.UseFromValue;
            if (!useFromValue) continue;
            var targetCom = item.Target;
            var resetValue = item.FromValue;
            switch (item.AnimationType)
            {
                case DOTweenType.DOMove:
                    {
                        (targetCom as Transform).position = resetValue;
                        break;
                    }
                case DOTweenType.DOMoveX:
                    {
                        (targetCom as Transform).SetPositionX(resetValue.x);
                        break;
                    }
                case DOTweenType.DOMoveY:
                    {
                        (targetCom as Transform).SetPositionY(resetValue.x);
                        break;
                    }
                case DOTweenType.DOMoveZ:
                    {
                        (targetCom as Transform).SetPositionZ(resetValue.x);
                        break;
                    }
                case DOTweenType.DOLocalMove:
                    {
                        (targetCom as Transform).localPosition = resetValue;
                        break;
                    }
                case DOTweenType.DOLocalMoveX:
                    {
                        (targetCom as Transform).SetLocalPositionX(resetValue.x);
                        break;
                    }
                case DOTweenType.DOLocalMoveY:
                    {
                        (targetCom as Transform).SetLocalPositionY(resetValue.x);
                        break;
                    }
                case DOTweenType.DOLocalMoveZ:
                    {
                        (targetCom as Transform).SetLocalPositionZ(resetValue.x);
                        break;
                    }
                case DOTweenType.DOAnchorPos:
                    {
                        (targetCom as RectTransform).anchoredPosition = resetValue;
                        break;
                    }
                case DOTweenType.DOAnchorPosX:
                    {
                        (targetCom as RectTransform).SetAnchoredPositionX(resetValue.x);
                        break;
                    }
                case DOTweenType.DOAnchorPosY:
                    {
                        (targetCom as RectTransform).SetAnchoredPositionY(resetValue.x);
                        break;
                    }
                case DOTweenType.DOAnchorPosZ:
                    {
                        (targetCom as RectTransform).SetAnchoredPosition3DZ(resetValue.x);
                        break;
                    }
                case DOTweenType.DOAnchorPos3D:
                    {
                        (targetCom as RectTransform).anchoredPosition3D = resetValue;
                        break;
                    }
                case DOTweenType.DOColor:
                    {
                        (targetCom as UnityEngine.UI.Graphic).color = resetValue;
                        break;
                    }
                case DOTweenType.DOFade:
                    {
                        (targetCom as UnityEngine.UI.Graphic).SetColorAlpha(resetValue.x);
                        break;
                    }
                case DOTweenType.DOCanvasGroupFade:
                    {
                        (targetCom as UnityEngine.CanvasGroup).alpha = resetValue.x;
                        break;
                    }
                case DOTweenType.DOValue:
                    {
                        (targetCom as UnityEngine.UI.Slider).value = resetValue.x;
                        break;
                    }
                case DOTweenType.DOSizeDelta:
                    {
                        (targetCom as RectTransform).sizeDelta = resetValue;
                        break;
                    }
                case DOTweenType.DOFillAmount:
                    {
                        (targetCom as UnityEngine.UI.Image).fillAmount = resetValue.x;
                        break;
                    }
                case DOTweenType.DOFlexibleSize:
                    {
                        (targetCom as LayoutElement).SetFlexibleSize(resetValue);
                        break;
                    }
                case DOTweenType.DOMinSize:
                    {
                        (targetCom as LayoutElement).SetMinSize(resetValue);
                        break;
                    }
                case DOTweenType.DOPreferredSize:
                    {
                        (targetCom as LayoutElement).SetPreferredSize(resetValue);
                        break;
                    }
                case DOTweenType.DOScale:
                    {
                        (targetCom as Transform).localScale = resetValue;
                        break;
                    }
                case DOTweenType.DOScaleX:
                    {
                        (targetCom as Transform).SetLocalScaleX(resetValue.x);
                        break;
                    }
                case DOTweenType.DOScaleY:
                    {
                        (targetCom as Transform).SetLocalScaleY(resetValue.x);
                        break;
                    }
                case DOTweenType.DOScaleZ:
                    {
                        (targetCom as Transform).SetLocalScaleZ(resetValue.z);
                        break;
                    }
                case DOTweenType.DORotate:
                    {
                        (targetCom as Transform).eulerAngles = resetValue;
                        break;
                    }
                case DOTweenType.DOLocalRotate:
                    {
                        (targetCom as Transform).localEulerAngles = resetValue;
                        break;
                    }
            }
        }
    }
    private Tween CreateTween(bool reverse = false)
    {
        if (m_Sequence == null || m_Sequence.Length == 0)
        {
            return null;
        }
        var sequence = DOTween.Sequence();
        if (reverse)
        {
            for (int i = m_Sequence.Length - 1; i >= 0; i--)
            {
                var item = m_Sequence[i];
                var tweener = item.CreateTween(reverse);
                if (tweener == null)
                {
                    Debug.LogErrorFormat("Tweener is null. Index:{0}, Animation Type:{1}, Component Type:{2}", i, item.AnimationType, item.Target == null ? "null" : item.Target.GetType().Name);
                    continue;
                }
                switch (item.AddType)
                {
                    case AddType.Append:
                        sequence.Append(tweener);
                        break;
                    case AddType.Join:
                        sequence.Join(tweener);
                        break;
                }
            }
        }
        else
        {
            for (int i = 0; i < m_Sequence.Length; i++)
            {
                var item = m_Sequence[i];
                var tweener = item.CreateTween(reverse);
                if (tweener == null)
                {
                    Debug.LogErrorFormat("Tweener is null. Index:{0}, Animation Type:{1}, Component Type:{2}", i, item.AnimationType, item.Target == null ? "null" : item.Target.GetType().Name);
                    continue;
                }
                switch (item.AddType)
                {
                    case AddType.Append:
                        sequence.Append(tweener);
                        break;
                    case AddType.Join:
                        sequence.Join(tweener);
                        break;
                }
            }
        }
        sequence.SetEase(m_Ease).SetUpdate(m_UpdateType, m_IgnoreTimeScale).SetLoops(m_Loops, m_LoopType).SetDelay(m_Delay);
        if (m_OnPlay != null) sequence.OnPlay(m_OnPlay.Invoke);
        if (m_OnUpdate != null) sequence.OnUpdate(m_OnUpdate.Invoke);
        if (m_OnComplete != null) sequence.OnComplete(m_OnComplete.Invoke);
        sequence.SetAutoKill(true);
        return sequence;
    }
    public void Play()
    {
        DOPlay();
    }
    public Tween DOPlay()
    {
        m_Tween = CreateTween();
        return m_Tween?.Play();
    }

    public Tween DORewind()
    {
        m_Tween = CreateTween(true);
        return m_Tween?.Play();
    }

    public void DOComplete(bool withCallback = false)
    {
        m_Tween?.Complete(withCallback);
    }

    public void DOKill()
    {
        m_Tween?.Kill();
        m_Tween = null;
    }

    public enum DOTweenType
    {
        DOMove,
        DOMoveX,
        DOMoveY,
        DOMoveZ,

        DOLocalMove,
        DOLocalMoveX,
        DOLocalMoveY,
        DOLocalMoveZ,

        DOScale,
        DOScaleX,
        DOScaleY,
        DOScaleZ,

        DORotate,
        DOLocalRotate,

        DOAnchorPos,
        DOAnchorPosX,
        DOAnchorPosY,
        DOAnchorPosZ,
        DOAnchorPos3D,


        DOColor,
        DOFade,
        DOCanvasGroupFade,
        DOFillAmount,
        DOFlexibleSize,
        DOMinSize,
        DOPreferredSize,
        DOSizeDelta,
        DOValue
    }

    [Serializable]
    public class SequenceAnimation
    {
        public AddType AddType = AddType.Append;
        public DOTweenType AnimationType = DOTweenType.DOMove;
        public Component Target = null;
        public Vector4 ToValue = Vector4.zero;

        public bool UseToTarget = false;
        public Component ToTarget = null;

        public bool UseFromValue = false;
        public Vector4 FromValue = Vector4.zero;
        public bool SpeedBased = false;
        public float DurationOrSpeed = 1;
        public float Delay = 0;
        public UpdateType UpdateType = UpdateType.Normal;
        public bool CustomEase = false;
        public AnimationCurve EaseCurve;
        public Ease Ease = Ease.OutQuad;
        public int Loops = 1;
        public LoopType LoopType = LoopType.Restart;
        public bool Snapping = false;
        public UnityEvent OnPlay = null;
        public UnityEvent OnUpdate = null;
        public UnityEvent OnComplete = null;
        public Tween CreateTween(bool reverse)
        {
            Tween result = null;
            float duration = this.DurationOrSpeed;

            switch (AnimationType)
            {
                case DOTweenType.DOMove:
                    {
                        var transform = Target as Transform;
                        Vector3 targetValue = UseToTarget ? (ToTarget as Transform).position : ToValue;
                        Vector3 startValue = UseFromValue ? FromValue : transform.position;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.position = startValue;
                        if (SpeedBased)
                            duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = transform.DOMove(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOMoveX:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).position.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.position.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.SetPositionX(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOMoveX(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOMoveY:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).position.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.position.y;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.SetPositionY(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOMoveY(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOMoveZ:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).position.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.position.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.SetPositionZ(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOMoveZ(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOLocalMove:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : transform.localPosition;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.localPosition = startValue;
                        if (SpeedBased)
                            duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMove(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOLocalMoveX:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.localPosition.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.SetLocalPositionX(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMoveX(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOLocalMoveY:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.localPosition.y;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.SetLocalPositionY(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMoveY(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOLocalMoveZ:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.localPosition.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.SetLocalPositionZ(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMoveZ(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOScale:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localScale : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : com.localScale;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.localScale = startValue;
                        if (SpeedBased) duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOScale(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOScaleX:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localScale.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.localScale.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetLocalScaleX(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOScaleX(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOScaleY:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localScale.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.localScale.y;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetLocalScaleY(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOScaleY(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOScaleZ:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localScale.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.localScale.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetLocalScaleZ(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOScaleZ(targetValue, duration);
                    }
                    break;
                case DOTweenType.DORotate:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).eulerAngles : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : com.eulerAngles;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.eulerAngles = startValue;
                        if (SpeedBased)
                            duration = GetEulerAnglesAngle(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DORotate(targetValue, duration, RotateMode.FastBeyond360);
                    }
                    break;
                case DOTweenType.DOLocalRotate:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localEulerAngles : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : com.localEulerAngles;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.localEulerAngles = startValue;
                        if (SpeedBased)
                            duration = GetEulerAnglesAngle(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOLocalRotate(targetValue, duration, RotateMode.FastBeyond360);
                    }
                    break;
                case DOTweenType.DOAnchorPos:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : rectTransform.anchoredPosition;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        rectTransform.anchoredPosition = startValue;
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPos(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOAnchorPosX:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : rectTransform.anchoredPosition.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        rectTransform.SetAnchoredPositionX(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPosX(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOAnchorPosY:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : rectTransform.anchoredPosition.y;
                        if (reverse)
                        {
                            var swapValue = startValue;
                            startValue = targetValue;
                            targetValue = swapValue;
                        }
                        rectTransform.SetAnchoredPositionY(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPosY(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOAnchorPosZ:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition3D.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : rectTransform.anchoredPosition3D.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        rectTransform.SetAnchoredPosition3DZ(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPos3DZ(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOAnchorPos3D:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition3D : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : rectTransform.anchoredPosition3D;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        rectTransform.anchoredPosition3D = startValue;
                        if (SpeedBased)
                            duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPos3D(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOSizeDelta:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).sizeDelta : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : rectTransform.sizeDelta;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        rectTransform.sizeDelta = startValue;
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOSizeDelta(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOColor:
                    {
                        var com = Target as UnityEngine.UI.Graphic;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.UI.Graphic).color : (Color)ToValue;
                        var startValue = UseFromValue ? (Color)FromValue : com.color;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.color = startValue;
                        if (SpeedBased)
                            duration = Vector4.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOColor(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOFade:
                    {
                        var com = Target as UnityEngine.UI.Graphic;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.UI.Graphic).color.a : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.color.a;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetColorAlpha(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOFade(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOCanvasGroupFade:
                    {
                        var com = Target as UnityEngine.CanvasGroup;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.CanvasGroup).alpha : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.alpha;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.alpha = startValue;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOFade(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOValue:
                    {
                        var com = Target as UnityEngine.UI.Slider;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.UI.Slider).value : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.value;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.value = startValue;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOValue(targetValue, duration, Snapping);
                    }
                    break;

                case DOTweenType.DOFillAmount:
                    {
                        var com = Target as UnityEngine.UI.Image;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.UI.Image).fillAmount : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.fillAmount;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.fillAmount = startValue;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOFillAmount(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOFlexibleSize:
                    {
                        var com = Target as LayoutElement;
                        var targetValue = UseToTarget ? (ToTarget as LayoutElement).GetFlexibleSize() : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : com.GetFlexibleSize();
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetFlexibleSize(startValue);
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOFlexibleSize(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOMinSize:
                    {
                        var com = Target as LayoutElement;
                        var targetValue = UseToTarget ? (ToTarget as LayoutElement).GetMinSize() : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : com.GetMinSize();
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetMinSize(startValue);
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOMinSize(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOPreferredSize:
                    {
                        var com = Target as LayoutElement;
                        var targetValue = UseToTarget ? (ToTarget as LayoutElement).GetPreferredSize() : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : com.GetPreferredSize();
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetPreferredSize(startValue);
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOPreferredSize(targetValue, duration, Snapping);
                    }
                    break;
            }

            if (result != null)
            {
                result.SetAutoKill(true).SetTarget(Target.gameObject).SetLoops(Loops, LoopType).SetUpdate(UpdateType);
                if (Delay > 0) result.SetDelay(Delay);
                if (CustomEase) result.SetEase(EaseCurve);
                else result.SetEase(Ease);
                
                if (OnPlay != null) result.OnPlay(OnPlay.Invoke);
                if (OnUpdate != null) result.OnUpdate(OnUpdate.Invoke);
                if (OnComplete != null) result.OnComplete(OnComplete.Invoke);
            }
            return result;
        }
        public static float GetEulerAnglesAngle(Vector3 euler1, Vector3 euler2)
        {
            // 计算差值
            Vector3 delta = euler2 - euler1;
            delta.x = Mathf.DeltaAngle(euler1.x, euler2.x);
            delta.y = Mathf.DeltaAngle(euler1.y, euler2.y);
            delta.z = Mathf.DeltaAngle(euler1.z, euler2.z);

            float angle = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
            return (angle + 360) % 360;
        }
    }
    public enum AddType
    {
        Append,
        Join
    }
}
