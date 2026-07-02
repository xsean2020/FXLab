using UnityEngine;
using UnityEditor;

public class WaterSceneSetup
{
    // Grid subdivisions: 128 → (129×129 = 16,641 verts).  Lower=coarser, higher=finer.
    // 64  → 4,225 verts (mobile-friendly)
    // 128 → 16,641 verts (balanced)
    // 256 → 65,537 verts (high quality)
    public const int GridSegments = 128;

    // Asset paths for water system
    const string ShaderPath = "Assets/Water/Shaders/WaterSurface.shader";
    const string MatDir = "Assets/Water/Materials/";
    const string TexDir = "Assets/Water/Textures/";

    [MenuItem("Tools/Water/Setup Water Scene")]
    public static void Setup()
    {
        // Clean old materials
        var oldMat = MatDir + "Water_Mat.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(oldMat) != null)
            AssetDatabase.DeleteAsset(oldMat);
        AssetDatabase.Refresh();

        // Destroy existing water & test objects
        var existing = GameObject.Find("WaterSurface");
        if (existing != null)
        {
            // Skip dialog in batch/automated mode
            bool proceed = true;
            if (!Application.isBatchMode)
                proceed = EditorUtility.DisplayDialog("Water Exists", "Recreate?", "Yes", "Cancel");
            if (!proceed) return;
            Object.DestroyImmediate(existing);
        }
        foreach (var n in new[] { "UnderwaterBox_Test", "FloatingSphere_Test", "SubmergedPillar_Test" })
        {
            var o = GameObject.Find(n);
            if (o) Object.DestroyImmediate(o);
        }

        // Water plane — use a subdivided mesh for Gerstner waves
        var water = new GameObject("WaterSurface");
        var mf = water.AddComponent<MeshFilter>();
        water.AddComponent<MeshRenderer>();
        water.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        water.transform.localScale = Vector3.one;
        water.layer = LayerMask.NameToLayer("Water");

        // Generate a fine grid mesh (128×128 segments)
        int segs = GridSegments;
        float size = 50f;
        var verts = new Vector3[(segs + 1) * (segs + 1)];
        var tris = new int[segs * segs * 6];
        var uvs = new Vector2[verts.Length];
        var norms = new Vector3[verts.Length];
        for (int iz = 0, i = 0; iz <= segs; iz++)
        for (int ix = 0; ix <= segs; ix++, i++)
        {
            verts[i] = new Vector3((float)ix / segs * size - size * 0.5f, 0,
                                   (float)iz / segs * size - size * 0.5f);
            uvs[i] = new Vector2((float)ix / segs, (float)iz / segs);
            norms[i] = Vector3.up;
        }
        for (int iz = 0, ti = 0; iz < segs; iz++)
        for (int ix = 0; ix < segs; ix++)
        {
            int i = iz * (segs + 1) + ix;
            tris[ti++] = i; tris[ti++] = i + segs + 1; tris[ti++] = i + 1;
            tris[ti++] = i + 1; tris[ti++] = i + segs + 1; tris[ti++] = i + segs + 2;
        }
        var mesh = new Mesh { name = "WaterGrid" };
        mesh.vertices = verts; mesh.triangles = tris;
        mesh.uv = uvs; mesh.normals = norms;
        // Generate tangents
        var tan = new Vector4[verts.Length];
        for (int i = 0; i < verts.Length; i++) tan[i] = new Vector4(1, 0, 0, 1);
        mesh.tangents = tan;
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;

        // Load textures (enable Read/Write for normal map generation)
        var skyMat = RenderSettings.skybox;
        var cube = skyMat && skyMat.HasProperty("_Tex") ? skyMat.GetTexture("_Tex") as Cubemap : null;
        var noise = LoadTextureReadable(TexDir + "FX_sence_17posuizhidi_xingyunnoise.png");
        var wtex = LoadTextureReadable(TexDir + "water001_zbh.png");
        var nml1 = noise ? CreateNormalMap(noise, 1.5f, "Water_Normal_01") : (wtex ? CreateNormalMap(wtex, 2f, "Water_Normal_01") : null);
        var nml2 = noise ? CreateNormalMap(noise, 3f, "Water_Normal_02") : (wtex ? CreateNormalMap(wtex, 4f, "Water_Normal_02") : null);
        var foamTex = CreateFoamTexture(512, 512, "Water_Foam_Noise");

        // Force import shader
        AssetDatabase.ImportAsset(ShaderPath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        // Create material
        var shader = Shader.Find("Water/WaterSurface");
        Material mat = null;
        if (shader)
        {
            mat = new Material(shader) { name = "Water_Mat" };
            mat.SetColor("_ShallowColor", new Color(0.05f, 0.25f, 0.15f, 0.5f));
            mat.SetColor("_DeepColor", new Color(0, 0.02f, 0.06f, 0.85f));
            mat.SetFloat("_FresnelPower", 3f);
            mat.SetFloat("_FresnelOffset", 0.02f);
            mat.SetFloat("_ReflectionStrength", 0.5f);
            mat.SetFloat("_NormalScale", 1.2f);
            mat.SetFloat("_NormalTiling", 2.5f);
            mat.SetFloat("_DepthFactor", 0.6f);
            mat.SetFloat("_Alpha", 0.9f);
            mat.SetVector("_NormalSpeed1", new Vector4(0.015f, 0.008f, 0, 0));
            mat.SetVector("_NormalSpeed2", new Vector4(-0.012f, 0.015f, 0, 0));
            mat.SetFloat("_Shininess", 64f);
            if (cube) mat.SetTexture("_Cubemap", cube);
            if (nml1) mat.SetTexture("_NormalMap1", nml1);
            if (nml2) mat.SetTexture("_NormalMap2", nml2);
            mat.SetFloat("_UseGerstner", 1f);
            mat.SetFloat("_WaveHeightScale", 0.3f);
            mat.SetFloat("_Steepness", 0.3f);
            mat.SetFloat("_WaveAmp1", 0.4f); mat.SetFloat("_WaveFreq1", 0.8f); mat.SetFloat("_WaveSpeed1", 1.2f);
            mat.SetVector("_WaveDir1", new Vector4(1, 0, 0, 0));
            mat.SetFloat("_WaveAmp2", 0.25f); mat.SetFloat("_WaveFreq2", 1.2f); mat.SetFloat("_WaveSpeed2", 0.8f);
            mat.SetVector("_WaveDir2", new Vector4(0.7f, 0.7f, 0, 0));
            mat.SetFloat("_WaveAmp3", 0.15f); mat.SetFloat("_WaveFreq3", 2f); mat.SetFloat("_WaveSpeed3", 2f);
            mat.SetVector("_WaveDir3", new Vector4(-0.3f, 0.9f, 0, 0));
            mat.SetFloat("_WaveAmp4", 0.08f); mat.SetFloat("_WaveFreq4", 3f); mat.SetFloat("_WaveSpeed4", 1.5f);
            mat.SetVector("_WaveDir4", new Vector4(-0.8f, 0.6f, 0, 0));
            mat.SetFloat("_MainFoamScale", 40f);
            mat.SetFloat("_MainFoamIntensity", 3.8f);
            mat.SetFloat("_MainFoamSpeed", 0.1f);
            mat.SetFloat("_MainFoamOpacity", 0.87f);
            mat.SetFloat("_MainFoamWidth", 0.2f);
            mat.SetColor("_FoamColor", Color.white);
            if (foamTex) mat.SetTexture("_FoamTexture", foamTex);
            mat.SetFloat("_TurbulenceStrength", 0.03f);
            mat.SetVector("_TurbulenceSpeed", new Vector4(0.01f, 0.01f, 0, 0));
            if (noise) mat.SetTexture("_TurbulenceTex", noise);
            var p = MatDir + "Water_Mat.mat";
            AssetDatabase.CreateAsset(mat, p);
            Debug.Log("Water material: " + p);
        }

        var rend = water.GetComponent<MeshRenderer>();
        if (rend) rend.sharedMaterial = mat;

        // Test objects
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "UnderwaterBox_Test";
        box.transform.position = new Vector3(-3, -1.5f, 0);
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "FloatingSphere_Test";
        sphere.transform.position = new Vector3(3, 0.3f, 0);
        sphere.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        sphere.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = Color.red };
        var rb = sphere.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.mass = 0.5f;
        rb.drag = 1f;
        rb.angularDrag = 2f;
        sphere.AddComponent<FloatingObject>();
        var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        // Camera
        var cam = Camera.main;
        if (cam)
        {
            cam.transform.SetPositionAndRotation(new Vector3(0, 2.5f, -7), Quaternion.Euler(15, 0, 0));
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.depthTextureMode |= DepthTextureMode.Depth;
        }

        // Light
        var light = Object.FindObjectOfType<Light>();
        if (light)
        {
            light.transform.position = new Vector3(10, 20, -5);
            light.transform.rotation = Quaternion.Euler(45, 30, 0);
            light.intensity = 1.2f;
        }

        Debug.Log("=== Water Scene Ready ===");
    }

