//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GameFramework.Editor.DataTableTools
{
    public sealed partial class DataTableProcessor
    {
        private sealed class CustomJsonProcessor : DataProcessor
        {
            private readonly Type m_Type;
            private readonly string m_LanguageKeyword;

            public CustomJsonProcessor(Type type)
            {
                m_Type = type ?? throw new ArgumentNullException(nameof(type));
                m_LanguageKeyword = GetCodeTypeName(type);
            }

            public override Type Type
            {
                get
                {
                    return m_Type;
                }
            }

            public override bool IsId
            {
                get
                {
                    return false;
                }
            }

            public override bool IsComment
            {
                get
                {
                    return false;
                }
            }

            public override bool IsSystem
            {
                get
                {
                    return false;
                }
            }

            public override string LanguageKeyword
            {
                get
                {
                    return m_LanguageKeyword;
                }
            }

            public override bool IsCustomJson
            {
                get
                {
                    return true;
                }
            }

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    m_LanguageKeyword
                };
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                binaryWriter.Write(NormalizeCustomJsonValue(m_Type, value));
            }
        }

        public bool IsCustomJson(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return m_DataProcessor[rawColumn].IsCustomJson;
        }

        public static bool IsCustomJsonType(string type)
        {
            return TryResolveCustomJsonType(type, out _);
        }

        internal static bool TryResolveCustomJsonType(string type, out Type dataType)
        {
            dataType = null;
            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            if (DataProcessorUtility.TryGetDataProcessor(type, out _))
            {
                return false;
            }

            if (TryResolveCustomJsonArrayType(type, out dataType))
            {
                return true;
            }

            Type resolvedType = Utility.Assembly.GetType(type);
            if (IsSupportedCustomJsonType(resolvedType))
            {
                dataType = resolvedType;
                return true;
            }

            Type[] matchedTypes = Utility.Assembly.GetTypes()
                .Where(t => IsSupportedCustomJsonType(t) && (string.Equals(t.Name, type, StringComparison.Ordinal) || string.Equals(GetCodeTypeName(t), type, StringComparison.Ordinal)))
                .Distinct()
                .ToArray();

            if (matchedTypes.Length == 1)
            {
                dataType = matchedTypes[0];
                return true;
            }

            return false;
        }

        internal static string GetCodeTypeName(Type type)
        {
            if (type == null)
            {
                throw new GameFrameworkException("Custom JSON type is invalid.");
            }

            return (type.FullName ?? type.Name).Replace('+', '.');
        }

        internal static string NormalizeCustomJsonValue(string type, string value)
        {
            if (!TryResolveCustomJsonType(type, out Type dataType))
            {
                return value;
            }

            return NormalizeCustomJsonValue(dataType, value);
        }

        internal static string NormalizeCustomJsonValue(Type dataType, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string normalizedValue = NormalizeCustomJsonRoot(dataType, value);
            try
            {
                JsonConvert.DeserializeObject(normalizedValue, dataType);
                return normalizedValue;
            }
            catch (Exception exception)
            {
                throw new GameFrameworkException(Utility.Text.Format("Normalize custom JSON value failure. Type='{0}' Value='{1}' Error='{2}'", GetCodeTypeName(dataType), value, exception.Message), exception);
            }
        }

        private static bool IsSupportedCustomJsonType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.IsAbstract || type.IsByRef || type.IsPointer || type.IsArray || type.IsEnum || type.IsPrimitive || type.IsGenericType || type.IsGenericTypeDefinition)
            {
                return false;
            }

            return type != typeof(string);
        }

        private static bool TryResolveCustomJsonArrayType(string type, out Type dataType)
        {
            dataType = null;
            if (!type.EndsWith("[]", StringComparison.Ordinal))
            {
                return false;
            }

            string elementTypeName = type[..^2];
            if (!TryResolveCustomJsonType(elementTypeName, out Type elementType))
            {
                return false;
            }

            dataType = elementType.MakeArrayType();
            return true;
        }

        private static string NormalizeCustomJsonRoot(Type dataType, string value)
        {
            string trimmedValue = value.Trim();
            if (dataType.IsArray)
            {
                if (trimmedValue.StartsWith("[", StringComparison.Ordinal))
                {
                    return QuoteUnquotedObjectKeys(trimmedValue);
                }

                return "[" + NormalizeRelaxedJsonObject(trimmedValue) + "]";
            }

            return NormalizeRelaxedJsonObject(trimmedValue);
        }

        private static string NormalizeRelaxedJsonObject(string value)
        {
            string trimmedValue = value.Trim();
            if (string.IsNullOrEmpty(trimmedValue))
            {
                return string.Empty;
            }

            if (!trimmedValue.StartsWith("{", StringComparison.Ordinal))
            {
                trimmedValue = "{" + trimmedValue + "}";
            }

            return QuoteUnquotedObjectKeys(trimmedValue);
        }

        private static string QuoteUnquotedObjectKeys(string json)
        {
            StringBuilder stringBuilder = new StringBuilder(json.Length + 16);
            Stack<JsonContainerState> states = new Stack<JsonContainerState>();
            bool inString = false;
            bool escaped = false;

            for (int i = 0; i < json.Length; i++)
            {
                char current = json[i];
                if (inString)
                {
                    stringBuilder.Append(current);
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (current == '\\')
                    {
                        escaped = true;
                    }
                    else if (current == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                    stringBuilder.Append(current);
                    continue;
                }

                switch (current)
                {
                    case '{':
                        states.Push(new JsonContainerState(isObject: true, expectingKey: true));
                        stringBuilder.Append(current);
                        continue;

                    case '[':
                        states.Push(new JsonContainerState(isObject: false, expectingKey: false));
                        stringBuilder.Append(current);
                        continue;

                    case '}':
                    case ']':
                        if (states.Count > 0)
                        {
                            states.Pop();
                        }

                        stringBuilder.Append(current);
                        continue;

                    case ':':
                        SetCurrentObjectExpectingKey(states, false);
                        stringBuilder.Append(current);
                        continue;

                    case ',':
                        SetCurrentObjectExpectingKey(states, true);
                        stringBuilder.Append(current);
                        continue;
                }

                if (IsCurrentObjectExpectingKey(states))
                {
                    if (char.IsWhiteSpace(current))
                    {
                        stringBuilder.Append(current);
                        continue;
                    }

                    int colonIndex = FindNextColon(json, i);
                    if (colonIndex <= i)
                    {
                        stringBuilder.Append(current);
                        continue;
                    }

                    int tokenEnd = colonIndex - 1;
                    while (tokenEnd >= i && char.IsWhiteSpace(json[tokenEnd]))
                    {
                        tokenEnd--;
                    }

                    string token = json.Substring(i, tokenEnd - i + 1).Trim();
                    stringBuilder.Append(JsonConvert.ToString(token));
                    for (int j = tokenEnd + 1; j < colonIndex; j++)
                    {
                        if (char.IsWhiteSpace(json[j]))
                        {
                            stringBuilder.Append(json[j]);
                        }
                    }

                    i = colonIndex - 1;
                    continue;
                }

                stringBuilder.Append(current);
            }

            return stringBuilder.ToString();
        }

        private static bool IsCurrentObjectExpectingKey(Stack<JsonContainerState> states)
        {
            return states.Count > 0 && states.Peek().IsObject && states.Peek().ExpectingKey;
        }

        private static void SetCurrentObjectExpectingKey(Stack<JsonContainerState> states, bool expectingKey)
        {
            if (states.Count <= 0)
            {
                return;
            }

            JsonContainerState state = states.Pop();
            if (state.IsObject)
            {
                state.ExpectingKey = expectingKey;
            }
            states.Push(state);
        }

        private static int FindNextColon(string json, int startIndex)
        {
            for (int i = startIndex; i < json.Length; i++)
            {
                if (json[i] == ':')
                {
                    return i;
                }
            }

            return -1;
        }

        private struct JsonContainerState
        {
            public JsonContainerState(bool isObject, bool expectingKey)
            {
                IsObject = isObject;
                ExpectingKey = expectingKey;
            }

            public bool IsObject;
            public bool ExpectingKey;
        }
    }
}
