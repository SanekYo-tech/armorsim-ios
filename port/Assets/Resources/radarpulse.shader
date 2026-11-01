Shader "ArmorSim/RadarPulse" {
	Properties {
		_Alpha ("Visual opacity", Range(0, 1)) = 1
		_ZWrite ("Depth write", Float) = 0
	}
	SubShader {
		Tags {
			"Queue"="Transparent"
			"RenderType"="Transparent"
			"IgnoreProjector"="True"
		}
		LOD 200

		Pass {
			// Лучи, импульсы и фазовые фронты светятся: аддитивное смешивание.
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

			float _Alpha;

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
				float4 c = i.color;
				c.a *= _Alpha;
				return c;
			}
			ENDCG
		}
	}
}