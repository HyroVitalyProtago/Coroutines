using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Coroutines;

public class Projectile
	: MonoBehaviour
{
	public Vector3 InitialForce;

	Coroutines.Coroutine _Main;

	// Use this for initialization
	void Start ()
	{
		_Main = new Coroutines.Coroutine(Main());
	}
	
	// Update is called once per frame
	void Update ()
	{
		_Main.Update();
	}

	System.Action<Collision> _OnCollisionEnter;
	void OnCollisionEnter(Collision col)
	{
		if (_OnCollisionEnter != null)
		{
			_OnCollisionEnter(col);
		}
	}

	IEnumerable<Instruction> Main()
	{
		// Apply an initial force
		Rigidbody rb = GetComponent<Rigidbody>();
		rb.AddForce(InitialForce, ForceMode.Acceleration);

		while (true)
		{
			Collider collidedWith = null;
			_OnCollisionEnter = col => collidedWith = col.collider;
			try
			{
				while (collidedWith == null)
				{
					yield return null;
				}
			}
			finally
			{
				_OnCollisionEnter = null;
			}

			if (collidedWith.gameObject.CompareTag("Player"))
			{
				Debug.Log("Pushing Player");
			}
			else if (collidedWith.gameObject.CompareTag("Wall"))
			{
				Destroy(gameObject);
			}
			// Else just bounce/roll/etc...
		}
	}
}
