using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeParticles : MonoBehaviour {

	[Tooltip("Compute Shader")]
	public ComputeShader shader;

	[Tooltip("Number of Particles to create")]
	[Range(1, 10000)]
	public int count = 1;

	[Tooltip("Maximum Speed for Particles")]
	[Range(0.01f, 10f)]
	public float maxSpeed = 6.0f;

	[Tooltip("Maximum Turning Speed for Particles")]
	[Range(0.0001f, 10f)]
	public float maxTurnSpeed = 6.0f;

	[Tooltip("Minimum Boid Mass")]
	public float minBoidMass;

	[Tooltip("Maximum Boid Mass")]
	public float maxBoidMass;

	[Tooltip("Boid prefab")]
	public GameObject prefab;

	[Tooltip("Boid Target")]
	public GameObject targetObject;

	[Tooltip("Boid Avoiders")]
	public GameObject[] avoiders;

	[Tooltip("Avoider Distance")]
	public float avoiderLength;

	[Tooltip("Avoider Rotation Speeds")]
	public Vector3 avoiderSpeeds;



	private Vector3 avoiderValues = Vector3.zero;

	private Node[] nodes;

	private GameObject[] prefabGOs;

	public Vector3 axisSpread;
	public Vector3 axisSpeeds;

	private Vector3 sinValues;


	void Start () {

		nodes = new Node[count];
		prefabGOs = new GameObject[count];

		int ct = 0;
		while (ct < count) {

			nodes[ct] = GetNewNode();
			prefabGOs[ct] = GetNewPrefab();
			prefabGOs[ct].transform.localPosition = nodes[ct].position;

			ct++;

		}

	}
	
	// Update is called once per frame
	void Update () {

		sinValues.x += axisSpeeds.x;
		sinValues.x %= 360;
		sinValues.y += axisSpeeds.y;
		sinValues.y %= 360;
		sinValues.z += axisSpeeds.z;
		sinValues.z %= 360;

		targetObject.transform.localPosition = new Vector3(
			Mathf.Sin(Mathf.Deg2Rad * sinValues.x) * axisSpread.x,
			Mathf.Sin(Mathf.Deg2Rad * sinValues.y) * axisSpread.y,
			Mathf.Sin(Mathf.Deg2Rad * sinValues.z) * axisSpread.z
		);

		avoiderValues.x += avoiderSpeeds.x;
		avoiderValues.x %= 360;
		avoiderValues.y += avoiderSpeeds.y;
		avoiderValues.y %= 360;
		avoiderValues.z += avoiderSpeeds.z;
		avoiderValues.z %= 360;

		avoiders[0].transform.localPosition = new Vector3(
			Mathf.Sin(Mathf.Deg2Rad * avoiderValues.x) * avoiderLength,
			Mathf.Sin(Mathf.Deg2Rad * avoiderValues.y) * avoiderLength,
			Mathf.Sin(Mathf.Deg2Rad * avoiderValues.z) * avoiderLength
		);


		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i].targetPosition = targetObject.transform.localPosition;
			nodes[i].avoidPosition = nodes[i].targetPosition + avoiders[0].transform.localPosition;
		}

		int kernelHandle = shader.FindKernel("CSMain");

		ComputeBuffer buffer = new ComputeBuffer(count, 72);

		buffer.SetData(nodes);

		Node[] newPositions = new Node[count];

		shader.SetBuffer(kernelHandle, "dataBuffer", buffer);
		shader.Dispatch(kernelHandle, count, 1, 1);
		buffer.GetData(newPositions);
		buffer.Release();

		nodes = newPositions;

		for (int i = 0; i < nodes.Length; i++)
		{
			prefabGOs[i].transform.localPosition = nodes[i].position;
			//prefabGOs[i].transform.localScale = Vector3.one * nodes[i].scale;
			if(nodes[i].velocity != Vector3.zero) prefabGOs[i].transform.rotation = Quaternion.LookRotation(nodes[i].velocity);
		}


	}

	private GameObject GetNewPrefab()
	{

		GameObject cube = Instantiate(prefab);
		cube.transform.parent = transform;
		return cube;

	}

	private Node GetNewNode()
	{

		Node n = new Node();
		n.position = new Vector3( Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f) );
		n.velocity = Vector3.zero;
		n.maxSpeed = maxSpeed;
		n.maxTurnSpeed = maxTurnSpeed;
		n.targetPosition = targetObject.transform.localPosition;
		n.avoidPosition = avoiders[0].transform.localPosition;
		n.mass = Random.Range(minBoidMass, maxBoidMass);
		return n;

	}

}


struct Node {
	
	public Vector3 position;
	public Vector3 velocity;
	public float scale;
	public float angle;
	public float increment;
	public float maxSpeed;
	public float maxTurnSpeed;
	public Vector3 targetPosition;
	public Vector3 avoidPosition;
	public float mass;

}