using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path
{
	public readonly Vector3[] lookPoints;
	public readonly Line[] turnBoundaries;
	public readonly int finishLineIndex;
	public readonly int slowDownIndex;

	public Path(Vector3[] waypoints, Vector3 startPos, float turnDst, float stoppingDst)
	{
		lookPoints = waypoints;
		turnBoundaries = new Line[lookPoints.Length];
		finishLineIndex = turnBoundaries.Length - 1;

		Vector2 previousPoint = V3ToV2(startPos);
		for (int i = 0; i < lookPoints.Length; i++)
		{
			Vector2 currentPoint = V3ToV2(lookPoints[i]);
			Vector2 dirToCurrentPoint = (currentPoint - previousPoint).normalized;
			// Aquí puede estar el error de que el coche gire como un cabron en el final
			//Vector2 turnBoundaryPoint = (i == finishLineIndex) ? currentPoint : currentPoint - dirToCurrentPoint * turnDst;
			Vector2 turnBoundaryPoint = currentPoint - dirToCurrentPoint * turnDst;
			turnBoundaries[i] = new Line(turnBoundaryPoint, previousPoint - dirToCurrentPoint * turnDst);
			previousPoint = turnBoundaryPoint;
		}

		float dstFromEndPoint = 0;
		for (int i = lookPoints.Length - 1; i > 0; i--)
		{
			dstFromEndPoint += Vector3.Distance(lookPoints[i], lookPoints[i - 1]);
			if (dstFromEndPoint > stoppingDst)
			{
				slowDownIndex = i;
				break;
			}
		}
	}

	Vector2 V3ToV2(Vector3 v3)
	{
		return new Vector2(v3.x, v3.z);
	}

	public void DrawWithGizmos()
	{

		Gizmos.color = Color.cyan;
		foreach (Vector3 p in lookPoints)
		{
			Gizmos.DrawCube(p + Vector3.up*0.5f, Vector3.one*0.5f);
		}

		Gizmos.color = Color.white;
		foreach (Line l in turnBoundaries)
		{
			l.DrawWithGizmos(4);
		}

	}
}
