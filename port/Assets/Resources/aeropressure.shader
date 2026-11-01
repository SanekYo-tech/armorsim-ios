Shader "ArmorSim/AeroPressure" {
	Properties {
		_ShowPressure ("Show pressure", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass {
			Cull Off
			ZWrite On
			ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			float _ShowPressure;

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 color : COLOR;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float3 worldNormal : TEXCOORD0;
				float3 viewDir : TEXCOORD1;
				float4 color : COLOR;
				float facing : VFACE;
			};

			v2f vert (appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.viewDir = WorldSpaceViewDir(v.vertex);
				o.color = v.color;
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				float3 n = normalize(i.worldNormal) * (i.facing < 0.0 ? -1.0 : 1.0);
				float3 l = normalize(_WorldSpaceLightPos0.xyz);
				float ndl = saturate(dot(n, l));
				float3 ambient = max(ShadeSH9(float4(n, 1.0)), 0.22);
				float3 lightCol = max(_LightColor0.rgb, 0.75);
				float3 neutral = float3(0.48, 0.54, 0.60);

				float3 albedo = lerp(neutral, i.color.rgb, saturate(_ShowPressure));
				float3 col = albedo * (ambient * 0.65 + lightCol * (0.30 + 0.70 * ndl));
				col += albedo * pow(1.0 - saturate(abs(dot(n, normalize(i.viewDir)))), 3.0) * 0.08;
				return float4(col, 1.0);
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}