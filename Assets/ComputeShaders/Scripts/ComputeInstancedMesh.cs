using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeInstancedMesh : MonoBehaviour {

	public ComputeShader shader;
	public int boidCount = 1;
	public int historyLength = 10;
	public Transform targetObject;

	public Mesh instanceMesh;
	public Material instanceMaterial;

	[Range(0.0001f, 1000f)]
	public float maxSpeed;
	[Range(0.0001f, 1000f)]
	public float maxTurnSpeed;

	[Range(0.01f, 1000f)]
	public float massArea;
	public int massSpread;

	private int cachedBoidCount = -1;

	private ColorCollection colors;

	private Node[] nodes;
	private Color[] nodeColours;
	private Vector4[] positions;


	private ComputeBuffer boidBuffer;
	private ComputeBuffer positionBuffer;
	private ComputeBuffer argsBuffer;

	private uint[] args = { 0, 0, 0, 0, 0 };

	private int boidNodeStride = 56;


	// Use this for initialization
	void Start () {

		colors = GetComponent<ColorCollection>();

		argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

		UpdateBuffers();

	}

	private Node GetNode()
	{

		Node n = new Node();
		n.position = new Vector3(
			Random.Range(-10f, 10f),
			Random.Range(-10f, 10f),
			Random.Range(-10f, 10f)
		);

		n.velocity = Vector3.zero;

		n.targetPosition = Vector3.zero;

		n.mass = massArea + Random.Range(-massSpread, massSpread);
		n.maxSpeed = maxSpeed;
		n.maxTurnSpeed = maxTurnSpeed;

		//n.positions = new Vector3[historyLength];
		//for (int i = 0; i < historyLength; i++)
		//{
		//	n.positions[i] = n.position;
		//}

		return n;

	}

	private void UpdateBuffers()
	{

		if (boidBuffer != null) boidBuffer.Release();
		if (positionBuffer != null) positionBuffer.Release();

		boidBuffer = new ComputeBuffer(boidCount, boidNodeStride);
		positionBuffer = new ComputeBuffer(boidCount * historyLength, 16);

		nodes = new Node[boidCount];
		nodeColours = new Color[boidCount];
		positions = new Vector4[boidCount * historyLength];

		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i] = GetNode();
			//nodeColours[i] = colors.GetRandomColor();
			//positions[i * historyLength] = nodes[i].position;
		}

		//Debug.Log("PRE");
		//Debug.Log("a:" + positions[0]);
		//Debug.Log("b:" + positions[1]);
		//Debug.Log("c:" + positions[2]);

		boidBuffer.SetData(nodes);
		//positionBuffer.SetData(positions);

		//shader.SetInt("historyLength", historyLength);

		// reset positions data
		//int kernel = shader.FindKernel("InitPositions");



		//shader.SetBuffer(kernel, "boidBuffer", boidBuffer);
		//shader.SetBuffer(kernel, "positionBuffer", positionBuffer);

		//shader.Dispatch(kernel, boidCount, 1, 1);

		//init positions
		for (int i = 0; i < boidCount; i++)
		{
			for (int j = 0; j < historyLength; j++)
			{
				positions[(i * historyLength) + j] = nodes[i].position;
			}
		}

		positionBuffer.SetData(positions);

		//DEBUG

		//positionBuffer.GetData(positions);
		//Debug.Log("POST : " + positions.Length);
		//Debug.Log("a:" + positions[0]);
		//Debug.Log("b:" + positions[1]);
		//Debug.Log("c:" + positions[2]);


		//end debug

		instanceMaterial.SetBuffer("positionBuffer", positionBuffer);

		uint numIndices = (instanceMesh != null) ? (uint)instanceMesh.GetIndexCount(0) : 0;
		args[0] = numIndices;
		args[1] = (uint)(boidCount * historyLength);
		argsBuffer.SetData(args);

		cachedBoidCount = boidCount;

	}

	// Update is called once per frame
	void Update()
	{

		if (cachedBoidCount != boidCount) UpdateBuffers();

		for (int i = 0; i < boidCount; i++)
		{
			nodes[i].targetPosition = targetObject.position;
		}
		boidBuffer.SetData(nodes);
		//positionBuffer.SetData(positions);

		RunShader();
		ShufflePositions();

		positionBuffer.SetData(positions);

		instanceMaterial.SetBuffer("positionBuffer", positionBuffer);

		//positionBuffer.GetData(positions);
		//Debug.Log("UPDATE: " + nodes[0].position);
		//Debug.Log("a:" + positions[0]);
		//Debug.Log("b:" + positions[1]);
		//Debug.Log("c:" + positions[2]);



		//Debug.Log(positions[0]);
		//argsBuffer.SetData(args);
		Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);


	}

	private void ShufflePositions()
	{
		for (int i = 0; i < boidCount; i++)
		{
			
			for (int j = 0; j < historyLength-1; j++)
			{
				positions[(i * historyLength) + j] = positions[(i * historyLength) + (j + 1)];
				positions[(i * historyLength) + j].w = ((float)j / historyLength);
				//positions[(i * historyLength) + j].w = 0.3f;
				//positions[(i * historyLength) + j] = nodes[i].position;
			}

			positions[(i * historyLength) + (historyLength - 1)] = nodes[i].position;
			positions[(i * historyLength) + (historyLength - 1)].w = 0.3f;

		}


	}

	private void RunShader()
	{
		
		int kernel = shader.FindKernel("CSMain");

		shader.SetBuffer(kernel, "boidBuffer", boidBuffer);

		shader.Dispatch(kernel, boidCount, 1, 1);

		Node[] newNodes = new Node[boidCount];
		boidBuffer.GetData(newNodes);

		nodes = newNodes;
		
	}

	void OnDestroy()
	{
		
		if (boidBuffer != null) boidBuffer.Release();
		if (positionBuffer != null) positionBuffer.Release();
		if (argsBuffer != null) argsBuffer.Release();

	}
}

struct Position {
	public Vector4 position;
}