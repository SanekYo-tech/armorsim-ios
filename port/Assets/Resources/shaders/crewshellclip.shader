Shader "ArmorSim/CrewShellClip" {
	Properties {
		_Color ("Color", Vector) = (0.62,0.63,0.66,1)
		_Metallic ("Metallic", Range(0, 1)) = 0
		_Glossiness ("Smoothness", Range(0, 1)) = 0.25
		_CrewHoleCount ("Impact channels", Float) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows addshadow
		#pragma target 3.0

		float4 _Color;
		float _Metallic;
		float _Glossiness;
		float _CrewHoleCount;

		// Заполняется из CrewImpactHoles.cs через MaterialPropertyBlock.
		float4 _CrewHoleStart[32];
		float4 _CrewHoleEnd[32];
		float4x4 _CrewWorldToMeters;

		struct Input {
			float3 worldPos;
		};

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float3 pm = mul(_CrewWorldToMeters, float4(IN.worldPos, 1.0)).xyz;
			int count = (int)clamp(_CrewHoleCount, 0.0, 32.0);

			for (int i = 0; i < 32; i++) {
				if (i >= count) {
					break;
				}
				float4 a = _CrewHoleStart[i];
				float4 b = _CrewHoleEnd[i];
				float3 ab = b.xyz - a.xyz;
				float len2 = max(dot(ab, ab), 1e-12);
				float t = saturate(dot(pm - a.xyz, ab) / len2);
				float3 closest = a.xyz + ab * t;
				clip(length(pm - closest) - a.w);
			}

			o.Albedo = _Color.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 1.0;
		}
		ENDCG
	}
	Fallback "Diffuse"
}