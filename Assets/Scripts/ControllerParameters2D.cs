﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class ControllerParameters2D
{
	public enum JumpBehavior
	{
		CanJumpOnGround,
		CanJumpAnywhere,
		CantJump
	}

	public Vector2 MaxVelocity = new Vector2(float.MaxValue, float.MaxValue);

	[Range(0, 90)]
	public float SlopeLimit = 30.0f;

	public float Gravity = -25.0f;

	public JumpBehavior JumpRestrictions;

	public float JumpFrequency = 0.25f;

	public float JumpMagnitude = 12.0f;
}
