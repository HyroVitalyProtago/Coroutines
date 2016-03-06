using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Coroutines;
using System;

namespace Behaviours
{
	/// <summary>
	/// Basic Behaviour node, built around a coroutine, of course
	/// </summary>
	public abstract class IBehaviourNode
		: State<BehaviourValue>
	{
		public override bool HasValue
		{
			get
			{
				// Behaviour node always has a value
				// If the underlying node doesn't then it's 'Waiting'!
				return true;
			}
		}

		// In essence we're just renaming this variable to what it really is in Behaviour nodes!
		public override BehaviourValue Value
		{
			get
			{
				return State;
			}
		}

		public abstract BehaviourValue State
		{
			get;
		}
	}

	public class BehaviourCoroutine
		: IBehaviourNode
	{
		// Extend by composition
		StateCoroutine<BehaviourValue> _Subroutine;

		// Stores the 'last' value returned by the coroutine, if any
		public override BehaviourValue State
		{
			get
			{
				if (_Subroutine.HasValue)
				{
					return _Subroutine.Value;
				}
				else
				{
					return BehaviourValue.Waiting;
				}
			}
		}

		public BehaviourCoroutine(IEnumerable<BehaviourState> behaviourSource)
		{
			_Subroutine = new StateCoroutine<BehaviourValue>(behaviourSource.Cast<Instruction<BehaviourValue>>());
		}

		public override void Update()
		{
			// Update the subroutine once
			_Subroutine.Update();
		}

		public override void Dispose()
		{
			_Subroutine.Dispose();
			_Subroutine = null;
		}

		public override void Reset()
		{
			// Pass it on!
			_Subroutine.Reset();
		}
	}

	/// <summary>
	/// Always attempts the first valid node in the list, interrupting lower-priority nodes if necessary
	/// </summary>
	public class FixedPriorityNode
		: IBehaviourNode
	{
		// This stores all the sub behaviours
		List<IBehaviourNode> _SubBehaviours;

		// This stores the last running subroutine, so we remember to reset it
		// if it gets interrupted by a higher priority subroutine
		int _LastActiveSubroutineIndex;

		/// <summary>
		/// Constructor, notice that the order of the sub-behaviours matters, the first subroutine will
		/// have highest priority, and the second will only be updated if the first one is 'waiting'
		/// </summary>
		public FixedPriorityNode(IEnumerable<IBehaviourNode> subBehaviours)
		{
			// Create our list, and initialize the running one with the behaviours we get passed
			_SubBehaviours = new List<IBehaviourNode>(subBehaviours);
			_LastActiveSubroutineIndex = -1;
		}

		/// <summary>
		/// Helper constructor, to manually pass arbitrary number of children
		/// </summary>
		public FixedPriorityNode(params IBehaviourNode[] subBehaviours)
			: this(subBehaviours as IEnumerable<IBehaviourNode>)
		{
		}

		/// <summary>
		/// Returns the state of this node, based on whether a child node is active
		/// </summary>
		public override BehaviourValue State
		{
			get
			{
				if (_LastActiveSubroutineIndex == -1)
				{
					// If no child is active, the node itself is waiting
					return BehaviourValue.Waiting;
				}
				else
				{
					// We one child is active, the node is active
					return BehaviourValue.Active;
				}
			}
		}

		public override void Update()
		{
			// Process (remaining) coroutines, one after the other, until one returns that it is active
			int newActiveIndex = -1;
			for (int i = 0; i < _SubBehaviours.Count; ++i)
			{
				var subroutine = _SubBehaviours[i];

				// Update the subroutine
				subroutine.Update();

				// If the coroutine isn't itself waiting for something, then it's our new active coroutine
				if (subroutine.Value == BehaviourValue.Active)
				{
					// We don't need to look at any more coroutines
					newActiveIndex = i;
					break;
				}
				// Else the subroutine isn't active, keep going
			}

			if (newActiveIndex != -1)
			{
				// Do we need to reset a previously executing subroutine?
				// Only if it had a lower priority, otherwise the subroutine has already been
				// looked at and set itself to a 'waiting' state.
				if (newActiveIndex < _LastActiveSubroutineIndex)
				{
					_SubBehaviours[_LastActiveSubroutineIndex].Reset();
				}
			}
			// Else we didn't find a new acive subroutine, we don't need to reset anyone

			// Remember the last active subroutine
			_LastActiveSubroutineIndex = newActiveIndex;
		}

		public override void Dispose()
		{
			// Clean up
			if (_SubBehaviours != null)
			{
				foreach (var subroutine in _SubBehaviours)
				{
					subroutine.Dispose();
				}
				_SubBehaviours = null;
			}
			_LastActiveSubroutineIndex = -1;
		}

		public override void Reset()
		{
			foreach (var subroutine in _SubBehaviours)
			{
				subroutine.Reset();
			}
			_LastActiveSubroutineIndex = -1;
		}
	}

	public struct PriorityNodeResult
	{
		public float Priority;
		public IEnumerable<BehaviourValue> Behaviour;
	}

	///// <summary>
	///// Always attempts the first valid node in the list, interrupting lower-priority nodes if necessary
	///// </summary>
	//public class PriorityNode
	//	: IBehaviourNode
	//{
	//	// This stores all the priority-computing nodes
	//	List<State<PriorityNodeResult>> _Priorities;

	//	// When one of the priority-computing nodes 'wins' we use its returned value to create an active behaviour
	//	BehaviourCoroutine _CurrentActiveNode;

	//	// This stores the last running subroutine, so we remember to reset it
	//	// if it gets interrupted by a higher priority subroutine
	//	int _CurrentActiveNodeIndex;

