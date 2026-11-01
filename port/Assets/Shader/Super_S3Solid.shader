Shader "Super/S3Solid" {
	Properties {
		_Color ("Цвет", Vector) = (0.62,0.63,0.66,1)
		_CutOn ("Срез включён", Float) = 0
		_CutPoint ("Точка плоскости", Vector) = (0,0,0,0)
		_CutNormal ("Нормаль (сторона камеры отбрасывается)", Vector) = (1,0,0,0)
		_LightDir ("Направление НА свет", Vector) = (0.4,0.7,0.3,0)
		_UseField ("Красить полем (вершинные цвета)", Float) = 0
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

			float4 _Color;
			float _CutOn;
			float4 _CutPoint;
			float4 _CutNormal;
			float4 _LightDir;
			float _UseField;

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 color : COLOR;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float3 viewDir : TEXCOORD2;
				float4 color : COLOR;
				float facing : VFACE;
			};

			v2f vert (appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.viewDir = WorldSpaceViewDir(v.vertex);
				o.color = v.color;
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				float3 cutN = normalize(_CutNormal.xyz);
				float cutD = (_CutOn > 0.5) ? dot(i.worldPos - _CutPoint.xyz, cutN) : -1.0;
				clip(-cutD);

				float3 N = normalize(i.worldNormal);
				if (i.facing < 0.0) {
					N = -N;
				}

				float3 base = (_UseField > 0.5) ? i.color.rgb : _Color.rgb;
				float3 L = normalize(_LightDir.xyz);
				float ndl = saturate(dot(N, L));
				float3 V = normalize(i.viewDir);
				float3 H = normalize(L + V);
				float spec = pow(saturate(dot(N, H)), 32.0) * 0.10;

				float3 ambient = max(ShadeSH9(float4(N, 1.0)), 0.18);
				float3 col = base * (ambient * 0.55 + (0.28 + 0.72 * ndl));
				col += spec;
				if (i.facing < 0.0) {
					col *= 0.62;
				}
				return float4(col, 1.0);
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}