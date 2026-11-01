Shader "Flat2D/Particles" {
	// Геометрия рисуется процедурно (Graphics.DrawProceduralNow), данные —
	// из ComputeBuffer через StructuredBuffer, как в Flat2DRuntimeView.cs.
	Properties {
	}
	SubShader {
		Tags { "RenderType"="Opaque" }

		// Pass 0: "Измерение" — сплошная сетка ячеек с тонкими швами.
		Pass {
			Blend Off
			ZWrite On
			ZTest LEqual
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5
			// Чтение ComputeBuffer в графическом шейдере требует compute-буферов.
			
			#include "UnityCG.cginc"

			StructuredBuffer<float2> _Pos;
			StructuredBuffer<float2> _Vel;
			StructuredBuffer<float4> _Stress;
			StructuredBuffer<float> _Damage;
			StructuredBuffer<float> _Fail;
			StructuredBuffer<uint> _Mat;
			StructuredBuffer<uint> _BodyId;
			StructuredBuffer<float> _Temp;
			StructuredBuffer<uint> _Paint;

			float4 _Bounds;
			float _HalfSize;
			float _DiscHalf;
			float4 _Range;
			int _Mode;
			int _PaintOn;
			float4 _Tint;
			float4 _WorldOffset;

			float3 HsvToRgb (float h, float s, float v) {
				float3 p = abs(frac(h + float3(1.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
				return v * lerp(float3(1.0, 1.0, 1.0), saturate(p - 1.0), s);
			}

			float4 UnpackPaint (uint p) {
				return float4(
					float(p & uint(255)),
					float((p >> 8) & uint(255)),
					float((p >> 16) & uint(255)),
					float((p >> 24) & uint(255))) / 255.0;
			}

			float3 FieldColor (uint pid) {
				float4 stress = _Stress[pid];
				float2 vel = _Vel[pid];
				float dmg = _Damage[pid];
				float fail = _Fail[pid];
				uint mat = _Mat[pid];

				float rmin = _Range.x;
				float rmax = _Range.y;
				float denom = max(rmax - rmin, 1e-9);
				float3 col = float3(0.5, 0.5, 0.52);

				if (_Mode == 1) {
					// Давление. В разных раскладках тензора это либо w, либо след.
					float p = stress.w;
					if (abs(p) < 1e-9) {
						p = 0.5 * (stress.x + stress.y);
					}
					float t = saturate((p - rmin) / denom);
					col = lerp(float3(0.06, 0.18, 0.72), float3(0.92, 0.18, 0.04), t);
					col.g += 0.25 * (1.0 - abs(t * 2.0 - 1.0));
				} else if (_Mode == 2) {
					float t = saturate(dmg);
					col = lerp(float3(0.08, 0.82, 0.24), float3(1.0, 0.72, 0.05), smoothstep(0.0, 0.5, t));
					col = lerp(col, float3(0.88, 0.04, 0.02), smoothstep(0.5, 1.0, t));
				} else if (_Mode == 3) {
					float t = saturate((length(vel) - rmin) / denom);
					col = lerp(float3(0.04, 0.05, 0.20), float3(0.10, 0.85, 0.95), smoothstep(0.0, 0.55, t));
					col = lerp(col, float3(1.0, 0.92, 0.35), smoothstep(0.55, 1.0, t));
				} else if (_Mode == 4) {
					col = (fail > 0.5) ? float3(1.0, 0.85, 0.25) : float3(0.10, 0.24, 0.28);
				} else if (_Mode == 5) {
					col = HsvToRgb(frac(float(_BodyId[pid]) * 0.37 + 0.11), 0.62, 0.95);
				} else if (_Mode == 6) {
					float temp = _Temp[pid];
					float t = saturate((temp - rmin) / denom);
					col = lerp(float3(0.015, 0.010, 0.012), float3(0.85, 0.06, 0.01), smoothstep(0.0, 0.55, t));
					col = lerp(col, float3(1.0, 0.72, 0.12), smoothstep(0.55, 0.88, t));
					col = lerp(col, float3(1.0, 1.0, 0.92), smoothstep(0.88, 1.0, t));
				} else {
					// Mode 0: материал.
					col = HsvToRgb(frac(float(mat) * 0.618034 + 0.07), 0.55, 0.92);
				}

				if (_PaintOn != 0) {
					float4 paint = UnpackPaint(_Paint[pid]);
					col = lerp(col, paint.rgb, paint.a);
				}
				return col;
			}

			struct appdata {
				uint vid : SV_VertexID;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 color : TEXCOORD1;
			};

			float2 Corner (uint c) {
				if (c == 0) return float2(-1.0, -1.0);
				if (c == 1) return float2( 1.0, -1.0);
				if (c == 2) return float2( 1.0,  1.0);
				if (c == 3) return float2(-1.0, -1.0);
				if (c == 4) return float2( 1.0,  1.0);
				return float2(-1.0, 1.0);
			}

			v2f vert (appdata v) {
				uint pid = v.vid / 6;
				uint corner = v.vid % 6;
				float2 c = _Pos[pid] + _WorldOffset.xy;
				float2 local = Corner(corner);
				float2 wp = c + local * _HalfSize;

				v2f o;
				o.pos = mul(UNITY_MATRIX_VP, float4(wp, 0.0, 1.0));
				o.uv = local * 0.5 + 0.5;
				o.color = FieldColor(pid);
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				float edge = min(min(i.uv.x, 1.0 - i.uv.x), min(i.uv.y, 1.0 - i.uv.y));
				float seam = smoothstep(0.0, 0.055, edge);
				float3 col = i.color * lerp(0.42, 1.0, seam);
				return float4(col, 1.0);
			}
			ENDCG
		}

		// Pass 1: "Точки" — круглые частицы с мягким краем.
		Pass {
			Blend Off
			ZWrite On
			ZTest LEqual
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5
			// Чтение ComputeBuffer в графическом шейдере требует compute-буферов.
			
			#include "UnityCG.cginc"

			StructuredBuffer<float2> _Pos;
			StructuredBuffer<float2> _Vel;
			StructuredBuffer<float4> _Stress;
			StructuredBuffer<float> _Damage;
			StructuredBuffer<float> _Fail;
			StructuredBuffer<uint> _Mat;
			StructuredBuffer<uint> _BodyId;
			StructuredBuffer<float> _Temp;
			StructuredBuffer<uint> _Paint;

			float4 _Bounds;
			float _HalfSize;
			float _DiscHalf;
			float4 _Range;
			int _Mode;
			int _PaintOn;
			float4 _Tint;
			float4 _WorldOffset;

			float3 HsvToRgb (float h, float s, float v) {
				float3 p = abs(frac(h + float3(1.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
				return v * lerp(float3(1.0, 1.0, 1.0), saturate(p - 1.0), s);
			}

			float4 UnpackPaint (uint p) {
				return float4(
					float(p & uint(255)),
					float((p >> 8) & uint(255)),
					float((p >> 16) & uint(255)),
					float((p >> 24) & uint(255))) / 255.0;
			}

			float3 FieldColor (uint pid) {
				float4 stress = _Stress[pid];
				float2 vel = _Vel[pid];
				float dmg = _Damage[pid];
				float fail = _Fail[pid];
				uint mat = _Mat[pid];

				float rmin = _Range.x;
				float rmax = _Range.y;
				float denom = max(rmax - rmin, 1e-9);
				float3 col = float3(0.5, 0.5, 0.52);

				if (_Mode == 1) {
					float p = stress.w;
					if (abs(p) < 1e-9) {
						p = 0.5 * (stress.x + stress.y);
					}
					float t = saturate((p - rmin) / denom);
					col = lerp(float3(0.06, 0.18, 0.72), float3(0.92, 0.18, 0.04), t);
					col.g += 0.25 * (1.0 - abs(t * 2.0 - 1.0));
				} else if (_Mode == 2) {
					float t = saturate(dmg);
					col = lerp(float3(0.08, 0.82, 0.24), float3(1.0, 0.72, 0.05), smoothstep(0.0, 0.5, t));
					col = lerp(col, float3(0.88, 0.04, 0.02), smoothstep(0.5, 1.0, t));
				} else if (_Mode == 3) {
					float t = saturate((length(vel) - rmin) / denom);
					col = lerp(float3(0.04, 0.05, 0.20), float3(0.10, 0.85, 0.95), smoothstep(0.0, 0.55, t));
					col = lerp(col, float3(1.0, 0.92, 0.35), smoothstep(0.55, 1.0, t));
				} else if (_Mode == 4) {
					col = (fail > 0.5) ? float3(1.0, 0.85, 0.25) : float3(0.10, 0.24, 0.28);
				} else if (_Mode == 5) {
					col = HsvToRgb(frac(float(_BodyId[pid]) * 0.37 + 0.11), 0.62, 0.95);
				} else if (_Mode == 6) {
					float temp = _Temp[pid];
					float t = saturate((temp - rmin) / denom);
					col = lerp(float3(0.015, 0.010, 0.012), float3(0.85, 0.06, 0.01), smoothstep(0.0, 0.55, t));
					col = lerp(col, float3(1.0, 0.72, 0.12), smoothstep(0.55, 0.88, t));
					col = lerp(col, float3(1.0, 1.0, 0.92), smoothstep(0.88, 1.0, t));
				} else {
					col = HsvToRgb(frac(float(mat) * 0.618034 + 0.07), 0.55, 0.92);
				}

				if (_PaintOn != 0) {
					float4 paint = UnpackPaint(_Paint[pid]);
					col = lerp(col, paint.rgb, paint.a);
				}
				return col;
			}

			struct appdata {
				uint vid : SV_VertexID;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 color : TEXCOORD1;
			};

			float2 Corner (uint c) {
				if (c == 0) return float2(-1.0, -1.0);
				if (c == 1) return float2( 1.0, -1.0);
				if (c == 2) return float2( 1.0,  1.0);
				if (c == 3) return float2(-1.0, -1.0);
				if (c == 4) return float2( 1.0,  1.0);
				return float2(-1.0, 1.0);
			}

			v2f vert (appdata v) {
				uint pid = v.vid / 6;
				uint corner = v.vid % 6;
				float2 c = _Pos[pid] + _WorldOffset.xy;
				float2 local = Corner(corner);
				float2 wp = c + local * max(_DiscHalf, _HalfSize * 0.5);

				v2f o;
				o.pos = mul(UNITY_MATRIX_VP, float4(wp, 0.0, 1.0));
				o.uv = local * 0.5 + 0.5;
				o.color = FieldColor(pid);
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				float2 d = i.uv * 2.0 - 1.0;
				float r2 = dot(d, d);
				clip(1.0 - r2);
				float shade = 0.62 + 0.38 * sqrt(saturate(1.0 - r2));
				float3 col = i.color * shade;
				if (_Tint.a > 0.001) {
					col = lerp(col, _Tint.rgb, _Tint.a);
				}
				return float4(col, 1.0);
			}
			ENDCG
		}

		// Pass 2: отрисовка поля в промежуточную RT для SurfaceScreen.
		// RGB — цвет поля, A — покрытие (маска поверхности).
		Pass {
			Blend Off
			ZWrite Off
			ZTest Always
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5
			// Чтение ComputeBuffer в графическом шейдере требует compute-буферов.
			
			#include "UnityCG.cginc"

			StructuredBuffer<float2> _Pos;
			StructuredBuffer<float2> _Vel;
			StructuredBuffer<float4> _Stress;
			StructuredBuffer<float> _Damage;
			StructuredBuffer<float> _Fail;
			StructuredBuffer<uint> _Mat;
			StructuredBuffer<uint> _BodyId;
			StructuredBuffer<float> _Temp;
			StructuredBuffer<uint> _Paint;

			float4 _Bounds;
			float _HalfSize;
			float _DiscHalf;
			float4 _Range;
			int _Mode;
			int _PaintOn;
			float4 _Tint;
			float4 _WorldOffset;

			float3 HsvToRgb (float h, float s, float v) {
				float3 p = abs(frac(h + float3(1.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
				return v * lerp(float3(1.0, 1.0, 1.0), saturate(p - 1.0), s);
			}

			float4 UnpackPaint (uint p) {
				return float4(
					float(p & uint(255)),
					float((p >> 8) & uint(255)),
					float((p >> 16) & uint(255)),
					float((p >> 24) & uint(255))) / 255.0;
			}

			float3 FieldColor (uint pid) {
				float4 stress = _Stress[pid];
				float2 vel = _Vel[pid];
				float dmg = _Damage[pid];
				float fail = _Fail[pid];
				uint mat = _Mat[pid];

				float rmin = _Range.x;
				float rmax = _Range.y;
				float denom = max(rmax - rmin, 1e-9);
				float3 col = float3(0.5, 0.5, 0.52);

				if (_Mode == 1) {
					float p = stress.w;
					if (abs(p) < 1e-9) {
						p = 0.5 * (stress.x + stress.y);
					}
					float t = saturate((p - rmin) / denom);
					col = lerp(float3(0.06, 0.18, 0.72), float3(0.92, 0.18, 0.04), t);
					col.g += 0.25 * (1.0 - abs(t * 2.0 - 1.0));
				} else if (_Mode == 2) {
					float t = saturate(dmg);
					col = lerp(float3(0.08, 0.82, 0.24), float3(1.0, 0.72, 0.05), smoothstep(0.0, 0.5, t));
					col = lerp(col, float3(0.88, 0.04, 0.02), smoothstep(0.5, 1.0, t));
				} else if (_Mode == 3) {
					float t = saturate((length(vel) - rmin) / denom);
					col = lerp(float3(0.04, 0.05, 0.20), float3(0.10, 0.85, 0.95), smoothstep(0.0, 0.55, t));
					col = lerp(col, float3(1.0, 0.92, 0.35), smoothstep(0.55, 1.0, t));
				} else if (_Mode == 4) {
					col = (fail > 0.5) ? float3(1.0, 0.85, 0.25) : float3(0.10, 0.24, 0.28);
				} else if (_Mode == 5) {
					col = HsvToRgb(frac(float(_BodyId[pid]) * 0.37 + 0.11), 0.62, 0.95);
				} else if (_Mode == 6) {
					float temp = _Temp[pid];
					float t = saturate((temp - rmin) / denom);
					col = lerp(float3(0.015, 0.010, 0.012), float3(0.85, 0.06, 0.01), smoothstep(0.0, 0.55, t));
					col = lerp(col, float3(1.0, 0.72, 0.12), smoothstep(0.55, 0.88, t));
					col = lerp(col, float3(1.0, 1.0, 0.92), smoothstep(0.88, 1.0, t));
				} else {
					col = HsvToRgb(frac(float(mat) * 0.618034 + 0.07), 0.55, 0.92);
				}

				if (_PaintOn != 0) {
					float4 paint = UnpackPaint(_Paint[pid]);
					col = lerp(col, paint.rgb, paint.a);
				}
				return col;
			}

			struct appdata {
				uint vid : SV_VertexID;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float4 color : COLOR;
			};

			float2 Corner (uint c) {
				if (c == 0) return float2(-1.0, -1.0);
				if (c == 1) return float2( 1.0, -1.0);
				if (c == 2) return float2( 1.0,  1.0);
				if (c == 3) return float2(-1.0, -1.0);
				if (c == 4) return float2( 1.0,  1.0);
				return float2(-1.0, 1.0);
			}

			v2f vert (appdata v) {
				uint pid = v.vid / 6;
				uint corner = v.vid % 6;
				float2 c = _Pos[pid] + _WorldOffset.xy;
				float2 local = Corner(corner);
				float2 wp = c + local * (_HalfSize * 1.06);

				v2f o;
				o.pos = mul(UNITY_MATRIX_VP, float4(wp, 0.0, 1.0));
				o.color = float4(FieldColor(pid), 1.0);
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				return i.color;
			}
			ENDCG
		}

		// Pass 3: тёплое свечение (аддитивный halo по LUT температуры).
		Pass {
			Blend One One
			ZWrite Off
			ZTest LEqual
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5
			// Чтение ComputeBuffer в графическом шейдере требует compute-буферов.
			
			#include "UnityCG.cginc"

			StructuredBuffer<float2> _Pos;
			StructuredBuffer<uint> _Mat;
			StructuredBuffer<float> _TempDisp;
			StructuredBuffer<float4> _GlowLut;
			StructuredBuffer<float> _GlowWMeltInv;

			float _HalfSize;
			float _DiscHalf;
			float4 _WorldOffset;
			float _GlowTmin;
			float _GlowTstep;
			int _GlowN;
			float _GlowHaloK;
			float _GlowExposure;
			float _GlowHaloBase;
			float _GlowIntensity;

			struct appdata {
				uint vid : SV_VertexID;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 glow : COLOR;
			};

			float2 Corner (uint c) {
				if (c == 0) return float2(-1.0, -1.0);
				if (c == 1) return float2( 1.0, -1.0);
				if (c == 2) return float2( 1.0,  1.0);
				if (c == 3) return float2(-1.0, -1.0);
				if (c == 4) return float2( 1.0,  1.0);
				return float2(-1.0, 1.0);
			}

			v2f vert (appdata v) {
				uint pid = v.vid / 6;
				uint corner = v.vid % 6;
				float2 c = _Pos[pid] + _WorldOffset.xy;
				float2 local = Corner(corner);
				float radius = max(_GlowHaloBase, _HalfSize * (1.5 + _GlowHaloK));
				float2 wp = c + local * radius;

				int n = max(_GlowN, 2);
				float fi = saturate((_TempDisp[pid] - _GlowTmin) / max(_GlowTstep * float(n - 1), 1e-6)) * float(n - 1);
				int i0 = min((int)floor(fi), n - 1);
				int i1 = min(i0 + 1, n - 1);
				float3 lut = lerp(_GlowLut[i0].rgb, _GlowLut[i1].rgb, frac(fi));

				uint mat = _Mat[pid];
				float weight = saturate(_GlowWMeltInv[mat]);
				float strength = weight * _GlowIntensity * _GlowExposure;

				v2f o;
				o.pos = mul(UNITY_MATRIX_VP, float4(wp, 0.0, 1.0));
				o.uv = local * 0.5 + 0.5;
				o.glow = float4(lut * strength, strength);
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				float2 d = i.uv * 2.0 - 1.0;
				float r2 = saturate(dot(d, d));
				float halo = pow(saturate(1.0 - r2), 2.0);
				return float4(i.glow.rgb * halo, 1.0);
			}
			ENDCG
		}
	}
}