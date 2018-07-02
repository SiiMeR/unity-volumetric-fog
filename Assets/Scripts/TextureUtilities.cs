using System;
using UnityEngine;
using UnityEngine.Rendering;

public static class TextureUtilities
{
    /* public static RenderTexture CreateFogLUT3DFrom2DSlices(Texture2D tex, Vector3Int dimensions)
     {
         
         var readableTexture2D = GetReadableTexture(tex);
 
         var colors = new Color[dimensions.x * dimensions.y * dimensions.z];
 
         var idx = 0;
 
         for (var z = 0; z < dimensions.z; ++z)
         {
             for (var y = 0; y < dimensions.y ; ++y)
             {
                 for (var x = 0; x < dimensions.x; ++x, ++idx)
                 {
                     colors[idx] = readableTexture2D.GetPixel(x + z * dimensions.z, y);
                 }
             }
         }
 
         var texture3D = new Texture3D(dimensions.x, dimensions.y, dimensions.z, TextureFormat.RGBAHalf, true);
         texture3D.SetPixels(colors);
         texture3D.Apply();
         return texture3D;
     }*/

    // https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-
    public static Texture2D GetReadableTexture(Texture2D texture)
    {
        // Create a temporary RenderTexture of the same size as the texture

        var tmp = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        // Blit the pixels on texture to the RenderTexture
        Graphics.Blit(texture, tmp);
        var previous = RenderTexture.active;
        RenderTexture.active = tmp;
        var myTexture2D = new Texture2D(texture.width, texture.height);
        myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        myTexture2D.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tmp);
        return myTexture2D; 
    }

    public static RenderTexture CreateFogLUT3D(Texture2D fogTexture, NoiseSource noiseSource, Vector3Int dimensions, ComputeShader shader)
    {
        switch (noiseSource)
        {
            case NoiseSource.SimplexNoiseCompute:
                return CreateFogLUT3DFromSimplexNoise(dimensions, shader);

            case NoiseSource.Texture3D:
            case NoiseSource.Texture3DCompute:
                return CreateFogLUT3DFrom2DSlicesCompute(fogTexture, dimensions, shader);

            //    return CreateFogLUT3DFrom2DSlices(fogTexture, dimensions);//160, 90, 128

            default:
                throw new ArgumentOutOfRangeException(nameof(noiseSource), noiseSource, null);
        }
    }

    private static RenderTexture CreateFogLUT3DFrom2DSlicesCompute(Texture2D fogTexture, Vector3Int dimensions, ComputeShader shader)
    {
        var kernel = shader.FindKernel("Create3DLUTFrom2D");
        
        var fogLut3D = new RenderTexture(dimensions.x, dimensions.y, 0, RenderTextureFormat.ARGBHalf,
            RenderTextureReadWrite.Linear)
        {
            volumeDepth = dimensions.z,
            dimension = TextureDimension.Tex3D,
            enableRandomWrite = true,
            name = "FogLUT3DFrom2D"
        };
        fogLut3D.Create();
        
        shader.SetTexture(kernel, "_FogLUT3DFrom2D", fogLut3D);
        shader.SetTexture(kernel, "_FogTexture2D", fogTexture);
        shader.Dispatch(kernel, dimensions.x, dimensions.y, dimensions.z);
        
        return fogLut3D;
    }


    private static RenderTexture CreateFogLUT3DFromSimplexNoise(Vector3Int dimensions, ComputeShader shader)
    {
        var kernel = shader.FindKernel("Create3DLUTSimplexNoise");
        
        var fogLut3D = new RenderTexture(dimensions.x, dimensions.y, 0, RenderTextureFormat.ARGBHalf,
            RenderTextureReadWrite.Linear)
        {
            volumeDepth = dimensions.z,
            dimension = TextureDimension.Tex3D,
            enableRandomWrite = true,
            name = "FogLUT3DSnoise"
        };
        fogLut3D.Create();
        
        shader.SetTexture(kernel, "_FogLUT3DSNoise", fogLut3D);
        shader.Dispatch(kernel, dimensions.x, dimensions.y, dimensions.z);
        
        return fogLut3D;
    }
}