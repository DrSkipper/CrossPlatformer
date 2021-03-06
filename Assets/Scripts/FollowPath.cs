﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class FollowPath : MonoBehaviour
{
	public enum FollowType
	{
		MoveTowards,
		Lerp
	}

	public FollowType Type = FollowType.MoveTowards;
	public PathDefinition Path;
	public float Speed = 1.0f;
	public float MaxDistanceToGoal = 0.1f;

	private IEnumerator<Transform> _currentPoint;

	public void Start()
	{
		if (Path == null)
		{
			Debug.LogError("Path cannon be null", gameObject);
			return;
		}

		_currentPoint = Path.GetPathEnumerator();
		_currentPoint.MoveNext();

		if (_currentPoint.Current == null)
			return;

		this.transform.position = _currentPoint.Current.position;
	}

	public void Update()
	{
		if (_currentPoint == null || _currentPoint.Current == null)
			return;

		if (Type == FollowType.MoveTowards)
			this.transform.position = Vector3.MoveTowards(this.transform.position, _currentPoint.Current.position, Time.deltaTime * Speed);
		else if (Type == FollowType.Lerp)
			this.transform.position = Vector3.Lerp(this.transform.position, _currentPoint.Current.position, Time.deltaTime * Speed);

		var distanceSquared = (this.transform.position - _currentPoint.Current.position).sqrMagnitude;
		if (distanceSquared < MaxDistanceToGoal * MaxDistanceToGoal)
			_currentPoint.MoveNext();
	}
}
