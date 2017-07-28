using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeParticles : MonoBehaviour {

	[Tooltip("Compute Shader")]
	public ComputeShader shader;

	[Tooltip("Number of Particles to create")]
	[Range(1, 10000)]
	public int count = 1;

	[Tooltip("Cube prefab")]
	public GameObject prefab;

	private Node[] nodes;

	private GameObject[] cubes;



	void Start () {

		nodes = new Node[count];
		cubes = new GameObject[count];

		int ct = 0;
		while (ct < count) {

			nodes[ct] = GetNewNode();
			cubes[ct] = GetNewCube();
			cubes[ct].transform.localPosition = nodes[ct].position;

			ct++;

		}

	}
	
	// Update is called once per frame
	void Update () {

		int kernelHandle = shader.FindKernel("CSMain");

		ComputeBuffer buffer = new ComputeBuffer(count, 36);

		buffer.SetData(nodes);

		Node[] newPositions = new Node[count];

		shader.SetBuffer(kernelHandle, "dataBuffer", buffer);
		shader.Dispatch(kernelHandle, count, 1, 1);
		buffer.GetData(newPositions);
		buffer.Release();

		nodes = newPositions;

		for (int i = 0; i < nodes.Length; i++)
		{
			cubes[i].transform.position = nodes[i].position;
			cubes[i].transform.localScale = Vector3.one * nodes[i].scale; //new Vector3(nodes[i].scale, nodes[i].scale, nodes[i].scale);
		}


	}

	private GameObject GetNewCube()
	{
		GameObject cube = Instantiate(prefab);
		cube.transform.parent = transform;
		return cube;
	}

	private Node GetNewNode()
	{
		Node n = new Node();
		n.position = new Vector3( Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f) );
		n.velocity = new Vector3( Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f) );
		n.scale = Random.Range(0.1f, 2f);
		n.angle = Random.Range(0f, 359.9f);
		n.increment = Random.Range(0.1f, 6.0f);
		return n;
	}

}


struct Node {
	
	public Vector3 position;
	public Vector3 velocity;
	public float scale;
	public float angle;
	public float increment;

}