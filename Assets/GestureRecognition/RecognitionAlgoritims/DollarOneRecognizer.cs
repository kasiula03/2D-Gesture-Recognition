using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DollarOneRecognizer : Recognizer, IRecognizer
{
    private int _size = 250;

    public enum Step
    {
        RAW,
        RESAMPLED,
        ROTATED,
        SCALED,
        TRANSLATED
    }

    public DollarPoint[] Normalize(DollarPoint[] points, int n, Step step = Step.TRANSLATED)
    {
        DollarPoint[] copyPoints = new DollarPoint[points.Length];
        points.CopyTo(copyPoints, 0);
        DollarPoint[] resampledPoints = ResamplePoints(copyPoints, n);
        DollarPoint[] rotatedPoints = RotateToZero(resampledPoints);
        DollarPoint[] scaledPoints = ScaleToSquare(rotatedPoints, _size);
        DollarPoint[] translatedToOrigin = TranslateToOrigin(scaledPoints);

        //Debug purpose only
        switch (step)
        {
            case Step.RAW:
                return points;
            case Step.RESAMPLED:
                return resampledPoints;
            case Step.ROTATED:
                return rotatedPoints;
            case Step.SCALED:
                return scaledPoints;
        }

        return translatedToOrigin;
    }

    public (string, float) DoRecognition(DollarPoint[] points, int n,
        List<RecognitionManager.GestureTemplate> gestureTemplates)
    {
        DollarPoint[] preparedPoints = Normalize(points, n);
        float angle = 0.5f * (-1 + Mathf.Sqrt(5));
        return Recognize(preparedPoints, gestureTemplates, 250, angle);
    }


    private DollarPoint[] RotateToZero(DollarPoint[] points)
    {
        float angle = IndicativeAngle(points);
        List<DollarPoint> newPoints = RotateBy(points, -angle);
        return newPoints.ToArray();
    }

    private List<DollarPoint> RotateBy(DollarPoint[] points, float angle)
    {
        List<DollarPoint> newPoints = new List<DollarPoint>(points.Length);
        Vector2 centroid = GetCentroid(points);
        foreach (DollarPoint point in points)
        {
            float rotatedX = (point.Point.x - centroid.x) * Mathf.Cos(angle) -
                             (point.Point.y - centroid.y) * Mathf.Sin(angle) +
                             centroid.x;
            float rotatedY = (point.Point.x - centroid.x) * Mathf.Sin(angle) +
                             (point.Point.y - centroid.y) * Mathf.Cos(angle) +
                             centroid.y;
            newPoints.Add(new DollarPoint(rotatedX, rotatedY, 0));
        }

        return newPoints;
    }

    private float IndicativeAngle(DollarPoint[] points)
    {
        Vector2 centroid = GetCentroid(points);
        return Mathf.Atan2(points[0].Point.y - centroid.y, points[0].Point.x - centroid.x);
    }

    private Vector2 GetCentroid(Vector2[] points)
    {
        float centerX = points.Sum(point => point.x) / points.Length;
        float centerY = points.Sum(point => point.y) / points.Length;
        return new Vector2(centerX, centerY);
    }

    private DollarPoint[] ScaleToSquare(DollarPoint[] points, float size)
    {
        List<DollarPoint> newPoints = new List<DollarPoint>(points.Length);
        Rect box = GetBoundingBox(points);
        foreach (DollarPoint point in points)
        {
            float scaledX = point.Point.x * size / box.width;
            float scaledY = point.Point.y * size / box.height;
            newPoints.Add(new DollarPoint(scaledX, scaledY, 0));
        }

        return newPoints.ToArray();
    }

    private Rect GetBoundingBox(DollarPoint[] points)
    {
        float minX = points.Select(point => point.Point.x).Min();
        float maxX = points.Select(point => point.Point.x).Max();
        float minY = points.Select(point => point.Point.y).Min();
        float maxY = points.Select(point => point.Point.y).Max();
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private DollarPoint[] TranslateToOrigin(DollarPoint[] points)
    {
        List<DollarPoint> newPoints = new List<DollarPoint>(points.Length);
        Vector2 centroid = GetCentroid(points);
        foreach (DollarPoint point in points)
        {
            float translatedX = point.Point.x - centroid.x;
            float translatedY = point.Point.y - centroid.y;
            newPoints.Add(new DollarPoint(translatedX, translatedY, 0));
        }

        return newPoints.ToArray();
    }

    private (string, float) Recognize(
        DollarPoint[] points,
        List<RecognitionManager.GestureTemplate> gestureTemplates,
        float size,
        float angle)
    {
        float theta = 45;
        float deltaTheta = 2;
        float bestDistance = float.MaxValue;
        RecognitionManager.GestureTemplate bestTemplate = new RecognitionManager.GestureTemplate();

        //Should be stored in proceesed, but for testing purpose we use RawPoints
        IEnumerable<RecognitionManager.GestureTemplate> proceedGestures = gestureTemplates.Select(template =>
            new RecognitionManager.GestureTemplate() {Points = Normalize(template.Points, 64), Name = template.Name});

        foreach (RecognitionManager.GestureTemplate gestureTemplate in proceedGestures.Where(template =>
            template.Points.Length == points.Length))
        {
            float distance = DistanceAtBestAngle(points, gestureTemplate, -theta, theta, deltaTheta, angle);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTemplate = gestureTemplate;
            }
        }

        double score = 1 - (bestDistance / (0.5f * Math.Sqrt(2 * size * size)));
        return ((string, float)) (bestTemplate.Name, score);
    }

    private float DistanceAtBestAngle(DollarPoint[] points, RecognitionManager.GestureTemplate template, float thetaA,
        float thetaB,
        float deltaTheta, float angle)
    {
        float firstX = angle * thetaA + (1 - angle) * thetaB;
        float firstDistance = DistanceAtAngle(points, template, firstX);
        float secondX = (1 - angle) * thetaA + angle * thetaB;
        float secondDistance = DistanceAtAngle(points, template, secondX);

        while (thetaB - thetaA > deltaTheta)
        {
            if (firstDistance < secondDistance)
            {
                thetaB = secondX;
                secondX = firstX;
                secondDistance = firstDistance;
                firstX = angle * thetaA + (1 - angle) * thetaB;
                firstDistance = DistanceAtAngle(points, template, firstX);
            }
            else
            {
                thetaA = firstX;
                firstX = secondX;
                firstDistance = secondDistance;
                secondX = (1 - angle) * thetaA + angle * thetaB;
                secondDistance = DistanceAtAngle(points, template, secondX);
            }
        }

        return Mathf.Min(firstDistance, secondDistance);
    }

    private float DistanceAtAngle(DollarPoint[] points, RecognitionManager.GestureTemplate template, float angle)
    {
        List<DollarPoint> newPoints = RotateBy(points, angle);
        return PathDistance(newPoints, template.Points);
    }

    private float PathDistance(List<DollarPoint> points, DollarPoint[] templatePoints)
    {
        float distance = 0;

        for (int i = 0; i < points.Count; i++)
        {
            distance += Vector2.Distance(points[i].Point, templatePoints[i].Point);
        }

        return distance / points.Count;
    }
}