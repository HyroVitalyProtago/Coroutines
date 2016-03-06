using UnityEngine;
using System.Collections;

public class FollowTarget : MonoBehaviour
{
	[SerializeField]
	GameObject _Target;

	Vector3 _Offset;

	// Use this for initialization
	void Start ()
	{
		_Offset = transform.position - _Target.transform.position;
	}
	
	// Update is called once per frame
	void LateUpdate ()
	{
		transform.position = _Target.transform.position + _Offset;
	}
}
