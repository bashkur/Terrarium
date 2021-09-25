/*
using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
    //PostProcessEvent
    //BeforeTransparent: the effect will only be applied to opaque objects before the transparent pass is done.
    //BeforeStack: the effect will be applied before the built-in stack kicks-in. That includes anti-aliasing, depth-of-field, tonemapping etc.
    //AfterStack: the effect will be applied after the builtin stack and before FXAA (if it's enabled) & final-pass dithering.

[Serializable]
[PostProcess(typeof(PostProcessOutlineRenderer), PostProcessEvent.BeforeStack, "Custom/Post Process Outline")]
public sealed class PostProcessOutline : PostProcessEffectSettings
{
   //exposes this value to the unity editor
   public IntParameter scale = new IntParameter { value = 2 };

   //exposes this value to the unity editor
   public FloatParameter depthThreshold = new FloatParameter { value = 1.5f };

   //exposes this value to the unity editor
   [Range(0, 1)]
   public FloatParameter normalThreshold = new FloatParameter { value = 0.4f };

   //exposes this value to the unity editor
   [Range(0, 1)]
   public FloatParameter depthNormalThreshold = new FloatParameter { value = 0.5f };

   //exposes this value to the unity editor
   public FloatParameter depthNormalThresholdScale = new FloatParameter { value = 7 };

   //exposes this value to unity editor
   public ColorParameter color = new ColorParameter { value = Color.white };

   //exposes this value to unity editor
   public TextureParameter texture = new TextureParameter { value = null };

   //exposes this value to unity editor
   [Range(0, 1)]
   public FloatParameter chunkDepth = new FloatParameter { value = 0.0f };
   //this value tells us how many units of depth will get what color

   //exposes this value to unity editor
   [Range(0, 128)]
   public FloatParameter pixelDepth = new FloatParameter { value = 4.0f };
   //how large of a step we should take within the given gradient to asign color
}

public sealed class PostProcessOutlineRenderer : PostProcessEffectRenderer<PostProcessOutline>
{
   public override void Render(PostProcessRenderContext context)
   {
       var sheet = context.propertySheets.Get(Shader.Find("S_Outline"));
       sheet.properties.SetFloat("_Scale", settings.scale);
       //sets the scale value like with uniforms opengl shaders
       sheet.properties.SetFloat("_DepthThreshold", settings.depthThreshold);
       //sets the depthThreshold value like with uniforms opengl shaders
       sheet.properties.SetFloat("_NormalThreshold", settings.normalThreshold);
       //sets the normalThreshold value like with uniforms opengl shaders
       Matrix4x4 clipToView = GL.GetGPUProjectionMatrix(context.camera.projectionMatrix, true).inverse;
       sheet.properties.SetMatrix("_ClipToView", clipToView);
       //we need this to get the camera view vector to get rid of artifacts
       sheet.properties.SetFloat("_DepthNormalThreshold", settings.depthNormalThreshold);
       //same as with depth test but now with normals to get the straggler edges
       sheet.properties.SetFloat("_DepthNormalThresholdScale", settings.depthNormalThresholdScale);
       //same as with depth test but now with normals to get the straggler edges
       sheet.properties.SetColor("_Color", settings.color);
       //gives shader our color
       sheet.properties.SetTexture("_GradientTexture", settings.texture);
       //gives shader our texture
       sheet.properties.SetFloat("_ChunkDepth", settings.chunkDepth);
       //gives shader our step # in regards to unity units of depth
       sheet.properties.SetFloat("_PixelDepth", settings.pixelDepth);
       //gives shader our step # in regards to pixels in the gradient (1 = smooth transition)

       context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
   }
}
*/

using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(GrayscaleRenderer), PostProcessEvent.AfterStack, "Custom/Grayscale")]
public sealed class Grayscale : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("Grayscale effect intensity.")]
    public FloatParameter blend = new FloatParameter { value = 0.5f };

    public override bool IsEnabledAndSupported(PostProcessRenderContext context)
    {
        return enabled.value
            && blend.value > 0f;
    }
}

public sealed class GrayscaleRenderer : PostProcessEffectRenderer<Grayscale>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/Grayscale"));
        sheet.properties.SetFloat("_Blend", settings.blend);
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}