Shader /*ase_name*/"Hidden/Templates/CustomRTInit"/*end*/
{
    Properties
    {
        /*ase_props*/
    }

    SubShader
    {
        Tags { }
		/*ase_all_modules*/
		/*ase_pass*/
        Pass
        {
			Name "Custom RT Init"
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"

            #pragma vertex ASEInitCustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0
			/*ase_pragma*/

			struct ase_appdata_init_customrendertexture
			{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
				/*ase_vdata:p=p;uv0=tc0*/
			};

			// User facing vertex to fragment structure for initialization materials
			struct ase_v2f_init_customrendertexture
			{
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float3 direction : TEXCOORD1;
				/*ase_interp(2,):sp=sp.xyzw;uv0=tc0;uv1=tc1*/
			};

			/*ase_globals*/

			ase_v2f_init_customrendertexture ASEInitCustomRenderTextureVertexShader (ase_appdata_init_customrendertexture v /*ase_vert_input*/)
			{
				ase_v2f_init_customrendertexture o;
				/*ase_vert_code:v=ase_appdata_init_customrendertexture;o=ase_v2f_init_customrendertexture*/
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = float3(v.texcoord.xy, CustomRenderTexture3DTexcoordW);
				o.direction = CustomRenderTextureComputeCubeDirection(v.texcoord.xy);
				return o;
			}

            float4 frag(ase_v2f_init_customrendertexture IN /*ase_frag_input*/) : COLOR
            {
                float4 finalColor;
				/*ase_frag_code:IN=ase_v2f_init_customrendertexture*/
                finalColor = /*ase_frag_out:Frag Color;Float4*/float4(1,1,1,1)/*end*/;
				return finalColor;
            }
            ENDCG
        }
    }
}
