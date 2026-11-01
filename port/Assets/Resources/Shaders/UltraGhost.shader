Shader "ArmorSim/UltraGhost" {
	Properties {
		_Color ("Цвет с прозрачностью", Vector) = (0.35,0.8,1,0.18)
		_ZTest ("Тест глубины", Float) = 4
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
			ZWrite Off
			ZTest [_ZTest]
			Cull Off
			Lighting Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			float4 _Color;

			struct appdata {
				float4 vertex : POSITION;
			};

			struct v2f {
				float4 pos : SV_POSITION;
			};

			v2f vert (appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				return _Color;
			}
			ENDCG
		}
	}
	Fallback "Unlit/Color"
}