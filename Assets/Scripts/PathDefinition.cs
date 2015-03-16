using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PathDefinition : MonoBehaviour
{
	public Transform[] Points;

	public IEnumerator<Transform> GetPathEnumerator()
	{
		if (Points == null || Points.Length < 1)
			yield break;

		var direction = 1;
		var index = 0;
		while (true)
		{
			yield return Points[index];

			if (Points.Length == 1)
				continue;

			if (index <= 0)
				direction = 1;
			else if (index >= Points.Length - 1)
				direction = -1;

			index += direction;
		}
	}

	// Draw stuff in scene view of Unity Editor
	// OnDrawGizmosSelected() exists too - will only draw if obj is selected
	public void OnDrawGizmos()
	{
		if (Points == null || Points.Length < 2)
			return;
		
		var nonNullPoints = Points.Where(t => t != null).ToList();

		if (nonNullPoints.Count < 2)
			return;

		for (var i = 1; i < nonNullPoints.Count; ++i)
		{
			Gizmos.DrawLine(nonNullPoints[i - 1].position, nonNullPoints[i].position);
		}
	}
}
