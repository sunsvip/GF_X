using System;
using System.Collections.Generic;

namespace UGF.EditorTools
{
    internal static class LocalizationMergeService
    {
        public static void MergeTexts(List<string> texts, ref List<LocalizationText> list)
        {
            foreach (string text in texts)
            {
                LocalizationText existingItem = list.Find(x => x.Key == text);
                if (existingItem == null)
                {
                    list.Add(new LocalizationText
                    {
                        Key = text,
                        Value = string.Empty,
                        Locked = false
                    });
                }
            }

            list.RemoveAll(item => !texts.Contains(item.Key) && !item.Locked);
            list.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));
        }

        public static void MergeTexts(List<LocalizationText> srcList, ref List<LocalizationText> destList)
        {
            foreach (var text in srcList)
            {
                LocalizationText existingItem = destList.Find(x => x.Key == text.Key);
                if (existingItem == null)
                {
                    destList.Add(new LocalizationText
                    {
                        Key = text.Key,
                        Value = string.Empty,
                        Locked = false
                    });
                }
            }

            destList.RemoveAll(item =>
            {
                var hasItem = srcList.Find(x => x.Key == item.Key) != null;
                return !hasItem && !item.Locked;
            });
            destList.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));
        }
    }
}
