Shader "ArmorSim/UltraSectionCap" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Поле", 2D) = "white" {}
		_FieldRMax ("Радиус сетки, м", Float) = 1
		_FieldZMin ("Начало сетки по z, м", Float) = 0
		_FieldZSpan ("Длина сетки по z, м", Float) = 1
		_FieldOn ("Считать UV из положения", Range(0, 1)) = 0
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

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			float _FieldRMax;
			float _FieldZMin;
			float _FieldZSpan;
			float _FieldOn;

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 objPos : TEXCOORD1;
			};

			v2f vert (appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.objPos = v.vertex.xyz;
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				float2 uv = i.uv;
				if (_FieldOn > 0.5) {
					float r = length(i.objPos.xy);
					float u = saturate(r / max(_FieldRMax, 1e-6));
					float v = saturate((i.objPos.z - _FieldZMin) / max(_FieldZSpan, 1e-6));
					uv = float2(u, v);
				}
				return tex2D(_MainTex, uv) * _Color;
			}
			ENDCG
		}
	}
	Fallback "Unlit/Texture"
}