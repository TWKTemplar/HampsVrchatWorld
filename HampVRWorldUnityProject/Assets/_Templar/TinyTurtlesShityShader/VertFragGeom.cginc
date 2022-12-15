//This file contains the vertex, fragment, and Geometry functions for both the ForwardBase and Forward Add pass.
#if defined(SHADOWS_CUBE) && !defined(SHADOWS_CUBE_IN_DEPTH_TEX)
#define V2F_SHADOW_CASTER_NOPOS float3 vec : TEXCOORD0;
#define TRANSFER_SHADOW_CASTER_NOPOS_GEOMETRY(o,opos) o.vec = mul(unity_ObjectToWorld, v[i].vertex).xyz - _LightPositionRange.xyz; opos = o.pos;
#else
#define V2F_SHADOW_CASTER_NOPOS
#define TRANSFER_SHADOW_CASTER_NOPOS_GEOMETRY(o, opos, vertexPosition, vertexNormal) \
        opos = UnityClipSpaceShadowCasterPos(vertexPosition, vertexNormal); \
        opos = UnityApplyLinearShadowBias(opos);
#endif

v2g vert (appdata v)
{
    v2g o = (v2g)0;
    //o.vertex = v.vertex;
    o.uv = v.uv;
    
    #ifndef UNITY_PASS_SHADOWCASTER
    o.uv1 = v.uv1;
    o.uv2 = v.uv2;
    #endif
    
    o.normal = v.normal;
    o.tangent = v.tangent;

	o.turtles_colour=v.turtles_colour;

    uint clipMe = 0;
    float nan = 0. / 0.;

    if(o.turtles_colour.g<0.1)
        clipMe+=(_Cutoff<0.3)?1:0;
        
    if(o.turtles_colour.r<0.1)
        clipMe+=(_Cutoff>0.5)?1:0;

    o.vertex = clipMe>0?float4(nan,nan,nan,nan):v.vertex;
    return o;
}

[maxvertexcount(3)]
void geom(triangle v2g v[3], inout TriangleStream<g2f> tristream)
{
    g2f o = (g2f)0;

    for (int i = 0; i < 3; i++)
    {
        v[i].vertex.xyz += _VertexOffset * v[i].normal;
        o.pos = UnityObjectToClipPos(v[i].vertex);
        o.uv = v[i].uv;

        #ifndef UNITY_PASS_SHADOWCASTER
        o.uv1 = v[i].uv1;
        o.uv2 = v[i].uv2;
        #endif 
        
        float3 worldNormal = UnityObjectToWorldNormal(v[i].normal);
        float3 tangent = UnityObjectToWorldDir(v[i].tangent);
        float3 bitangent = cross(tangent, worldNormal) * v[i].tangent.w;

        o.btn[0] = bitangent;
        o.btn[1] = tangent;
        o.btn[2] = worldNormal;
        o.worldPos = mul(unity_ObjectToWorld, v[i].vertex);
        o.objPos = v[i].vertex;
        o.objNormal = v[i].normal;
        o.screenPos = ComputeScreenPos(o.pos);

	    o.turtles_colour=v[i].turtles_colour;

 /*
        float clipMe = 0;
        if(o.turtles_colour.g<0.1)
            clipMe+=(_Cutoff<0.3)?1:0;
        
        if(o.turtles_colour.r<0.1)
            clipMe+=(_Cutoff>0.5)?1:0;
*/

        //Only pass needed things through for shadow caster
        #if !defined(UNITY_PASS_SHADOWCASTER)
        UNITY_TRANSFER_SHADOW(o, o.uv);
        #else
        TRANSFER_SHADOW_CASTER_NOPOS_GEOMETRY(o, o.pos, v[i].vertex, v[i].normal);
        #endif
        
            tristream.Append(o);
    }
    tristream.RestartStrip();
}
			
fixed4 frag (g2f i) : SV_Target
{
    //clip(i.turtles_colour.r-_Cutoff);
    //if(i.turtles_colour.g<0.5) {
    //    clip(_Cutoff-0.3);
    //}

        //Return only this if in the shadowcaster
    #if defined(UNITY_PASS_SHADOWCASTER)
        float4 albedo = texTP(_MainTex, _MainTex_ST, i.worldPos, i.objPos, i.btn[2], i.objNormal, _TriplanarFalloff, i.uv) * _Color;
        float alpha;
        /*float mAlpha;

        mAlpha = i.turtles_colour.r;
        */
        
        doAlpha(alpha, albedo.a, i.screenPos);
        SHADOW_CASTER_FRAGMENT(i);
    #else
        return CustomStandardLightingBRDF(i);
    #endif
}