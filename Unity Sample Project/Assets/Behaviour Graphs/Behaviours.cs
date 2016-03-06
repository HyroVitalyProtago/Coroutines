using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Coroutines;
using Coroutines.Instructions;
using System;

namespace Behaviours
{
	public enum BehaviourValue
	{
		// Means the node is actively doing something
		Active,

		// Means the node is waiting, and only processing so that it can 'silently' check things
		// (like conditions for triggering, distances, etc...)
		Waiting
	}

	public class BehaviourState
		: Instruction<BehaviourValue>
	{
		public BehaviourState()
			: base()
		{
		}

		public BehaviourState(BehaviourValue returnValue)
			: base(returnValue)
		{
		}

		public BehaviourState(CallInstruction instruction)
			: base(instruction)
		{
		}

		public static implicit operator BehaviourState(CallInstruction instruction)
		{
			return new BehaviourState(instruction);
		}

		public static implicit operator BehaviourState(BehaviourValue result)
		{
			return new BehaviourState(result);
		}
	}
}