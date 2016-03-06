using UnityEngine;
using System.Collections;

public class TankController
	: MonoBehaviour
{
	[SerializeField]
	float _MaxSpeed = 3.0f;

	[SerializeField]
	float _MaxAcceleration = 10.0f;

	Rigidbody _RigidBody;

	// Use this for initialization
	void Start()
	{
		_RigidBody = GetComponent<Rigidbody>();
		_RigidBody.isKinematic = false;
	}

	// Update is called once per frame
	void Update()
	{

	}

	void FixedUpdate()
	{
		Vector3 targetVel = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical")) * _MaxSpeed;

		if (targetVel.sqrMagnitude > 0.001f)
		{
			Vector3 currentVel = _RigidBody.velocity;

			Vector3 acc = (targetVel - currentVel) / Time.fixedDeltaTime;
			if (acc.magnitude > _MaxAcceleration)
			{
				acc.Normalize();
				acc *= _MaxAcceleration;
			}

			// Apply force (F = mA)
			_RigidBody.AddForce(acc, ForceMode.Acceleration);

			//_RigidBody.velocity = targetVel;
			if (currentVel.sqrMagnitude > 0.001f)
			{
				_RigidBody.MoveRotation(Quaternion.LookRotation(currentVel, Vector3.up));
			}
		}
	}
}
