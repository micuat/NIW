Shader "Custom/GlassReflective" {
	Properties {
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
		_ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
		_Cube ("Reflection Cubemap", Cube) = "black" { TexGen CubeReflect }
	}
	SubShader {
		Tags {
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
		}
		LOD 300
		
		CGPROGRAM
//			#pragma surface surf BlinnPhong decal:add nolightmap
			#pragma surface surf SimpleSpecular decal:add
			samplerCUBE _Cube;
			
			fixed4 _ReflectColor;
			half _Shininess;
			
			struct Input {
				float3 worldRefl;
				float3 viewDir;
			};
			
			half4 LightingSimpleSpecular (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
				half3 h = normalize (lightDir + viewDir);

				half diff = max (0, dot (s.Normal, lightDir));

				float nh = max (0, dot (s.Normal, h));
				float spec = pow (nh, 4.0);

				half4 c;
				c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * (atten * 2);
				c.a = s.Alpha;
				return c;
			}
 			
			void surf (Input IN, inout SurfaceOutput o) {
				o.Albedo = 0;
				o.Gloss = 1;
				o.Specular = _Shininess;
				
				fixed4 reflcol = texCUBE (_Cube, IN.worldRefl);
				o.Albedo = reflcol.rgb * _ReflectColor.rgb;
//				o.Emission = reflcol.rgb * _ReflectColor.rgb;
		 		o.Alpha = reflcol.a * _ReflectColor.a;
			}
		ENDCG
	}
	FallBack "Transparent/VertexLit"
}