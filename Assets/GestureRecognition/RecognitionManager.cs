using System;
using System.Collections.Generic;
using System.Linq;
using FreeDraw;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecognitionManager : MonoBehaviour
{
    [SerializeField] private GestureTemplates _templates;
    [SerializeField] private DisplayTemplate _displayTemplate;
    [SerializeField] private Drawable _drawable;
    [SerializeField] private Button _templateModeButton;
    [SerializeField] private Button _recognitionModeButton;
    [SerializeField] private Button _reviewTemplates;

    //State Template
    [SerializeField] private TMP_InputField _templateName;

    //State Template Review
    [SerializeField] private TMP_Dropdown _templateNameFromList;
    [SerializeField] private Button _previous;
    [SerializeField] private Button _next;
    [SerializeField] private Button _remove;

    private readonly DollarOneRecognizer _dollarOneRecognizer = new DollarOneRecognizer();
    private RecognizerState _state = RecognizerState.RECOGNITION;

    public enum RecognizerState
    {
        TEMPLATE,
        RECOGNITION,
        TEMPLATE_REVIEW
    }

    [Serializable]
    public struct GestureTemplate
    {
        public string Name;
        public Vector2[] Points;

        public GestureTemplate(string templateName, Vector2[] preparePoints)
        {
            Name = templateName;
            Points = preparePoints;
        }
    }

    private string TemplateName => _templateName.text;
    private List<string> _templateNames;

    private void Start()
    {
        _drawable.OnDrawFinished += OnDrawFinished;
        _templateModeButton.onClick.AddListener(() => SetupState(RecognizerState.TEMPLATE));
        _recognitionModeButton.onClick.AddListener(() => SetupState(RecognizerState.RECOGNITION));
        _reviewTemplates.onClick.AddListener(() => SetupState(RecognizerState.TEMPLATE_REVIEW));
        _previous.onClick.AddListener(() => ChooseTemplateIndex(-1));
        _next.onClick.AddListener(() => ChooseTemplateIndex(1));
        _remove.onClick.AddListener(RemoveTemplate);

        SetupState(_state);
        _templateNameFromList.onValueChanged.AddListener(ChooseTemplateToShow);
    }

    private void RemoveTemplate()
    {
        GestureTemplate templateToRemove = _templates.ProceedTemplates
            .Where(template => template.Name == _displayTemplate.GeTemplateName())
            .ElementAt(_displayTemplate.GetTemplateIndex());
        int indexToRemove = _templates.ProceedTemplates.IndexOf(templateToRemove);
        _templates.ProceedTemplates.RemoveAt(indexToRemove);
        _templates.RawTemplates.RemoveAt(indexToRemove);

        _displayTemplate.SetName(templateToRemove.Name);
    }

    private void ChooseTemplateIndex(int increment)
    {
        _displayTemplate.AddIndex(increment);
    }

    private void SetupAllTemplates()
    {
        _templateNames = _templates
            .RawTemplates
            .Select(template => template.Name).Distinct().ToList();

        _templateNameFromList.options = _templateNames
            .Select(templateName => new TMP_Dropdown.OptionData(templateName))
            .ToList();

        ChooseTemplateToShow(0);
    }

    private void ChooseTemplateToShow(int choose)
    {
        string chosenTemplate = _templateNames[choose];
        _displayTemplate.SetName(chosenTemplate);
    }

    private void SetupState(RecognizerState state)
    {
        SetupAllTemplates();

        _state = state;
        _templateModeButton.image.color = _state == RecognizerState.TEMPLATE ? Color.green : Color.white;
        _recognitionModeButton.image.color = _state == RecognizerState.RECOGNITION ? Color.green : Color.white;
        _reviewTemplates.image.color = _state == RecognizerState.TEMPLATE_REVIEW ? Color.green : Color.white;
        _templateName.gameObject.SetActive(_state == RecognizerState.TEMPLATE);

        _displayTemplate.gameObject.SetActive(state == RecognizerState.TEMPLATE_REVIEW);
        _drawable.gameObject.SetActive(state != RecognizerState.TEMPLATE_REVIEW);
        _templateNameFromList.gameObject.SetActive(state == RecognizerState.TEMPLATE_REVIEW);
        _previous.gameObject.SetActive(state == RecognizerState.TEMPLATE_REVIEW);
        _next.gameObject.SetActive(state == RecognizerState.TEMPLATE_REVIEW);
        _remove.gameObject.SetActive(state == RecognizerState.TEMPLATE_REVIEW);
    }

    public void OnDrawFinished(Vector2[] points)
    {
        Debug.Log(points.Length);
        Debug.Log("Finished!");
        if (_state == RecognizerState.TEMPLATE)
        {
            GestureTemplate preparedTemplate =
                new GestureTemplate(TemplateName, _dollarOneRecognizer.PreparePoints(points, 64, 4));
            _templates.RawTemplates.Add(new GestureTemplate(TemplateName, points));
            _templates.ProceedTemplates.Add(preparedTemplate);
        }
        else
        {
            (string, float) result = _dollarOneRecognizer.DoRecognition(points, 64, _templates.GetTemplates());
            Debug.Log($"Recognized: {result.Item1}, Score: {result.Item2}");
        }
    }
}