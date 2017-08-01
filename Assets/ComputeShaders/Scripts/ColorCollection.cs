using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorCollection : MonoBehaviour {


	public Color32[] colors;


	private int lastReturnedColor = -1;


	public Color32 GetRandomColor()
	{

		int tgt = -1;

		while(tgt == lastReturnedColor || tgt == -1)
		{
			tgt = Random.Range(0, colors.Length);
		}

		lastReturnedColor = tgt;

		return colors[tgt];

	}

}
