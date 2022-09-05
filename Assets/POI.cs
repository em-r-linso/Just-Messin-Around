using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POI : MonoBehaviour
{
	void OnDrawGizmos()
	{
		// only execute in editor mode
		if (Application.isPlaying) return;

		Gizmos.color = Color.white;
		Gizmos.DrawSphere(transform.position, 1);
	}
}