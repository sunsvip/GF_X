//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UGF.EditorTools;
using System.Linq;
namespace GameFramework.Editor.DataTableTools
{
    public sealed class DataTableGenerator
    {
        private static readonly Regex EndWithNumberRegex = new Regex(@"\d+$");
        private static readonly Regex NameRegex = new Regex(@"^[A-Z][A-Za-z0-9_]*$");

        public static DataTableProcessor CreateDataTableProcessor(string dataTableFile)
        {
            return new DataTableProcessor(dataTableFile, Encoding.Unicode, 1, 2, null, 3, 4, 1);//Encoding.GetEncoding("GB2312")
        }
        public static bool CheckRawData(DataTableProcessor dataTableProcessor, string dataTableFile)
        {
            for (int i = 0; i < dataTableProcessor.RawColumnCount; i++)
            {
                string name = dataTableProcessor.GetName(i);
                if (string.IsNullOrEmpty(name) || name == "#")
                {
                    continue;
                }

                if (!NameRegex.IsMatch(name))
                {
                    Debug.LogWarning($"数据表'{dataTableFile}'中字段名'{name}'不合法. 字段名必须以大写字母开头、只包含字母下划线字符");
                    return false;
                }
            }

            return true;
        }

        public static void GenerateDataFile(DataTableProcessor dataTableProcessor, string dataTableFile)
        {
            string binaryDataFileName = Path.ChangeExtension(dataTableFile, ".bytes");
            if (!dataTableProcessor.GenerateDataFile(binaryDataFileName) && File.Exists(binaryDataFileName))
            {
                File.Delete(binaryDataFileName);
            }
        }
        public static void GenerateCodeFile(DataTableProcessor dataTableProcessor, string dataTableFile)
        {
            dataTableProcessor.SetCodeTemplate(ConstEditor.DataTableCodeTemplate, Encoding.UTF8);
            dataTableProcessor.SetCodeGenerator(DataTableCodeGenerator);
            var dataTableName = GameDataGenerator.GetGameDataRelativeName(dataTableFile, ConstEditor.DataTablePath);
            string csharpCodeFileName = Utility.Path.GetRegularPath(Path.Combine(ConstEditor.DataTableCodePath, dataTableName + ".cs"));
            if (!dataTableProcessor.GenerateCodeFile(csharpCodeFileName, Encoding.UTF8, dataTableFile))
            {
                GFBuiltin.LogError(Utility.Text.Format("生成{0}数据表结构代码失败:{1}", dataTableName, csharpCodeFileName));
            }
        }

        private static void DataTableCodeGenerator(DataTableProcessor dataTableProcessor, StringBuilder codeContent, object userData)
        {
            string dataTableClassName = Path.GetFileNameWithoutExtension((string)userData);

            // codeContent.Replace("__DATA_TABLE_CREATE_TIME__", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            //codeContent.Replace("__DATA_TABLE_NAME_SPACE__", "StarForce");
            codeContent.Replace("__DATA_TABLE_CLASS_NAME__", dataTableClassName);
            codeContent.Replace("__DATA_TABLE_COMMENT__", dataTableProcessor.GetValue(0, 1));
            codeContent.Replace("__DATA_TABLE_ID_COMMENT__", dataTableProcessor.GetComment(dataTableProcessor.IdColumn));
            codeContent.Replace("__DATA_TABLE_PROPERTIES__", GenerateDataTableProperties(dataTableProcessor));
            codeContent.Replace("__DATA_TABLE_PARSER__", GenerateDataTableParser(dataTableProcessor));
            //codeContent.Replace("__DATA_TABLE_PROPERTY_ARRAY__", GenerateDataTablePropertyArray(dataTableProcessor));
        }

