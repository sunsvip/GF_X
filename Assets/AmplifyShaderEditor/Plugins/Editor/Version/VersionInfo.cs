// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class VersionInfo
	{
		public const byte Major = 1;
		public const byte Minor = 9;
		public const byte Release = 1;
		public static byte Revision = 5;

		public static string StaticToString()
		{
			return string.Format( "{0}.{1}.{2}", Major, Minor, Release ) + ( Revision > 0 ? "." + Revision.ToString() : "" );
		}

		public static int FullNumber { get { return Major * 10000 + Minor * 1000 + Release * 100 + Revision; } }
		public static string FullLabel { get { return "Version=" + FullNumber; } }
	}
}
