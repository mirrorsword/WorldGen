Shader "Unlit/FlatColorTrans"
{
	Properties
	{
		//_MainTex ("Texture", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags {"Queue"="Transparent" "RenderType"="Transparent" }
		LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				//float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				//float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				UNITY_VERTEX_INPUT_INSTANCE_ID 
				//fixed4 color : COLOR;
			};

            UNITY_INSTANCING_CBUFFER_START(MyProperties)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_CBUFFER_END

			//sampler2D _MainTex;
			//float4 _MainTex_ST;
			//fixed4 _Color;
			
			v2f vert (appdata v)
			{

				v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o); 
				o.vertex = UnityObjectToClipPos(v.vertex);
				//o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				//o.color = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				fixed4 col = UNITY_ACCESS_INSTANCED_PROP(_Color);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