        private static string GenerateDataTableProperties(DataTableProcessor dataTableProcessor)
        {
            StringBuilder stringBuilder = new StringBuilder();
            bool firstProperty = true;
            for (int i = 0; i < dataTableProcessor.RawColumnCount; i++)
            {
                if (dataTableProcessor.IsCommentColumn(i))
                {
                    // 注释列
                    continue;
                }

                if (dataTableProcessor.IsIdColumn(i))
                {
                    // 编号列
                    continue;
                }

                if (firstProperty)
                {
                    firstProperty = false;
                }
                else
                {
                    stringBuilder.AppendLine().AppendLine();
                }
                string dataTypeKeyword = dataTableProcessor.GetLanguageKeyword(i);
                string dataComment = dataTableProcessor.GetComment(i);
                if (dataTypeKeyword == "enum")
                {
                    var firstEnumValue = dataTableProcessor.GetValue(4, i);
                    if (!DataTableExtension.TryParseEnum(firstEnumValue, out Type enumType))
                    {
                        GFBuiltin.LogError(Utility.Text.Format("解析枚举类型失败:{0}, 配置枚举格式为: EnumType.Item1", firstEnumValue));
                        continue;
                    }

                    stringBuilder
                    .AppendLine("        /// <summary>")
                    .AppendFormat("        /// {0}", dataTableProcessor.GetComment(i)).AppendLine()
                    .AppendLine("        /// </summary>")
                    .AppendFormat("        public {0} {1}", enumType.FullName.Replace('+', '.'), dataTableProcessor.GetName(i)).AppendLine()
                    .AppendLine("        {")
                    .AppendLine("            get;")
                    .AppendLine("            private set;")
                    .Append("        }");
                }
                else
                {
                    stringBuilder
                    .AppendLine("        /// <summary>")
                    .AppendFormat("        /// {0}", dataTableProcessor.GetComment(i)).AppendLine()
                    .AppendLine("        /// </summary>")
                    .AppendFormat("        public {0} {1}", dataTypeKeyword, dataTableProcessor.GetName(i)).AppendLine()
                    .AppendLine("        {")
                    .AppendLine("            get;")
                    .AppendLine("            private set;")
                    .Append("        }");
                }
            }

            return stringBuilder.ToString();
        }

        private static string GenerateDataTableParser(DataTableProcessor dataTableProcessor)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder
                .AppendLine("        public override bool ParseDataRow(string dataRowString, object userData)")
                .AppendLine("        {")
                .AppendLine("            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);")
                .AppendLine("            for (int i = 0; i < columnStrings.Length; i++)")
                .AppendLine("            {")
                .AppendLine("                columnStrings[i] = columnStrings[i].Trim(DataTableExtension.DataTrimSeparators);")
                .AppendLine("            }")
                .AppendLine()
                .AppendLine("            int index = 0;");

            for (int i = 0; i < dataTableProcessor.RawColumnCount; i++)
            {
                if (dataTableProcessor.IsCommentColumn(i))
                {
                    // 注释列
                    stringBuilder.AppendLine("            index++;");
                    continue;
                }

                if (dataTableProcessor.IsIdColumn(i))
                {
                    // 编号列
                    stringBuilder.AppendLine("            m_Id = int.Parse(columnStrings[index++]);");
                    continue;
                }

                string languageKeyword = dataTableProcessor.GetLanguageKeyword(i);

                int isArrayType = ParseArrayType(languageKeyword);

                if (dataTableProcessor.IsSystem(i))
                {
                    if (isArrayType > 0)
                    {
                        if (isArrayType == 1)
                        {
                            stringBuilder.AppendFormat("            {0} = DataTableExtension.ParseArray<{1}>(columnStrings[index++]);", dataTableProcessor.GetName(i), languageKeyword.Replace("[]", string.Empty)).AppendLine();
                        }
                        else if (isArrayType == 2)
                        {
                            stringBuilder.AppendFormat("            {0} = DataTableExtension.Parse2DArray<{1}>(columnStrings[index++]);", dataTableProcessor.GetName(i), languageKeyword.Replace("[][]", string.Empty)).AppendLine();
                        }
                    }
                    else
                    {
                        if (languageKeyword == "string")
                        {
                            stringBuilder.AppendFormat("            {0} = columnStrings[index++];", dataTableProcessor.GetName(i)).AppendLine();
                        }
                        else if (languageKeyword == "enum")
                        {
                            var firstEnumValue = dataTableProcessor.GetValue(4, i);
                            if (!DataTableExtension.TryParseEnum(firstEnumValue, out Type enumType))
                            {
                                GFBuiltin.LogError(Utility.Text.Format("解析枚举类型失败:{0}, 配置枚举格式为: EnumType.Item1", firstEnumValue));
                                continue;
                            }

                            stringBuilder.AppendFormat("            {0} = DataTableExtension.ParseEnum<{1}>(columnStrings[index++]);", dataTableProcessor.GetName(i), enumType.FullName.Replace('+', '.')).AppendLine();
                        }
                        else
                        {
                            stringBuilder.AppendFormat("            {0} = {1}.Parse(columnStrings[index++]);", dataTableProcessor.GetName(i), languageKeyword).AppendLine();
                        }
                    }
                }
                else
                {
                    if (isArrayType > 0)
                    {
                        if (isArrayType == 1)
                        {
                            stringBuilder.AppendFormat("            {0} = DataTableExtension.Parse{1}Array(columnStrings[index++]);", dataTableProcessor.GetName(i), languageKeyword.Replace("[]", string.Empty)).AppendLine();
                        }
                        else if (isArrayType == 2)
                        {
                            stringBuilder.AppendFormat("            {0} = DataTableExtension.Parse{1}2DArray(columnStrings[index++]);", dataTableProcessor.GetName(i), languageKeyword.Replace("[][]", string.Empty)).AppendLine();
                        }
                    }
                    else
                    {
                        stringBuilder.AppendFormat("            {0} = DataTableExtension.Parse{1}(columnStrings[index++]);", dataTableProcessor.GetName(i), dataTableProcessor.GetType(i).Name).AppendLine();
                    }
                }
            }

