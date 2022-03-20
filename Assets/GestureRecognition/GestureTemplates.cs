using System.Collections.Generic;
using FreeDraw;
using UnityEngine;

[CreateAssetMenu(fileName = "Templates", menuName = "Gestures/Templates", order = 1)]
public class GestureTemplates : ScriptableObject
{
    public List<RecognitionManager.GestureTemplate> RawTemplates = new List<RecognitionManager.GestureTemplate>();
    public List<RecognitionManager.GestureTemplate> ProceedTemplates = new List<RecognitionManager.GestureTemplate>();

    public List<RecognitionManager.GestureTemplate> GetTemplates()
    {
        return ProceedTemplates;
    }
}