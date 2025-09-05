#ifndef _CUSTOM_SHADOW_ATTENUATION
#define _CUSTOM_SHADOW_ATTENUATION

void CustomShadowAttenuation(in float4 positionWS, out float shadowAttenuation)
{
	float4 shadowCoords = TransformWorldToShadowCoord( positionWS );
	Light mainLight = GetMainLight(shadowCoords);
	shadowAttenuation = mainLight.shadowAttenuation;
}

#endif