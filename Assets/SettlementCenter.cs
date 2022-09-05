using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettlementCenter : MonoBehaviour
{
	public float      _buildCooldownDuration         = 1;
	public float      _attemptSpacing                = 1;
	public float      _attemptSpacingScalingPerHouse = 0.1f;
	public GameObject _housePrefab;

	List<House> Houses { get; set; }

	bool BuildCooldown { get; set; }

	void Update()
	{
		if (BuildCooldown)
		{
			return;
		}

		SpawnHouse();

		StartCoroutine(BuildCooldownCoroutine());
	}

	public void SpawnHouse()
	{
		var newHouse = Instantiate(_housePrefab, transform).GetComponent<House>();

		Houses ??= new();
		Houses.Add(newHouse);

		newHouse.transform.position = Vector3.up * (newHouse.transform.localScale.y / 2);

		var attemptSpacing = _attemptSpacing + Houses.Count * _attemptSpacingScalingPerHouse;
		newHouse.Init();
		newHouse.Position(Houses, attemptSpacing);
	}

	IEnumerator BuildCooldownCoroutine()
	{
		BuildCooldown = true;

		yield return new WaitForSeconds(_buildCooldownDuration);
		BuildCooldown = false;
	}
}