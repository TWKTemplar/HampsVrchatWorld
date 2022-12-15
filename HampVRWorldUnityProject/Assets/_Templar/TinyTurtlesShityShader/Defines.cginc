struct VertexLightInformation {
    float3 Direction[4];
    float3 ColorFalloff[4];
    float Attenuation[4];
};

sampler2D _MainTex; float4 _MainTex_ST;
sampler2D _OcclusionMap; float4 _OcclusionMap_ST;
sampler2D _EmissionMap; float4 _EmissionMap_ST;

// turtles
float _EmissionScale;

sampler2D _BumpMap; float4 _BumpMap_ST;
sampler2D _ClearcoatMap; float4 _ClearcoatMap_ST;
sampler2D _CurvatureThicknessMap; float4 _CurvatureThicknessMap_ST;
sampler2D _SubsurfaceColorMap; float4 _SubsurfaceColorMap_ST;

float4 _Color, _EmissionColor, _OcclusionColor, _SubsurfaceScatteringColor;
float _Metallic, _Glossiness, _Reflectance, _Anisotropy;
float _ClearcoatAnisotropy, _Clearcoat, _ClearcoatGlossiness; 
float _BumpScale;
float _Cutoff;
float _SubsurfaceInheritDiffuse, _TransmissionNormalDistortion, _TransmissionPower, _TransmissionScale;

float _VertexOffset;
float _TessellationUniform;
float _TessClose;
float _TessFar;

float _SpecularLMOcclusion, _SpecLMOcclusionAdjust;
float _TriplanarFalloff;
float _LMStrength, _RTLMStrength;

int _TextureSampleMode;
int _LightProbeMethod;
int _TessellationMode;
int _SubsurfaceMethod;

// turtles

int _MainTexSC;

float _BrightnessMultiplier;
float _MaxAttenuation;
float _MinLightLevel;

sampler2D _MetallicMap; float4 _MetallicMap_ST;
sampler2D _RoughnessMap; float4 _RoughnessMap_ST;
sampler2D _ReflectanceMap; float4 _ReflectanceMap_ST;
sampler2D _AnisotropyMap; float4 _AnisotropyMap_ST;

int _InvertRoughness;

sampler2D _BumpMap2; float4 _BumpMap2_ST;
float _BumpScale2;

int _DirectXNormal, _DirectXNormal2;

float _PrideFlag;

sampler2D _FluffMap; float4 _FluffMap_ST;