            stringBuilder.AppendLine()
                .AppendLine("            return true;")
                .AppendLine("        }")
                .AppendLine()
                .AppendLine("        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)")
                .AppendLine("        {")
                .AppendLine("            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))")
                .AppendLine("            {")
                .AppendLine("                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))")
                .AppendLine("                {");

            for (int i = 0; i < dataTableProcessor.RawColumnCount; i++)
            {
                if (dataTableProcessor.IsCommentColumn(i))
                {
                    // 注释列
                    continue;
                }

                if (dataTableProcessor.IsIdColumn(i))
                {
                    // 编号列
                    stringBuilder.AppendLine("                    m_Id = binaryReader.Read7BitEncodedInt32();");
                    continue;
                }

                string languageKeyword = dataTableProcessor.GetLanguageKeyword(i);
                int isArrayType = ParseArrayType(languageKeyword);

                if (dataTableProcessor.IsSystem(i))
                {
                    if (isArrayType > 0)
                    {
                        if (isArrayType == 1)
                        {
                            stringBuilder.AppendFormat("                    {0} = binaryReader.ReadArray<{1}>();", dataTableProcessor.GetName(i), languageKeyword.Replace("[]", string.Empty)).AppendLine();
                        }
                        else if (isArrayType == 2)
                        {
                            stringBuilder.AppendFormat("                    {0} = binaryReader.Read2DArray<{1}>();", dataTableProcessor.GetName(i), languageKeyword.Replace("[][]", string.Empty)).AppendLine();
                        }
                    }
                    else
                    {
                        if (languageKeyword == "int" || languageKeyword == "uint" || languageKeyword == "long" || languageKeyword == "ulong")
                        {
                            stringBuilder.AppendFormat("                    {0} = binaryReader.Read7BitEncoded{1}();", dataTableProcessor.GetName(i), dataTableProcessor.GetType(i).Name).AppendLine();
                        }
                        else if (languageKeyword == "enum")
                        {
                            var firstEnumValue = dataTableProcessor.GetValue(4, i);
                            if (!DataTableExtension.TryParseEnum(firstEnumValue, out Type enumType))
                            {
                                GFBuiltin.LogError(Utility.Text.Format("解析枚举类型失败:{0}, 配置枚举格式为: EnumType.Item1", firstEnumValue));
                                continue;
                            }
                            stringBuilder.AppendFormat("                    {0} = binaryReader.ReadEnum<{1}>();", dataTableProcessor.GetName(i), enumType.FullName.Replace('+', '.')).AppendLine();
                        }
                        else
                        {
                            stringBuilder.AppendFormat("                    {0} = binaryReader.Read{1}();", dataTableProcessor.GetName(i), dataTableProcessor.GetType(i).Name).AppendLine();
                        }
                    }
                }
                else
                {
                    if (isArrayType > 0)
                    {
                        if (isArrayType == 1)
                        {
                            stringBuilder.AppendFormat("                    {0} = binaryReader.Read{1}Array();", dataTableProcessor.GetName(i), languageKeyword.Replace("[]", string.Empty)).AppendLine();
                        }
                        else if (isArrayType == 2)
                        {
                            stringBuilder.AppendFormat("                    {0} = binaryReader.Read{1}2DArray();", dataTableProcessor.GetName(i), languageKeyword.Replace("[][]", string.Empty)).AppendLine();
                        }
                    }
                    else
                    {
                        stringBuilder.AppendFormat("                    {0} = binaryReader.Read{1}();", dataTableProcessor.GetName(i), dataTableProcessor.GetType(i).Name).AppendLine();
                    }
                }
            }

            stringBuilder
                .AppendLine("                }")
                .AppendLine("            }")
                .AppendLine()
                .AppendLine("            return true;")
                .Append("        }");

            return stringBuilder.ToString();
        }
        /// <summary>
        /// 0:非数组; 1:一维数组; 2:二维数组
        /// </summary>
        /// <param name="languageKeyword"></param>
        /// <returns></returns>
        private static int ParseArrayType(string languageKeyword)
        {
            if (languageKeyword.EndsWith("[][]"))
            {
                return 2;
            }
            if (languageKeyword.EndsWith("[]"))
            {
                return 1;
            }

            return 0;
        }
        
    }
}
