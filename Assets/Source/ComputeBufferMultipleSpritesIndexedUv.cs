using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Random = UnityEngine.Random;

public class BurstSpriteRender : MonoBehaviour {
    [SerializeField] private Sprite _sprite;
    private Mesh _mesh;
    private Matrix4x4 _matrix4X4;
    [SerializeField] private Material _material;
    private SpriteRenderer _spriteRenderer;
    private void Start() {
        _mesh = CreateQuad();
        _matrix4X4 = transform.localToWorldMatrix;

    }

    private void Update() {
        Graphics.DrawMeshNow(_mesh, _matrix4X4);
    }

    private static Mesh CreateQuad() {
        var mesh = new Mesh();
        var vertices = new Vector3[4];
        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(1, 0, 0);
        vertices[2] = new Vector3(0, 1, 0);
        vertices[3] = new Vector3(1, 1, 0);
        mesh.vertices = vertices;

        var tri = new int[6];
        tri[0] = 0;
        tri[1] = 2;
        tri[2] = 1;
        tri[3] = 2;
        tri[4] = 3;
        tri[5] = 1;
        mesh.triangles = tri;

        var normals = new Vector3[4];
        normals[0] = -Vector3.forward;
        normals[1] = -Vector3.forward;
        normals[2] = -Vector3.forward;
        normals[3] = -Vector3.forward;
        mesh.normals = normals;

        var uv = new Vector2[4];
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);
        mesh.uv = uv;

        return mesh;
    }
}
public class ComputeBufferMultipleSpritesIndexedUv : MonoBehaviour {
    private const int UV_X_ELEMENTS = 1;
    private const int UV_Y_ELEMENTS = 1;

    private static readonly Bounds BOUNDS = new(Vector2.zero, Vector3.one * 10);
    [SerializeField] private Shader _shader;
    [SerializeField] private Camera referenceCamera;
    [SerializeField] private SpriteAtlas _atlas;
    [SerializeField] private Sprite[] _run;
    [SerializeField] private Sprite[] _idle;
    [SerializeField] private int _animationFrames;
    [SerializeField] private int _currentFrame;
    [SerializeField] private float _frameTime  = 0.08f;
    private float _frameTimeCurrent = 0.08f;
    [SerializeField] private float spriteScale = 0.3f;

    [SerializeField] private int count;

    public float moveSpeed;

    private readonly Dictionary<string, Material> _materials = new();

    private uint[] args;

    private ComputeBuffer argsBuffer;

    private ComputeBuffer colorBuffer;
    
    private ComputeBuffer flipBuffer;
    private int[] flips;
    private Vector4[] colors;
    private Vector2 input;

    private Mesh mesh;

    private ComputeBuffer scaleBuffer;
    private float[] scales;
    private float[] speeds;

    // Matrix here is a compressed transform information
    // xyz is the position, w is rotation
    private ComputeBuffer translationAndRotationBuffer;
    private Vector4[] translationAndRotations;

