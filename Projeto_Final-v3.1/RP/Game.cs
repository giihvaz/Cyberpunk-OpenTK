using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RP.Core;

namespace RP
{
    internal class Game : GameWindow
    {
        private float _time = 0f;
        private bool _dayNightCycle = true;

        private Mesh _cubeMesh;
        private Mesh _planeMesh;
        private Mesh _skyboxMesh;
        private Mesh _postMesh;
        private Mesh _rainMesh;

        private Transform _planeTransform = new();
        private Transform _skyboxTransform = new();

        private Material _skyboxMaterial;
        private Material _planeMaterial;
        private Material _postMaterial;
        private Material _rainMaterial;

        private readonly Camera _camera;
        private readonly DirectionalLight _directionalLight = new();

        private RenderTarget _shadowMap;
        private RenderTarget _postTarget;

        private readonly List<SceneObject> _cityObjects = new();
        private readonly List<SceneObject> _emissiveObjects = new();
        private readonly Random _random = new(11);

        private int _rainCount = 1200;

        private struct SceneObject
        {
            public Material material;
            public Transform transform;
            public float phase;
            public float baseEmission;
        }

        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
            _cubeMesh = Primitive.CreateCube(1f);
            _planeMesh = Primitive.CreatePlane(120f);
            _skyboxMesh = Primitive.CreateSphere(10f);
            _postMesh = Primitive.CreatePost();
            _rainMesh = CreateRainDropMesh();

            _shadowMap = RenderTarget.CreateDepth(4096, 4096);
            _postTarget = RenderTarget.CreateColor(Size.X, Size.Y);

            ShaderProgram phongProgram = new(
                VertexShader.LoadFromFile("./assets/shaders/phong.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/phong.frag")
            );

            ShaderProgram skyboxProgram = new(
                VertexShader.LoadFromFile("./assets/shaders/skybox.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/skybox.frag")
            );

            ShaderProgram postProgram = new(
                VertexShader.LoadFromFile("./assets/shaders/post.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/post.frag")
            );

            ShaderProgram rainProgram = new(
                VertexShader.LoadFromFile("./assets/shaders/rain.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/rain.frag")
            );

            _camera = new PerspectiveCamera(90f, (float)Size.X / Size.Y);
            _camera.position = new Vector3(0f, 2.2f, 12f);
            _camera.rotation.X = -8f;

            _directionalLight.rotation.X = -80f;
            _directionalLight.rotation.Y = 25f;
            _directionalLight.color = new Vector3(0.45f, 0.5f, 0.8f);

            _skyboxMaterial = new(skyboxProgram);
            _skyboxMaterial.cullMode = TriangleFace.Front;
            _skyboxMaterial.depthWrite = false;

            _planeMaterial = CreatePhongMaterial(phongProgram, new Vector4(0.035f, 0.04f, 0.055f, 1f), 0.9f);
            _planeMaterial.SetFloat("u_Wetness", 1.0f);

            _postMaterial = new(postProgram);
            _postMaterial.SetTexture("u_Texture", _postTarget.Texture);

            _rainMaterial = new(rainProgram);
            _rainMaterial.cull = false;
            _rainMaterial.depthWrite = false;

            GenerateCyberpunkCity(phongProgram);
            GenerateRainInstances();

            CursorState = CursorState.Grabbed;
        }

        private Material CreatePhongMaterial(ShaderProgram program, Vector4 color, float smoothness, Vector3? emission = null, float emissionStrength = 0f)
        {
            Material material = new(program);
            material.SetVec4("u_Color", color);
            material.SetFloat("u_Smoothness", smoothness);
            material.SetFloat("u_Wetness", 0f);
            material.SetVec3("u_EmissionColor", emission ?? Vector3.Zero);
            material.SetFloat("u_EmissionStrength", emissionStrength);
            material.SetTexture("u_Texture", Texture.Default);
            return material;
        }

        private void AddCube(List<SceneObject> list, Material material, Vector3 position, Vector3 scale, float phase = 0f, float baseEmission = 0f)
        {
            Transform transform = new();
            transform.position = position;
            transform.scale = scale;
            list.Add(new SceneObject
            {
                material = material,
                transform = transform,
                phase = phase,
                baseEmission = baseEmission
            });
        }

