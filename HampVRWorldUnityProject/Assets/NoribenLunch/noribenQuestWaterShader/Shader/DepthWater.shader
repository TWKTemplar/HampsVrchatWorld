Shader "Noriben/DepthWater"
{
	Properties
	{	
		[Header(Color)]
		_Color ("MainColor", Color) = (0.1, 0.2, 0.3, 1)
		_Transparency ("Transparency", Range(0, 1)) = 0.8

		[Space]
		[Header(Texture)]
		[Normal]_MainTex ("NormalWaveTexture", 2D) = "white" {}
		[NoScaleOffset]_HeightTex ("HeightTexture", 2D) = "white" {} //_MainTexとUVは同一なのでNoScaleOffset
		_EnvMap ("CustomCubeMap", Cube) = "white"{}
		[Enum(On, 0, Off, 1)] _CubemapOn ("UseCustomCubeMap", Int) = 0

		[Space]
		[Header(Wave)]
		_WaveScale ("WaveScale", Range(0, 3)) = 1
		_NormalPower ("WaveNormalPower", Range(0, 1)) = 0.2
		_WaveHeight ("WaveHeight", Range(0, 5)) = 0.1

		[Space]
		[Header(Reflection)]
		_Reflection ("Reflection", Range(0, 10)) = 1
		[Enum(SkyOnly, 0, Defalut, 1)] _RefMode ("ReflectionMode", Int) = 1

		[Space]
		[Header(Scroll)]
		[PowerSlider(2)]_Scrollx ("Scroll_X" , Range(-0.5, 0.5)) = 0.01
		[PowerSlider(2)]_Scrolly ("Scroll_Y" , Range(-0.5, 0.5)) = 0

		[Space]
		[Header(Lighting)]
		_Fresnel ("Fresnel", Range(0, 1)) = 0.5
		_Diffuse ("Diffuse", Range(0, 1)) = 1
		_Specular ("Specular", Range(0, 3)) = 0.3

		[Space]
		[Header(Depth)]
		_DepthFadeStart ("DepthFadeStart", Range(0.001, 100)) = 0
		_DepthFadeDistance ("DepthFadeDistance", Range(0, 100)) = 0
		_DepthFadePower("DepthFadePower", Range(0, 10)) = 1

		[Space]
		[Header(Cull Mode)]
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2

	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue" = "Transparent" "LightMode" = "ForwardBase"}
		LOD 100
		Cull [_Cull]
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			#pragma multi_compile_instancing

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				half3 normal : NORMAL;
				half4 tangent : TANGENT;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1) //TEXCOORD1使用
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD2;
				half3 normal : TEXCOORD3;
				half3 tangent : TEXCOORD4;
				half3 binormal : TEXCOORD5;
				half3 lightDir : TEXCOORD6;
				half3 viewDir : TEXCOORD7;
				float4 screenPos : TEXCOORD8;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			fixed4 _Color;
			fixed4 _LightColor0;
			sampler2D _MainTex;
			sampler2D _HeightTex;
			samplerCUBE _EnvMap;
			float4 _MainTex_ST;
			fixed _Specular;
			fixed _Reflection;
			fixed _Transparency;
			fixed _NormalPower;
			fixed _WaveHeight;
			fixed _Diffuse;
			half _Scrollx;
			half _Scrolly;
			fixed _Fresnel;
			int _RefMode;
			int _CubemapOn;
			fixed _WaveScale;
			//Depth
			sampler2D _CameraDepthTexture;
			float4 _CameraDepthTexture_ST;
			float _DepthFadeDistance;
			float _DepthFadePower;
			float _DepthFadeStart;
			
			v2f vert (appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.normal = UnityObjectToWorldNormal(v.normal);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				

				//uvスケール
				o.uv *= _WaveScale;
				
				//テクスチャで頂点移動
				half4 heighttex = tex2Dlod(_HeightTex, half4(half2(o.uv.x + _Time.y * _Scrollx, o.uv.y + _Time.y * _Scrolly), 0, 0));
				v.vertex.xyz = v.vertex.xyz + heighttex.xyz * v.normal * _WaveHeight;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.screenPos = ComputeScreenPos(o.vertex);

				//ワールド空間の各ベクトル
				o.viewDir       = normalize(mul(unity_ObjectToWorld, ObjSpaceViewDir(v.vertex)));
				o.lightDir 		= normalize(mul(unity_ObjectToWorld, ObjSpaceLightDir(v.vertex)));
                o.binormal      = normalize(cross(v.normal.xyz, v.tangent.xyz) * v.tangent.w * unity_WorldTransformParams.w);
                o.normal        = UnityObjectToWorldNormal(v.normal);
                o.tangent       = mul(unity_ObjectToWorld, v.tangent.xyz);
                o.binormal      = mul(unity_ObjectToWorld, o.binormal);
				
				//UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{	
				UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				
				//uvスケール
				i.uv *= _WaveScale;

				//ノーマルマップ
				half3 normalmap = UnpackNormal(tex2D(_MainTex, half2(i.uv.x + _Time.y * _Scrollx, i.uv.y + _Time.y * _Scrolly)));
				normalmap = normalize(normalmap);
				normalmap = lerp(float3(0,0,1), normalmap, _NormalPower);

				//ワールド空間のノーマル
				half3 worldNormal = (i.tangent * normalmap.x) + (i.binormal * normalmap.y) + (i.normal * normalmap.z);
				
				//環境マップ
                half3 refDir = reflect(-i.viewDir, worldNormal);
				//反射にCubemapの空のみを使うか全部を使うか
				half3 refDirsky = refDir;
				refDirsky.y *= refDirsky.y < 0 ? -1 : 1;
				refDir = lerp(refDirsky, refDir, _RefMode);
                // キューブマップと反射方向のベクトルから反射先の色を取得する
                half4 refColor = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, refDir, 0);

				//Custom Cube map
				half4 cubemap = texCUBE(_EnvMap, refDir);
				refColor = lerp(cubemap, refColor, _CubemapOn);

				//フレネル反射
				half fresnelColor = pow(1.0 - max(0, dot(normalmap, i.viewDir)), _Fresnel);
				fresnelColor = lerp(fresnelColor, 1, 0.4);
				
				//ライティング
				half3 halfDir = normalize(i.lightDir + i.viewDir);
				half3 diffuse = max(0, dot(worldNormal, i.lightDir)) * _LightColor0.rgb;
				half3 specular = pow(max(0, dot(worldNormal, halfDir)), 128.0) * _LightColor0.rgb;

				//mix
				fixed4 col = _Color;
				col.rgb = col.rgb * diffuse * _Diffuse + refColor.rgb * _Reflection + specular * _Specular;
				col.rgb *= half3(fresnelColor, fresnelColor, fresnelColor);
				col = saturate(col);
				col = fixed4(col.xyz, _Transparency);
				

				
				//depth
				float4 screenPos = float4(i.screenPos.xyz , i.screenPos.w + 0.00000000001);
				float4 screenPosNorm = screenPos / screenPos.w;
				screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0 ) ? screenPosNorm.z : screenPosNorm.z * 0.5 + 0.5;
				fixed depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenPosNorm.xy);
				depth = LinearEyeDepth(depth);
				float distanceDepth = (depth - LinearEyeDepth(screenPosNorm.z)) / _DepthFadeDistance;
				distanceDepth = saturate(abs(distanceDepth));
				float Opacity = pow(distanceDepth, _DepthFadePower) * saturate(distance(_WorldSpaceCameraPos, i.worldPos) * (1/_DepthFadeStart));
				Opacity = saturate(Opacity);
				
				col.w *= Opacity;

				// apply fog
				//UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
