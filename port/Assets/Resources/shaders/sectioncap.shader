Shader "Hidden/ArmorSim/SectionCap" {
	Properties {
		_Color ("Цвет вещества", Vector) = (0.5,0.5,0.5,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		// ---------------------------------------------------------------
		// Pass 0: Mark. Задние грани оставшейся (дальней) половины тела
		// помечаются stencil = 1. Unmark (Pass 1) снимает метку там, где
		// видны передние грани. Для тела целиком за плоскостью метки
		// взаимно уничтожаются, для пересечённого — остаётся окно среза.
		// ---------------------------------------------------------------
		Pass {
			ZTest Always
			ZWrite Off
			Cull Front
			ColorMask 0
			Stencil {
				Ref 1
				Comp Always
				Pass Replace
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			float4 _SectionPlane;
			float _SectionEnabled;

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

			float4 frag (v2f i) : SV_Target {
				if (_SectionEnabled > 0.5) {
					clip(dot(i.worldPos, _SectionPlane.xyz) - _SectionPlane.w);
				}
				return 0;
			}
			ENDCG
		}

		// ---------------------------------------------------------------
		// Pass 1: Unmark. Передние грани снимают метку (stencil = 0).
		// ---------------------------------------------------------------
		Pass {
			ZTest Always
			ZWrite Off
			Cull Back
			ColorMask 0
			Stencil {
				Ref 0
				Comp Always
				Pass Replace
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			float4 _SectionPlane;
			float _SectionEnabled;

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

			float4 frag (v2f i) : SV_Target {
				if (_SectionEnabled > 0.5) {
					clip(dot(i.worldPos, _SectionPlane.xyz) - _SectionPlane.w);
				}
				return 0;
			}
			ENDCG
		}

		// ---------------------------------------------------------------
		// Pass 2: Fill. Проецируем вершины тела на плоскость реза и
		// закрашиваем только область со stencil == 1. Stencil не
		// сбрасываем: следом идут остальные submesh этого же тела.
		// ---------------------------------------------------------------
		Pass {
			ZTest LEqual
			ZWrite On
			Cull Off
			Stencil {
				Ref 1
				Comp Equal
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			float4 _Color;
			float4 _SectionPlane;
			float _SectionCapMul;
			float _SectionCapFloor;

			struct appdata {
				float4 vertex : POSITION;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				float side : TEXCOORD1;
			};

			v2f vert (appdata v) {
				v2f o;
				float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
				float3 n = normalize(_SectionPlane.xyz);
				float d = dot(wp, n) - _SectionPlane.w;
				o.side = d;
				wp -= n * d;
				o.worldPos = wp;
				o.pos = UnityWorldToClipPos(wp);
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				// Заливаем только проекцию оставшейся (дальней) половины.
				clip(i.side + 1e-5);
				float mul = (_SectionCapMul > 1e-4) ? _SectionCapMul : 1.0;
				float floorV = (_SectionCapFloor > 1e-4) ? _SectionCapFloor : 0.035;
				float3 n = normalize(_SectionPlane.xyz);
				float3 l = normalize(_WorldSpaceLightPos0.xyz);
				float shade = 0.82 + 0.18 * abs(dot(n, l));
				float3 rgb = max(_Color.rgb * mul * shade, float3(floorV, floorV, floorV));
				return float4(rgb, 1.0);
			}
			ENDCG
		}
	}
}