using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class GestureTemplates
{
    private static GestureTemplates instance;

    public static GestureTemplates Get()
    {
        if (instance == null)
        {
            instance = new GestureTemplates();
            instance.Load();
        }

        return instance;
    }


    public List<RecognitionManager.GestureTemplate> RawTemplates = new List<RecognitionManager.GestureTemplate>();
    public List<RecognitionManager.GestureTemplate> ProceedTemplates = new List<RecognitionManager.GestureTemplate>();

    public List<RecognitionManager.GestureTemplate> GetTemplates()
    {
        return ProceedTemplates;
    }

    public void RemoveAtIndex(int indexToRemove)
    {
        ProceedTemplates.RemoveAt(indexToRemove);
        RawTemplates.RemoveAt(indexToRemove);
    }

    public RecognitionManager.GestureTemplate[] GetRawTemplatesByName(string name)
    {
        return RawTemplates.Where(template => template.Name == name).ToArray();
    }

    public void Save()
    {
        string path = Application.persistentDataPath + "/SavedTemplates.json";
        string potion = JsonUtility.ToJson(this);
        File.WriteAllText(path, potion);
    }

    private void Load()
    {
        string path = Application.persistentDataPath + "/SavedTemplates.json";
        if (File.Exists(path))
        {
            GestureTemplates data = JsonUtility.FromJson<GestureTemplates>(File.ReadAllText(path));
            RawTemplates.Clear();
            RawTemplates.AddRange(data.RawTemplates);
            ProceedTemplates.Clear();
            ProceedTemplates.AddRange(data.ProceedTemplates);
        }
    }
}