    // uvBuffer contains float4 values in which xy is the uv dimension and zw is the texture offset
    private ComputeBuffer uvBuffer;
    private ComputeBuffer uvIndexBuffer;
    private int[] uvIndices;
    private int[] animationIndexes;
    private int[] animationIndexData;
    private int[] animationFramesCounts;
    private ComputeBuffer animationIndexBuffer;
    private Vector4[] uvs;
    private Material material;
    private void Awake() {
        //Assertion.AssertNotNull(this.referenceCamera);
        Application.targetFrameRate = 144;

        material = new Material(_shader);
        _animationFrames = _idle.Length;
        mesh = CreateQuad();

        var (frames, animIndexes) =  BakeAnimations(_idle, _run);
        uvs = frames;
        animationIndexData = animIndexes;
        
        material.mainTexture = _run[0].texture;
        unsafe {
            uvBuffer = new ComputeBuffer(uvs.Length, sizeof(Vector4) * uvs.Length);
        }
        
        uvBuffer.SetData(uvs);
        var uvBufferId = Shader.PropertyToID("uvBuffer");
        material.SetBuffer(uvBufferId, uvBuffer);

        // Prepare values
        translationAndRotations = new Vector4[count];
        scales = new float[count];
        colors = new Vector4[count];
        uvIndices = new int[count];
        flips = new int[count];
        animationIndexes = new int[count];
        animationFramesCounts = new int[count];
        const float maxRotation = Mathf.PI * 2;
        var screenRatio = (float)Screen.width / Screen.height;
        var orthoSize = referenceCamera.orthographicSize;
        var maxX = orthoSize * screenRatio;
        speeds = new float[count];
        for (var i = 0; i < count; ++i) {
            // transform
            var y = Random.Range(-orthoSize, orthoSize);
            var x = Random.Range(-maxX, maxX);
            var z = y; // Negate y so that higher sprites are rendered prior to sprites below
            var rotation = Random.Range(0, maxRotation);
            translationAndRotations[i] = new Vector4(x, y, z, 0);
            scales[i] = spriteScale;

            // UV index
            uvIndices[i] = Random.Range(0, uvs.Length);
            flips[i] = 1;
            // color
            var r = Random.Range(0f, 1.0f);
            var g = Random.Range(0f, 1.0f);
            var b = Random.Range(0f, 1.0f);
            colors[i] = new Vector4(r, g, b, 1.0f);
            speeds[i] = Random.Range(4f, 10.0f);
            animationFramesCounts[i] = _idle.Length;
        }

        translationAndRotationBuffer = new ComputeBuffer(count, 16);
        translationAndRotationBuffer.SetData(translationAndRotations);
        var translationAndRotationBufferId = Shader.PropertyToID("translationAndRotationBuffer");
        material.SetBuffer(translationAndRotationBufferId, translationAndRotationBuffer);

        scaleBuffer = new ComputeBuffer(count, sizeof(float));
        scaleBuffer.SetData(scales);
        var scaleBufferId = Shader.PropertyToID("scaleBuffer");
        material.SetBuffer(scaleBufferId, scaleBuffer);

        uvIndexBuffer = new ComputeBuffer(count, sizeof(int));
        uvIndexBuffer.SetData(uvIndices);
        var uvIndexBufferId = Shader.PropertyToID("uvIndexBuffer");
        material.SetBuffer(uvIndexBufferId, uvIndexBuffer);

        colorBuffer = new ComputeBuffer(count, 16);
        colorBuffer.SetData(colors);
        var colorsBufferId = Shader.PropertyToID("colorsBuffer");
        material.SetBuffer(colorsBufferId, colorBuffer);

        flipBuffer = new ComputeBuffer(count, sizeof(int));
        flipBuffer.SetData(flips);
        var flipBufferId = Shader.PropertyToID("flipBuffer");
        material.SetBuffer(flipBufferId, flipBuffer);

        animationIndexBuffer = new ComputeBuffer(count, sizeof(int));
        animationIndexBuffer.SetData(animationIndexes);
        var animationIndexBufferId = Shader.PropertyToID("animationIndexBuffer");
        material.SetBuffer(animationIndexBufferId, animationIndexBuffer);
        
        args = new uint[] {
            6, (uint)count, 0, 0, 0
        };
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
    }

    private const string uvIndexBufferName = "uvIndexBuffer";

    private void OnDestroy() {
        Destroy(material);
    }

