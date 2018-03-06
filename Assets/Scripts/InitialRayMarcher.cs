using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Effects/Raymarch (Generic Complete)")]
public class InitialRayMarcher : SceneViewFilter
{
    public Transform SunLight;

    [SerializeField] private Shader _ApplyFogShader;
    [SerializeField] private Shader _CalculateFogShader;
    [SerializeField] private Shader _ApplyBlurShader;
    
    
    [SerializeField] private float _RaymarchDrawDistance = 40;
    [SerializeField] private Texture2D _FogTexture2D;
    
    [SerializeField] private float _FogDensityCoef = 0.3f;
    [SerializeField] private float _ScatteringCoef = 0.25f;
    [SerializeField] private float _ExtinctionCoef = 0.01f;
    

    private Texture3D _FogTexture3D;
    
    private Material _ApplyFogMaterial;
    
    public Material ApplyFogMaterial
    {
        get
        {
            if (!_ApplyFogMaterial && _ApplyFogShader)
            {
                _ApplyFogMaterial = new Material(_ApplyFogShader);
                _ApplyFogMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return _ApplyFogMaterial;
        }
    }


    private Material _CalculateFogMaterial;

    public Material CalculateFogMaterial
    {
        get
        {
            if (!_CalculateFogMaterial && _CalculateFogShader)
            {
                _CalculateFogMaterial = new Material(_CalculateFogShader);
                _CalculateFogMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return _CalculateFogMaterial;
        }
    }

    private Material _ApplyBlurMaterial;

    public Material ApplyBlurMaterial
    {
        get
        {
            if (!_ApplyBlurMaterial && _ApplyBlurShader)
            {
                _ApplyBlurMaterial = new Material(_ApplyBlurShader);
                _ApplyBlurMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return _ApplyBlurMaterial;
        }
    }
    
    private Camera _CurrentCamera;
    
    public Camera CurrentCamera
    {
        get
        {
            if (!_CurrentCamera)
                _CurrentCamera = GetComponent<Camera>();
            return _CurrentCamera;
        }
    }


    private CommandBuffer _AfterShadowPass;
    
    void Start()
    {
        AddLightCommandBuffer();
    }
    
    void OnDestroy()
    {
        RemoveLightCommandBuffer();
    }
    
    private void RemoveLightCommandBuffer()
    {
        // TODO : SUPPORT MULTIPLE LIGHTS 
        if (SunLight != null)
        {
            Light light = SunLight.GetComponent<Light>();    
        }
        
        if (_AfterShadowPass != null && GetComponent<Light>())
        {
            GetComponent<Light>().RemoveCommandBuffer(LightEvent.AfterShadowMap, _AfterShadowPass);
        }
    }



    
    
    // based on https://interplayoflight.wordpress.com/2015/07/03/adventures-in-postprocessing-with-unity/
    void AddLightCommandBuffer()
    {
        _AfterShadowPass = new CommandBuffer();
        _AfterShadowPass.name = "Volumetric Fog ShadowMap";
        _AfterShadowPass.SetGlobalTexture("ShadowMap", new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive));

        Light light = SunLight.GetComponent<Light>();
    
        if (light)
        {
            light.AddCommandBuffer(LightEvent.AfterShadowMap, _AfterShadowPass);
        }

    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Matrix4x4 corners = GetFrustumCorners(CurrentCamera);
        Vector3 pos = CurrentCamera.transform.position;

        for (int x = 0; x < 4; x++) {
            corners.SetRow(x, CurrentCamera.cameraToWorldMatrix * corners.GetRow(x));
            Gizmos.DrawLine(pos, pos + (Vector3)(corners.GetRow(x)));
        }

        /*
        // UNCOMMENT TO DEBUG RAY DIRECTIONS
        Gizmos.color = Color.red;
        int n = 10; // # of intervals
        for (int x = 1; x < n; x++) {
            float i_x = (float)x / (float)n;

            var w_top = Vector3.Lerp(corners.GetRow(0), corners.GetRow(1), i_x);
            var w_bot = Vector3.Lerp(corners.GetRow(3), corners.GetRow(2), i_x);
            for (int y = 1; y < n; y++) {
                float i_y = (float)y / (float)n;
                
                var w = Vector3.Lerp(w_top, w_bot, i_y).normalized;
                Gizmos.DrawLine(pos + (Vector3)w, pos + (Vector3)w * 1.2f);
            }
        }
        */
    }

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!ApplyFogMaterial || !_ApplyFogShader || !CalculateFogMaterial || !_CalculateFogShader )
        {
            print("sum ting fong");
            Graphics.Blit(source, destination); // do nothing
            return;
        }

        if (!_FogTexture3D)
        {
            _FogTexture3D = TextureUtilities.CreateTexture3DFrom2DSlices(_FogTexture2D, 16);
        }
        
        int fogRTW = source.width;
        int fogRTH = source.height;
        
        RenderTexture fogRenderTexture1 = RenderTexture.GetTemporary(fogRTW, fogRTH, 0 , RenderTextureFormat.ARGBHalf);
        RenderTexture fogRenderTexture2 = RenderTexture.GetTemporary(fogRTW, fogRTH, 0 , RenderTextureFormat.ARGBHalf);

        fogRenderTexture1.filterMode = FilterMode.Bilinear;
        fogRenderTexture2.filterMode = FilterMode.Bilinear;
        
        CalculateFogMaterial.SetVector ("_LightColor", SunLight.GetComponent<Light>().color.linear);
        CalculateFogMaterial.SetColor("_ShadowColor", Color.black); // TODO : CHANGE THIS  
        CalculateFogMaterial.SetFloat ("_LightIntensity", SunLight.GetComponent<Light>().intensity);
        CalculateFogMaterial.SetTexture("_NoiseTex", _FogTexture2D);
        CalculateFogMaterial.SetFloat("_FogDensityCoef", _FogDensityCoef);
        CalculateFogMaterial.SetFloat("_ScatteringCoef", _ScatteringCoef);
        CalculateFogMaterial.SetFloat("_ExtinctionCoef", _ExtinctionCoef);
        CalculateFogMaterial.SetFloat("_DrawDistance", _RaymarchDrawDistance);
        CalculateFogMaterial.SetMatrix("_FrustumCornersES", GetFrustumCorners(CurrentCamera));
        CalculateFogMaterial.SetMatrix("_CameraInvViewMatrix", CurrentCamera.cameraToWorldMatrix);
        CalculateFogMaterial.SetMatrix("_CameraInvProjMatrix", CurrentCamera.projectionMatrix.inverse);
        CalculateFogMaterial.SetVector("_CameraWS", CurrentCamera.transform.position);
        
        //CustomGraphicsBlit(source, fogRenderTexture1, CalculateFogMaterial, 0);
        Graphics.Blit(source, fogRenderTexture1, CalculateFogMaterial);
        
        
        //TODO : BLUR IMAGE AFTER CALCULATING FOG
        ApplyFogMaterial.SetTexture("_FogRenderTargetPoint", fogRenderTexture1);
        ApplyFogMaterial.SetTexture("_FogRenderTargetLinear", fogRenderTexture1);
        
        //CustomGraphicsBlit(source, destination,  ApplyFogMaterial, 0);
        Graphics.Blit(source, destination, ApplyFogMaterial);
        RenderTexture.ReleaseTemporary(fogRenderTexture1);
   /*     int rtW = source.width/4;
        int rtH = source.height/4;
        RenderTexture buffer = RenderTexture.GetTemporary(rtW, rtH, 0);

        // Copy source to the 4x4 smaller texture.
        DownSample4x (source, buffer);

        // Blur the small texture
        for(int i = 0; i < 3; i++)
        {
            RenderTexture buffer2 = RenderTexture.GetTemporary(rtW, rtH, 0);
            FourTapCone (buffer, buffer2, i);
            RenderTexture.ReleaseTemporary(buffer);
            buffer = buffer2;
        }
        CustomGraphicsBlit(buffer,destination, EffectMaterial,0);
    //    Graphics.Blit(buffer, destination);
            

        
        // TODO : blur
        */
        // Set any custom shader variables here.  For example, you could do:
        // EffectMaterial.SetFloat("_MyVariable", 13.37f);
        // This would set the shader uniform _MyVariable to value 13.37
        
    //    ApplyFogMaterial.SetTexture("_FogTex", _FogTexture3D);
    //    EffectMaterial.SetTexture("_BlurTex", buffer);
        
    //    ApplyFogMaterial.SetVector("_LightDir", SunLight ? SunLight.forward : Vector3.down);



     /*   CustomGraphicsBlit(source, fogRenderTexture1, ApplyFogMaterial, 0);

        ApplyFogMaterial.SetTexture("FogRendertargetLinear", fogRenderTexture1);
        
        CustomGraphicsBlit(source, destination, ApplyFogMaterial, 1);*/
        
  
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
    
    
    
    // Performs one blur iteration.
    public void FourTapCone (RenderTexture source, RenderTexture dest, int iteration)
    {
        float off = 0.5f + iteration*0.6f;
        Graphics.BlitMultiTap (source, dest, ApplyFogMaterial,
            new Vector2(-off, -off),
            new Vector2(-off,  off),
            new Vector2( off,  off),
            new Vector2( off, -off)
        );
    }

    // Downsamples the texture to a quarter resolution.
    private void DownSample4x (RenderTexture source, RenderTexture dest)
    {
        float off = 1.0f;
        Graphics.BlitMultiTap (source, dest, ApplyFogMaterial,
            new Vector2(-off, -off),
            new Vector2(-off,  off),
            new Vector2( off,  off),
            new Vector2( off, -off)
        );
    }

}
