using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
	public Transform Player;
	public Vector2 Margin;
	public Vector2 Smoothing;
	public BoxCollider2D Bounds;

	private Vector3 _min;
	private Vector3 _max;

	public bool IsFollowing { get; set; }

	public void Start()
	{
		IsFollowing = true;
		_min = Bounds.bounds.min;
		_max = Bounds.bounds.max;
	}

	public void Update()
	{
		var x = this.transform.position.x;
		var y = this.transform.position.y;

		if (IsFollowing)
		{
			if (Mathf.Abs(x - Player.position.x) > Margin.x)
				x = Mathf.Lerp(x, Player.position.x, Smoothing.x * Time.deltaTime);

			if (Mathf.Abs(y - Player.position.y) > Margin.y)
				y = Mathf.Lerp(y, Player.position.y, Smoothing.y * Time.deltaTime);
		}

		var cameraHalfWidth = GetComponent<Camera>().orthographicSize * ((float) Screen.width / Screen.height);

		x = Mathf.Clamp(x, _min.x + cameraHalfWidth, _max.x - cameraHalfWidth);
		y = Mathf.Clamp(y, _min.y + GetComponent<Camera>().orthographicSize, _max.y - GetComponent<Camera>().orthographicSize);

		this.transform.position = new Vector3(x, y, this.transform.position.z);
	}
}
