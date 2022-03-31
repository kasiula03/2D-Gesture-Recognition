using System.Linq;
using UnityEngine;

public class DisplayTemplate : MonoBehaviour
{
    [SerializeField] private Texture2D drawable_texture;
    [SerializeField] private bool _preparedPoints;

    private Color32[] cur_colors;
    private readonly DollarOneRecognizer _dollarOneRecognizer = new DollarOneRecognizer();
    private readonly DollarPRecognizer _dollarPRecognizer = new DollarPRecognizer();


    public void Draw(RecognitionManager.GestureTemplate gestureTemplate, DollarOneRecognizer.Step step)
    {
        Clear();

        cur_colors = drawable_texture.GetPixels32();

        DollarPoint[] points = gestureTemplate.Points.Distinct().ToArray(); // For NaN
        if (step != DollarOneRecognizer.Step.RAW)
        {
            points = _dollarPRecognizer.Normalize(gestureTemplate.Points, 64, step);
        }

        float xMin = points.Select(point => point.Point.x).Min();
        float xMax = points.Select(point => point.Point.x).Max();
        float yMin = points.Select(point => point.Point.y).Min();
        float yMax = points.Select(point => point.Point.y).Max();

        for (var i = 1; i < points.Length; i++)
        {
            Vector2 previous = points[i - 1].Point;
            Vector2 current = points[i].Point;
            if (step == DollarOneRecognizer.Step.TRANSLATED)
            {
                previous = new Vector2(previous.x.Remap(xMin, 0, xMax, xMax - xMin),
                    previous.y.Remap(yMin, 0, yMax, yMax - yMin));
                current = new Vector2(current.x.Remap(xMin, 0, xMax, xMax - xMin),
                    current.y.Remap(yMin, 0, yMax, yMax - yMin));
            }

            if (points[i - 1].StrokeIndex == points[i].StrokeIndex)
            {
                ColourBetween(previous, current, 2, Color.red);
            }
        }

        ApplyMarkedPixelChanges(drawable_texture, cur_colors);
    }

    public void Clear()
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
}