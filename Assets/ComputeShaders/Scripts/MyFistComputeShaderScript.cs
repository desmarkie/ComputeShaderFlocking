using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyFistComputeShaderScript : MonoBehaviour {

	public ComputeShader shader;

	public RenderTexture tex;

	public Renderer targetObject;

	public Transform cubeParent;

	[RangeAttribute(-50f, 50f)]
	public float rotationSpeed = 10f;

	void Update()
	{

		int kernelHandle = shader.FindKernel("CSMain");

		tex = new RenderTexture(256, 256, 24);
		tex.enableRandomWrite = true;
		tex.Create();

		shader.SetTexture(kernelHandle, "Result", tex);
		shader.Dispatch(kernelHandle, 256/8, 256/8, 1);

		targetObject.material.SetTexture("_MainTex", tex);

		Vector3 cubeRot = cubeParent.localRotation.eulerAngles;
		cubeRot.y += Time.deltaTime * rotationSpeed;
		cubeParent.localRotation = Quaternion.Euler(cubeRot);
		
	}
}
