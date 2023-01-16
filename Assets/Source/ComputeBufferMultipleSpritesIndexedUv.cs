using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeBufferMultipleSpritesIndexedUv : MonoBehaviour
{
[SerializeField]
    private Camera referenceCamera;
     
    [SerializeField]
    private Material material;
     
    [SerializeField]
    private float spriteScale = 0.3f;  
 
    [SerializeField]
    private int count;
 
    private Mesh mesh;
     
    // Matrix here is a compressed transform information
    // xyz is the position, w is rotation
    private ComputeBuffer translationAndRotationBuffer;
 
    private ComputeBuffer scaleBuffer;
     
    private ComputeBuffer colorBuffer;
     
    // uvBuffer contains float4 values in which xy is the uv dimension and zw is the texture offset
    private ComputeBuffer uvBuffer;
    private ComputeBuffer uvIndexBuffer;
 
    private uint[] args;
     
    private ComputeBuffer argsBuffer;
 
    private const int UV_X_ELEMENTS = 1;
    private const int UV_Y_ELEMENTS = 1;
    private Vector4[] translationAndRotations;
    private float[] speeds;
    private float[] scales;
    private int[] uvIndices;
    private Vector4[] uvs;
    private Vector4[] colors;
    
    private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
    public void AddSrpite(Material material, Vector4 uv, int uvIndex, Vector4 color, Vector4 translation) {

        var mat = GetOrCreate(material);
        uvs[count] = uv;
        colors[count] = color;
        uvIndices[count] = uvIndex;
        count++;
    }

    private Material GetOrCreate(Material material) {
        if (_materials.TryGetValue(material.name, out var mat)) {
            return mat;
        }
        _materials.Add(material.name, material);
        return _materials[material.name];
    }
    private void Awake() {
        //Assertion.AssertNotNull(this.referenceCamera);
         
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
         
        this.mesh = CreateQuad();
        
        // Prepare available UVs
        const int uvCount = UV_X_ELEMENTS * UV_Y_ELEMENTS;
        uvs = new Vector4[uvCount];
        for (int u = 0; u < UV_X_ELEMENTS; u++) {
            for (int v = 0; v < UV_Y_ELEMENTS; v++) {
                int index = v * UV_X_ELEMENTS + u;
                uvs[index] = new Vector4(1f, 1f, u * 1f, v * 1f);
            }
        }
         
        this.uvBuffer = new ComputeBuffer(uvs.Length, 16);
        this.uvBuffer.SetData(uvs);
        int uvBufferId = Shader.PropertyToID("uvBuffer");
        this.material.SetBuffer(uvBufferId, this.uvBuffer);
         
        // Prepare values
        translationAndRotations = new Vector4[this.count];
        scales = new float[this.count];
        colors = new Vector4[this.count];
        uvIndices = new int[this.count]; 
 
        const float maxRotation = Mathf.PI * 2;
        float screenRatio = (float) Screen.width / Screen.height;
        float orthoSize = this.referenceCamera.orthographicSize;
        float maxX = orthoSize * screenRatio;
        speeds = new float[count];
        for (int i = 0; i < this.count; ++i) {
            // transform
            float y = Random.Range(-orthoSize, orthoSize);
            float x = Random.Range(-maxX, maxX);
            float z = y; // Negate y so that higher sprites are rendered prior to sprites below
            float rotation = UnityEngine.Random.Range(0, maxRotation);
            translationAndRotations[i] = new Vector4(x, y, z, rotation);
            scales[i] = this.spriteScale;
 
            // UV index
            uvIndices[i] = UnityEngine.Random.Range(0, uvCount);
             
            // color
            float r = Random.Range(0f, 1.0f);
            float g = Random.Range(0f, 1.0f);
            float b = Random.Range(0f, 1.0f);
            colors[i] = new Vector4(r, g, b, 1.0f);
            speeds[i] = Random.Range(4f, 10.0f);
        }
         
        this.translationAndRotationBuffer = new ComputeBuffer(this.count, 16);
        this.translationAndRotationBuffer.SetData(translationAndRotations);
        int translationAndRotationBufferId = Shader.PropertyToID("translationAndRotationBuffer");
        this.material.SetBuffer(translationAndRotationBufferId, this.translationAndRotationBuffer);
 
        this.scaleBuffer = new ComputeBuffer(this.count, sizeof(float));
        this.scaleBuffer.SetData(scales);
        int scaleBufferId = Shader.PropertyToID("scaleBuffer");
        this.material.SetBuffer(scaleBufferId, this.scaleBuffer);
 
        this.uvIndexBuffer = new ComputeBuffer(this.count, sizeof(int));
        this.uvIndexBuffer.SetData(uvIndices);
        int uvIndexBufferId = Shader.PropertyToID("uvIndexBuffer");
        this.material.SetBuffer(uvIndexBufferId, this.uvIndexBuffer);
         
        this.colorBuffer = new ComputeBuffer(this.count, 16);
        this.colorBuffer.SetData(colors);
        int colorsBufferId = Shader.PropertyToID("colorsBuffer");
        this.material.SetBuffer(colorsBufferId, this.colorBuffer);
 
        this.args = new uint[] {
            6, (uint)this.count, 0, 0, 0
        };
        this.argsBuffer = new ComputeBuffer(1, this.args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        this.argsBuffer.SetData(this.args);
    }
 
    private static readonly Bounds BOUNDS = new Bounds(Vector2.zero, Vector3.one * 10);
    private Vector2 input;
    public float moveSpeed;
    private void Move() {
        // var x = moveSpeed * input.x * Time.deltaTime;
        // var y = moveSpeed * input.y * Time.deltaTime;
        var r = moveSpeed * Time.deltaTime;
        var dt = Time.deltaTime;
        for (int i = 0; i < count; i++) {
            ref var pos = ref translationAndRotations[i];
            
            pos.x += speeds[i] * input.x * dt;
            pos.y += speeds[i] * input.y * dt;
            pos.w += r;
        }
    }
    private void Update() {
        input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Move();
        this.translationAndRotationBuffer.SetData(translationAndRotations);
        int translationAndRotationBufferId = Shader.PropertyToID("translationAndRotationBuffer");
        this.material.SetBuffer(translationAndRotationBufferId, this.translationAndRotationBuffer);
        // Draw
        Graphics.DrawMeshInstancedIndirect(this.mesh, 0, this.material, BOUNDS, this.argsBuffer);
    }
    private static Mesh CreateQuad() {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(1, 0, 0);
        vertices[2] = new Vector3(0, 1, 0);
        vertices[3] = new Vector3(1, 1, 0);
        mesh.vertices = vertices;
 
        int[] tri = new int[6];
        tri[0] = 0;
        tri[1] = 2;
        tri[2] = 1;
        tri[3] = 2;
        tri[4] = 3;
        tri[5] = 1;
        mesh.triangles = tri;
 
        Vector3[] normals = new Vector3[4];
        normals[0] = -Vector3.forward;
        normals[1] = -Vector3.forward;
        normals[2] = -Vector3.forward;
        normals[3] = -Vector3.forward;
        mesh.normals = normals;
 
        Vector2[] uv = new Vector2[4];
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);
        mesh.uv = uv;
 
        return mesh;
    }
}
