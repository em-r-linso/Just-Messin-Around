using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetPlanner : MonoBehaviour
{
	void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(transform.position, 10);
	}
}