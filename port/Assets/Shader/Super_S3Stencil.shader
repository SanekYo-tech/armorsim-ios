Shader "Super/S3Stencil" {
	Properties {
		_CutPoint ("Точка плоскости", Vector) = (0,0,0,0)
		_CutNormal ("Нормаль (сторона камеры отбрасывается)", Vector) = (1,0,0,0)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		// Помечаем задние грани оставшейся (дальней) половины тела.
		// Крышка S3Cap затем рисуется только там, где stencil == 1.
		Pass {
			Cull Front
			ZTest Always
			ZWrite Off
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

			float4 _CutPoint;
			float4 _CutNormal;

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
				float3 n = normalize(_CutNormal.xyz);
				clip(-dot(i.worldPos - _CutPoint.xyz, n));
				return 0;
			}
			ENDCG
		}

		// Unmark: снимаем метку там, где у оставшейся геометрии есть
		// передние грани. Для тела целиком за плоскостью это убирает
		// ложную крышку; для пересечённого тела остаётся только срез.
		Pass {
			Cull Back
			ZTest Always
			ZWrite Off
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

			float4 _CutPoint;
			float4 _CutNormal;

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
				float3 n = normalize(_CutNormal.xyz);
				clip(-dot(i.worldPos - _CutPoint.xyz, n));
				return 0;
			}
			ENDCG
		}
	}
}