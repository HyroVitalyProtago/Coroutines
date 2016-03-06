using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Coroutines;

public class Test
	: MonoBehaviour
{
	Coroutines.Coroutine _Main;
		
	// Use this for initialization
	void Start ()
	{
		_Main = new Coroutines.Coroutine(Main());
	}
	
	// Update is called once per frame
	void Update ()
	{
		// Explicitly update the tree, we don't want these coroutines to
		// appear to work by magic. We'll make a new base class or utility
		// method to make coroutines that update automatically, like Unity's.
		_Main.Update();
	}

	IEnumerable<Instruction> Main()
	{
		//yield return ControlFlow.Call(TestCall());
		//yield return ControlFlow.Call(TestConcurrent());
		yield return ControlFlow.Call(TestMasterSlave());
		//yield return ControlFlow.Call(TestControlFlowReturnValue());
		//yield return ControlFlow.Call(TestBehaviourGraph());
	}

	IEnumerable<Instruction> TestCall()
	{
		// Do some stuff
		Debug.Log("Beginning test 1, frame #" + Time.frameCount);

		yield return null;

		Debug.Log("Next frame #" + Time.frameCount);

		yield return null;

		Debug.Log("Next frame #" + Time.frameCount);

		yield return Coroutines.Utils.WaitForFrames(30);

		Debug.Log("Finishing test 1, one second later, frame #" + Time.frameCount);
	}

	///---------------------------------------------------------------------------------------

	IEnumerable<Instruction> TestConcurrent()
	{
		// Do some stuff
		Debug.Log("Beginning test 2, frame #" + Time.frameCount);

		yield return ControlFlow.ConcurrentCall(TestConcurrentPart1(), TestConcurrentPart2());

		Debug.Log("Finishing test 2 frame #" + Time.frameCount);
	}

	IEnumerable<Instruction> TestConcurrentPart1()
	{
		Debug.Log("Beginning part 1, frame #" + Time.frameCount);

		yield return Coroutines.Utils.WaitForFrames(30);

		Debug.Log("Finishing part 1, one second later, frame #" + Time.frameCount);
	}

	IEnumerable<Instruction> TestConcurrentPart2()
	{
		Debug.Log("Beginning part 2, frame #" + Time.frameCount);

		yield return Coroutines.Utils.WaitForFrames(60);

		Debug.Log("Finishing part 2, two seconds later, frame #" + Time.frameCount);
	}

	///---------------------------------------------------------------------------------------

	IEnumerable<Instruction> TestMasterSlave()
	{
		// Do some stuff
		Debug.Log("Beginning test 3, frame #" + Time.frameCount);

		try
		{
			// Control execution of two slaves based on running state of master
			//yield return ControlFlow.ExecuteWhileRunning(TestMaster(), TestSlave(15), TestSlave(30));

			// Alternate syntax:
			yield return ControlFlow.ExecuteWhile(Utils.TrueWhileRunning(TestMaster()), running => running == true, TestSlave(15), TestSlave(30));

			// Control execution of two slaves based on value returned by master
			yield return ControlFlow.ExecuteWhile(TestMasterFloat(), val => val < 1.0f, TestSlave(5), TestSlave(10));
		}
		finally
		{
			Debug.Log("Finishing test 3 frame #" + Time.frameCount);
		}
	}

	IEnumerable<Instruction> TestMaster()
	{
		Debug.Log("Beginning master, frame #" + Time.frameCount);

		yield return Coroutines.Utils.WaitForFrames(90);

		Debug.Log("Finishing master, 3 seconds later, frame #" + Time.frameCount);
	}

	IEnumerable<Instruction<float>> TestMasterFloat()
	{
		Debug.Log("Beginning master float, frame #" + Time.frameCount);
		try
		{
			float currentValue = 0.0f;
			while (true)
			{
				yield return currentValue;
				yield return Coroutines.Utils.WaitForFrames(10);
				currentValue += 0.1f;
			}
		}
		finally
		{
			Debug.Log("Finishing master float, frame #" + Time.frameCount);
		}
	}

	IEnumerable<Instruction> TestSlave(int delay)
	{
		Debug.Log("Beginning slave " + delay + ", creating a new GameObject for fun, frame #" + Time.frameCount);
		var go = new GameObject("TestSlaveGO " + delay);
		try
		{
			while (true)
			{
				yield return Coroutines.Utils.WaitForFrames(delay);
				Debug.Log("slave " + delay + ", frame #" + Time.frameCount);
			}
		}
		finally
		{
			// Make sure this gets executed when the coroutine gets disposed!
			Debug.Log("Finishing slave, destroying gameobject " + go.name + ", frame #" + Time.frameCount);
			GameObject.Destroy(go);
		}
	}

	///---------------------------------------------------------------------------------------
	IEnumerable<Instruction> TestControlFlowReturnValue()
	{
		int generatorResult = 0;
		yield return ControlFlow.Call(WaitAndResultInt(res => generatorResult = res));
		Debug.Log("returned value is " + generatorResult);
	}

	IEnumerable<Instruction> WaitAndResultInt(System.Action<int> setReturnValue)
	{
		yield return null;

		yield return Coroutines.Utils.WaitForFrames(10);

		setReturnValue(2); // Clearer!
	}

	///---------------------------------------------------------------------------------------
	IEnumerable<Instruction> TestBehaviourGraph()
	{
		yield return ControlFlow.ExecuteWhileRunning(Wait30Frames(), TestBehaviourCodeUsedAsCoroutine());
	}

	IEnumerable<Instruction> Wait30Frames()
	{
		yield return Coroutines.Utils.WaitForFrames(30);
	}

	IEnumerable<Instruction> TestBehaviourCodeUsedAsCoroutine()
	{
		Debug.Log("Testing behaviour graph");
		// We're calling code that is normally used for behaviour graphs, we just ignore the return values...
		yield return ControlFlow.Call<Behaviours.BehaviourValue, Behaviours.BehaviourState>(SomeBehaviourGraphCode());
		Debug.Log("End of testing behaviour graph");
	}


	IEnumerable<Behaviours.BehaviourState> SomeBehaviourGraphCode()
	{
		Debug.Log("Starting behaviour graph node");

		yield return null;

		yield return Coroutines.Utils.WaitForFrames(10);

		yield return Behaviours.BehaviourValue.Active; // These get ignored...

		yield return Behaviours.BehaviourValue.Waiting;

		Debug.Log("Finishing behaviour graph node");
	}

	IEnumerable<Instruction> TestAdapter()
	{
		// Control execution of two slaves based on value returned by master
		yield return ControlFlow.ExecuteWhile(
			Utils.Adapt(TestMasterFloat(), floatVal => floatVal > 1.0f),
			boolVal => boolVal == true,
			TestSlave(5),
			TestSlave(10));

		// Alternate syntax
		yield return ControlFlow.ExecuteWhile(
			TestMasterFloat().Adapt(floatVal => floatVal > 1.0f),
			boolVal => boolVal == true,
			TestSlave(5),
			TestSlave(10));
	}
}
