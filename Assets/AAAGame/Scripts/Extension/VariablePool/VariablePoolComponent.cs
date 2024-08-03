using GameFramework;
using System.Collections.Generic;
using UnityGameFramework.Runtime;
using UnityEngine;
using UnityEngine.Pool;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(VariablePoolComponent))]
public class VariablePoolComponentInspector : UnityEditor.Editor
{
    VariablePoolComponent m_Target;
    int m_UnfoldId;
    int m_TotalVariableCount;
    bool m_Debug;
    private void OnEnable()
    {
        m_Target = target as VariablePoolComponent;
        m_UnfoldId = -1;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (null != m_Target.Variables)
        {
            m_Debug = EditorGUILayout.Toggle("Enable Debug", m_Debug);
            EditorGUILayout.LabelField($"Variables Count:{m_TotalVariableCount}");
            m_TotalVariableCount = 0;
            foreach (var item in m_Target.Variables)
            {
                m_TotalVariableCount += item.Value.Count;
                if (m_Debug)
                {
                    bool unfold = item.Key == m_UnfoldId;
                    if (GUILayout.Button(unfold ? $"▼ ID:{item.Key}" : $"▶ ID:{item.Key}", EditorStyles.label))
                    {
                        m_UnfoldId = unfold ? -1 : item.Key; ;
                    }
                    if (unfold)
                    {
                        EditorGUILayout.BeginVertical("box");
                        {
                            foreach (var element in item.Value)
                            {
                                EditorGUILayout.LabelField($"{element.Key} : {element.Value}");
                            }
                            EditorGUILayout.EndVertical();
                        }
                    }
                }
                Repaint();
            }
        }
    }
}
#endif

/// <summary>
/// 用于通过引用池管理变量, 避免频繁new对象, 用于Entity和UI参数传递
/// </summary>
public class VariablePoolComponent : GameFrameworkComponent
{
    private Dictionary<int, Dictionary<string, Variable>> m_Variables;
    private DictionaryPool<string, Variable> m_ValuesPool;
#if UNITY_EDITOR
    public Dictionary<int, Dictionary<string, Variable>> Variables => m_Variables;
#endif
    protected override void Awake()
    {
        base.Awake();
        m_Variables = new Dictionary<int, Dictionary<string, Variable>>(1024);
    }


    private void OnDestroy()
    {
        var keys = m_Variables.Keys.ToArray();
        foreach (var key in keys)
        {
            ClearVariables(key);
        }
        m_Variables.Clear();
    }

    public bool TryGetVariable<T>(int rootId, string key, out T value) where T : Variable
    {
        value = null;
        if (m_Variables.TryGetValue(rootId, out var values) && values.TryGetValue(key, out Variable v))
        {
            value = v as T;
            return true;
        }
        return false;
    }

    public T GetVariable<T>(int rootId, string key) where T : Variable
    {
        if (m_Variables.TryGetValue(rootId, out var values) && values.TryGetValue(key, out var value))
        {
            return value as T;
        }
        return null;
    }

    public void SetVariable<T>(int rootId, string key, T value) where T : Variable
    {
        if (m_Variables.TryGetValue(rootId, out var values))
        {
            values[key] = value;
        }
        else
        {
            values = DictionaryPool<string, Variable>.Get();
            values[key] = value;
            m_Variables.Add(rootId, values);
        }
    }

    public bool HasVariable(int rootId, string key)
    {
        return m_Variables.TryGetValue(rootId, out var values) && values.ContainsKey(key);
    }

    public void ClearVariables(int rootId)
    {
        if (m_Variables.TryGetValue(rootId, out var values))
        {
            foreach (var item in values)
            {
                ReferencePool.Release(item.Value);
            }
            DictionaryPool<string, Variable>.Release(values);
            m_Variables.Remove(rootId);
        }
    }
}
