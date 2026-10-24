using UnityEngine;

namespace UnityStandardAssets.Water
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(WaterBase))]
	public class SpecularLighting : MonoBehaviour
	{
		public Transform specularLight;

		private WaterBase m_WaterBase;

		public void Start()
		{
		}

		public void Update()
		{
		}
	}
}
