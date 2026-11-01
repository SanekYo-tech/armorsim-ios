Shader "Hidden/ArmorSim/SectionClip" {
	Properties {
		_Color ("Color", Vector) = (0.5,0.5,0.55,1)
		_Metallic ("Metallic", Range(0, 1)) = 0
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_MainTex ("Albedo (tileable)", 2D) = "white" {}
		_TexScale ("Метров на тайл", Float) = 1.5
		_TexSharpness ("Резкость смешивания", Range(1, 16)) = 6
		_TexInfluence ("Сила текстуры", Range(0, 1)) = 1
		_TexMean ("Средняя яркость текстуры", Vector) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows addshadow
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

		// Глобалы SectionView.cs
		float4 _SectionPlane;
		float _SectionEnabled;

		struct Input {
			float3 worldPos;
			float3 worldNormal;
		};

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Оставляем дальнюю от камеры половину: signed distance >= 0.
			if (_SectionEnabled > 0.5) {
				clip(dot(IN.worldPos, _SectionPlane.xyz) - _SectionPlane.w);
			}

			float3 n = normalize(IN.worldNormal);
			float3 w = pow(abs(n), max(_TexSharpness, 1.0));
			float wSum = max(w.x + w.y + w.z, 1e-5);
			w /= wSum;

			float scale = max(_TexScale, 1e-4);
			float3 p = IN.worldPos / scale;

			float2 uvX = p.zy * _MainTex_ST.xy + _MainTex_ST.zw;
			float2 uvY = p.xz * _MainTex_ST.xy + _MainTex_ST.zw;
			float2 uvZ = p.xy * _MainTex_ST.xy + _MainTex_ST.zw;

			float3 tex = tex2D(_MainTex, uvX).rgb * w.x
			           + tex2D(_MainTex, uvY).rgb * w.y
			           + tex2D(_MainTex, uvZ).rgb * w.z;

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
}