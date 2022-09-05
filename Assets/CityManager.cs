using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CityManager : MonoBehaviour
{
	[field: SerializeField] float MaxPoiConnectionDistance   { get; set; }
	[field: SerializeField] float MinIntersectionDistance    { get; set; }
	[field: SerializeField] float MaxPoiConnections          { get; set; }
	[field: SerializeField] float MaxIntersectionConnections { get; set; }

	Vector3[]                             Pois                { get; set; }
	Dictionary<(Vector3, Vector3), float> PoiConnections      { get; set; }
	Dictionary<Vector3, int>              PoiConnectionsCount { get; set; }
	List<Vector3>                         RoadIntersections   { get; set; }

	void Start()
	{
		CreateRoadLayout();
	}

	void Update()
	{
		CreateRoadLayout();
	}

	void CreateRoadLayout()
	{
		// get POI positions
		Pois = GetComponentsInChildren<POI>().Select(p => p.transform.position).ToArray();

		// create connections between POIs
		PoiConnections      = new();
		PoiConnectionsCount = new();
		for (var i = 0; i < Pois.Length; i++)
		{
			for (var j = i + 1; j < Pois.Length; j++)
			{
				var poi1 = Pois[i];
				var poi2 = Pois[j];

				var distance = Vector3.Distance(poi1, poi2);

				PoiConnections.Add((poi1, poi2), distance);
				PoiConnectionsCount[poi1] = PoiConnectionsCount.GetValueOrDefault(poi1) + 1;
				PoiConnectionsCount[poi2] = PoiConnectionsCount.GetValueOrDefault(poi2) + 1;
			}
		}

		// order connections by distance (high to low)
		var orderedConnections = PoiConnections.OrderByDescending(c => c.Value).ToArray();

		// create list of connection pairs that result in intersecting lines
		var intersections =
			new List<((Vector3, Vector3), (Vector3, Vector3))>(); // NOTE: always ordered by distance (shortest first)
		for (var i = 0; i < orderedConnections.Length; i++)
		{
			for (var j = i + 1; j < orderedConnections.Length; j++)
			{
				// get the lines involved
				var connection1 = orderedConnections.ElementAt(i);
				var connection2 = orderedConnections.ElementAt(j);
				var lineA = new[]
				{
					new[] { connection1.Key.Item1.x, connection1.Key.Item1.z },
					new[] { connection1.Key.Item2.x, connection1.Key.Item2.z }
				};
				var lineB = new[]
				{
					new[] { connection2.Key.Item1.x, connection2.Key.Item1.z },
					new[] { connection2.Key.Item2.x, connection2.Key.Item2.z }
				};

				// if the lines share any points, skip
				if (lineA[0].SequenceEqual(lineB[0]) ||
					lineA[0].SequenceEqual(lineB[1]) ||
					lineA[1].SequenceEqual(lineB[0]) ||
					lineA[1].SequenceEqual(lineB[1]))
				{
					continue;
				}

				// check if the lines intersect
				var dx0            = lineA[1][0]                       - lineA[0][0];
				var dx1            = lineB[1][0]                       - lineB[0][0];
				var dy0            = lineA[1][1]                       - lineA[0][1];
				var dy1            = lineB[1][1]                       - lineB[0][1];
				var p0             = dy1 * (lineB[1][0] - lineA[0][0]) - dx1 * (lineB[1][1] - lineA[0][1]);
				var p1             = dy1 * (lineB[1][0] - lineA[1][0]) - dx1 * (lineB[1][1] - lineA[1][1]);
				var p2             = dy0 * (lineA[1][0] - lineB[0][0]) - dx0 * (lineA[1][1] - lineB[0][1]);
				var p3             = dy0 * (lineA[1][0] - lineB[1][0]) - dx0 * (lineA[1][1] - lineB[1][1]);
				var isIntersecting = p0 * p1 <= 0 && p2 * p3 <= 0;

				// if the lines intersect, add the pair to the list, such that the first item is the line with the lower distance
				if (isIntersecting)
				{
					intersections.Add(connection1.Value < connection2.Value
										  ? (connection1.Key, connection2.Key)
										  : (connection2.Key, connection1.Key));
				}
			}
		}

		// remove connections that are causing intersections
		while (intersections.Count > 0)
		{
			// remove the offending connection
			var connection2 = intersections[0].Item2;
			PoiConnections.Remove(connection2);
			PoiConnectionsCount[connection2.Item1] -= 1;
			PoiConnectionsCount[connection2.Item2] -= 1;

			// remove any intersections that involve the removed connection
			intersections.RemoveAll(i => i.Item1 == connection2 || i.Item2 == connection2);
		}

		// order connections by distance (high to low)
		// TODO: redundant?
		orderedConnections = PoiConnections.OrderByDescending(c => c.Value).ToArray();

		var removedConnections = new Dictionary<(Vector3, Vector3), float>();

		foreach (var ((a, b), d) in orderedConnections)
		{
			if (
				// DON'T remove if either POI has only one connection left
				PoiConnectionsCount[a] > 1 &&
				PoiConnectionsCount[b] > 1 &&

				// DON'T remove if doing so would orphan a POI
				!CheckIfOrphanedWithout(a, b) &&
				!CheckIfOrphanedWithout(b, a) &&
				(
					// DO remove if the connection is too long
					d > MaxPoiConnectionDistance ||

					// DO remove if there are too many connections on one of the POIs
					PoiConnectionsCount[a] > MaxPoiConnections ||
					PoiConnectionsCount[b] > MaxPoiConnections
				)
			)
			{
				// it's safe to remove this connection, so GET OUT OF HERE!
				PoiConnections.Remove((a, b));
				PoiConnectionsCount[a]--;
				PoiConnectionsCount[b]--;

				// save the removed connection for later if it's not too long
				if (d <= MaxPoiConnectionDistance)
				{
					removedConnections.Add((a, b), d);
				}
			}
		}

		// add back connections that won't put POIs over their connection limits (starting with shortest)
		orderedConnections = removedConnections.OrderBy(c => c.Value).ToArray();
		foreach (var ((a, b), d) in orderedConnections)
		{
			// if neither POI has too many connections, add the connection back
			if (PoiConnectionsCount[a] < MaxPoiConnections && PoiConnectionsCount[b] < MaxPoiConnections)
			{
				PoiConnections.Add((a, b), d);
				PoiConnectionsCount[a]++;
				PoiConnectionsCount[b]++;
			}
		}

		// add road intersections along connections
		RoadIntersections = new();
		foreach (var ((a, b), d) in PoiConnections)
		{
			var numIntersections = (int)(d / MinIntersectionDistance);
			for (var i = 1; i <= numIntersections; i++)
			{
				var intersection = Vector3.Lerp(a, b, i / (float)(numIntersections + 1));
				RoadIntersections.Add(intersection);
			}
		}
	}

	void OnDrawGizmos()
	{
		// only execute in play mode
		if (!Application.isPlaying)
		{
			return;
		}

		if (RoadIntersections != null)
		{
			foreach (var intersection in RoadIntersections)
			{
				Gizmos.color = Color.white;
				Gizmos.DrawSphere(intersection, 1f);
			}
		}

		if (PoiConnections != null)
		{
			foreach (var ((a,b), _) in PoiConnections)
			{
				Gizmos.color = Color.white;
				Gizmos.DrawLine(a, b);
			}
		}
	}

	// make sure this wouldn't orphan a
	// (we do this by checking if the graph is complete WITHOUT the connection between a & b)
	bool CheckIfOrphanedWithout(Vector3 a, Vector3 b)
	{
		var visited = new List<Vector3>();
		var toVisit = new Stack<Vector3>();

		// start at a
		toVisit.Push(a);

		// while there are still nodes to visit
		while (toVisit.Count > 0)
		{
			// get the next node
			var v = toVisit.Pop();

			// if we've already visited this node, skip
			if (visited.Contains(v))
			{
				continue;
			}

			// add this node to the list of visited nodes
			visited.Add(v);

			// add all neighbors to the list of nodes to visit
			var neighbors = PoiConnections
						   .Where(c => c.Key != (a, b) &&
									   c.Key != (b, a)) // ignore the connection we're trying to remove
						   .Where(c => c.Key.Item1 == v || c.Key.Item2 == v) // where one is v
						   .Select(c => c.Key.Item1 == v
											? c.Key.Item2
											: c.Key.Item1) // get the one that isn't v
						   .ToList();
			foreach (var n in neighbors)
			{
				toVisit.Push(n);
			}
		}

		// if every node except b was visited, then this wouldn't orphan a
		return visited.Count < Pois.Length - 1;
	}
}