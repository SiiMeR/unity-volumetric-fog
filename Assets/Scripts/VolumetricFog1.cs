// base code from https://github.com/Flafla2/Generic-Raymarch-Unity
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class VolumetricFog1 : SceneViewFilter {

    public Transform SunLight;

    [SerializeField]
    private Shader _EffectShader;
    [SerializeField]
    private Texture2D _NoiseTex;


    private Texture3D fog3D;

    public Material EffectMaterial
    {
        get
        {
            if (!_EffectMaterial && _EffectShader)
            {
                _EffectMaterial = new Material(_EffectShader);
                _EffectMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return _EffectMaterial;
        }
    }
    private Material _EffectMaterial;
    
    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (EffectMaterial == null)
        {
            Graphics.Blit(source, destination); // do nothing
            return;
        }
        if (!fog3D)
        {
            fog3D = CreateTexture3DFrom2DSlices(_NoiseTex, 128);
        }

        // Set any custom shader variables here.  For example, you could do:
        // EffectMaterial.SetFloat("_MyVariable", 13.37f);
        // This would set the shader uniform _MyVariable to value 13.37
     //   Vector3 cameraPos = CurrentCamera.transform.position;
        // sunLight.rotation.x 364 -> 339, 175 -> 201

        EffectMaterial.SetTexture("_MainTex", source);

        EffectMaterial.SetTexture("_FogTex", fog3D);
        
//        EffectMaterial.SetMatrix("_FrustumCornersES", GetFrustumCorners(CurrentCamera));
   //     EffectMaterial.SetMatrix("_CameraInvViewMatrix", CurrentCamera.cameraToWorldMatrix);
//        EffectMaterial.SetVector("_CameraWS", CurrentCamera.transform.position);
        
          
//        Shader.SetGlobalVector("_Screen_TexelSize", new Vector4(1.0f / source.width, 1.0f / source.height, source.width, source.height));
//        Shader.SetGlobalVector("_FogTex_TexelSize", new Vector4(1.0f / 256, 1.0f / 256, 1.0f/128, 0));
        
//        EffectMaterial.SetFloat("_CameraFarOverMaxFar", CurrentCamera.farClipPlane / farClip);
//        EffectMaterial.SetFloat("_NearOverFarClip", nearClip / farClip);

       // RaycastCornerBlit(source, destination, EffectMaterial);
        Graphics.Blit(source, destination, EffectMaterial, 0);
    }

    /// \brief Stores the normalized rays representing the camera frustum in a 4x4 matrix.  Each row is a vector.
    /// 
    /// The following rays are stored in each row (in eyespace, not worldspace):
    /// Top Left corner:     row=0
    /// Top Right corner:    row=1
    /// Bottom Right corner: row=2
    /// Bottom Left corner:  row=3
    private Matrix4x4 GetFrustumCorners(Camera cam)
    {
        float camFov = cam.fieldOfView;
        float camAspect = cam.aspect;

        Matrix4x4 frustumCorners = Matrix4x4.identity;

        float fovWHalf = camFov * 0.5f;

        float tan_fov = Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

        Vector3 toRight = Vector3.right * tan_fov * camAspect;
        Vector3 toTop = Vector3.up * tan_fov;

        Vector3 topLeft = (-Vector3.forward - toRight + toTop);
        Vector3 topRight = (-Vector3.forward + toRight + toTop);
        Vector3 bottomRight = (-Vector3.forward + toRight - toTop);
        Vector3 bottomLeft = (-Vector3.forward - toRight - toTop);

        frustumCorners.SetRow(0, topLeft);
        frustumCorners.SetRow(1, topRight);
        frustumCorners.SetRow(2, bottomRight);
        frustumCorners.SetRow(3, bottomLeft);

        return frustumCorners;
    }
    
    void RaycastCornerBlit(RenderTexture source, RenderTexture dest, Material mat)
    {
        // Compute Frustum Corners
        float camFar = CurrentCamera.farClipPlane;
        float camFov = CurrentCamera.fieldOfView;
        float camAspect = CurrentCamera.aspect;

        float fovWHalf = camFov * 0.5f;

        Vector3 toRight = CurrentCamera.transform.right * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * camAspect;
        Vector3 toTop = CurrentCamera.transform.up * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

        Vector3 topLeft = (CurrentCamera.transform.forward - toRight + toTop);
        float camScale = topLeft.magnitude * camFar;

        topLeft.Normalize();
        topLeft *= camScale;

        Vector3 topRight = (CurrentCamera.transform.forward + toRight + toTop);
        topRight.Normalize();
        topRight *= camScale;

        Vector3 bottomRight = (CurrentCamera.transform.forward + toRight - toTop);
        bottomRight.Normalize();
        bottomRight *= camScale;

        Vector3 bottomLeft = (CurrentCamera.transform.forward - toRight - toTop);
        bottomLeft.Normalize();
        bottomLeft *= camScale;

        // Custom Blit, encoding Frustum Corners as additional Texture Coordinates
        RenderTexture.active = dest;

        mat.SetTexture("_MainTex", source);

        GL.PushMatrix();
        GL.LoadOrtho();

        mat.SetPass(0);

        GL.Begin(GL.QUADS);

        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.MultiTexCoord(1, bottomLeft);
        GL.Vertex3(0.0f, 0.0f, 0.0f);

        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.MultiTexCoord(1, bottomRight);
        GL.Vertex3(1.0f, 0.0f, 0.0f);

        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.MultiTexCoord(1, topRight);
        GL.Vertex3(1.0f, 1.0f, 0.0f);

        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.MultiTexCoord(1, topLeft);
        GL.Vertex3(0.0f, 1.0f, 0.0f);

        GL.End();
        GL.PopMatrix();
    }
    
    
    /// \brief Custom version of Graphics.Blit that encodes frustum corner indices into the input vertices.
    /// 
    /// In a shader you can expect the following frustum cornder index information to get passed to the z coordinate:
    /// Top Left vertex:     z=0, u=0, v=0
    /// Top Right vertex:    z=1, u=1, v=0
    /// Bottom Right vertex: z=2, u=1, v=1
    /// Bottom Left vertex:  z=3, u=1, v=0
    /// 
    /// \warning You may need to account for flipped UVs on DirectX machines due to differing UV semantics
    ///          between OpenGL and DirectX.  Use the shader define UNITY_UV_STARTS_AT_TOP to account for this.
    static void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material fxMaterial, int passNr)
    {
        RenderTexture.active = dest;

        fxMaterial.SetTexture("_MainTex", source);

        GL.PushMatrix();
        GL.LoadOrtho(); // Note: z value of vertices don't make a difference because we are using ortho projection

        fxMaterial.SetPass(passNr);

        GL.Begin(GL.QUADS);

        // Here, GL.MultitexCoord2(0, x, y) assigns the value (x, y) to the TEXCOORD0 slot in the shader.
        // GL.Vertex3(x,y,z) queues up a vertex at position (x, y, z) to be drawn.  Note that we are storing
        // our own custom frustum information in the z coordinate.
        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f); // BL

        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f); // BR

        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f); // TR

        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f); // TL
        
        GL.End();
        GL.PopMatrix();
}
    public Camera CurrentCamera
    {
        get
        {
            if (!_CurrentCamera)
            {
                _CurrentCamera = GetComponent<Camera>();
                _CurrentCamera.depthTextureMode = DepthTextureMode.Depth;
            }
            
            return _CurrentCamera;
        }
    }
    private Camera _CurrentCamera;

    Texture3D CreateTexture3DFrom2DSlices(Texture2D tex, int size)
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

        Texture3D texture3D = new Texture3D(size, size, size, TextureFormat.ARGB32, true);
        texture3D.SetPixels(colors);
        texture3D.Apply();
        return texture3D;
    }
    // https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-
    Texture2D getReadableTexture(Texture2D texture)
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
