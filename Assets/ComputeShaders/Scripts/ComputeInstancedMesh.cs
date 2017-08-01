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

		n.mass = 40f + Random.Range(-10f, 10f);
		n.maxSpeed = 7f;
		n.maxTurnSpeed = 0.7f;

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
		positions = new Vector4[boidCount * 100];

		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i] = GetNode();
			nodeColours[i] = colors.GetRandomColor();
			positions[i * historyLength] = nodes[i].position;
		}


		boidBuffer.SetData(nodes);

		// reset positions data
		int kernel = shader.FindKernel("InitPositions");

		//shader.SetInt("historyLength", historyLength);
		positionBuffer.SetData(positions);

		shader.Dispatch(kernel, boidCount, 1, 1);

		positionBuffer.GetData(positions);

		instanceMaterial.SetBuffer("positionBuffer", positionBuffer);

		uint numIndices = (instanceMesh != null) ? (uint)instanceMesh.GetIndexCount(0) : 0;
		args[0] = numIndices;
		args[1] = (uint)boidCount;
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
		positionBuffer.SetData(positions);

		Debug.Log("a:" + positions[0]);
		Debug.Log("b:" + positions[1]);
		Debug.Log("c:" + positions[2]);

		RunShader();

		//Debug.Log(positions[0]);
		//argsBuffer.SetData(args);
		Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);


	}

	private void RunShader()
	{
		
		int kernel = shader.FindKernel("CSMain");

		shader.SetBuffer(kernel, "boidBuffer", boidBuffer);
		shader.SetBuffer(kernel, "positionBuffer", positionBuffer);

		shader.Dispatch(kernel, boidCount, 1, 1);

		boidBuffer.GetData(nodes);
		positionBuffer.GetData(positions);

		instanceMaterial.SetBuffer("positionBuffer", positionBuffer);
		
	}

	void OnDestroy()
	{
		
		if (boidBuffer != null) boidBuffer.Release();
		if (positionBuffer != null) positionBuffer.Release();
		if (argsBuffer != null) argsBuffer.Release();

	}
}