        private void GenerateCyberpunkCity(ShaderProgram phongProgram)
        {
            Material buildingA = CreatePhongMaterial(phongProgram, new Vector4(0.055f, 0.06f, 0.09f, 1f), 0.45f);
            Material buildingB = CreatePhongMaterial(phongProgram, new Vector4(0.025f, 0.03f, 0.055f, 1f), 0.65f);
            Material asphalt = CreatePhongMaterial(phongProgram, new Vector4(0.015f, 0.017f, 0.022f, 1f), 0.9f);
            asphalt.SetFloat("u_Wetness", 1.0f);
            Material sidewalk = CreatePhongMaterial(phongProgram, new Vector4(0.08f, 0.08f, 0.095f, 1f), 0.55f);
            Material cyanNeon = CreatePhongMaterial(phongProgram, new Vector4(0.0f, 0.75f, 1.0f, 1f), 1f, new Vector3(0.0f, 0.9f, 1.4f), 2.6f);
            Material pinkNeon = CreatePhongMaterial(phongProgram, new Vector4(1.0f, 0.06f, 0.75f, 1f), 1f, new Vector3(1.4f, 0.0f, 0.9f), 2.9f);
            Material yellowNeon = CreatePhongMaterial(phongProgram, new Vector4(1.0f, 0.85f, 0.20f, 1f), 1f, new Vector3(1.7f, 1.1f, 0.15f), 2.2f);
            Material pole = CreatePhongMaterial(phongProgram, new Vector4(0.07f, 0.07f, 0.08f, 1f), 0.5f);
            Material hologram = CreatePhongMaterial(phongProgram, new Vector4(0.25f, 0.8f, 1.0f, 0.62f), 1f, new Vector3(0.1f, 0.8f, 1.5f), 1.8f);
            hologram.depthWrite = false;

            AddCube(_cityObjects, asphalt, new Vector3(0f, 0.015f, 0f), new Vector3(10f, 0.03f, 115f));
            AddCube(_cityObjects, sidewalk, new Vector3(-7.2f, 0.04f, 0f), new Vector3(3.2f, 0.08f, 115f));
            AddCube(_cityObjects, sidewalk, new Vector3(7.2f, 0.04f, 0f), new Vector3(3.2f, 0.08f, 115f));

            for (int z = -48; z <= 48; z += 8)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float height = 5f + _random.NextSingle() * 16f;
                    float width = 3.6f + _random.NextSingle() * 2.6f;
                    float depth = 4f + _random.NextSingle() * 3f;
                    float x = side * (10.5f + _random.NextSingle() * 7f);
                    Material mat = _random.NextSingle() > 0.5f ? buildingA : buildingB;
                    AddCube(_cityObjects, mat, new Vector3(x, height / 2f, z + _random.NextSingle() * 2f), new Vector3(width, height, depth));

                    // Janelas neon repetidas na fachada.
                    for (int y = 2; y < height - 1; y += 2)
                    {
                        if (_random.NextSingle() < 0.55f)
                        {
                            Material windowMat = _random.NextSingle() > 0.5f ? cyanNeon : pinkNeon;
                            float frontX = x - side * (width / 2f + 0.035f);
                            AddCube(_emissiveObjects, windowMat, new Vector3(frontX, y, z), new Vector3(0.08f, 0.42f, 1.4f), _random.NextSingle() * 6.28f, 1.4f);
                        }
                    }

                    // Placas grandes, boas para mostrar bloom.
                    if (_random.NextSingle() < 0.75f)
                    {
                        Material signMat = _random.NextSingle() > 0.5f ? pinkNeon : cyanNeon;
                        float signX = x - side * (width / 2f + 0.08f);
                        AddCube(_emissiveObjects, signMat, new Vector3(signX, 3f + _random.NextSingle() * 7f, z + depth * 0.15f), new Vector3(0.12f, 1.2f, 2.2f), _random.NextSingle() * 6.28f, 2.8f);
                    }
                }
            }

            // Postes com luzes múltiplas ao longo da rua.
            for (int z = -48; z <= 48; z += 8)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 4.8f;
                    AddCube(_cityObjects, pole, new Vector3(x, 1.35f, z), new Vector3(0.18f, 2.7f, 0.18f));
                    AddCube(_cityObjects, pole, new Vector3(x - side * 0.45f, 2.7f, z), new Vector3(0.9f, 0.12f, 0.12f));
                    AddCube(_emissiveObjects, yellowNeon, new Vector3(x - side * 0.95f, 2.65f, z), new Vector3(0.45f, 0.18f, 0.45f), z, 2.5f);
                }
            }

            // Hologramas centrais e objetos urbanos.
            for (int i = 0; i < 8; i++)
            {
                float z = -42f + i * 12f;
                AddCube(_emissiveObjects, hologram, new Vector3(0f, 3.5f, z), new Vector3(0.08f, 2.1f, 2.1f), i, 1.6f);
            }

            Material trash = CreatePhongMaterial(phongProgram, new Vector4(0.09f, 0.1f, 0.11f, 1f), 0.35f);
            for (int i = 0; i < 22; i++)
            {
                float side = _random.NextSingle() > 0.5f ? 1f : -1f;
                AddCube(_cityObjects, trash, new Vector3(side * (5.8f + _random.NextSingle() * 2f), 0.35f, -50f + _random.NextSingle() * 100f), new Vector3(0.55f, 0.7f, 0.55f));
            }
        }

        private Mesh CreateRainDropMesh()
        {
            float[] vertices =
            [
                -0.015f, 0.0f, 0.0f,  0f, 1f, 0f,  0f, 0f,
                 0.015f, 0.0f, 0.0f,  0f, 1f, 0f,  1f, 0f,
                 0.015f, 1.1f, 0.0f,  0f, 1f, 0f,  1f, 1f,
                -0.015f, 1.1f, 0.0f,  0f, 1f, 0f,  0f, 1f,
            ];
            uint[] indices = [0, 1, 2, 0, 2, 3];
            return new Mesh(vertices, indices);
        }

        private void GenerateRainInstances()
        {
            Matrix4[] modelMatrices = new Matrix4[_rainCount];
            Vector3[] colors = new Vector3[_rainCount];

            for (int i = 0; i < _rainCount; i++)
            {
                Transform t = new();
                t.position = new Vector3(
                    -26f + _random.NextSingle() * 52f,
                    0f,
                    -56f + _random.NextSingle() * 112f
                );
                t.rotation.Z = -12f;
                t.scale.Y = 0.55f + _random.NextSingle() * 1.5f;
                modelMatrices[i] = t.ModelMatrix;
                colors[i] = new Vector3(0.35f, 0.75f, 1f) * (0.55f + _random.NextSingle() * 0.45f);
            }

            _rainMesh.SetInstanceData(3, modelMatrices);
            _rainMesh.SetInstanceData(7, colors);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            float delta = (float)args.Time;
            float cameraSpeed = KeyboardState.IsKeyDown(Keys.LeftShift) ? 10f : 5f;

            if (KeyboardState.IsKeyDown(Keys.D)) _camera.position += _camera.Right * delta * cameraSpeed;
            if (KeyboardState.IsKeyDown(Keys.A)) _camera.position -= _camera.Right * delta * cameraSpeed;
            if (KeyboardState.IsKeyDown(Keys.E)) _camera.position += _camera.Up * delta * cameraSpeed;
            if (KeyboardState.IsKeyDown(Keys.Q)) _camera.position -= _camera.Up * delta * cameraSpeed;
            if (KeyboardState.IsKeyDown(Keys.W)) _camera.position += _camera.Forward * delta * cameraSpeed;
            if (KeyboardState.IsKeyDown(Keys.S)) _camera.position -= _camera.Forward * delta * cameraSpeed;

            _camera.rotation -= new Vector3(MouseState.Delta.Y, MouseState.Delta.X, 0f) * 0.3f;
            _camera.rotation.X = MathF.Min(MathF.Max(_camera.rotation.X, -89f), 89f);

            if (KeyboardState.IsKeyPressed(Keys.N)) _dayNightCycle = !_dayNightCycle;
            if (KeyboardState.IsKeyDown(Keys.Escape)) CursorState = CursorState.Normal;
        }

        private void UpdateDynamicLights()
        {
            float cycle = _dayNightCycle ? (MathF.Sin(_time * 0.18f) * 0.5f + 0.5f) : 0f;
            float nightAmount = 1f - cycle;
            _directionalLight.rotation.X = -80f + cycle * 110f;
            _directionalLight.color = Vector3.Lerp(new Vector3(0.18f, 0.22f, 0.38f), new Vector3(1.0f, 0.9f, 0.75f), cycle);
            Material.SetGlobalVec3("u_AmbientLight", Vector3.Lerp(new Vector3(0.045f, 0.045f, 0.08f), new Vector3(0.34f, 0.34f, 0.38f), cycle));

            for (int i = 0; i < _emissiveObjects.Count; i++)
            {
                SceneObject obj = _emissiveObjects[i];
                float flicker = 0.78f + 0.22f * MathF.Sin(_time * 4.0f + obj.phase);
                obj.material.SetFloat("u_EmissionStrength", obj.baseEmission * nightAmount * flicker + 0.15f);
            }
        }

        private void DrawScene(Camera camera, bool drawRain = true)
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
            GL.DepthMask(true);

            GL.ClearColor(0.0f, 0.02f, 0.05f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            camera.GlobalApply();
            Material.SetGlobalTexture("u_ShadowMap", _shadowMap.Texture);

            _skyboxMaterial.Use();
            _skyboxTransform.Apply(_skyboxMaterial);
            _skyboxMesh.Draw();

            _planeMaterial.Use();
            _planeTransform.Apply(_planeMaterial);
            _planeMesh.Draw();

            foreach (SceneObject obj in _cityObjects)
            {
                obj.material.Use();
                obj.transform.Apply(obj.material);
                _cubeMesh.Draw();
            }

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            foreach (SceneObject obj in _emissiveObjects)
            {
                obj.material.Use();
                obj.transform.Apply(obj.material);
                _cubeMesh.Draw();
            }

            if (drawRain)
            {
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                _rainMaterial.Use();
                _rainMesh.Draw(_rainCount);
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            _time += (float)args.Time;

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);

            Material.SetGlobalFloat("u_Time", _time);
            UpdateDynamicLights();
            _directionalLight.GlobalApply();

            Camera shadowMapCamera = _directionalLight.GetLightMapCamera(_camera.position);
            Material.SetGlobalMat4("u_Light", shadowMapCamera.ViewMatrix * shadowMapCamera.ProjectionMatrix);

            _shadowMap.Use();
            DrawScene(shadowMapCamera, false);

            _postTarget.Use();
            DrawScene(_camera, true);

            RenderTarget.Reset(this);
            GL.Disable(EnableCap.Blend);
            GL.DepthMask(true);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _postMaterial.Use();
            _postMesh.Draw();

            SwapBuffers();
        }
    }
}
