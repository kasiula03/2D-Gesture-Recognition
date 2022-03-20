using System;
using System.Collections.Generic;
using System.Linq;
using FreeDraw;
using UnityEngine;

public class DisplayTemplate : MonoBehaviour
{
    [SerializeField] private Texture2D drawable_texture;
    [SerializeField] private GestureTemplates _gestureTemplates;
    [SerializeField] private bool _preparedPoints;
    [SerializeField] private int _step;

    private string _name;
    private int _index;

    private Color32[] cur_colors;

    private IEnumerable<RecognitionManager.GestureTemplate> _currentTemplatesByName =
        new List<RecognitionManager.GestureTemplate>();

    private readonly DollarOneRecognizer _dollarOneRecognizer = new DollarOneRecognizer();

    public void SetName(string name)
    {
        _index = 0;
        _name = name;
        _currentTemplatesByName = _gestureTemplates.RawTemplates.Where(template => template.Name == _name).ToArray();
        Redraw();
    }

    public void AddIndex(int increment)
    {
        _index += increment;
        _index = Math.Min(_index, _currentTemplatesByName.Count() - 1);
        _index = Math.Max(0, _index);
        Redraw();
    }

    private void Redraw()
    {
        if (_index >= 0 && _index < _currentTemplatesByName.Count())
        {
            Draw(_currentTemplatesByName.ElementAt(_index));
        }
        else
        {
            Clear();
        }
    }

    private void Draw(RecognitionManager.GestureTemplate gestureTemplate)
    {
        Clear();

        cur_colors = drawable_texture.GetPixels32();

        Vector2[] points = gestureTemplate.Points.Distinct().ToArray(); // For NaN
        if (_preparedPoints)
        {
            points = _dollarOneRecognizer.PreparePoints(gestureTemplate.Points, 64, _step);
        }

        float xMin = points.Select(point => point.x).Min();
        float xMax = points.Select(point => point.x).Max();
        float yMin = points.Select(point => point.y).Min();
        float yMax = points.Select(point => point.y).Max();

        for (var i = 1; i < points.Length; i++)
        {
            Vector2 previous = points[i - 1];
            Vector2 current = points[i];
            if (_preparedPoints && _step == 4)
            {
                previous = new Vector2(previous.x.Remap(xMin, 0, xMax, xMax - xMin),
                    previous.y.Remap(yMin, 0, yMax, yMax - yMin));
                current = new Vector2(current.x.Remap(xMin, 0, xMax, xMax - xMin),
                    current.y.Remap(yMin, 0, yMax, yMax - yMin));
            }

            ColourBetween(previous, current, 2, Color.red);
        }

        ApplyMarkedPixelChanges(drawable_texture, cur_colors);
    }

    private void Clear()
    {
        Color[] clean_colours_array = new Color[(int) drawable_texture.width * (int) drawable_texture.height];
        for (int x = 0; x < clean_colours_array.Length; x++)
            clean_colours_array[x] = Color.white;

        drawable_texture.SetPixels(clean_colours_array);
        drawable_texture.Apply();
    }


    public void ColourBetween(Vector2 start_point, Vector2 end_point, int width, Color color)
    {
        // Get the distance from start to finish
        float distance = Vector2.Distance(start_point, end_point);
        Vector2 direction = (start_point - end_point).normalized;

        Vector2 cur_position = start_point;

        // Calculate how many times we should interpolate between start_point and end_point based on the amount of time that has passed since the last update
        float lerp_steps = 1 / distance;

        for (float lerp = 0; lerp <= 1; lerp += lerp_steps)
        {
            cur_position = Vector2.Lerp(start_point, end_point, lerp);
            MarkPixelsToColour(cur_position, width, color);
        }
    }

    public void MarkPixelsToColour(Vector2 center_pixel, int pen_thickness, Color color_of_pen)
    {
        // Figure out how many pixels we need to colour in each direction (x and y)
        int center_x = (int) center_pixel.x;
        int center_y = (int) center_pixel.y;
        //int extra_radius = Mathf.Min(0, pen_thickness - 2);

        for (int x = center_x - pen_thickness; x <= center_x + pen_thickness; x++)
        {
            // Check if the X wraps around the image, so we don't draw pixels on the other side of the image
            if (x >= (int) drawable_texture.width || x < 0)
                continue;

            for (int y = center_y - pen_thickness; y <= center_y + pen_thickness; y++)
            {
                MarkPixelToChange(x, y, color_of_pen, cur_colors);
            }
        }
    }

    public void MarkPixelToChange(int x, int y, Color color, Color32[] textureColors)
    {
        // Need to transform x and y coordinates to flat coordinates of array
        int array_pos = y * (int) drawable_texture.width + x;

        // Check if this is a valid position
        if (array_pos > textureColors.Length || array_pos < 0)
            return;

        textureColors[array_pos] = color;
    }

    public void ApplyMarkedPixelChanges(Texture2D texture, Color32[] colors)
    {
        texture.SetPixels32(colors);
        texture.Apply(false);
    }

    public string GeTemplateName()
    {
        return _name;
    }

    public int GetTemplateIndex()
    {
        return _index;
    }
}