    private void Update() {
        input = new Vector2(EzInput.Horizontal, EzInput.Vertical);
        
        Move();
        Animation();
        // Draw
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, BOUNDS, argsBuffer);

    }
    private void Animation() {
        _frameTimeCurrent -= Time.deltaTime;
        if (_frameTimeCurrent <= 0F) {
            for (var i = 0; i < count; i++) {
                ref var frame = ref uvIndices[i];
                ref var animatonFramesCount = ref animationFramesCounts[i];
                frame++;
                if (frame == animatonFramesCount) {
                    frame = 0;
                }
            }

            _frameTimeCurrent = _frameTime;
            uvIndexBuffer.SetData(uvIndices);
            var uvIndexBufferId = Shader.PropertyToID(uvIndexBufferName);
            material.SetBuffer(uvIndexBufferId, uvIndexBuffer);
        }

    }
    private void Move() {
        // var x = moveSpeed * input.x * Time.deltaTime;
        // var y = moveSpeed * input.y * Time.deltaTime;
        //var r = moveSpeed * Time.deltaTime;
        var flip = 1;
        var dt = Time.deltaTime;
        for (var i = 0; i < count; i++) {
            ref var pos = ref translationAndRotations[i];
            if (input.x > -.01f) flip = 1;
            else flip = -1;

            if (input.x > 0) {
                animationIndexes[i] = animationIndexData[1];
                animationFramesCounts[i] = _run.Length;
            }
            else {
                animationIndexes[i] = animationIndexData[0];
                animationFramesCounts[i] = _idle.Length;
            }
            pos.x += moveSpeed * input.x * dt;
            pos.y += moveSpeed * input.y * dt;
            flips[i] = flip;
            
            //pos.w += r;
        }
        
        flipBuffer.SetData(flips);
        var flipBufferId = Shader.PropertyToID("flipBuffer");
        material.SetBuffer(flipBufferId, flipBuffer);
        translationAndRotationBuffer.SetData(translationAndRotations);
        var translationAndRotationBufferId = Shader.PropertyToID("translationAndRotationBuffer");
        material.SetBuffer(translationAndRotationBufferId, translationAndRotationBuffer);
        animationIndexBuffer.SetData(animationIndexes);
        var animationIndexBufferId = Shader.PropertyToID("animationIndexBuffer");
        material.SetBuffer(animationIndexBufferId, animationIndexBuffer);
    }

    private static Mesh CreateQuad() {
        var mesh = new Mesh();
        var vertices = new Vector3[4];
        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(1, 0, 0);
        vertices[2] = new Vector3(0, 1, 0);
        vertices[3] = new Vector3(1, 1, 0);
        mesh.vertices = vertices;

        var tri = new int[6];
        tri[0] = 0;
        tri[1] = 2;
        tri[2] = 1;
        tri[3] = 2;
        tri[4] = 3;
        tri[5] = 1;
        mesh.triangles = tri;

        var normals = new Vector3[4];
        normals[0] = -Vector3.forward;
        normals[1] = -Vector3.forward;
        normals[2] = -Vector3.forward;
        normals[3] = -Vector3.forward;
        mesh.normals = normals;

        var uv = new Vector2[4];
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);
        mesh.uv = uv;

        return mesh;
    }

    private static (Vector4[],int[]) BakeAnimations(params Sprite[][] animations) {
        var totalSprites = 0;
        var animationsIndexes = new int[animations.Length];
        for (var i = 0; i < animations.Length; i++) {
            var array = animations[i];
            totalSprites += array.Length;
            animationsIndexes[i] = totalSprites - array.Length;
        }

        var frames = new Vector4[totalSprites];
        var index = 0;
        foreach (var array in animations) {
            var anim = array;
            for (var i = 0; i < anim.Length; i++) {
                frames[index] = BakeSprite(anim[i]);
                index++;
            }
        }
        return (frames, animationsIndexes);
    }
    private static Vector4[] BakeSprites(Sprite[] sprites, Material mat) {
        var frames = new Vector4[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) {
            frames[i] = BakeSprite(sprites[i]);
        }

        return frames;
    }
    public static Vector4 BakeSprite(Sprite s)
    {
        Texture texture = s.texture;
        
        float w = texture.width;
        float h = texture.height;
        Vector4 uv = new Vector4();
        var tilingX = 1f / (w / s.rect.width);
        var tilingY = 1f / (h / s.rect.height);
        var OffsetX = tilingX * (s.rect.x / s.rect.width);
        var OffsetY = tilingY * (s.rect.y / s.rect.height);
        uv.x = tilingX;
        uv.y = tilingY;
        uv.z = OffsetX;
        uv.w = OffsetY;
        return uv;
    }

    
    public static class EzInput {
        public static float Horizontal {
            get {
                return Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0;
            }
        }
        public static float Vertical {
            get {
                return Input.GetKey(KeyCode.S) ? -1 : Input.GetKey(KeyCode.W) ? 1 : 0;
            }
        }
    }
    public struct AnimationData {
        public int len;
        public int index;
    }
}