Shader "ArmorSim/AeroCloud" {
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
			Blend SrcAlpha OneMinusSrcAlpha
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
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			v2f vert (appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				// Квад облака: мягкий круглый puff.
				float2 d = i.uv * 2.0 - 1.0;
				float r2 = dot(d, d);
				float a = saturate(1.0 - r2);
				a = a * a * (3.0 - 2.0 * a);
				float4 c = i.color;
				c.a *= a;
				return c;
			}
			ENDCG
		}
	}
}