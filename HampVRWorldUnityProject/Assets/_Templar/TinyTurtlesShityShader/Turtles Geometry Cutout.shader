Shader "Tiny Turtles/Tri Shader"
{
	Properties
	{
        [Header(MAIN)]
        [Enum(Unity Default, 0, Non Linear, 1)]_LightProbeMethod("Light Probe Sampling", Int) = 0
        [Enum(UVs, 0, Triplanar World, 1, Triplanar Object, 2)]_TextureSampleMode("Texture Mode", Int) = 0
        _TriplanarFalloff("Triplanar Blend", Range(0.5,1)) = 1
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [Enum(Full Colour,0,Single Channel,1)]_MainTexSC("Full Colour or Single-Channel",Int) = 0
        
        [Space(16)]
        [Header(NORMALS)]
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(-1,1)) = 1
        [Enum(Whew,0,Gaah,1)]_DirectXNormal("Oh No DirectX Normal Map",Int) = 0

        _BumpMap2("Second Normal Map", 2D) = "bump" {}
        _BumpScale2("Second Normal Scale", Range(-1,1)) = 0
        [Enum(Whew,0,Gaah,1)]_DirectXNormal2("Oh No DirectX Normal Map",Int) = 0

        [Space(16)]
        [Header(METALLIC)]
        _MetallicMap("Metallic Map", 2D) = "white" {}
        _RoughnessMap("Roughness Map", 2D) = "white" {}
        [Enum(No Thanks, 0, Yes Please, 1)]_InvertRoughness("Invert Roughness", Int) = 0
        _ReflectanceMap("Reflectance Map", 2D) = "white" {}
        _AnisotropyMap("Anisotropy Map",2D) = "white" {}

        [Gamma] _Metallic("Metallic", Range(0,1)) = 0
        _Glossiness("Roughness", Range(0,1)) = 0
        _Reflectance("Reflectance", Range(0,1)) = 0.5
        _Anisotropy("Anisotropy", Range(-1,1)) = 0
        
        [Space(16)]
        [Header(OCCLUSION)]
        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _OcclusionColor("Occlusion Color", Color) = (0,0,0,1)
        
        [Space(16)]
        [Header(SUBSURFACE)]
        [Enum(Off, 0, Estimate, 1)]_SubsurfaceMethod("Subsurface Scattering Method", Int) = 0
        _CurvatureThicknessMap("Curvature Thickness Map", 2D) = "gray" {}
        _SubsurfaceColorMap("Subsurface Color Map", 2D) = "white" {}
        _SubsurfaceScatteringColor("Subsurface Color", Color) = (1,1,1,1)
        _SubsurfaceInheritDiffuse("Subsurface Inherit Diffuse", Range(0,1)) = 0
        _TransmissionNormalDistortion("Transmission Distortion", Range(0,3)) = 1
        _TransmissionPower("Transmission Power", Range(0,3)) = 1
        _TransmissionScale("Transmission Scale", Range(0,3)) = 0.1
        
        [Space(16)]
      //  [Header(EMISSION)]
      //  _EmissionMap("Emission Map", 2D) = "white" {}
        [HDR]_EmissionColor("Emission Color", Color) = (0,0,0,1)

        // turtles
        _EmissionScale("Emission Scale", Range(0,2)) = 0
        
        [Space(16)]
        [Header(CLEARCOAT)]
        _ClearcoatMap("Clearcoat Map", 2D) = "white" {}
        _Clearcoat("Clearcoat", Range(0,1)) = 0
        _ClearcoatGlossiness("Clearcoat Smoothness", Range(0,1)) = 0.5
        _ClearcoatAnisotropy("Clearcoat Anisotropy", Range(-1,1)) = 0
        
        [Space(16)]
        [Header(GEOMETRY SETTINGS)]
        _VertexOffset("Face Offset", float) = 0
        
        //#TESS![Space(16)]
        //#TESS![Header(GEOMETRYTESSELLATION SETTINGS)]
        //#TESS![Enum(Uniform, 0, Edge Length, 1, Distance, 2)]_TessellationMode("Tessellation Mode", Int) = 1
        //#TESS!_TessellationUniform("Tessellation Factor", Range(0,1)) = 0.05
        //#TESS!_TessClose("Tessellation Close", Float) = 10
        //#TESS!_TessFar("Tessellation Far", Float) = 50
        
        [Space(16)]
        [Header(TURTLES EXTRA SHIT)]
        //[Enum(Off, 0, Front, 1, Back, 2)]_BackfaceCull("Backface Culling", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _BackfaceCull ("Culling", Float) = 0
        _MaxAttenuation("Maximum Attenuation", Range(0,1)) = 1
        _MinLightLevel("Minimum Light Level", Range(0,1)) = 0
        _BrightnessMultiplier("Brightness Multiplier", Range(1,5)) = 1
        _PrideFlag("Pride!",Range(0,1)) = 0

        _FluffMap("Fluff Map",2D) = "black" {}

        [Space(16)]
        [Header(LIGHTMAPPING HACKS)]
        _SpecularLMOcclusion("Specular Occlusion", Range(0,1)) = 0
        _SpecLMOcclusionAdjust("Spec Occlusion Sensitiviy", Range(0,1)) = 0.2
        _LMStrength("Lightmap Strength", Range(0,1)) = 1
        _RTLMStrength("Realtime Lightmap Strength", Range(0,1)) = 1
    }

	SubShader
	{
        //Tags{"RenderType"="TransparentCutout" "Queue"="AlphaTest"}

        Tags{"RenderType"="Opaque" "Queue"="Geometry"}

		Pass
		{
            Tags {"LightMode"="ForwardBase"}

            Cull [_BackfaceCull]

			CGPROGRAM
			#pragma vertex vert
            #pragma geometry geom
			#pragma fragment frag
            #pragma multi_compile _ VERTEXLIGHT_ON
            #pragma multi_compile_fwdbase
            #pragma target 4.0
            #define GEOMETRY
           //#define ALPHATEST

            #ifndef UNITY_PASS_FORWARDBASE
                #define UNITY_PASS_FORWARDBASE
            #endif
			
			#include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;

                float4 turtles_colour : COLOR0;
			};

			struct v2g
			{
                float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float3 normal : TEXCOORD3;
                float4 tangent : TEXCOORD4;

                float4 turtles_colour : COLOR0;
			};

            struct g2f
            {
                float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float3 btn[3] : TEXCOORD3; //TEXCOORD2, TEXCOORD3 | bitangent, tangent, worldNormal
                float3 worldPos : TEXCOORD6;
                float3 objPos : TEXCOORD7;
                float3 objNormal : TEXCOORD8;
                float4 screenPos : TEXCOORD9;

                float4 turtles_colour : COLOR0;

                SHADOW_COORDS(10)
            };

            #include "Defines.cginc"
            #include "LightingFunctions.cginc"
            #include "LightingBRDF.cginc"
			#include "VertFragGeom.cginc"
			
			ENDCG
		}

        Pass
		{
            Tags {"LightMode"="ForwardAdd"}
            Blend One One
            ZWrite Off
            
            Cull [_BackfaceCull]

			CGPROGRAM
			#pragma vertex vert
            #pragma geometry geom
			#pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #pragma target 4.0
            #define GEOMETRY
           //#define ALPHATEST

            #ifndef UNITY_PASS_FORWARDADD
                #define UNITY_PASS_FORWARDADD
            #endif
			
			#include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;

                float4 turtles_colour : COLOR;
			};

			struct v2g
			{
                float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;

                float3 normal : TEXCOORD3;
                float4 tangent : TEXCOORD4;

                float4 turtles_colour : COLOR;
			};

            struct g2f
            {
                float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;


                float3 btn[3] : TEXCOORD3; //TEXCOORD2, TEXCOORD3 | bitangent, tangent, worldNormal
                float3 worldPos : TEXCOORD6;
                float3 objPos : TEXCOORD7;
                float3 objNormal : TEXCOORD8;
                float4 screenPos : TEXCOORD9;
                float4 turtles_colour : COLOR;
                
                SHADOW_COORDS(10)
            };

			
            #include "Defines.cginc"
            #include "LightingFunctions.cginc"
            #include "LightingBRDF.cginc"
			#include "VertFragGeom.cginc"
			
			ENDCG
		}

               Pass
        {
            Tags{"LightMode" = "ShadowCaster"} //Removed "DisableBatching" = "True". If issues arise re-add this.
            Cull Off
            CGPROGRAM
            #include "UnityCG.cginc" 
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #pragma multi_compile_shadowcaster
            #pragma target 4.0
            #define GEOMETRY
            
            #ifndef UNITY_PASS_SHADOWCASTER
                #define UNITY_PASS_SHADOWCASTER
            #endif
            
            struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 turtles_colour : COLOR;
			};

			struct v2g
			{
                float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 tangent : TEXCOORD2;
                float4 turtles_colour : COLOR;
			};

            struct g2f
            {
                float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
                float3 btn[3] : TEXCOORD1; //TEXCOORD2, TEXCOORD3 | bitangent, tangent, worldNormal
                float3 worldPos : TEXCOORD4;
                float3 objPos : TEXCOORD5;
                float3 objNormal : TEXCOORD6;
                float4 screenPos : TEXCOORD7;
                float4 turtles_colour : COLOR;
            };

            #include "Defines.cginc"
            #include "LightingFunctions.cginc"
            #include "VertFragGeom.cginc"
            ENDCG
        }
	}
}
