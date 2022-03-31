using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DollarPRecognizer : Recognizer, IRecognizer
{
    public (string, float) DoRecognition(DollarPoint[] points, int n,
        List<RecognitionManager.GestureTemplate> gestureTemplates)
    {
        DollarPoint[] normalizedPoints = Normalize(points, n);
        RecognitionManager.GestureTemplate winnerGesture = new RecognitionManager.GestureTemplate();
        float minDistance = Mathf.Infinity;
        float distance = 0;
        foreach (RecognitionManager.GestureTemplate gestureTemplate in gestureTemplates)
        {
            //Should be stored in proceesed, but for testing purpose we use RawPoints
            DollarPoint[] normalizedTemplatePoints = Normalize(gestureTemplate.Points, n);

            distance = GreedyCloudMatch(normalizedPoints, normalizedTemplatePoints, n);
            if (minDistance > distance)
            {
                minDistance = distance;
                winnerGesture = gestureTemplate;
            }
        }

        return (winnerGesture.Name, minDistance);
    }

    private float GreedyCloudMatch(DollarPoint[] points, DollarPoint[] templatePoints, int n)
    {
        float epsilon = 0.5f;
        int step = (int) Math.Floor(Math.Pow(n, 1.0f - epsilon));
        float min = float.MaxValue;
        for (int i = 0; i < n; i += step)
        {
            float firstDistance = CloudDistance(points, templatePoints, n, i);
            float secondDistance = CloudDistance(templatePoints, points, n, i);
            min = Mathf.Min(min, firstDistance, secondDistance);
        }

        return min;
    }

    private float CloudDistance(DollarPoint[] points, DollarPoint[] templatePoints, int n, int start)
    {
        bool[] matched = new bool[n];
        float sum = 0;
        int i = start;
        do
        {
            float min = float.MaxValue;
            int index = 0;
            for (int j = 0; j < matched.Length; j++)
            {
                if (!matched[j])
                {
                    try
                    {
                        float distance = Vector2.Distance(points[i].Point, templatePoints[j].Point);
                        if (distance < min)
                        {
                            min = distance;
                            index = j;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                        return sum;
                    }
                }
            }

            matched[index] = true;
            float weight = 1.0f - ((i - start + n) % n) / (1.0f * n);
            sum += weight * min;
            i = (i + 1) % n;
        } while (i != start);

        return sum;
    }

    public DollarPoint[] Normalize(DollarPoint[] points, int n,
        DollarOneRecognizer.Step step = DollarOneRecognizer.Step.TRANSLATED)
    {
        DollarPoint[] copyPoints = new DollarPoint[points.Length];
        points.CopyTo(copyPoints, 0);
        DollarPoint[] resampled = ResamplePoints(copyPoints, n);
        DollarPoint[] scaled = Scale(resampled);
        DollarPoint[] translatedToOrigin = TranslateToOrigin(scaled);
        switch (step)
        {
            case DollarOneRecognizer.Step.RAW:
                return copyPoints;
            case DollarOneRecognizer.Step.RESAMPLED:
                return resampled;
            case DollarOneRecognizer.Step.SCALED:
                return scaled;
            case DollarOneRecognizer.Step.TRANSLATED:
                return translatedToOrigin;
        }

        return new DollarPoint[] {new DollarPoint()};
    }

    private DollarPoint[] TranslateToOrigin(DollarPoint[] points)
    {
        List<DollarPoint> newPoints = new List<DollarPoint>(points.Length);
        Vector2 centroid = GetCentroid(points);
        foreach (DollarPoint point in points)
        {
            float translatedX = point.Point.x - centroid.x;
            float translatedY = point.Point.y - centroid.y;
            newPoints.Add(new DollarPoint()
                {Point = new Vector2(translatedX, translatedY), StrokeIndex = point.StrokeIndex});
        }

        return newPoints.ToArray();
    }


    private DollarPoint[] Scale(DollarPoint[] resampled)
    {
        List<DollarPoint> newPoints = new List<DollarPoint>();
        float minX = resampled.Select(point => point.Point.x).Min();
        float maxX = resampled.Select(point => point.Point.x).Max();
        float minY = resampled.Select(point => point.Point.y).Min();
        float maxY = resampled.Select(point => point.Point.y).Max();
        float scale = Mathf.Max(maxX - minX, maxY - minY);
        foreach (DollarPoint pointP in resampled)
        {
            float x = (pointP.Point.x - minX) / scale;
            float y = (pointP.Point.y - minY) / scale;
            newPoints.Add(new DollarPoint() {Point = new Vector2(x, y), StrokeIndex = pointP.StrokeIndex});
        }

        return newPoints.ToArray();
    }
}