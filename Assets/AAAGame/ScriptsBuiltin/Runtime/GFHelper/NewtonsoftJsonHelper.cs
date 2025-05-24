using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using System;
using Newtonsoft.Json;
[Obfuz.ObfuzIgnore]
public class NewtonsoftJsonHelper : Utility.Json.IJsonHelper
{
    public string ToJson(object obj)
    {
        return JsonConvert.SerializeObject(obj);
    }

    public T ToObject<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }

    public object ToObject(Type objectType, string json)
    {
        return JsonConvert.DeserializeObject(json, objectType);
    }
}
