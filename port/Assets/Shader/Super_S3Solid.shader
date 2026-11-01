Shader "Super/S3Solid" {
	Properties {
		_Color ("Цвет", Vector) = (0.62,0.63,0.66,1)
		_CutOn ("Срез включён", Float) = 0
		_CutPoint ("Точка плоскости", Vector) = (0,0,0,0)
		_CutNormal ("Нормаль (сторона камеры отбрасывается)", Vector) = (1,0,0,0)
		_LightDir ("Направление НА свет", Vector) = (0.4,0.7,0.3,0)
		_UseField ("Красить полем (вершинные цвета)", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 _Color;

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return _Color; // RGBA
			}

			ENDHLSL
		}
	}
}