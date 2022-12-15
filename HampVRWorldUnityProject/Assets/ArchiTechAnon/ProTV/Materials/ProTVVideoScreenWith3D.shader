// Sourced from AudioLink https://github.com/llealloo/vrc-udon-audio-link via Texelsaur
// LICENSE https://github.com/llealloo/vrc-udon-audio-link/blob/dce105719e9307e4cc29c8d01cb932b1b883eaa0/AudioLink/LICENSE.txt
// --------------
// Copy of VRCSDK Video/RealtimeEmissiveGamma
// Aspect ratio correction by Merlin from USharpVideo
// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "ProTV/VideoScreen3D"
{
	Properties{
		_MainTex("Emissive (RGB)", 2D) = "black" {}
		_Emission("Emission Scale", Float) = 1
		_AspectRatio("Aspect Ratio", Float) = 1.777777
		_SBSThreshhold("SBS Threshhold", Float) = 2
		_ThreeDMode("3D Mode", range(0,2)) = 1
		[Toggle(APPLY_GAMMA)] _ApplyGamma("Apply Gamma", Float) = 0
	}

	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		#pragma shader_feature _EMISSION
		#pragma multi_compile APPLY_GAMMA_OFF APPLY_GAMMA

		fixed _Emission;
		sampler2D _MainTex;
		float4 _MainTex_TexelSize;
		float _SBSThreshhold;
		int _ThreeDMode;


		struct Input {
			float2 uv_MainTex;
		};

		float _AspectRatio;

		bool IsInMirror()
		{
			return unity_CameraProjection[2][0] != 0.f || unity_CameraProjection[2][1] != 0.f;
		}

		bool IsInRightEye()
		{
			return (UNITY_MATRIX_P._13 > 0);
		}

		bool IsInLeftEye()
		{
			return (UNITY_MATRIX_P._13 < 0);
		}

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf(Input IN, inout SurfaceOutputStandard o) {
			float2 res = _MainTex_TexelSize.zw;
			float curAspectRatio = res.x / res.y;

			float2 uv = float2(IN.uv_MainTex.x, IN.uv_MainTex.y);
			float visibility = 1;

			if (abs(curAspectRatio - _AspectRatio) > .001) {
				float2 normRes = float2(res.x / _AspectRatio, res.y);
				float2 correction;

				if (normRes.x > normRes.y)
				correction = float2(1, normRes.y / normRes.x);
				else
				correction = float2(normRes.x / normRes.y, 1);

				uv = ((uv - 0.5) / correction) + 0.5;

				float2 uvPadding = (1 / res) * 0.1;
				float2 uvFwidth = fwidth(uv.xy);
				float2 maxf = smoothstep(uvFwidth + uvPadding + 1, uvPadding + 1, uv.xy);
				float2 minf = smoothstep(-uvFwidth - uvPadding, -uvPadding, uv.xy);
				// calculate the min/max of the true size to apply a 0 to anything beyond the ratio value.
				// This creates the "Black Bars" around the video when the video doesn't match the texel size.
				// If this isn't used, the edge pixels end up getting repeated where the black bars are.
				visibility = maxf.x * maxf.y * minf.x * minf.y;
			}

			float2 Mirroreduv = float2 (-uv.x + 1, uv.y);

			bool inMirror = IsInMirror();

			// 0 will force 3D mode off
			// 1 will be normal/autoDetect
			// 2 or greater will force 3D on
			curAspectRatio = curAspectRatio * _ThreeDMode;

			if (curAspectRatio > _SBSThreshhold)
			{
				if(IsInRightEye())
				{
					uv.x = (uv.x / 2) + .5;
					Mirroreduv.x = (Mirroreduv.x / 2) + .5;
				}
				else
				{
					uv.x = uv.x / 2;
					Mirroreduv.x = Mirroreduv.x / 2;
				}
			}

			fixed4 e = tex2D(_MainTex, inMirror ? Mirroreduv : uv);
			
			o.Albedo = fixed4(0,0,0,0);
			o.Alpha = e.a;

			#if APPLY_GAMMA
				e.rgb = pow(e.rgb, 2.2);
			#endif
			o.Emission = e * _Emission * visibility;
			o.Metallic = 0;
			o.Smoothness = 0;
		}
		ENDCG
	}

	FallBack "Diffuse"
	// CustomEditor "RealtimeEmissiveGammaGUI"
}