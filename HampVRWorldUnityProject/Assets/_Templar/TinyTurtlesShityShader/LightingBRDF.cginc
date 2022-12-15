// turtles utility functions

#define glsl_mod(x,y) (((x)-(y)*floor((x)/(y)))) 

float3 HUEtoRGB(in float H) {
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R,G,B));
}

float3 HSVtoRGB(in float3 HSV) {
    float3 RGB = HUEtoRGB(HSV.x);
    return ((RGB - 1) * HSV.y + 1) * HSV.z;
}

float Epsilon = 1e-10;
 
  float3 RGBtoHCV(in float3 RGB) {
    // Based on work by Sam Hocevar and Emil Persson
    float4 P = (RGB.g < RGB.b) ? float4(RGB.bg, -1.0, 2.0/3.0) : float4(RGB.gb, 0.0, -1.0/3.0);
    float4 Q = (RGB.r < P.x) ? float4(P.xyw, RGB.r) : float4(RGB.r, P.yzx);
    float C = Q.x - min(Q.w, Q.y);
    float H = abs((Q.w - Q.y) / (6 * C + Epsilon) + Q.z);
    return float3(H, C, Q.x);
}

float3 RGBtoHSV(in float3 RGB) {
    float3 HCV = RGBtoHCV(RGB);
    float S = HCV.y / (HCV.z + Epsilon);
    return float3(HCV.x, S, HCV.z);
}


float3 slerp(float3 start, float3 end, float percent) {
    float d = dot(start, end);
    d = clamp(d, -1.0, 1.0);
    float theta = acos(d)*percent;
    float3 RelativeVec  = normalize(end - start*d);
    return((start*cos(theta)) + (RelativeVec*sin(theta)));
}

