using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class House : MonoBehaviour
{
	public int _maxRepositionAttempts = 100;

	int     UpgradeLevel   { get; set; }
	float   InitialWidth   { get; set; }
	Vector3 GroundPosition { get; set; }

	float UpgradeCandidacy
	{
		get => 1 / ((1 * UpgradeLevel + 4 * Vector3.Distance(transform.position, transform.parent.position)) * Random.Range(-1f, 1f));
	}

	public void Init()
	{
		UpgradeLevel  = 1;
		InitialWidth  = transform.localScale.x;

		var randomColor = Color.HSVToRGB(Random.Range(0f, 0.2f), Random.Range(0f, 0.4f), Random.Range(0.5f, 0.9f));
		GetComponent<Renderer>().material.color = randomColor;
	}

	public void Position(List<House> houses, float attemptSpacing)
	{
		if (houses.Count <= 1)
		{
			CheckGround();
			Align();
			return;
		}

		var thisHouseCollider = GetComponent<BoxCollider>();

		var otherHousesColliders = houses
								  .Where(h => h != this)
								  .Select(h => h.GetComponent<BoxCollider>())
								  .ToList();

		for (var repositionAttempt = 0; repositionAttempt < _maxRepositionAttempts; repositionAttempt++)
		{
			var repositioned = false;

			foreach (var house in otherHousesColliders)
			{
				if (thisHouseCollider.bounds.Intersects(house.bounds))
				{
					repositioned = true;
					transform.position += new Vector3(Random.Range(-attemptSpacing, attemptSpacing),
													  0,
													  Random.Range(-attemptSpacing, attemptSpacing));
					Physics.SyncTransforms();
				}
			}

			// break because this house didn't intersect with any other house
			if (!repositioned)
			{
				// but only break if there's actually ground underneath
				if (CheckGround())
				{
					Align();
					break;
				}
			}

			// upgrade instead if we've tried too many times
			if (repositionAttempt == _maxRepositionAttempts - 1)
			{
				var houseToUpgrade = houses
									.Where(h => h != this)
									.OrderBy(h => h.UpgradeCandidacy)
									.Last();
				houseToUpgrade.Upgrade(houses, attemptSpacing);

				Deconstruct(houses);

				break;
			}
		}
	}

	bool CheckGround()
	{
		if (!Physics.Raycast(transform.position + Vector3.up * 10000, Vector3.down, out var hit))
		{
			return false;
		}

		GroundPosition = hit.point;

		return true;

	}

	void Align()
	{
		var newWidth = InitialWidth * (1 + 0.1f * UpgradeLevel);
		var newY     = (UpgradeLevel * 4) - (transform.localScale.y / 2);

		transform.localScale = new(newWidth * (1+Random.Range(-0.2f, 0.2f)), transform.localScale.y, newWidth *
									   (1 + Random.Range(-0.2f, 0.2f)));
		transform.position   = new(GroundPosition.x, GroundPosition.y + newY, GroundPosition.z);
		transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
	}

	void RespawnOverlappingHouses(List<House> houses)
	{
		// respawn overlapping houses
		var otherHouses = houses.Where(h => h != this).ToList();
		for (var i = 0; i < otherHouses.Count; i++)
		{
			var house = otherHouses[i];
			if (GetComponent<BoxCollider>().bounds.Intersects(house.GetComponent<BoxCollider>().bounds))
			{
				var levelsInDeconstructedHouse = house.UpgradeLevel;

				house.Deconstruct(houses);

				for (var j = 0; j < levelsInDeconstructedHouse; j++)
				{
					GetComponentInParent<SettlementCenter>().SpawnHouse();
				}
			}
		}
	}

	void Upgrade(List<House> houses, float attemptSpacing, int levelsToGain = 1)
	{
		UpgradeLevel += levelsToGain;

		Align();

		RespawnOverlappingHouses(houses);
	}

	void Deconstruct(List<House> houses)
	{
		houses.Remove(this);
		Destroy(gameObject);
	}
}