using System;
using UnityEngine;
using UnityEngine.UI;

public class RecognitionPanel : MonoBehaviour
{
    [SerializeField] private Button _dollar1;
    [SerializeField] private Button _dollarP;

    public void Initialize(Action<int> onAlgorithmChoose)
    {
        SetupButtons(0);
        _dollar1.onClick.AddListener(() =>
        {
            SetupButtons(0);
            onAlgorithmChoose.Invoke(0);
        });
        _dollarP.onClick.AddListener(() =>
        {
            SetupButtons(1);
            onAlgorithmChoose.Invoke(1);
        });
    }

    private void SetupButtons(int choose)
    {
        _dollar1.image.color = choose == 0 ? Color.green : Color.white;
        _dollarP.image.color = choose == 1 ? Color.green : Color.white;
    }

    public void SetVisibility(bool visible)
    {
        _dollar1.gameObject.SetActive(visible);
        _dollarP.gameObject.SetActive(visible);
    }
}