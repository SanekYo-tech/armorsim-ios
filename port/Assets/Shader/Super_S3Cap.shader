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
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
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

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return float4(1.0, 1.0, 1.0, 1.0); // RGBA
			}

			ENDHLSL
		}
	}
}