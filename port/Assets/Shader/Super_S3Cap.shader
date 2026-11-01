Shader "Super/S3Cap" {
	Properties {
		_FieldTex ("Поле (объёмный блок)", 3D) = "black" {}
		_BlockLo ("Угол блока, м", Vector) = (0,0,0,0)
		_BlockInv ("1/(cell·n) по осям", Vector) = (1,1,1,0)
		_HalfTexel ("0.5/n по осям", Vector) = (0,0,0,0)
		_RangeMin ("Минимум шкалы", Float) = 0
		_RangeMax ("Максимум шкалы", Float) = 1
		_Bands ("Число полос", Float) = 9
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass {
			Cull Off
			ZWrite On
			ZTest LEqual
			Stencil {
				Ref 1
				Comp Equal
				Pass Zero
				ZFail Zero
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"

			UNITY_DECLARE_TEX3D(_FieldTex);

			float4 _BlockLo;
			float4 _BlockInv;
			float4 _HalfTexel;
			float _RangeMin;
			float _RangeMax;
			float _Bands;

			struct appdata {
				float4 vertex : POSITION;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
			};

			v2f vert (appdata v) {
				v2f o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.pos = UnityWorldToClipPos(o.worldPos);
				return o;
			}

			float3 Rainbow (float t) {
				return saturate(float3(
					1.5 - abs(4.0 * t - 3.0),
					1.5 - abs(4.0 * t - 2.0),
					1.5 - abs(4.0 * t - 1.0)));
			}

			float4 frag (v2f i) : SV_Target {
				float3 uvw = (i.worldPos - _BlockLo.xyz) * _BlockInv.xyz;
				float value = UNITY_SAMPLE_TEX3D(_FieldTex, saturate(uvw)).r;

				float range = max(_RangeMax - _RangeMin, 1e-9);
				float t = saturate((value - _RangeMin) / range);

				float bands = max(_Bands, 2.0);
				float level = floor(t * bands) / (bands - 1.0);
				return float4(Rainbow(saturate(level)), 1.0);
			}
			ENDCG
		}
	}
}