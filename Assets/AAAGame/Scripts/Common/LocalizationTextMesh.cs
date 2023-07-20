using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationTextMesh : MonoBehaviour
{
    [SerializeField] string mKey;
    void Start()
    {
        var txtMesh = GetComponent<UnityEngine.TextMesh>();
        if (txtMesh != null)
        {
            txtMesh.text = GF.Localization.GetText(mKey);//.Replace("\\n", "\n");
        }
    }
}
