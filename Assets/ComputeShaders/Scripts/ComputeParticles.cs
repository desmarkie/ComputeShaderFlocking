using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeParticles : MonoBehaviour {

	[Tooltip("Compute Shader")]
	public ComputeShader shader;

	[Tooltip("Number of Boids to create")]
	[Range(1, 10000)]
	public int count = 1;

	[Tooltip("Maximum Speed for Boid")]
	[Range(0.01f, 10f)]
	public float maxSpeed = 6.0f;

	[Tooltip("Maximum Turning Speed for Boid")]
	[Range(0.0001f, 10f)]
	public float maxTurnSpeed = 6.0f;

	[Tooltip("Minimum Boid Mass")]
	public float minBoidMass;

	[Tooltip("Maximum Boid Mass")]
	public float maxBoidMass;

	[Tooltip("Boid prefab")]
	public GameObject prefab;

	[Tooltip("Boid Seek Target")]
	public GameObject targetObject;

	[Tooltip("Boid Avoid Obstacles")]
	public SimpleTargetObject[] obstacles;

	[Tooltip("Show / Hide Target Objects")]
	public bool showTargets = false;

	// obstacle objects stored as structs for compute shader buffer
	private Obstacle[] obstacleStructs;

	// Node structs
	private Node[] nodes;

	// Node geometries
	private GameObject[] prefabGOs;



	void Start () {

		// create Nodes and Node prefabs
		nodes = new Node[count];
		prefabGOs = new GameObject[count];

		int ct = 0;
		while (ct < count) {

			nodes[ct] = GetNewNode();
			prefabGOs[ct] = GetNewPrefab();
			prefabGOs[ct].transform.localPosition = nodes[ct].position;

			ct++;

		}


		// create obstacle structs
		obstacleStructs = new Obstacle[obstacles.Length];
		for (int i = 0; i < obstacleStructs.Length; i++)
		{
			
			obstacleStructs[i] = new Obstacle();
			obstacleStructs[i].position = obstacles[i].currentPosition;
			obstacleStructs[i].radius = obstacles[i].radius;

		}

	}

	// switch target geometry on/off
	public void ToggleTargetVisibility()
	{
		
		targetObject.GetComponent<MeshRenderer>().enabled = showTargets;
		for (int i = 0; i < obstacles.Length; i++)
		{
			obstacles[i].GetComponent<MeshRenderer>().enabled = showTargets;
		}

	}
	
	// Update is called once per frame
	void Update () {

		// check for target visibliity toggle
		if (showTargets != targetObject.GetComponent<MeshRenderer>().enabled) ToggleTargetVisibility();

		// update obstacle struct data
		for (int i = 0; i < obstacleStructs.Length; i++)
		{
			obstacleStructs[i].position = obstacles[i].currentPosition;
		}

		// create obstacle buffer for compute shader (float3 + float = 4. Multiply by 4 for total data per element)
		ComputeBuffer obsBuffer = new ComputeBuffer(obstacleStructs.Length, 16);
		// set buffer data
		obsBuffer.SetData(obstacleStructs);

		// update target position for each Node
		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i].targetPosition = targetObject.transform.localPosition;
		}

		// create buffer of Nodes
		ComputeBuffer buffer = new ComputeBuffer(count, 64);
		// set buffer data
		buffer.SetData(nodes);

		// storing the new data in here
		Node[] newPositions = new Node[count];

		// grab a reference to the computer shader method
		int kernelHandle = shader.FindKernel("CSMain");
		// set our buffers
		shader.SetBuffer(kernelHandle, "dataBuffer", buffer);
		shader.SetBuffer(kernelHandle, "obstacleBuffer", obsBuffer);
		// run it
		shader.Dispatch(kernelHandle, count, 1, 1);
		// get the data back
		buffer.GetData(newPositions);
		// clear the buffers
		buffer.Release();
		obsBuffer.Release();

		// update local data
		nodes = newPositions;

		// update node positions + velocities
		for (int i = 0; i < nodes.Length; i++)
		{
			prefabGOs[i].transform.localPosition = nodes[i].position;
			if(nodes[i].velocity != Vector3.zero) prefabGOs[i].transform.rotation = Quaternion.LookRotation(nodes[i].velocity);
		}

	}

	// instantiate a new prefab and return it
	private GameObject GetNewPrefab()
	{

		GameObject go = Instantiate(prefab);

		go.transform.parent = transform;

		return go;

	}

	// create a new Node object and return it
	private Node GetNewNode()
	{

		Node n = new Node();

		n.position = new Vector3( Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f) );
		n.velocity = Vector3.zero;
		n.maxSpeed = maxSpeed;
		n.maxTurnSpeed = maxTurnSpeed;
		n.targetPosition = targetObject.transform.localPosition;
		n.mass = Random.Range(minBoidMass, maxBoidMass);
		n.numberOfNodes = count;
		n.obsCount = obstacles.Length;

		return n;

	}

}
