using System;
using System.Collections.Generic;
using UnityEngine;
namespace AmplifyShaderEditor
{
	
	[Serializable]
	public class ASESaveBundleAsset : ScriptableObject
	{
		[SerializeField]
		private string m_packageContentsOrigin = string.Empty;

		[SerializeField]
		private List<string> m_allExtras = new List<string>();

		[SerializeField]
		private string m_packageTargetPath = string.Empty;

		[SerializeField]
		private string m_packageTargetName = string.Empty;

		[SerializeField]
		private List<Shader> m_allShaders = new List<Shader>();

		public string PackageContentsOrigin { get { return m_packageContentsOrigin; } }
		public List<string> AllExtras{ get { return m_allExtras; } }
		public string PackageTargetPath{ get{ return m_packageTargetPath;}}
		public string PackageTargetName{ get { return m_packageTargetName; } }
		public List<Shader> AllShaders { get { return m_allShaders; } }
	}
}