	//	/// <summary>
	//	/// Constructor, notice that the order of the sub-behaviours matters, the first subroutine will
	//	/// have highest priority, and the second will only be updated if the first one is 'waiting'
	//	/// </summary>
	//	public PriorityNode(IEnumerable<State<PriorityNodeResult>> priorities)
	//	{
	//		// Create our list, and initialize the running one with the behaviours we get passed
	//		_Priorities = new List<State<PriorityNodeResult>>(priorities);
	//		_CurrentActiveNode = null;
	//		_CurrentActiveNodeIndex = -1;
	//	}

	//	/// <summary>
	//	/// Helper constructor, to manually pass arbitrary number of children
	//	/// </summary>
	//	public PriorityNode(params State<PriorityNodeResult>[] subBehaviours)
	//		: this(subBehaviours as IEnumerable<State<PriorityNodeResult>>)
	//	{
	//	}

	//	/// <summary>
	//	/// Returns the state of this node, based on whether a child node is active
	//	/// </summary>
	//	public override BehaviourValue State
	//	{
	//		get
	//		{
	//			if (_CurrentActiveNodeIndex == -1)
	//			{
	//				// If no child is active, the node itself is waiting
	//				return BehaviourValue.Waiting;
	//			}
	//			else
	//			{
	//				// Pass through the state of the child
	//				return _CurrentActiveNode.State;
	//			}
	//		}
	//	}

	//	public override void Update()
	//	{
	//		// Check the priority on all the sub nodes
	//		int newActiveIndex = -1;
	//		float highestPriority = float.MinValue;
	//		for (int i = 0; i < _Priorities.Count; ++i)
	//		{
	//			var subroutine = _Priorities[i];

	//			// Update the priority-computing state
	//			subroutine.Update();

	//			float priority = subroutine.Value.Priority;
	//			if (priority > highestPriority)
	//			{
	//				highestPriority = priority;
	//				newActiveIndex = i;
	//			}
	//		}

	//		if (newActiveIndex != _CurrentActiveNodeIndex)
	//		{
	//			// Kill the last active node if necessary
	//			if (_CurrentActiveNodeIndex != -1)
	//			{
	//				_CurrentActiveNode.Dispose();
	//				_CurrentActiveNode = null;
	//			}

	//			// Remember the last active subroutine
	//			_CurrentActiveNodeIndex = newActiveIndex;

	//			// Create the new node
	//			if (_CurrentActiveNodeIndex != -1)
	//			{
	//				_CurrentActiveNode = new BehaviourCoroutine(_Priorities[_CurrentActiveNodeIndex].Value.Behaviour)
	//			}
	//		}

	//		if (newActiveIndex != -1)
	//		{
	//			// Do we need to reset a previously executing subroutine?
	//			// Only if it had a lower priority, otherwise the subroutine has already been
	//			// looked at and set itself to a 'waiting' state.
	//			if (_CurrentActiveNodeIndex != -1 && _CurrentActiveNodeIndex)
	//			{
	//				_SubBehaviours[_CurrentActiveNodeIndex].Reset();
	//			}
	//		}
	//		// Else we didn't find a new acive subroutine, we don't need to reset anyone
	//	}

	//	public override void Dispose()
	//	{
	//		// Clean up
	//		if (_SubBehaviours != null)
	//		{
	//			foreach (var subroutine in _SubBehaviours)
	//			{
	//				subroutine.Dispose();
	//			}
	//			_SubBehaviours = null;
	//		}
	//		_CurrentActiveNodeIndex = -1;
	//	}

	//	public override void Reset()
	//	{
	//		foreach (var subroutine in _SubBehaviours)
	//		{
	//			subroutine.Reset();
	//		}
	//		_CurrentActiveNodeIndex = -1;
	//	}
	//}



	public class ConcurrentNode
		: IBehaviourNode
	{
		// Extend by composition
		Concurrent<BehaviourValue> _ConcurrentNode;

		/// <summary>
		/// Constructor, pass in all the nodes you wish to execute concurrently,
		/// along with a function to sort out the state of this node.
		/// </summary>
		public ConcurrentNode(System.Func<IEnumerable<BehaviourValue>, BehaviourValue> stateResolutionFunc, IEnumerable<IBehaviourNode> subBehaviours)
		{
			// Pass through
			_ConcurrentNode = new Concurrent<BehaviourValue>(stateResolutionFunc, subBehaviours.Cast<ICoroutine<BehaviourValue>>());
		}

		/// <summary>
		/// Constructor, pass in all the nodes you wish to execute concurrently,
		/// along with a function to sort out the state of this node.
		/// </summary>
		public ConcurrentNode(System.Func<IEnumerable<BehaviourValue>, BehaviourValue> stateResolutionFunc, params IBehaviourNode[] subBehaviours)
			: this(stateResolutionFunc, subBehaviours as IEnumerable<IBehaviourNode>)
		{
		}

		/// <summary>
		/// Returns the resolved state of this node, based on the child nodes
		/// </summary>
		public override BehaviourValue State
		{
			get
			{
				// The concurrent node always has a value, because we know that its children are all behaviours and
				// so always have a value themselves.
				return _ConcurrentNode.Value;
			}
		}

		public override void Update()
		{
			_ConcurrentNode.Update();
		}

		public override void Dispose()
		{
			if (_ConcurrentNode != null)
			{
				_ConcurrentNode.Dispose();
				_ConcurrentNode = null;
			}
		}

		public override void Reset()
		{
			_ConcurrentNode.Reset();
		}
	}

}
