Shader "RUIS/KeystoneBorder" {
    Properties {
	   _MainTex ("Base (RGB)", 2D) = "white" {}
    }

    Category {
       Lighting Off
	   ZWrite On
	   ZTest Always
       Cull Back

       SubShader {
	       Pass {
		        SetTexture [_MainTex] 
			}
		} 
	}
}