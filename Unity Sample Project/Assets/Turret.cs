using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Coroutines;
using System.Linq;

public class Turret
	: MonoBehaviour
{
	[SerializeField]
	float _LockOnRadius = 3.0f;

	[SerializeField]
	float _LockOffRadius = 4.0f;

	[SerializeField]
	float _MaxRotationSpeed = 90.0f;

	[SerializeField]
	float _RandomLookRotationSpeed = 90.0f;

	[SerializeField]
	float _ProjectileInitialVelocity = 2.0f;

	[SerializeField]
	Projectile _ProjectilePrefab;

	[SerializeField]
	Transform _ProjectileExit;

	[SerializeField]
	Light _TrackingLight;

	[SerializeField]
	Renderer Mohawk;

	Coroutines.Coroutine _Main;

	// Use this for initialization
	void Start()
	{
		_Main = new Coroutines.Coroutine(Main());
	}

	// Update is called once per frame
	void Update()
	{
		// Just tick our root coroutine
		_Main.Update();
	}

	// Root Coroutine
	IEnumerable<Instruction> Main()
	{
		// Save the initial orientation, we'll pass it to the Idle subroutine
		float startYAngle = transform.rotation.eulerAngles.y;

		while (true)
		{
			// Look for a target
			Transform target = null;

			yield return ControlFlow.ExecuteWhileRunning(
				// This is the master, it looks for a target, puts it in 'target' before completing
				FindTargetInRadius(_LockOnRadius, trgt => target = trgt),

				// This happens for as long as we're looking for a target
				Idle(startYAngle));

			// Track the target and shoot at it simultaneously
			if (target != null)
			{
				yield return ControlFlow.ExecuteWhile(
					// Condition
					() => Vector3.Distance(target.position, transform.position) < _LockOffRadius,

					// Is executed while the condition is true, tracks the target
					TrackTarget(target),

					// Also executed while the condition is true, Fires projectiles
					FireProjectiles());
			}
		}
	}

	IEnumerable<Instruction> FindTargetInRadius(float radius, System.Action<Transform> targetFound)
	{
		// For now there is only a single potential target, so...
		var playerObj = GameObject.FindGameObjectWithTag("Player");
		if (playerObj != null)
		{
			while (Vector3.Distance(playerObj.transform.position, transform.position) > radius)
			{
				yield return null;
			}

			// Got it!
			targetFound(playerObj.transform);
		}
		// Else maybe we should warn...
	}

	IEnumerable<Instruction> TrackTarget(Transform target)
	{
		// While we track the player, Turn our tracking light on
		_TrackingLight.enabled = true;
		try
		{
			// Constantly track the target
			while (true)
			{
				Quaternion targetRot = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _MaxRotationSpeed * Time.deltaTime);
				yield return null;
			}
		}
		finally
		{
			// If we get interrupted (player left the tacking area), turn off the light
			_TrackingLight.enabled = false;
		}
	}

	IEnumerable<Instruction> FireProjectiles()
	{
		while (true)
		{
			// Create a projectile, move it and give it an impulse forward!
			var proj = GameObject.Instantiate<Projectile>(_ProjectilePrefab);
			proj.transform.position = _ProjectileExit.position;
			proj.InitialForce = transform.forward * _ProjectileInitialVelocity;

			// Wait before firing next projectile
			yield return Utils.WaitForSeconds(1.0f);
		}
	}

	IEnumerable<Instruction<bool>> ShouldFireProjectiles(Transform target, float initialAngle)
	{
		System.Func<IEnumerable<float>, float> ScoreArbitration = (scores) =>
		{
			return scores.Min();
		};

		System.Func<float, bool> ScoreThreshold = (combinedScore) =>
		{
			return combinedScore > 0.5f;
		};

		yield return Utils.Adapt(
			ControlFlow.ConcurrentCall(
				ScoreArbitration,
				ScoreDistance(target),
				ScoreAngle(initialAngle, target)),
			ScoreThreshold);
	}

	IEnumerable<Instruction> Idle(float defaultAngle)
	{
		// Rotate back to default position
		yield return ControlFlow.Call(RotateTo(defaultAngle, _RandomLookRotationSpeed));

		// Then blink our mohawk
		try
		{
			MaterialPropertyBlock block = new MaterialPropertyBlock();
			while (true)
			{
				block.SetColor("_EmissionColor", Color.red);
				Mohawk.SetPropertyBlock(block);
				yield return Utils.WaitForSeconds(0.5f);
				block.SetColor("_EmissionColor", Color.white);
				Mohawk.SetPropertyBlock(block);
				yield return Utils.WaitForSeconds(0.5f);
			}
		}
		finally
		{
			// Reset the mohawk
			Mohawk.SetPropertyBlock(null);
		}
	}

	IEnumerable<Instruction> RotateTo(float targetYAngle, float rotationSpeed)
	{
		// Extract current euler angles
		float startYAngle = transform.rotation.eulerAngles.y;

		// Wrap if necessary
		float deltaYAngle = targetYAngle - startYAngle;
		if (deltaYAngle < -180.0f)
		{
			deltaYAngle += 360.0f;
		}
		else if (deltaYAngle > 180.0f)
		{
			deltaYAngle -= 360.0f;
		}

		if (deltaYAngle > 0.0f)
		{
			float currentDelta = rotationSpeed * Time.deltaTime;
			while (currentDelta < deltaYAngle)
			{
				transform.rotation = Quaternion.Euler(0.0f, startYAngle + currentDelta, 0.0f);
				yield return null;
				currentDelta += rotationSpeed * Time.deltaTime;
			}
			transform.rotation = Quaternion.Euler(0.0f, targetYAngle, 0.0f);
		}
		else
		{
			float currentDelta = -rotationSpeed * Time.deltaTime;
			while (currentDelta > deltaYAngle)
			{
				transform.rotation = Quaternion.Euler(0.0f, startYAngle + currentDelta, 0.0f);
				yield return null;
				currentDelta -= rotationSpeed * Time.deltaTime;
			}
			transform.rotation = Quaternion.Euler(0.0f, targetYAngle, 0.0f);
		}
	}

	IEnumerable<Instruction<float>> ScoreDistance(Transform target)
	{
		while (true)
		{
			float distance = Vector3.Distance(target.position, transform.position);
			if (distance < _LockOffRadius)
			{
				yield return 1.0f - (distance / _LockOffRadius);
			}
			else
			{
				yield return 0.0f;
			}
		}
	}

	IEnumerable<Instruction<float>> ScoreAngle(float initialAngle, Transform target)
	{
		while (true)
		{
			Vector3 deltaToTarget = target.position - transform.position;
			float angleToTarget = Mathf.Atan2(deltaToTarget.z, deltaToTarget.x) * Mathf.Rad2Deg;
			float deltaAngle = Mathf.Abs(Mathf.DeltaAngle(initialAngle, angleToTarget));
			yield return 1.0f - deltaAngle / 180.0f;

			// For no reason other than to show it off, wait!
			yield return Utils.WaitForSeconds(0.5f);
		}
	}
}
