using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeShaderGrass : MonoBehaviour {

	[Header("Compute Shader")]

	[Tooltip("Compute Shader")]
	public ComputeShader shader;

	[Header("Grass Prefab")]

	[Tooltip("Prefab Game Object")]
	public GameObject prefab;

	[Header("Grass Dimensions")]

	[Tooltip("Blade Count")]
	public int count = 1000;

	[Tooltip("Area Width")]
	public float width = 10f;

	[Tooltip("Area Height")]
	public float height = 10f;

	[Tooltip("Minimum Blade Height")]
	public float minHeight = 10f;

	[Tooltip("Maximum Blade Height")]
	public float maxHeight = 10f;


	private ComputeBuffer _bladeBuffer;
	private ComputeBuffer _obstacleBuffer;

	private GameObject[] _bladeObjects;
	private Blade[] _bladeStructs;

	// Use this for initialization
	void Start () {

		_bladeObjects = new GameObject[count];
		_bladeStructs = new Blade[count];

		//offsets so points are centered
		float minX = -(width * 0.5f);
		float minZ = -(height * 0.5f);

		for (int i = 0; i < count; i++)
		{
			_bladeStructs[i] = GetBlade(minX, minZ);
			_bladeObjects[i] = GetBladeObject(_bladeStructs[i]);
		}

		_bladeBuffer = new ComputeBuffer(count, 36);
		_bladeBuffer.SetData(_bladeStructs);

	}
	
	// Update is called once per frame
	void Update () {






		// grab a reference to the computer shader method
		int kernelHandle = shader.FindKernel("CSMain");
		// set our buffers
		shader.SetBuffer(kernelHandle, "_bladeBuffer", _bladeBuffer);
		// run it
		shader.Dispatch(kernelHandle, count, 1, 1);
		// get the data back
		_bladeBuffer.GetData(_bladeStructs);


		for (int i = 0; i < _bladeStructs.Length; i++)
		{
			_bladeObjects[i].transform.localPosition = _bladeStructs[i].position;

			if (_bladeStructs[i].offsetPosition != Vector3.zero) {

				_bladeObjects[i].transform.localRotation = Quaternion.LookRotation(Vector3.Normalize(_bladeStructs[i].position - (_bladeStructs[i].offsetPosition * 3f)));
				Vector3 rot = _bladeObjects[i].transform.localRotation.eulerAngles;
				rot.y = Random.Range(-0.1f, 0.1f);
				_bladeObjects[i].transform.localRotation = Quaternion.Euler(rot);

			} 
		}
		
	}

	void OnDestroy()
	{
		if (_bladeBuffer != null) _bladeBuffer.Release();
	}

	private GameObject GetBladeObject(Blade blade)
	{

		GameObject go = Instantiate(prefab);

		go.transform.parent = transform;
		go.transform.localPosition = blade.position;
		go.transform.localScale = new Vector3(1, blade.height, 1);

		return go;

	}

	private Blade GetBlade(float minX, float minZ) {

		Blade blade = new Blade();
		blade.position = new Vector3(
			Random.Range(minX, minX + width),
			0,
			Random.Range(minZ, minZ + height)
		);

		blade.height = Random.Range(minHeight, maxHeight);

		blade.offsetPosition = Vector3.zero;
		blade.offsetPosition.y = blade.height;

		blade.sinValue = Random.Range(0f, 360f);
		blade.increment = Random.Range(3f, 7f);

		return blade;

	}

}

struct Blade {

	public Vector3 position;
	public Vector3 offsetPosition;
	public float height;
	public float increment;
	public float sinValue;

}
