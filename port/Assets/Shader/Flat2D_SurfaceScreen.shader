Shader "Flat2D/SurfaceScreen" {
	Properties {
		_MainTex ("cover", 2D) = "black" {}
		[HideInInspector] _BlurPx ("Blur, px", Float) = 1
		[HideInInspector] _Thresh ("Surface threshold", Float) = 0.35
		[HideInInspector] _Band ("Surface band", Float) = 0.1
		[HideInInspector] _ShadowPx ("Shadow offset, px", Vector) = (0,0,0,0)
		[HideInInspector] _ShadowCol ("Shadow color", Vector) = (0.3,0.33,0.38,0.55)
		[HideInInspector] _EdgeCol ("Edge color", Vector) = (0.1,0.11,0.13,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGINCLUDE
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;
		float _BlurPx;
		float _Thresh;
		float _Band;
		float4 _ShadowPx;
		float4 _ShadowCol;
		float4 _EdgeCol;

		struct v2f_img {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		v2f_img vert_img (appdata_img v) {
			v2f_img o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord.xy;
			return o;
		}

		float4 BlurH (v2f_img i) : SV_Target {
			float2 d = float2(_BlurPx * _MainTex_TexelSize.x, 0.0);
			float4 c = tex2D(_MainTex, i.uv) * 0.2270;
			c += (tex2D(_MainTex, i.uv + d * 1.3846) + tex2D(_MainTex, i.uv - d * 1.3846)) * 0.3162;
			c += (tex2D(_MainTex, i.uv + d * 3.2308) + tex2D(_MainTex, i.uv - d * 3.2308)) * 0.0703;
			return c;
		}

		float4 BlurV (v2f_img i) : SV_Target {
			float2 d = float2(0.0, _BlurPx * _MainTex_TexelSize.y);
			float4 c = tex2D(_MainTex, i.uv) * 0.2270;
			c += (tex2D(_MainTex, i.uv + d * 1.3846) + tex2D(_MainTex, i.uv - d * 1.3846)) * 0.3162;
			c += (tex2D(_MainTex, i.uv + d * 3.2308) + tex2D(_MainTex, i.uv - d * 3.2308)) * 0.0703;
			return c;
		}

		float4 SurfaceFrag (v2f_img i) : SV_Target {
			float2 uv = i.uv;
			float4 s = tex2D(_MainTex, uv);

			float lo = _Thresh - _Band;
			float hi = _Thresh + _Band;
			float mask = smoothstep(lo, hi, s.a);

			// Тень от «рельефа»: смещаем выборку маски, сильнее в тёмных местах.
			float height = saturate(dot(s.rgb, float3(0.299, 0.587, 0.114)));
			float2 off = _ShadowPx.xy * _MainTex_TexelSize.xy * (1.0 - height);
			float shadowA = tex2D(_MainTex, uv + off).a;
			float shadow = smoothstep(_Thresh, hi, shadowA) * (1.0 - mask) * _ShadowCol.a;

			float2 dx = float2(_MainTex_TexelSize.x, 0.0);
			float2 dy = float2(0.0, _MainTex_TexelSize.y);
			float gx = tex2D(_MainTex, uv + dx).a - tex2D(_MainTex, uv - dx).a;
			float gy = tex2D(_MainTex, uv + dy).a - tex2D(_MainTex, uv - dy).a;
			float edge = saturate(length(float2(gx, gy)) * 3.0);

			float3 col = s.rgb;
			col *= 0.88 + 0.24 * height;
			col = lerp(col, _EdgeCol.rgb, edge * 0.70);
			col = lerp(col, _ShadowCol.rgb, shadow * 0.55);
			float a = max(mask, shadow);
			return float4(col, a);
		}
		ENDCG

		// Pass 0: горизонтальный blur.
		Pass {
			Blend Off
			ZTest Always
			ZWrite Off
			Cull Off

			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment BlurH
			#pragma target 3.0
			ENDCG
		}

		// Pass 1: вертикальный blur.
		Pass {
			Blend Off
			ZTest Always
			ZWrite Off
			Cull Off

			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment BlurV
			#pragma target 3.0
			ENDCG
		}

		// Pass 2: финальная поверхность (порог, кромка, тень).
		Pass {
			Blend SrcAlpha OneMinusSrcAlpha
			ZTest Always
			ZWrite Off
			Cull Off

			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment SurfaceFrag
			#pragma target 3.0
			ENDCG
		}
	}
}