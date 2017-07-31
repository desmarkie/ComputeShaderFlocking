using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CylinderOfPoints : MonoBehaviour
{

	[Header("Compute Shader")]
	public ComputeShader shader;


	[Header("Cylinder Properties")]

	[Tooltip("Prebaf to Instantiate")]
	public GameObject prefab;

	public float radius;
	public float height;

	public int points;
	public int sections;

	[Header("Scalers")]
	public SimpleTargetObject[] scalers;

	[Header("Sphere Properties")]
	public float farScale = 0.1f;
	public float maxScale = 1.0f;


	private CylinderPoint[] cylinderPoints;
	private GameObject[] prefabs;
	private Obstacle[] obstacles;

	// Use this for initialization
	void Start()
	{

		cylinderPoints = new CylinderPoint[points * sections];
		prefabs = new GameObject[points * sections];

		int prefabId = 0;

		for (int i = 0; i < sections; i++)
		{

			for (int j = 0; j < points; j++)
			{

				cylinderPoints[prefabId] = GetPoint(i, j);
				prefabs[prefabId] = GetSphere(cylinderPoints[prefabId].position, prefabId);
				prefabId++;

			}

		}

		obstacles = new Obstacle[scalers.Length];
		for (int i = 0; i < scalers.Length; i++)
		{
			obstacles[i] = new Obstacle();
			obstacles[i].position = scalers[i].gameObject.transform.localPosition;
			obstacles[i].radius = scalers[i].radius;
		}

	}

	// Update is called once per frame
	void Update()
	{
		
		for (int i = 0; i < scalers.Length; i++)
		{
			
			obstacles[i].position = scalers[i].gameObject.transform.localPosition;
			obstacles[i].radius = scalers[i].radius;

		}

		ComputeBuffer obsBuffer = new ComputeBuffer(scalers.Length, 16);
		obsBuffer.SetData(obstacles);

		ComputeBuffer pointsBuffer = new ComputeBuffer(cylinderPoints.Length, 20);
		pointsBuffer.SetData(cylinderPoints);

		// grab a reference to the computer shader method
		int kernelHandle = shader.FindKernel("CSMain");
		// set our buffers
		shader.SetBuffer(kernelHandle, "pointsBuffer", pointsBuffer);
		shader.SetBuffer(kernelHandle, "obstacleBuffer", obsBuffer);
		// run it
		shader.Dispatch(kernelHandle, prefabs.Length, 1, 1);
		// get the data back
		pointsBuffer.GetData(cylinderPoints);
		// clear the buffers
		pointsBuffer.Release();
		obsBuffer.Release();


		int pt = 0;
		for (int i = 0; i < sections; i++)
		{

			for (int j = 0; j < points; j++)
			{

				prefabs[pt].transform.localScale = Vector3.one * Mathf.Lerp(farScale, maxScale, Mathf.InverseLerp(0, 1, cylinderPoints[pt].scale));
				Vector3 scaledPos = cylinderPoints[pt].position * prefabs[pt].transform.localScale.x * 2;
				//scaledPos.y = cylinderPoints[pt].position.y;
				prefabs[pt].transform.localPosition = scaledPos;
				pt++;

			}

		}

	}

	private GameObject GetSphere(Vector3 position, int id = 0)
	{
		
		GameObject go = Instantiate(prefab);

		go.name = "prefab_" + id;
		go.transform.parent = transform;
		go.transform.localPosition = position;
		go.transform.localScale = Vector3.one * farScale;
		return go;

	}

	private CylinderPoint GetPoint(int section, int point)
	{

		CylinderPoint pt = new CylinderPoint();

		float angle = Mathf.Deg2Rad * ((360f / points) * point);

		pt.position = new Vector3(
			Mathf.Cos(angle) * radius,
			(height / sections) * section,
			Mathf.Sin(angle) * radius
		);

		pt.scale = farScale;

		pt.obstacleCount = scalers.Length;

		return pt;

	}
}