//Since this is shared, and the output structs/input structs are all slightly differently named in each shader template, just handle them all here.
float4 CustomStandardLightingBRDF(
    #if defined(GEOMETRY)
        g2f i
    #elif defined(TESSELLATION)
        vertexOutput i
    #else
        v2f i
    #endif
    )
{
    //LIGHTING PARAMS
    UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos.xyz);
    float3 worldPos = i.worldPos;
    // fix for rare bug where light atten is 0 when there is no directional light in the scene
	#ifdef UNITY_PASS_FORWARDBASE
		if(all(_LightColor0.rgb == 0.0))
		{
			attenuation = 1.0;
		}
	#endif

    //turtles max attenuation
    attenuation=clamp(attenuation,0.0,_MaxAttenuation);

    // are we fluff

    float isFluff = texTP(_FluffMap,_FluffMap_ST,i.worldPos,i.objPos,i.btn[2],i.objNormal,_TriplanarFalloff,i.uv);

    //NORMAL
        float3 unmodifiedWorldNormal = normalize(i.btn[2]);
        float3 unmodifiedTangent = i.btn[1];
        float3 unmodifiedBitangent = i.btn[0];
        float4 normalMap1 = texTP(_BumpMap, _BumpMap_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv);
        float4 normalMap2 = texTP(_BumpMap2, _BumpMap2_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv1);

        if(_DirectXNormal==1)
            normalMap1.y = 1 - normalMap1.y;
        
        if(_DirectXNormal2==1)
            normalMap2.y = 1 - normalMap2.y;

        float3 worldNormal = i.btn[2];
        float3 tangent = i.btn[1];
        float3 bitangent = i.btn[0];
        initBumpedNormalTangentBitangent(normalMap1, normalMap2, bitangent, tangent, worldNormal);
    //----

    //DIFFUSE

        // turtles take single-channel albedo texture
        float4 albedo = texTP(_MainTex, _MainTex_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv) * _Color;
        if(_MainTexSC==1) {
            float greyscaleAlbedo = pow(albedo.r,2.2);
            albedo = float4(greyscaleAlbedo,greyscaleAlbedo,greyscaleAlbedo,1) * _Color;
        }
        // turtles fluff uses second UV map
        albedo=lerp(albedo,20*abs(i.uv1.x-0.5),isFluff);
        // float4 albedo = texTP(_MainTex, _MainTex_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv) * _Color;
        float4 diffuse = albedo;
        float alpha;
        doAlpha(alpha, diffuse.a, i.screenPos);

            float4 malbedo;
            float po;

            po=i.objPos.y*_SinTime+i.objPos.x*_CosTime;
            float prideHue=fmod(1.0-(po-0.2)*5,1);
            
            malbedo = albedo*float4(HSVtoRGB(float3(prideHue,1,1)),1);
            
            albedo=lerp(albedo,malbedo,_PrideFlag);

            diffuse=albedo;
        
    //----

    //METALLIC SMOOTHNESS

        // turtles no packed maps pl0x

        float turtles_metallic = texTP(_MetallicMap, _MetallicMap_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv).r;
        float turtles_roughness = texTP(_RoughnessMap, _RoughnessMap_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv).r;
        if(_InvertRoughness==1) turtles_roughness=1-turtles_roughness;
        float turtles_reflectance = texTP(_ReflectanceMap, _ReflectanceMap_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv).r;
        float turtles_anisotropy = texTP(_AnisotropyMap, _AnisotropyMap_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv).r;

        float metallic = turtles_metallic*_Metallic;
        float reflectance = turtles_reflectance*_Reflectance;
        float roughness = max(turtles_roughness*_Glossiness, getGeometricSpecularAA(worldNormal));
        albedo.rgb *= 1-metallic;
    //----

    //CURVATURE THICKNESS MAP
    float4 curvatureThicknessMap = 0;
    float4 subsurfaceColorMap = 0;
    if(_SubsurfaceMethod == 1)
    {
        //Contains Curvature, Thickness, and a Mask for both.
        curvatureThicknessMap = texTP(_CurvatureThicknessMap, _CurvatureThicknessMap_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv);
        //SUBSURFACE COLOR TEXTURE
        subsurfaceColorMap = texTP(_CurvatureThicknessMap, _CurvatureThicknessMap_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv);

        float curvature = isFluff*(abs(i.uv1.y-0.5));
        //float curvature = isFluff;
        float thickness = pow((i.uv1.y)*(1-isFluff),0.5);
        curvatureThicknessMap=float4(curvature,thickness,1-isFluff,1);
    }

    //OCCLUSION
        float4 occlusionMap = texTP(_OcclusionMap, _OcclusionMap_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv);
        float4 occlusion = lerp(_OcclusionColor, 1, occlusionMap);
    //----

    //EMISSION
        float4 emission = 0;
        #if defined(UNITY_PASS_FORWARDBASE) || defined(UNITY_PASS_META) // Emissions should only happen in the forward base pass (and meta pass)
            //float4 emissionMap = texTP(_EmissionMap, _EmissionMap_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv);
            //emission = emissionMap * _EmissionColor * _EmissionScale;

            // turtles
            emission = albedo * _EmissionColor * log2(_EmissionScale+1);
        #endif
    //----

    //CLEARCOAT MAP
        float4 clearcoatMap = texTP(_ClearcoatMap, _ClearcoatMap_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv);
        float4 clearcoatReflectivitySmoothness = getClearcoatSmoothness(clearcoatMap);
        float clearcoatReflectivity = clearcoatReflectivitySmoothness.r;
        float clearcoatRoughness = clearcoatReflectivitySmoothness.a;
    //----

    //LIGHTING VECTORS
        bool lightEnv = any(_WorldSpaceLightPos0.xyz);
        float3 indirectDominantColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
        float3 lightDir = getLightDir(lightEnv, i.worldPos);
        float3 lightCol = getLightCol(lightEnv, _LightColor0.rgb, indirectDominantColor);
        float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
        float3 halfVector = normalize(lightDir + viewDir);
        float3 reflViewDir = getAnisotropicReflectionVector(viewDir, bitangent, tangent, worldNormal, roughness, _Anisotropy*turtles_anisotropy);
        float3 reflLightDir = reflect(lightDir, worldNormal);
    //----

    //DOT PRODUCTS FOR LIGHTING
        float ndl = dot(lightDir, worldNormal);
        float ndl01 = ndl * 0.5 + 0.5;
        float unsaturatedNdl = ndl;
        ndl = saturate(ndl);
        float vdn = abs(dot(viewDir, worldNormal));
        float vdh = saturate(dot(viewDir, halfVector));
        float rdv = saturate(dot(reflLightDir, float4(-viewDir, 0)));
        float ldh = saturate(dot(lightDir, halfVector));
        float ndh = saturate(dot(worldNormal, halfVector));
    //----

    //LIGHTING
    float3 diffuseNDL = ndl; //Modified for diffuse if using Subsurface Preintegrated mode, otherwise use normal Lambertian NDL.

    //Diffuse BRDF
        #if defined(LIGHTMAP_ON)
            float3 indirectDiffuse = 0;
            float3 directDiffuse = albedo * getLightmap(i.uv1, worldNormal, i.worldPos);
            #if defined(DYNAMICLIGHTMAP_ON)
                float3 realtimeLM = getRealtimeLightmap(i.uv2, worldNormal);
                directDiffuse += realtimeLM;
            #endif
        #else
            //Gather up non-important lights
            float3 vertexLightData = 0;
            #if defined(VERTEXLIGHT_ON) && !defined(LIGHTMAP_ON)
                VertexLightInformation vLight = (VertexLightInformation)0;
                float4 vertexLightAtten = float4(0,0,0,0);
                float3 vertexLightColor = get4VertexLightsColFalloff(vLight, worldPos, worldNormal, vertexLightAtten);
                float3 vertexLightDir = getVertexLightsDir(vLight, worldPos, vertexLightAtten);
                for(int i = 0; i < 4; i++)
                {
                    vertexLightData += saturate(dot(vLight.Direction[i], worldNormal)) * vLight.ColorFalloff[i];
                }
            #endif
            float3 indirectDiffuse = (getIndirectDiffuse(worldNormal) * occlusion) + vertexLightData;

            if(_SubsurfaceMethod == 1)
            {
                //Calculates a Subsurface Scattering LUT with some cursed ass shit.
                float3 subsurfaceColor = _SubsurfaceScatteringColor * subsurfaceColorMap * lerp(1, diffuse, _SubsurfaceInheritDiffuse);
                float3 transmission = getTransmission(subsurfaceColor, attenuation, diffuse, 1-curvatureThicknessMap.g, lightDir, viewDir, worldNormal, lightCol, indirectDiffuse);
                float3 subsurface = getSubsurfaceFalloff(ndl01, unsaturatedNdl, curvatureThicknessMap, subsurfaceColor);
                diffuseNDL = lerp(subsurface + transmission, ndl, curvatureThicknessMap.b);
            }

            //turtles minimum light level

            float3 minLight = _MinLightLevel;
            float3 atten = clamp((attenuation * diffuseNDL * lightCol) + indirectDiffuse, minLight*lightCol, float3(1,1,1));
            
            float3 directDiffuse = (albedo * atten);
        #endif
    //----

    //Specular BRDF
    // This is a pretty big hack of a specular brdf but I didn't like other implementations entirely. This is my own, mixed with some other stuff from other places.
    // This probably means it breaks energy conservation, fails the furnace test, etc, but, in my opinion, it looks better.
    // This makes things look a little bit better in baked lighting by forcing a "direct" specular highlight to always be visible by getting the dominant light probe direction and color.
        float3 f0 = 0.16 * reflectance * reflectance * (1.0 - metallic) + diffuse * metallic;
        float3 fresnel = lerp(F_Schlick(vdn, f0), f0, metallic); //Kill fresnel on metallics, it looks bad.
        float3 directSpecular = getDirectSpecular(roughness, ndh, vdn, ndl, ldh, f0, halfVector, tangent, bitangent, _Anisotropy*turtles_anisotropy) * attenuation * ndl * lightCol;
        
        float3 indirectSpecular = getIndirectSpecular(metallic, roughness, reflViewDir, worldPos, directDiffuse, worldNormal) * lerp(fresnel, f0, roughness); //Lightmap is stored in directDiffuse and used for specular lightmap occlusion

    //TODO: Move this into its own function...
        float3 vertexLightSpec = 0;
        float3 vertexLightClearcoatSpec = 0;
        #if defined(VERTEXLIGHT_ON) && !defined(LIGHTMAP_ON)
            [UNROLL(4)]
            for(int i = 0; i < 4; i++)
            {
                // All of these need to be recalculated for each individual light to treat them how we want to treat them.
                float3 vHalfVector = normalize(vLight.Direction[i] + viewDir);
                float vNDL = saturate(dot(vLight.Direction[i], worldNormal));
                float vLDH = saturate(dot(vLight.Direction[i], vHalfVector));
                float vNDH = saturate(dot(worldNormal, vHalfVector));
                float vCndl = saturate(dot(vLight.Direction[i], unmodifiedWorldNormal));
                float vCvdn = abs(dot(viewDir, unmodifiedWorldNormal));
                float vCndh = saturate(dot(unmodifiedWorldNormal, vHalfVector));

                float3 vLspec = getDirectSpecular(roughness, vNDH, vdn, vNDL, vLDH, f0, vHalfVector, tangent, bitangent, _Anisotropy*turtles_anisotropy) * vNDL;
                float3 vLspecCC = getDirectSpecular(clearcoatRoughness, vCndh, vCvdn, vCndl, vLDH, f0, vHalfVector, unmodifiedTangent, unmodifiedBitangent, _ClearcoatAnisotropy) * vNDL;
                vertexLightSpec += vLspec * vLight.ColorFalloff[i];
                vertexLightClearcoatSpec += vLspecCC * vLight.ColorFalloff[i];
            }
        #endif
        float3 specular = (indirectSpecular + directSpecular + vertexLightSpec);
    //----

    //Clearcoat BRDF
        float3 creflViewDir = getAnisotropicReflectionVector(viewDir, unmodifiedBitangent, unmodifiedTangent, unmodifiedWorldNormal, roughness, _ClearcoatAnisotropy);
        float cndl = saturate(dot(lightDir, unmodifiedWorldNormal));
        float cvdn = abs(dot(viewDir, unmodifiedWorldNormal));
        float cndh = saturate(dot(unmodifiedWorldNormal, halfVector));

        float3 clearcoatf0 = 0.16 * clearcoatReflectivity * clearcoatReflectivity;
        float3 clearcoatFresnel = F_Schlick(cvdn, clearcoatf0);
        float3 clearcoatDirectSpecular = getDirectSpecular(clearcoatRoughness, cndh, cvdn, cndl, ldh, clearcoatf0, halfVector, unmodifiedTangent, unmodifiedBitangent, _ClearcoatAnisotropy) * attenuation * cndl * lightCol;
        float3 clearcoatIndirectSpecular = getIndirectSpecular(0, clearcoatRoughness, creflViewDir, worldPos, directDiffuse, unmodifiedWorldNormal);
        float3 clearcoat = (clearcoatDirectSpecular + clearcoatIndirectSpecular + vertexLightClearcoatSpec) * clearcoatReflectivity * clearcoatFresnel;
    //----

    //TODO: Implement subsurface scattering
    float3 litPixel = ( directDiffuse + ((specular + clearcoat) * occlusion)) * _BrightnessMultiplier + emission;
    return float4(max(0, litPixel), alpha);
}