    static Texture2D CreateNormalMap(Texture2D src, float strength, string name)
    {
        int w = src.width, h = src.height;
        var nml = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = name };
        var px = src.GetPixels();
        var nm = new Color[px.Length];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float tl = G(px, w, h, x-1,y+1), t = G(px,w,h,x,y+1), tr = G(px,w,h,x+1,y+1);
            float l = G(px,w,h,x-1,y), r = G(px,w,h,x+1,y);
            float bl = G(px,w,h,x-1,y-1), b = G(px,w,h,x,y-1), br = G(px,w,h,x+1,y-1);
            float dx = (tr+2*r+br)-(tl+2*l+bl), dy = (bl+2*b+br)-(tl+2*t+tr);
            var n = new Vector3(dx*strength, dy*strength, 1).normalized;
            nm[y*w+x] = new Color(n.x*0.5f+0.5f, n.y*0.5f+0.5f, n.z*0.5f+0.5f, 1);
        }
        nml.SetPixels(nm); nml.Apply(true);
        var path = $"{TexDir}{name}.png";
        System.IO.File.WriteAllBytes(path, nml.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp) { imp.textureType = TextureImporterType.NormalMap; imp.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static float G(Color[] px, int w, int h, int x, int y)
    {
        x = Mathf.Clamp(x, 0, w-1); y = Mathf.Clamp(y, 0, h-1);
        var c = px[y*w+x];
        return c.r*0.299f + c.g*0.587f + c.b*0.114f;
    }

    static Texture2D CreateFoamTexture(int w, int h, string name)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = name };
        var px = new Color[w*h];
        var rng = new System.Random(42);
        int cs = 8;
        int cx = w/cs+2, cy = h/cs+2;
        float[,] cv = new float[cx,cy];
        for (int yy=0;yy<cy;yy++) for(int xx=0;xx<cx;xx++) cv[xx,yy]=(float)rng.NextDouble();
        for (int y=0;y<h;y++) for(int x=0;x<w;x++)
        {
            float fx=(float)x/cs, fy=(float)y/cs;
            float md=99, mv=0;
            for (int yy=Mathf.Max(0,(int)fy-1);yy<=Mathf.Min(cy-1,(int)fy+2);yy++)
            for (int xx=Mathf.Max(0,(int)fx-1);xx<=Mathf.Min(cx-1,(int)fx+2);xx++)
            {
                float d = (fx-(xx+(float)rng.NextDouble()*0.8f))*(fx-(xx+(float)rng.NextDouble()*0.8f))
                         +(fy-(yy+(float)rng.NextDouble()*0.8f))*(fy-(yy+(float)rng.NextDouble()*0.8f));
                if (d<md){md=d; mv=cv[xx,yy];}
            }
            float v = Mathf.Clamp01(1-Mathf.Sqrt(md)/0.8f*3);
            float dt=0, ap=0.5f, fr=4;
            for (int o=0;o<3;o++){dt+=ap*(Mathf.Sin(x*fr/w*10*3.14159f)*Mathf.Cos(y*fr/h*10*3.14159f)*0.5f+0.5f); ap*=0.5f; fr*=2.3f;}
            v = Mathf.Lerp(v,dt,0.3f);
            v = Mathf.Clamp01((v*v*(3-2*v)-0.15f)/0.7f);
            px[y*w+x] = new Color(v,v,v,1);
        }
        tex.SetPixels(px); tex.Apply(true);
        var path = $"{TexDir}{name}.png";
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// <summary>Load a texture with Read/Write enabled (needed for GetPixels).</summary>
    static Texture2D LoadTextureReadable(string path)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return null;
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && !imp.isReadable)
        {
            imp.isReadable = true;
            imp.SaveAndReimport();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        return tex;
    }
}
