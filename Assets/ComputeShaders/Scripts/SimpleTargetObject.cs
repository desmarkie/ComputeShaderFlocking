/*
 * Simple Target class for Boid Simulations
 */

using UnityEngine;
using System.Collections;

public class SimpleTargetObject : MonoBehaviour
{

	public Vector3 axisMagnitude = Vector3.zero;
	public Vector3 axisWavelength = Vector3.zero;
	private Vector3 axisValues = Vector3.zero;

	private Vector3 _startPosition = Vector3.zero;
	private Vector3 _currentPosition = Vector3.zero;
	public Vector3 currentPosition {
		get { return _currentPosition; }
		set { 
			_currentPosition = value;
			transform.localPosition = _currentPosition;
		}
	}

	public float radius {
		get { return transform.localScale.x * 0.5f; }

	}

	void Start()
	{
		_startPosition = _currentPosition = transform.localPosition;
	}

	void Update()
	{
		if(axisMagnitude != Vector3.zero && axisWavelength != Vector3.zero)
		{
			axisValues += axisWavelength;
			currentPosition = new Vector3(
				_startPosition.x + (Mathf.Sin(Mathf.Deg2Rad * axisValues.x) * axisMagnitude.x),
				_startPosition.y + (Mathf.Sin(Mathf.Deg2Rad * axisValues.y) * axisMagnitude.y),
				_startPosition.z + (Mathf.Sin(Mathf.Deg2Rad * axisValues.z) * axisMagnitude.z)
			);
		}
	}
}
