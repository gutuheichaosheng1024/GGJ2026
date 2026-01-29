using System;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static string basePath = "Save";

    /*
     * extraName用于多个需要保存的不同场景或多个的同命类
     * */
    public static bool save<T> (T _data,string extraName = null) where T : class
    {
        string fileName = typeof(T).Name;
        if (extraName != null) fileName += ("-" + extraName);
        string filePath = Path.Combine(basePath, fileName);

        string json = JsonUtility.ToJson(_data);
        File.WriteAllText(filePath, json);

        return true;
    }

    public static T load<T>(string extraName = null) where T : class
    {
        string fileName = typeof(T).Name;
        if (extraName != null) fileName += ("-" + extraName);
        string filePath = Path.Combine(basePath, fileName);

        if (!File.Exists(filePath))
        {
            return null;
        }

        string json = File.ReadAllText(filePath);


        T _obj = JsonUtility.FromJson<T>(json);

        return _obj;
    }
}
