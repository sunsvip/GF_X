//---------------------------------
//此文件由工具自动生成,请勿手动修改
//更新自:525105219@qq.com
//更新时间:07/11/2022 18:51:23
//---------------------------------
using UnityEngine;
using TMPro;
public partial class GameUIForm
{
	private TextMeshProUGUI coinNumText = null;
	protected override void InitUIProperties()
	{
		var fields = this.GetFieldsProperties();
		coinNumText = fields[0].GetComponent<TextMeshProUGUI>(0);
	}
}
