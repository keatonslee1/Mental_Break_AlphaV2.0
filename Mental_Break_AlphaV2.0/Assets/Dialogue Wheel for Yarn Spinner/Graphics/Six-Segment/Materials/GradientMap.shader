// Sprites/Gradient Map is a sprite shader that takes the red channel of the
// main texture, and uses that with a 1D lookup texture to get a final colour. 
// It's used in the Yarn Spinner Dialogue Wheel to allow for easy re-colouring
// of the black and white dialogue wheel sprites.

// Based on Unity built-in shader source. Copyright (c) 2016 Unity Technologies

Shader "Sprites/Gradient Map"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        // The gradient texture to sample from.
        _GradientTex ("Gradient", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment GradientSpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            sampler2D _GradientTex;

            fixed4 GradientSpriteFrag(v2f IN) : SV_Target
            {
                // Fetch the pixel from the source texture.
                fixed4 sourceColour = SampleSpriteTexture (IN.texcoord);

                // Use the red value in that pixel to look up the gradient colour.
                fixed4 mappedColour = tex2D(_GradientTex, float2(sourceColour.r, 0));

                // Tint the colour based on the renderer settings.
                mappedColour *= IN.color;
                mappedColour.rgb *= sourceColour.a * IN.color.a;
                mappedColour.a = sourceColour.a * IN.color.a;

                return mappedColour;
            }
        ENDCG
        }
    }
}