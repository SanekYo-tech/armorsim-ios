Shader "ArmorSim/UltraSectionBody" {
	Properties {
		_Color ("Color", Vector) = (0.5,0.5,0.55,1)
		_Metallic ("Metallic", Range(0, 1)) = 0.85
		_Glossiness ("Smoothness", Range(0, 1)) = 0.45
		_InsideDim ("Затемнение изнанки", Range(0, 1)) = 0.62
		_InsideProbe ("Зонд изнанки", Range(0, 1)) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Cull Off

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0

		float4 _Color;
		float _Metallic;
		float _Glossiness;
		float _InsideDim;
		float _InsideProbe;

		struct Input {
			float3 worldNormal;
			float facing : VFACE;
		};

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float3 albedo = _Color.rgb;
			float metallic = _Metallic;
			float smoothness = _Glossiness;

			if (IN.facing < 0.0) {
				// Изнанка (стенка кратера / внутренняя поверхность).
				if (_InsideProbe > 0.5) {
					// Диагностика ContinuumViewer: бок стержня синий, преграды зелёный.
					o.Emission = albedo;
					albedo = 0.0;
					metallic = 0.0;
					smoothness = 0.05;
				} else {
					albedo *= _InsideDim;
					// Мягкая подсветка изнанки, чтобы кратер не был чёрной дырой.
					o.Emission = albedo * 0.22;
					metallic *= 0.35;
					smoothness *= 0.55;
				}
			}

			o.Albedo = max(albedo, 0.0);
			o.Metallic = saturate(metallic);
			o.Smoothness = saturate(smoothness);
			o.Alpha = 1.0;
		}
		ENDCG
	}
	Fallback "Diffuse"
}