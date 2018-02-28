﻿Shader "Custom/Terrain" {
	Properties {
		_TexArr("Texture Array", 2DArray) = "" {}
		_Type("Type (0=terrain, 1=trees)", int)=0
	}
	SubShader {
		Pass {
			Tags{ 
				"Queue" = "Transparent"
				"LightMode" = "ForwardBase" 
			}
			LOD 300

			Blend SrcAlpha OneMinusSrcAlpha
			AlphaToMask On

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma target 3.5

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "utils.hlsl"
			#include "textures.hlsl"
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

			struct appdata {
				float4 vertex : POSITION;
				float4 normal : NORMAL;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;

			};

			struct v2f {
				float4 pos : SV_POSITION;
				float4 color : COLOR0;
				float3 uv : TEXCOORD0;
				float3 posEye : TEXCOORD1;
				float3 lightDirEye : TEXCOORD2;
				float3 eyeNormal : TEXCOORD3;
				float3 worldPos : TEXCOORD4;
				SHADOW_COORDS(5) // put shadows data into TEXCOORD5
				fixed3 diff : COLOR2;
				fixed3 ambient : COLOR3;
			};
	
			v2f vert(appdata v) {
				v2f o;
				//Usuefull data
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
				o.eyeNormal = worldNormal; // normalize(UnityObjectToViewPos(v.normal));
				o.posEye = UnityObjectToViewPos(v.vertex);
				o.lightDirEye = normalize(_WorldSpaceLightPos0); //It's a directional light
				//Light
				half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = nl;
				o.ambient = ShadeSH9(half4(worldNormal, 1));
				//Shadow
				TRANSFER_SHADOW(o);

				o.uv.xy = v.uv;
				o.color = v.color;	
				return o;
			}

			UNITY_DECLARE_TEX2DARRAY(_TexArr);

			float4 frag(v2f i) : SV_Target {
				//shadow
				fixed shadow = SHADOW_ATTENUATION(i);
				//light
				float3 specular = calcSpecular(i.lightDirEye, i.eyeNormal, i.posEye, 1);
				fixed3 light = (/*i.diff +*/ specular * 1) /** shadow + i.ambient*/;

				int baseType = i.color.r + 0.5;
				int modType = i.color.g + 0.5;
				half4 o;

				//o = getTexel(baseType, modType, i.worldPos, i.posEye, UNITY_SAMPLE_TEX2DARRAY(_TexArr, float3(i.uv.x, i.uv.y, 1)));
				o.a = 1;
				o.rbg = i.eyeNormal;
				return o;
			}
			ENDCG
		}
	
		// shadow caster rendering pass
		// using macros from UnityCG.cginc
		Pass {
			Tags{ "LightMode" = "ShadowCaster" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"

			struct v2f {
				V2F_SHADOW_CASTER;
			};

			v2f vert(appdata_base v) {
				v2f o;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
					return o;
			}

			float4 frag(v2f i) : SV_Target{
				SHADOW_CASTER_FRAGMENT(i)
			}
				ENDCG
		}
		// shadow casting support
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}