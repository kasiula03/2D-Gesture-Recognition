using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DollarOneRecognizer
{
    private int _size = 250;

    public Vector2[] PreparePoints(Vector2[] points, int n, int step = 4)
    {
        Vector2[] copyPoints = new Vector2[points.Length];
        points.CopyTo(copyPoints, 0);
        ;
        Vector2[] resampledPoints = ResamplePoints(copyPoints, n);
        Vector2[] rotatedPoints = RotateToZero(resampledPoints);
        Vector2[] scaledPoints = ScaleToSquare(rotatedPoints, _size);
        Vector2[] translatedToOrigin = TranslateToOrigin(scaledPoints);

        //Debug purpose only
        if (step == 0)
        {
            return points;
        }

        if (step == 1)
        {
            return resampledPoints;
        }

        if (step == 2)
        {
            return rotatedPoints;
        }

        if (step == 3)
        {
            return scaledPoints;
        }

        return translatedToOrigin;
    }

    public (string, float) DoRecognition(Vector2[] points, int n,
        List<RecognitionManager.GestureTemplate> gestureTemplates)
    {
        Vector2[] preparedPoints = PreparePoints(points, n, 4);
        float angle = 0.5f * (-1 + Mathf.Sqrt(5));
        return Recognize(preparedPoints, gestureTemplates, 250, angle);
    }


    private Vector2[] ResamplePoints(Vector2[] points, int n)
    {
        float incrementValue = PathLength(points) / (n - 1);
        float proceedDistance = 0;
        List<Vector2> newPoints = new List<Vector2> {points[0]};
        for (int i = 1; i < points.Length; i++)
        {
            Vector2 previousPoint = points[i - 1];
            Vector2 currentPoint = points[i];
            float distance = Vector2.Distance(previousPoint, currentPoint);
            if (float.IsNaN(distance))
            {
                continue;
            }

            if (proceedDistance + distance >= incrementValue)
            {
                float approximatedX = previousPoint.x +
                                      ((incrementValue - proceedDistance) / distance) *
                                      (currentPoint.x - previousPoint.x);
                float approximatedY = previousPoint.y +
                                      ((incrementValue - proceedDistance) / distance) *
                                      (currentPoint.y - previousPoint.y);
                Vector2 approximatedPoint = new Vector2(approximatedX, approximatedY);
                newPoints.Add(approximatedPoint);
                points[i] = approximatedPoint;
                proceedDistance = 0;
            }
            else
            {
                proceedDistance += distance;
            }
        }

        if (proceedDistance > 0)
        {
            newPoints.Add(points[points.Length - 1]);
        }

        return newPoints.ToArray();
    }

    private float PathLength(Vector2[] points)
    {
        float length = 0;
        for (int i = 1; i < points.Length; i++)
        {
            float distance = Vector2.Distance(points[i - 1], points[i]);
            if (!float.IsNaN(distance))
            {
                length += distance;
            }
        }

        return length;
    }

    private Vector2[] RotateToZero(Vector2[] points)
    {
        float angle = IndicativeAngle(points);
        List<Vector2> newPoints = RotateBy(points, -angle);

        return newPoints.ToArray();
    }

    private List<Vector2> RotateBy(Vector2[] points, float angle)
    {
        List<Vector2> newPoints = new List<Vector2>(points.Length);
        Vector2 centroid = GetCentroid(points);
        foreach (Vector2 point in points)
        {
            float rotatedX = (point.x - centroid.x) * Mathf.Cos(angle) - (point.y - centroid.y) * Mathf.Sin(angle) +
                             centroid.x;
            float rotatedY = (point.x - centroid.x) * Mathf.Sin(angle) + (point.y - centroid.y) * Mathf.Cos(angle) +
                             centroid.y;
            newPoints.Add(new Vector2(rotatedX, rotatedY));
        }

        return newPoints;
    }

    private float IndicativeAngle(Vector2[] points)
    {
        Vector2 centroid = GetCentroid(points);
        return Mathf.Atan2(points[0].y - centroid.y, points[0].x - centroid.x);
    }

    private Vector2 GetCentroid(Vector2[] points)
    {
        float centerX = points.Sum(point => point.x) / points.Length;
        float centerY = points.Sum(point => point.y) / points.Length;
        return new Vector2(centerX, centerY);
    }

    private Vector2[] ScaleToSquare(Vector2[] points, float size)
    {
        List<Vector2> newPoints = new List<Vector2>(points.Length);
        Rect box = GetBoundingBox(points);
        foreach (Vector2 point in points)
        {
            float scaledX = point.x * size / box.width;
            float scaledY = point.y * size / box.height;
            newPoints.Add(new Vector2(scaledX, scaledY));
        }

        return newPoints.ToArray();
    }

    private Rect GetBoundingBox(Vector2[] points)
    {
        float minX = points.Select(point => point.x).Min();
        float maxX = points.Select(point => point.x).Max();
        float minY = points.Select(point => point.y).Min();
        float maxY = points.Select(point => point.y).Max();
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private Vector2[] TranslateToOrigin(Vector2[] points)
    {
        List<Vector2> newPoints = new List<Vector2>(points.Length);
        Vector2 centroid = GetCentroid(points);
        foreach (Vector2 point in points)
        {
            float translatedX = point.x - centroid.x;
            float translatedY = point.y - centroid.y;
            newPoints.Add(new Vector2(translatedX, translatedY));
        }

        return newPoints.ToArray();
    }

    private (string, float) Recognize(Vector2[] points, List<RecognitionManager.GestureTemplate> gestureTemplates,
        float size,
        float angle)
    {
        float theta = 45;
        float deltaTheta = 2;
        float bestDistance = float.MaxValue;
        RecognitionManager.GestureTemplate bestTemplate = new RecognitionManager.GestureTemplate();

        foreach (RecognitionManager.GestureTemplate gestureTemplate in gestureTemplates.Where(template =>
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

    private float DistanceAtBestAngle(Vector2[] points, RecognitionManager.GestureTemplate template, float thetaA,
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

    private float DistanceAtAngle(Vector2[] points, RecognitionManager.GestureTemplate template, float angle)
    {
        List<Vector2> newPoints = RotateBy(points, angle);
        return PathDistance(newPoints, template.Points);
    }

    private float PathDistance(List<Vector2> points, Vector2[] templatePoints)
    {
        float distance = 0;

        for (int i = 0; i < points.Count; i++)
        {
            distance += Vector2.Distance(points[i], templatePoints[i]);
        }

        return distance / points.Count;
    }
}