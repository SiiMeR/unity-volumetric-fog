using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureUtilities {

	public static Texture3D CreateTexture3DFrom2DSlices(Texture2D tex, int size)
	{
		Texture2D readableTexture2D = getReadableTexture(tex);

		Color[] colors = new Color[size * size * size];

		int idx = 0;
        
		for (int z = 0; z < size; ++z)
		{
			for (int y = 0; y < size; ++y)
			{
				for (int x = 0; x < size; ++x, ++idx)
				{
					colors[idx] = readableTexture2D.GetPixel(x + z * size, y);
				}
			}
		}

		Texture3D texture3D = new Texture3D(size, size, size, TextureFormat.RGBA32, true);
		texture3D.SetPixels(colors);
		texture3D.Apply();
		return texture3D;
	}
	// https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-
	public static Texture2D getReadableTexture(Texture2D texture)
	{
		// Create a temporary RenderTexture of the same size as the texture
        
		RenderTexture tmp = RenderTexture.GetTemporary(
			texture.width,
			texture.height,
			0,
			RenderTextureFormat.Default,
			RenderTextureReadWrite.Linear);

		// Blit the pixels on texture to the RenderTexture
		Graphics.Blit(texture, tmp);
		RenderTexture previous = RenderTexture.active;
		RenderTexture.active = tmp;
		Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
		myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
		myTexture2D.Apply();
		RenderTexture.active = previous;
		RenderTexture.ReleaseTemporary(tmp);
		return myTexture2D;
	}
}
