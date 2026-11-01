Shader "ArmorSim/AeroPaths" {
	Properties {
		[HideInInspector] _ZWrite ("Depth write", Float) = 0
	}
	SubShader {
		Tags {
			"Queue"="Transparent"
			"RenderType"="Transparent"
			"IgnoreProjector"="True"
		}
		LOD 200

		Pass {
			// Тонкие линии путей: аддитивно, чтобы слабые альфы не гасили цвет.
			Blend SrcAlpha One
			ZWrite [_ZWrite]
			ZTest LEqual
			Cull Off
			Lighting Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float4 color : COLOR;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float4 color : COLOR;
			};

			v2f vert (appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				return i.color;
			}
			ENDCG
		}
	}
}