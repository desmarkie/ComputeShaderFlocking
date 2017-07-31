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
		get {
			if (_parentTarget == null) return _currentPosition;
			return _parentTarget.transform.localPosition + _currentPosition;
		}
		set { 
			_currentPosition = value;
			transform.localPosition = _currentPosition;
		}
	}


	public float radius {
		get { return transform.localScale.x * 0.5f; }
		set { transform.localScale = new Vector3(value, value, value); }
	}

	public SimpleTargetObject _parentTarget;

	void Start()
	{
		_startPosition = _currentPosition = transform.localPosition;

	}

	void Update()
	{
		if(axisMagnitude != Vector3.zero && axisWavelength != Vector3.zero)
		{
			axisValues += axisWavelength;
			if(_parentTarget == null)
			{
				currentPosition = new Vector3(
					_startPosition.x + (Mathf.Sin(Mathf.Deg2Rad * axisValues.x) * axisMagnitude.x),
					_startPosition.y + (Mathf.Sin(Mathf.Deg2Rad * axisValues.y) * axisMagnitude.y),
					_startPosition.z + (Mathf.Cos(Mathf.Deg2Rad * axisValues.z) * axisMagnitude.z)
				);
			}
			else
			{
				currentPosition = new Vector3(
					_parentTarget.transform.localPosition.x + (Mathf.Sin(Mathf.Deg2Rad * axisValues.x) * axisMagnitude.x),
					_parentTarget.transform.localPosition.y + (Mathf.Sin(Mathf.Deg2Rad * axisValues.y) * axisMagnitude.y),
					_parentTarget.transform.localPosition.z + (Mathf.Cos(Mathf.Deg2Rad * axisValues.z) * axisMagnitude.z)
				);
			}
		}
	}
}
