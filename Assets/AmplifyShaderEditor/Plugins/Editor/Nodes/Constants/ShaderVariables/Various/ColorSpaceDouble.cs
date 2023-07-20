namespace AmplifyShaderEditor
{
	[System.Serializable]
	[NodeAttributes( "Color Space Double", "Miscellaneous", "Color Space Double" )]
	public class ColorSpaceDouble : ParentNode
	{
		private const string ColorSpaceDoubleStr = "unity_ColorSpaceDouble";

		private readonly string[] ColorSpaceDoubleDef =
		{
			"#ifdef UNITY_COLORSPACE_GAMMA//ASE Color Space Def",
			"#define unity_ColorSpaceDouble half4(2.0, 2.0, 2.0, 2.0)//ASE Color Space Def",
			"#else // Linear values//ASE Color Space Def",
			"#define unity_ColorSpaceDouble half4(4.59479380, 4.59479380, 4.59479380, 2.0)//ASE Color Space Def",
			"#endif//ASE Color Space Def"
		};

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputColorPorts( "RGBA" );
			m_previewShaderGUID = "ac680a8772bb97c46851a7f075fd04e3";
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( dataCollector.IsTemplate && dataCollector.IsSRP )
			{
				for( int i = 0; i < ColorSpaceDoubleDef.Length; i++ )
				{
					dataCollector.AddToDirectives( ColorSpaceDoubleDef[ i ], -1 );
				}
			}
			return GetOutputVectorItem( 0, outputId, ColorSpaceDoubleStr ); ;
		}
	}
}
