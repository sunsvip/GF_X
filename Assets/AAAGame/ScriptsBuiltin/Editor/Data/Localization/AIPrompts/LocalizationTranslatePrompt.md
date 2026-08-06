你正在为 Unity 编辑器工具执行一次多语言翻译任务。

## 输入

- 母语文件：`[SOURCE_JSON_PATH]`
- 母语枚举：`[SOURCE_LANGUAGE]`
- 输出目录：`[OUTPUT_DIR]`

## 目标语言

[TARGET_LANGUAGES]

## Language 枚举约束

输出 JSON 顶层 `language` 字段只能使用以下枚举名之一：

[LANGUAGE_ENUM_CONSTRAINT]

禁止使用自然语言名称、地区化写法或其它别名，例如：

- `Japanese (JP)`
- `English-US`
- `简体中文`

## 执行要求

1. 先完整读取母语文件。
2. 以母语 JSON 的结构为唯一模板。
3. 为每个目标语言生成一个 JSON 文件到输出目录。
4. 文件名必须严格等于 `<LanguageEnum>.json`。
5. JSON 顶层 `language` 必须严格等于目标语言枚举名。
6. `items` 数量必须与母语完全一致。
7. `items[*].key` 必须完全保留，不能新增、删除、改名、改顺序。
8. 只翻译 `items[*].text`。
9. 保留占位符、富文本标签、换行、前后空白。
10. 如果母语文本里出现字面量 `\n` 或 `\r\n`，必须原样保留为这两个字符序列，禁止把它改成真实换行符。
11. 当母语文本为空时，目标文本也保持为空。
12. 不允许修改母语文件。
13. 不允许修改输出目录之外的任何项目文件。
14. `output` 目录中只允许出现目标语言 JSON 文件，不允许额外创建 Markdown、说明文本、汇总文件或其它格式。
15. 必须直接使用你的文件编辑/写入能力生成目标 JSON 文件，禁止通过 PowerShell here-string、`python -`、shell 重定向、管道脚本或其它命令行文本拼接方式生成翻译结果，因为这些方式会在 Windows 上破坏非 ASCII 字符。
16. 所有目标 JSON 文件必须以 UTF-8 编码写入。
17. 对于中文繁体、日文、韩文等非 ASCII 语言，禁止输出 `??`、`???` 这类问号占位乱码；如果你的当前写入方式会产生乱码，必须先改用安全的 UTF-8 文件写入方式，再继续生成结果。

## 输出格式

每个目标语言都输出一个 UTF-8 JSON 文件，结构必须如下：

```json
{
  "language": "English",
  "items": [
    { "key": "Confirm", "text": "Confirm" }
  ]
}
```

不要输出 Markdown。
不要输出额外说明。
不要修改 key。
不要额外创建其它结果格式。
