using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Coroutines;
using Behaviours;

public class TestBehaviour
	: MonoBehaviour
{
	IBehaviourNode _Main;

	// Use this for initialization
	void Start ()
	{
		_Main = new ConcurrentNode(
						Utils.FirstOrDefault<BehaviourValue>,
						new FixedPriorityNode(
							new BehaviourCoroutine(SomeCoroutine()),
							new BehaviourCoroutine(SomeOtherCoroutine())),
						new BehaviourCoroutine(SomeThirdCoroutine()));
	}
	
	// Update is called once per frame
	void Update ()
	{
		_Main.Update();
	}

	IEnumerable<BehaviourState> SomeCoroutine()
	{
		Debug.Log("SomeCoroutine starting");
		try
		{
			while (true)
			{
				yield return BehaviourValue.Active;
				yield return Utils.WaitForFrames(10);
				Debug.Log("Some coroutine doing stuff 1");
				yield return BehaviourValue.Waiting;
				yield return Utils.WaitForFrames(60);
				Debug.Log("Some coroutine doing stuff 2");
			}
		}
		finally
		{
			Debug.Log("SomeCoroutine cleaning up");
		}
	}

	IEnumerable<BehaviourState> SomeOtherCoroutine()
	{
		Debug.Log("SomeOtherCoroutine starting");
		try
		{
			yield return BehaviourValue.Active;
			yield return Utils.WaitForFrames(30);
			yield return BehaviourValue.Waiting;
		}
		finally
		{
			Debug.Log("SomeOtherCoroutine cleaning up");
		}
		// Because this is used as a behaviour node, it will implicitly loop forever!
	}

	IEnumerable<BehaviourState> SomeThirdCoroutine()
	{
		Debug.Log("SomeThirdCoroutine starting");
		try
		{
			yield return Utils.WaitForFrames(30);
			yield return BehaviourValue.Active;
			yield return Utils.WaitForFrames(10);
			yield return BehaviourValue.Waiting;
		}
		finally
		{
			Debug.Log("SomeThirdCoroutine cleaning up");
		}
		// Because this is used as a behaviour node, it will implicitly loop forever!
	}
}
