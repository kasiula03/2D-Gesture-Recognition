using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TemplateReviewPanel : MonoBehaviour
{
    [SerializeField] private RectTransform _panel;
    [SerializeField] private DisplayTemplate _displayTemplate;
    [SerializeField] private TMP_Dropdown _templateNameFromList;
    [SerializeField] private Button _previous;
    [SerializeField] private Button _next;
    [SerializeField] private Button _remove;
    [SerializeField] private TMP_Dropdown _transitionStep;

    private List<string> _templateNames;
    private GestureTemplates _templates => GestureTemplates.Get();

    private string _currentTemplateName;
    private int _currentTemplateIndex;
    private int _step = 0;

    public void Awake()
    {
        _previous.onClick.AddListener(() => ChooseTemplateIndex(-1));
        _next.onClick.AddListener(() => ChooseTemplateIndex(1));
        _remove.onClick.AddListener(RemoveTemplate);
        _templateNameFromList.onValueChanged.AddListener(ChooseTemplateToShow);
        _transitionStep.options = new List<TMP_Dropdown.OptionData>()
        {
            new TMP_Dropdown.OptionData(DollarOneRecognizer.Step.RAW.ToString()),
            new TMP_Dropdown.OptionData(DollarOneRecognizer.Step.RESAMPLED.ToString()),
            new TMP_Dropdown.OptionData(DollarOneRecognizer.Step.ROTATED.ToString()),
            new TMP_Dropdown.OptionData(DollarOneRecognizer.Step.SCALED.ToString()),
            new TMP_Dropdown.OptionData(DollarOneRecognizer.Step.TRANSLATED.ToString())
        };
        _transitionStep.onValueChanged.AddListener(ChangeStep);

        UpdateTemplates();
    }

    private void ChangeStep(int step)
    {
        _step = step;
        UpdateState();
    }

    public void SetVisibility(bool visible)
    {
        _panel.gameObject.SetActive(visible);
        _displayTemplate.gameObject.SetActive(visible);
        if (visible)
        {
            UpdateState();
            ChooseTemplateToShow(0);
        }
    }

    private void UpdateState()
    {
        UpdateTemplates();
        RecognitionManager.GestureTemplate[] candidates = _templates.GetRawTemplatesByName(_currentTemplateName);
        if (candidates.Length > 0)
        {
            _displayTemplate.Draw(candidates[_currentTemplateIndex], (DollarOneRecognizer.Step) _step);
        }
        else
        {
            _displayTemplate.Clear();
        }
    }

    private void UpdateTemplates()
    {
        _templateNames = _templates
            .RawTemplates
            .Select(template => template.Name).Distinct().ToList();

        _templateNameFromList.options = _templateNames
            .Select(templateName => new TMP_Dropdown.OptionData(templateName))
            .ToList();

        bool anyTemplatesAvailable = _templateNames.Any();
        _remove.gameObject.SetActive(anyTemplatesAvailable);
        _templateNameFromList.gameObject.SetActive(anyTemplatesAvailable);
        _previous.gameObject.SetActive(anyTemplatesAvailable);
        _next.gameObject.SetActive(anyTemplatesAvailable);
    }


    private void ChooseTemplateIndex(int increment)
    {
        _currentTemplateIndex += increment;
        _currentTemplateIndex = Mathf.Max(0, _currentTemplateIndex);
        _currentTemplateIndex = Mathf.Min(_currentTemplateIndex,
            _templates.GetRawTemplatesByName(_currentTemplateName).Length - 1);
        UpdateState();
    }

    private void ChooseTemplateToShow(int choose)
    {
        string chosenTemplate = _templateNames[choose];
        _currentTemplateName = chosenTemplate;
        _currentTemplateIndex = 0;
        UpdateState();
    }

    private void RemoveTemplate()
    {
        IEnumerable<RecognitionManager.GestureTemplate> templatesByName = _templates.ProceedTemplates
            .Where(template => template.Name == _currentTemplateName).ToList();
        RecognitionManager.GestureTemplate templateToRemove = templatesByName
            .ElementAt(_currentTemplateIndex);
        int indexToRemove = _templates.ProceedTemplates.IndexOf(templateToRemove);
        _templates.RemoveAtIndex(indexToRemove);
        if (_currentTemplateIndex != 0)
        {
            _currentTemplateIndex--;
        }

        if (templatesByName.Count() == 1)
        {
            ChooseTemplateToShow(0);
        }


        UpdateState();
    }
}