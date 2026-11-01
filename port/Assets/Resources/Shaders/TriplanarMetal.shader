Shader "ArmorSim/TriplanarMetal" {
	Properties {
		_Color ("Цвет вещества (тинт)", Vector) = (1,1,1,1)
		_MainTex ("Albedo (tileable)", 2D) = "white" {}
		_TexScale ("Метров на тайл", Float) = 1.5
		_TexSharpness ("Резкость смешивания", Range(1, 16)) = 6
		_TexInfluence ("Сила текстуры", Range(0, 1)) = 1
		_TexMean ("Средняя яркость текстуры", Vector) = (1,1,1,1)
		_Metallic ("Metallic", Range(0, 1)) = 0
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0

		sampler2D _MainTex;
		float4 _MainTex_ST;
		float4 _Color;
		float _TexScale;
		float _TexSharpness;
		float _TexInfluence;
		float4 _TexMean;
		float _Metallic;
		float _Glossiness;

		struct Input {
			float3 worldPos;
			float3 worldNormal;
		};

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float3 n = normalize(IN.worldNormal);
			float3 w = pow(abs(n), max(_TexSharpness, 1.0));
			float wSum = max(w.x + w.y + w.z, 1e-5);
			w /= wSum;

			float scale = max(_TexScale, 1e-4);
			float3 p = IN.worldPos / scale;

			float2 uvX = p.zy * _MainTex_ST.xy + _MainTex_ST.zw;
			float2 uvY = p.xz * _MainTex_ST.xy + _MainTex_ST.zw;
			float2 uvZ = p.xy * _MainTex_ST.xy + _MainTex_ST.zw;

			float3 tx = tex2D(_MainTex, uvX).rgb;
			float3 ty = tex2D(_MainTex, uvY).rgb;
			float3 tz = tex2D(_MainTex, uvZ).rgb;
			float3 tex = tx * w.x + ty * w.y + tz * w.z;

			float3 mean = max(_TexMean.rgb, 0.001);
			float3 normalised = tex / mean;
			float3 albedo = _Color.rgb * lerp(float3(1.0, 1.0, 1.0), normalised, _TexInfluence);

			o.Albedo = max(albedo, 0.0);
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 1.0;
		}
		ENDCG
	}
	Fallback "Diffuse"
}