// https://github.com/Unity-Technologies/VolumetricLighting/blob/master/Assets/VolumetricFog/Shaders/VolumetricFog.cginc
/*sampler3D _VolumeScatter;
float4 _VolumeScatter_TexelSize;
float4 _Screen_TexelSize;
float _CameraFarOverMaxFar;
float _NearOverFarClip;
*/


int ihash(int n)
{
	n = (n<<13)^n;
	return (n*(n*n*15731+789221)+1376312589) & 2147483647;
}

float frand(int n)
{
	return ihash(n) / 2147483647.0;
}

float3 cellNoise(int3 p)
{
	int i = p.y*256 + p.x + p.z;
	return float3(frand(i), frand(i + 57), frand(i-57)) - 0.5;//*2.0-1.0;
}


