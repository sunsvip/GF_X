//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UGF.EditorTools.Data.DataTable
{
    public delegate void DataTableCodeGenerator(DataTableProcessor dataTableProcessor, StringBuilder codeContent, object userData);

    public sealed partial class DataTableProcessor
    {
        public const string CommentLineSeparator = "#";
        public static readonly char[] DataSplitSeparators = new char[] { '\t' };
        private static readonly char[] DataTrimSeparators = new char[] { '\"' };

        private readonly string[] _nameRow;
        private readonly string[] _typeRow;
        private readonly string[] _defaultValueRow;
        private readonly string[] _commentRow;
        private readonly int _contentStartRow;
        private readonly int _idColumn;

        private readonly DataProcessor[] _dataProcessors;
        private readonly string[][] _rawValues;
        private readonly string[] _strings;

        private string _codeTemplate;
        private DataTableCodeGenerator _codeGenerator;

        public DataTableProcessor(string dataTableFileName, Encoding encoding, int nameRow, int typeRow, int? defaultValueRow, int? commentRow, int contentStartRow, int idColumn)
            : this(dataTableFileName, ReadDataTableLines(dataTableFileName, encoding), nameRow, typeRow, defaultValueRow, commentRow, contentStartRow, idColumn)
        {
        }

        public DataTableProcessor(string dataTableName, string[] lines, int nameRow, int typeRow, int? defaultValueRow, int? commentRow, int contentStartRow, int idColumn)
        {
            if (string.IsNullOrEmpty(dataTableName))
            {
                throw new GameFrameworkException("Data table file name is invalid.");
            }

            if (lines == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Data table '{0}' lines is invalid.", dataTableName));
            }

            int rawRowCount = lines.Length;

            int rawColumnCount = 0;
            List<string[]> rawValues = new List<string[]>();
            for (int i = 0; i < lines.Length; i++)
            {
                string[] rawValue = lines[i].Split(DataSplitSeparators);
                for (int j = 0; j < rawValue.Length; j++)
                {
                    rawValue[j] = rawValue[j].Trim(DataTrimSeparators);
                }

                if (i == 0)
                {
                    rawColumnCount = rawValue.Length;
                }
                else if (rawValue.Length != rawColumnCount)
                {
                    throw new GameFrameworkException(Utility.Text.Format("Data table file '{0}', raw Column is '{2}', but line '{1}' column is '{3}'.", dataTableName, i.ToString(), rawColumnCount.ToString(), rawValue.Length.ToString()));
                }

                rawValues.Add(rawValue);
            }

            _rawValues = rawValues.ToArray();

            if (nameRow < 0)
            {
                throw new GameFrameworkException(Utility.Text.Format("Name row '{0}' is invalid.", nameRow.ToString()));
            }

            if (typeRow < 0)
            {
                throw new GameFrameworkException(Utility.Text.Format("Type row '{0}' is invalid.", typeRow.ToString()));
            }

            if (contentStartRow < 0)
            {
                throw new GameFrameworkException(Utility.Text.Format("Content start row '{0}' is invalid.", contentStartRow.ToString()));
            }

            if (idColumn < 0)
            {
                throw new GameFrameworkException(Utility.Text.Format("Id column '{0}' is invalid.", idColumn.ToString()));
            }

            if (nameRow >= rawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Name row '{0}' >= raw row count '{1}' is not allow.", nameRow.ToString(), rawRowCount.ToString()));
            }

            if (typeRow >= rawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Type row '{0}' >= raw row count '{1}' is not allow.", typeRow.ToString(), rawRowCount.ToString()));
            }

            if (defaultValueRow.HasValue && defaultValueRow.Value >= rawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Default value row '{0}' >= raw row count '{1}' is not allow.", defaultValueRow.Value.ToString(), rawRowCount.ToString()));
            }

            if (commentRow.HasValue && commentRow.Value >= rawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Comment row '{0}' >= raw row count '{1}' is not allow.", commentRow.Value.ToString(), rawRowCount.ToString()));
            }

            if (contentStartRow > rawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Content start row '{0}' > raw row count '{1}' is not allow.", contentStartRow.ToString(), rawRowCount.ToString()));
            }

            if (idColumn >= rawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Id column '{0}' >= raw column count '{1}' is not allow.", idColumn.ToString(), rawColumnCount.ToString()));
            }

            _nameRow = _rawValues[nameRow];
            _typeRow = _rawValues[typeRow];
            _defaultValueRow = defaultValueRow.HasValue ? _rawValues[defaultValueRow.Value] : null;
            _commentRow = commentRow.HasValue ? _rawValues[commentRow.Value] : null;
            _contentStartRow = contentStartRow;
            _idColumn = idColumn;

            _dataProcessors = new DataProcessor[rawColumnCount];
            for (int i = 0; i < rawColumnCount; i++)
            {
                if (i == IdColumn)
                {
                    _dataProcessors[i] = DataProcessorUtility.GetDataProcessor("id");
                }
                else
                {
                    _dataProcessors[i] = DataProcessorUtility.GetDataProcessor(_typeRow[i]);
                }
            }

            Dictionary<string, int> strings = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = contentStartRow; i < rawRowCount; i++)
            {
                if (IsCommentRow(i))
                {
                    continue;
                }

                for (int j = 0; j < rawColumnCount; j++)
                {
                    if (_dataProcessors[j].LanguageKeyword != "string")
                    {
                        continue;
                    }

                    string str = _rawValues[i][j];
                    if (strings.ContainsKey(str))
                    {
                        strings[str]++;
                    }
                    else
                    {
                        strings[str] = 1;
                    }
                }
            }

            _strings = strings.OrderBy(value => value.Key).OrderByDescending(value => value.Value).Select(value => value.Key).ToArray();

            _codeTemplate = null;
            _codeGenerator = null;
        }

        private static string[] ReadDataTableLines(string dataTableFileName, Encoding encoding)
        {
            if (string.IsNullOrEmpty(dataTableFileName))
            {
                throw new GameFrameworkException("Data table file name is invalid.");
            }

            if (!dataTableFileName.EndsWith(".txt", StringComparison.Ordinal))
            {
                throw new GameFrameworkException(Utility.Text.Format("Data table file '{0}' is not a txt.", dataTableFileName));
            }

            if (!File.Exists(dataTableFileName))
            {
                throw new GameFrameworkException(Utility.Text.Format("Data table file '{0}' is not exist.", dataTableFileName));
            }

            return File.ReadAllLines(dataTableFileName, encoding);
        }

        internal static IList<KeyValuePair<int, string>> GetDropdownTypes()
        {
            return DataProcessorUtility.GetDropdownTypes();
        }

        public int RawRowCount
        {
            get
            {
                return _rawValues.Length;
            }
        }

        public int RawColumnCount
        {
            get
            {
                return _rawValues.Length > 0 ? _rawValues[0].Length : 0;
            }
        }

        public int StringCount
        {
            get
            {
                return _strings.Length;
            }
        }

        public int ContentStartRow
        {
            get
            {
                return _contentStartRow;
            }
        }

        public int IdColumn
        {
            get
            {
                return _idColumn;
            }
        }

        public bool IsIdColumn(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return _dataProcessors[rawColumn].IsId;
        }

        public bool IsCommentRow(int rawRow)
        {
            if (rawRow < 0 || rawRow >= RawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw row '{0}' is out of range.", rawRow.ToString()));
            }

            return GetValue(rawRow, 0).StartsWith(CommentLineSeparator, StringComparison.Ordinal);
        }

        public bool IsCommentColumn(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return string.IsNullOrEmpty(GetName(rawColumn)) || _dataProcessors[rawColumn].IsComment;
        }

        public string GetName(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            if (IsIdColumn(rawColumn))
            {
                return "Id";
            }

            return _nameRow[rawColumn];
        }

        public bool IsSystem(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return _dataProcessors[rawColumn].IsSystem;
        }

        public System.Type GetType(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return _dataProcessors[rawColumn].Type;
        }

        public string GetLanguageKeyword(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return _dataProcessors[rawColumn].LanguageKeyword;
        }

        public string GetDefaultValue(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return _defaultValueRow != null ? _defaultValueRow[rawColumn] : null;
        }

        public string GetComment(int rawColumn)
        {
            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return _commentRow != null ? _commentRow[rawColumn] : null;
        }

        public string GetValue(int rawRow, int rawColumn)
        {
            if (rawRow < 0 || rawRow >= RawRowCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw row '{0}' is out of range.", rawRow.ToString()));
            }

            if (rawColumn < 0 || rawColumn >= RawColumnCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("Raw column '{0}' is out of range.", rawColumn.ToString()));
            }

            return _rawValues[rawRow][rawColumn];
        }

        public string GetString(int index)
        {
            if (index < 0 || index >= StringCount)
            {
                throw new GameFrameworkException(Utility.Text.Format("String index '{0}' is out of range.", index.ToString()));
            }

            return _strings[index];
        }

        public int GetStringIndex(string str)
        {
            for (int i = 0; i < StringCount; i++)
            {
                if (_strings[i] == str)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool GenerateDataFile(string outputFileName)
        {
            if (string.IsNullOrEmpty(outputFileName))
            {
                throw new GameFrameworkException("Output file name is invalid.");
            }

            try
            {
                using (FileStream fileStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write))
                {
                    using (BinaryWriter binaryWriter = new BinaryWriter(fileStream, Encoding.UTF8))
                    {
                        for (int rawRow = ContentStartRow; rawRow < RawRowCount; rawRow++)
                        {
                            if (IsCommentRow(rawRow))
                            {
                                continue;
                            }

                            byte[] bytes = GetRowBytes(outputFileName, rawRow);
                            if (bytes == null)
                            {
                                return false;
                            }

                            binaryWriter.Write7BitEncodedInt32(bytes.Length);
                            binaryWriter.Write(bytes);
                        }
                    }
                }

                Debug.Log(Utility.Text.Format("Parse data table '{0}' success.", outputFileName));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(Utility.Text.Format("Parse data table '{0}' failure, exception is '{1}'.", outputFileName, exception.ToString()));
                return false;
            }
        }

        public bool SetCodeTemplate(string codeTemplateFileName, Encoding encoding)
        {
            try
            {
                _codeTemplate = File.ReadAllText(codeTemplateFileName, encoding);
                //Debug.Log(Utility.Text.Format("Set code template '{0}' success.", codeTemplateFileName));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(Utility.Text.Format("Set code template '{0}' failure, exception is '{1}'.", codeTemplateFileName, exception.ToString()));
                return false;
            }
        }

        public void SetCodeGenerator(DataTableCodeGenerator codeGenerator)
        {
            _codeGenerator = codeGenerator;
        }

        public bool GenerateCodeFile(string outputFileName, Encoding encoding, object userData = null)
        {
            if (string.IsNullOrEmpty(_codeTemplate))
            {
                throw new GameFrameworkException("You must set code template first.");
            }

            if (string.IsNullOrEmpty(outputFileName))
            {
                throw new GameFrameworkException("Output file name is invalid.");
            }

            try
            {
                StringBuilder stringBuilder = new StringBuilder(_codeTemplate);
                if (_codeGenerator != null)
                {
                    _codeGenerator(this, stringBuilder, outputFileName);
                }
                var outputDir = Path.GetDirectoryName(outputFileName);
                if (!string.IsNullOrWhiteSpace(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                GameDataGenerator.WriteTextFile(outputFileName, stringBuilder.ToString(), encoding);

                Debug.Log(Utility.Text.Format("Generate code file '{0}' success.", outputFileName));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(Utility.Text.Format("Generate code file '{0}' failure, exception is '{1}'.", outputFileName, exception.ToString()));
                return false;
            }
        }

        private byte[] GetRowBytes(string outputFileName, int rawRow)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8))
                {
                    for (int rawColumn = 0; rawColumn < RawColumnCount; rawColumn++)
                    {
                        if (IsCommentColumn(rawColumn))
                        {
                            continue;
                        }

                        try
                        {
                            _dataProcessors[rawColumn].WriteToStream(this, binaryWriter, GetValue(rawRow, rawColumn));
                        }
                        catch
                        {
                            if (_dataProcessors[rawColumn].IsId || string.IsNullOrEmpty(GetDefaultValue(rawColumn)))
                            {
                                Debug.LogError(Utility.Text.Format("Parse raw value failure. OutputFileName='{0}' RawRow='{1}' RowColumn='{2}' Name='{3}' Type='{4}' RawValue='{5}'", outputFileName, rawRow.ToString(), rawColumn.ToString(), GetName(rawColumn), GetLanguageKeyword(rawColumn), GetValue(rawRow, rawColumn)));
                                return null;
                            }
                            else
                            {
                                Debug.LogWarning(Utility.Text.Format("Parse raw value failure, will try default value. OutputFileName='{0}' RawRow='{1}' RowColumn='{2}' Name='{3}' Type='{4}' RawValue='{5}'", outputFileName, rawRow.ToString(), rawColumn.ToString(), GetName(rawColumn), GetLanguageKeyword(rawColumn), GetValue(rawRow, rawColumn)));
                                try
                                {
                                    _dataProcessors[rawColumn].WriteToStream(this, binaryWriter, GetDefaultValue(rawColumn));
                                }
                                catch
                                {
                                    Debug.LogError(Utility.Text.Format("Parse default value failure. OutputFileName='{0}' RawRow='{1}' RowColumn='{2}' Name='{3}' Type='{4}' RawValue='{5}'", outputFileName, rawRow.ToString(), rawColumn.ToString(), GetName(rawColumn), GetLanguageKeyword(rawColumn), GetComment(rawColumn)));
                                    return null;
                                }
                            }
                        }
                    }

                    return memoryStream.ToArray();
                }
            }
        }
    }
}


