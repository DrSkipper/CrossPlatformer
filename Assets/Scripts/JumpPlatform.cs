using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class JumpPlatform : MonoBehaviour
{
	public float JumpMagnitude = 20;

	public void ControllerEnter2D(CharacterController2D controller)
	{
		controller.SetVerticalForce(JumpMagnitude);
	}
}
