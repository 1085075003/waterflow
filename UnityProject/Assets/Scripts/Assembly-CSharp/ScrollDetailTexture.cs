using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ScrollDetailTexture : MonoBehaviour
{
	public bool uniqueMaterial;

	public Vector2 scrollPerSecond;

	private Matrix4x4 m_Matrix;

	private Material mCopy;

	private Material mOriginal;

	private Image mSprite;

	private Material m_Mat;

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
	}

	private void Update()
	{
	}
}
