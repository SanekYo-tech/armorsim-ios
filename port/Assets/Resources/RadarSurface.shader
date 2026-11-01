Shader "ArmorSim/RadarSurface" {
	Properties {
		_Color ("Цвет поверхности", Vector) = (0.76,0.78,0.8,1)
		_ShowMap ("Show result map", Float) = 0
		_Alpha ("Alpha", Range(0, 1)) = 1
		_ZWrite ("Depth write", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
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
			#include "Lighting.cginc"

			float4 _Color;
			float _ShowMap;
			float _Alpha;

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 color : COLOR;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float3 worldNormal : TEXCOORD0;
				float4 color : COLOR;
			};

			v2f vert (appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.color = v.color;
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				float3 n = i.worldNormal;
				float hasNormal = step(1e-5, dot(n, n));
				n = normalize(n + 1e-6);
				float3 l = normalize(_WorldSpaceLightPos0.xyz);
				float ndl = saturate(dot(n, l));
				float3 lit = _Color.rgb * (0.42 + 0.58 * ndl);

				// Линии (без нормалей) и карта рисуются как есть, поверхность — с
				// простым ламбертовым освещением.
				float3 mapped = i.color.rgb;
				float3 surface = lerp(lit, _Color.rgb * 0.55, 1.0 - hasNormal);
				float3 col = lerp(surface, mapped, saturate(_ShowMap));
				float a = _Alpha * lerp(1.0, i.color.a, saturate(_ShowMap));
				return float4(col, a);
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}