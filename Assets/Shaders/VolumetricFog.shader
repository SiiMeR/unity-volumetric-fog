
Shader "Custom/VolumetricFog" {
	Properties
	{
		_Radius("Radius", Range(0,3)) = 1
		_SpecularPower("Specular Power", Range(0,1)) = 0.5
		_Gloss("Gloss", Range(0,1)) = 0.5
		_Centre("Centre", float) = 0
		_Color("Color", Color) = (1,1,0,1)
	}
	SubShader
	{

		Tags {"Queue"="Transparent" "RenderType"="Transparent"}
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Pass
	{
	    CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
    
        #include "UnityCG.cginc"
    
        sampler2D _MainTex;
        float _Radius;
        float _Centre;
        fixed4 _Color;
        float _SpecularPower;
        float _Gloss;
        float3 viewDirection;
    
        #define STEPS 64
        #define STEP_SIZE 0.01
        #define MIN_DISTANCE 0.01
    
        struct appdata {
            float4 vertex : POSITION;
        };
    
        struct v2f {
            float4 vertex : SV_POSITION;
            float3 wPos : TEXCOORD1; // World Position
        };
    
        v2f vert(appdata v) {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.wPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            return o;
        }
        #include "Lighting.cginc"
    
        fixed4 simpleLambert(fixed3 normal) {
            fixed3 lightDir = _WorldSpaceLightPos0.xyz;
            fixed3 lightCol = _LightColor0.rgb;
            fixed NdotL = max(dot(normal, lightDir), 0);
    
            fixed3 h = (lightDir - viewDirection) / 2;
            fixed s = pow(dot(normal, h), _SpecularPower) * _Gloss;
            fixed4 c;
            c.rgb = _Color * lightCol * NdotL + s;
            c.a = 0.5;
            return c;
        }
    	float sphereDistance(float3 p){
		    return distance(p, _Centre) - _Radius;
	    }
	    
        float3 normal(float3 p) {
            const float eps = 0.01;
            return normalize(
                float3(
                    sphereDistance(p + float3(eps, 0, 0)) - sphereDistance(p - float3(eps, 0, 0)),
                    sphereDistance(p + float3(0, eps, 0)) - sphereDistance(p - float3(0, eps, 0)),
                    sphereDistance(p + float3(0, 0, eps)) - sphereDistance(p - float3(0, 0, eps))
                    )
            );
        }
        fixed4 renderSurface(float3 p) {
            float3 n = normal(p);
            return simpleLambert(n);
        }
        fixed4 raymarch(float3 position, float3 direction)
        {
            for (int i = 0; i < STEPS; i++)
            {
                float dist = sphereDistance(position);
                if (dist < MIN_DISTANCE)
                    return renderSurface(position); 
    
                position += dist * direction;
            }
    
            return fixed4(1, 1, 1, 1); // White
        }
    
        fixed4 frag(v2f i) : SV_Target
        {
            float3 worldPosition = i.wPos;
            viewDirection = normalize(i.wPos - _WorldSpaceCameraPos);
    
            return raymarch(worldPosition, viewDirection);
        }
        ENDCG
	}
	}
}

