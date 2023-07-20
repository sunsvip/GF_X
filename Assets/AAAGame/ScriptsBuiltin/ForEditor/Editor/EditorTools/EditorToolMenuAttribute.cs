using System;
namespace UGF.EditorTools
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EditorToolMenuAttribute : Attribute
    {
        public string ToolMenuPath { get; private set; }
        public int MenuOrder { get; private set; }
        public bool IsUtility { get; private set; }
        /// <summary>
        /// 标记子工具类所属的工具Editor
        /// </summary>
        public Type OwnerType { get; private set; }
        public EditorToolMenuAttribute(string menu, Type owner, int menuOrder = 0, bool isUtility = false)
        {
            this.ToolMenuPath = menu;
            OwnerType = owner;
            MenuOrder = menuOrder;
            IsUtility = isUtility;
        }
    }

}
