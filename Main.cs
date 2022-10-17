﻿using System;
using ImGuiNET;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace OpenTK_Learning
{
    class Main : GameWindow
    {
        // Create a window and assign values
        public Main(int width, int height, string title)
            : base(GameWindowSettings.Default, new NativeWindowSettings()
            {
                Title = title,
                Size = new Vector2i(width, height),
                WindowBorder = WindowBorder.Resizable,
                StartVisible = false,
                StartFocused = true,
                WindowState = WindowState.Normal,
                API = ContextAPI.OpenGL,
                Profile = ContextProfile.Core,
                APIVersion = new Version(3, 3)
            })
        {
            // Center the window
            CenterWindow();
            WindowHeight = Size.Y;
            WindowWidth = Size.X;
            CameraHeight = Size.Y;
            CameraWidth = Size.X;
        }

        ImGuiController _controller;

        private Texture _diffuseMap;
        public static Shader _PhongShader;
        static Shader _LightShader;

        public static R_3D.Material M_Default;
        public static R_3D.Material M_Floor;

        public static bool CloseWindow = false;

        // Camera settings
        public bool firstMove = true;
        float WindowWidth;
        float WindowHeight;
        float CameraWidth;
        float CameraHeight;
        float Yaw;
        float Pitch = -90f;
        int FOV = 90;
        int speed = 12;
        float sensitivity = 0.25f;

        public static int FBO;
        public static int PPfbo;
        public static int RBO;
        public static int PPtexture;
        public static int framebufferTexture;

        // Camera transformations
        public static Vector3 position = new Vector3(0.0f, 3.0f, 3.0f);
        Vector3 front = new Vector3(0.0f, 0.0f, -1.0f);
        Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);

        // Runs when the window is resizeds
        protected override void OnResize(ResizeEventArgs e)
        {
            // Matches window scale to new resize
            WindowWidth = e.Width;
            WindowHeight = e.Height;

            _controller.WindowResized((int)WindowWidth, (int)WindowHeight);

            GL.BindTexture(TextureTarget.Texture2DMultisample, Main.framebufferTexture);
            GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, 4, PixelInternalFormat.Rgb, (int)CameraWidth, (int)CameraHeight, true);
            GL.BindTexture(TextureTarget.Texture2D, Main.PPtexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, (int)CameraWidth, (int)CameraHeight, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);

            GL.Viewport(0, 0, e.Width, e.Height);
            base.OnResize(e);
        }

        // Runs after Run();
        protected override void OnLoad()
        {
            //OBJ_Loader.LoadOBJ("./../../../Resources/3D_Files/Icosphere.obj");
            R_3D.GenFBO(CameraWidth, CameraHeight);

            IsVisible = true;
            VSync = VSyncMode.On;

            // Render objects in correct depth
            GL.Enable(EnableCap.DepthTest);
            // Cull faces
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);
            // Wireframe line width
            GL.LineWidth(1.5f);
            // Background color
            GL.ClearColor(new Color4(0.5f, 0.5f, 0.5f, 1f));

            // Bind and use shaders
            _PhongShader = new Shader("./../../../Resources/shaders/default.vert", "./../../../Resources/shaders/default.frag", true);
            _LightShader = new Shader("./../../../Resources/shaders/light.vert", "./../../../Resources/shaders/light.frag");

            M_Default = new R_3D.Material
            {
                ambient = new Vector3(0.1f),
                diffuse = new Vector3(1.0f, 0.5f, 0.25f),
                specular = new Vector3(0.5f),
                shininess = 64.0f
            };

            M_Floor = new R_3D.Material
            {
                ambient = new Vector3(0.1f),
                diffuse = new Vector3(0.75f),
                specular = new Vector3(0.5f),
                shininess = 8.0f
            };

            // Add default objects
            R_3D.AddObjectToArray(false,
                "Cube",                     // Name
                M_Default,                  // Material
                new Vector3(2f, 3f, 8f),    // Scale
                new Vector3(0f, 4f, 0f),    // Location
                new Vector3(45f, 10, 20f),  // Rotation
                Cube.vertices,
                Cube.indices);
            R_3D.AddObjectToArray(false,
                "Plane",          // Name
                M_Floor,          // Material
                new Vector3(10f), // Scale
                new Vector3(0f),  // Location
                new Vector3(0f),  // Rotation
                Plane.vertices,
                Plane.indices);

            // Generate all the data shit
            R_3D.ConstructObjects();

            // Add lights
            R_3D.AddLightToArray("Light1", new Vector3(1.0f), _LightShader, new Vector3(1f), new Vector3(4f, 6f, 4f), new Vector3(0f), Cube.vertices, Cube.indices);
            R_3D.AddLightToArray("Light2", new Vector3(1.0f), _LightShader, new Vector3(1f), new Vector3(-4f, 4f, 4f), new Vector3(0f), Cube.vertices, Cube.indices);
            R_3D.ConstructLights();

            // Generate two screen triangles
            R_3D.GenScreenRect();

            // Load textures
            _diffuseMap = Texture.LoadFromFile("./../../../Resources/Images/hex.png", TextureUnit.Texture0);

            

            FBO = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);

            framebufferTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DMultisample, framebufferTexture);
            GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, 4, PixelInternalFormat.Rgb, (int)CameraWidth, (int)CameraHeight, true);
            GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, framebufferTexture, 0);

            RBO = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, RBO);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, 4, RenderbufferStorage.Depth24Stencil8, (int)CameraWidth, (int)CameraHeight);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, RBO);

            var fboStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (fboStatus != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Framebuffer error: " + fboStatus);
            }

            PPfbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, PPfbo);

            PPtexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, PPtexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, (int)CameraWidth, (int)CameraHeight, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, PPtexture, 0);

            var fboStatus2 = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (fboStatus2 != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Framebuffer error: " + fboStatus2);
            }

            _controller = new ImGuiController((int)WindowWidth, (int)WindowHeight);
            UI.LoadTheme();
            base.OnLoad();
        }

        protected override void OnUnload()
        {
            base.OnUnload();
        }

        public static System.Numerics.Vector3 BG_Color = new System.Numerics.Vector3(0.3f);
        bool vsynconoff = true;
        float spacing = 5f;
        public static float fontSize = 1.0f;
        int selectedObject = 0;
        int selectedLight = 0;

        public static bool showDemoWindow = false;
        public static bool showStatistics = false;
        public static bool showObjectProperties = true;
        public static bool showLightProperties = true;
        public static bool showOutliner = true;
        public static bool showSettings = false;

        // Render loop
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            // Bind FBO and clear color and depth buffer
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
            GL.ClearColor(new Color4(BG_Color.X, BG_Color.Y, BG_Color.Z, 1f));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.DepthTest);

            // Draw 3D objects
            {
                GeneralInput(args);

                // Camera pos and FOV
                Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), CameraWidth / CameraHeight, 0.1f, 100.0f);
                Matrix4 view = Matrix4.LookAt(position, position + front, up);

                // Use texture
                _diffuseMap.Use(TextureUnit.Texture0);

                R_3D.Objects[0] = new R_3D.Object
                {
                    RelTransform = R_3D.Objects[0].RelTransform,
                    Name = R_3D.Objects[0].Name,
                    Material = R_3D.Objects[0].Material,
                    VertData = R_3D.Objects[0].VertData,
                    Indices = R_3D.Objects[0].Indices,
                    Location = R_3D.Objects[0].Location,
                    Rotation = new Vector3(
                        R_3D.Objects[0].Rotation.X,
                        R_3D.Objects[0].Rotation.Y + (float)args.Time * 10,
                        R_3D.Objects[0].Rotation.Z),
                    Scale = R_3D.Objects[0].Scale
                };

                // Main function for drawing the array of objects
                R_3D.DrawObjects(projection, view);
                R_3D.DrawLights(projection, view);

                // Editor navigation on right click
                if (IsMouseButtonDown(MouseButton.Right) | IsKeyDown(Keys.LeftAlt))
                {
                    MouseInput();
                }
            }

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, FBO);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, PPfbo);
            GL.BlitFramebuffer(0, 0, (int)CameraWidth, (int)CameraHeight, 0, 0, (int)CameraWidth, (int)CameraHeight, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            R_3D.fboShader.Use();
            GL.BindVertexArray(R_3D.rectVAO);
            GL.Disable(EnableCap.DepthTest);
            GL.BindTexture(TextureTarget.Texture2DMultisample, PPtexture);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            //R_3D.FBOlogic(CameraWidth, CameraHeight);

            // UI
            _controller.Update(this, (float)args.Time);
            ImGui.DockSpaceOverViewport();

            if (showDemoWindow)
            {
                ImGui.ShowDemoWindow();
            }

            UI.LoadMenuBar();
            if (showStatistics) UI.LoadStatistics(CameraWidth, CameraHeight, Yaw, Pitch, position, spacing);
            if (showObjectProperties) UI.LoadObjectProperties(ref selectedObject, spacing);
            if (showLightProperties) UI.LoadLightProperties(ref selectedLight, spacing);
            if (showOutliner) UI.LoadOutliner(ref selectedObject, ref selectedLight, spacing);

            if (showSettings)
            {
                // Settings
                ImGui.Begin("Settings");

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                if (ImGui.Checkbox("V-Sync", ref vsynconoff))
                {
                    if (vsynconoff == false)
                    {
                        VSync = VSyncMode.Off;
                    }

                    if (vsynconoff == true)
                    {
                        VSync = VSyncMode.On;
                    }
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                if (ImGui.TreeNode("Camera"))
                {
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.SliderInt("Field of View", ref FOV, 10, 179);
                    ImGui.SliderFloat("Sensitivity", ref sensitivity, 0.1f, 2.0f, "%.1f");
                    ImGui.SliderInt("Speed", ref speed, 1, 30);
                    ImGui.TreePop();
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                if (ImGui.TreeNode("Lightning"))
                {
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Text("Background Color");
                    if (ImGui.ColorEdit3("Background Color", ref BG_Color, ImGuiColorEditFlags.NoLabel))
                    {
                        GL.ClearColor(new Color4(BG_Color.X, BG_Color.Y, BG_Color.Z, 1f));
                    }
                    ImGui.TreePop();
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                if (ImGui.TreeNode("Editor"))
                {
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    if (ImGui.SliderFloat("Font Size", ref fontSize, 0.8f, 2.0f, "%.1f"))
                    {
                        ImGui.GetIO().FontGlobalScale = fontSize;
                    }
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    if (ImGui.SliderFloat("Spacing", ref spacing, 1f, 10f, "%.1f"))
                        ImGui.TreePop();
                }
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.End();
            }

            UI.LoadGameWindow(ref CameraWidth, ref CameraHeight, WindowWidth);

            _controller.Render();
            ImGuiController.CheckGLError("End of frame");

            Context.SwapBuffers();
            base.OnRenderFrame(args);
        }

        private void MouseInput()
        {
            if (Yaw > 89.0f)
            {
                Yaw = 89.0f;
            }

            else if (Yaw < -89.0f)
            {
                Yaw = -89.0f;
            }

            else
            {
                Yaw -= MouseState.Delta.Y * sensitivity;
            }

            Pitch += MouseState.Delta.X * sensitivity;

            front.X = (float)Math.Cos(MathHelper.DegreesToRadians(Yaw)) * (float)Math.Cos(MathHelper.DegreesToRadians(Pitch));
            front.Y = (float)Math.Sin(MathHelper.DegreesToRadians(Yaw));
            front.Z = (float)Math.Cos(MathHelper.DegreesToRadians(Yaw)) * (float)Math.Sin(MathHelper.DegreesToRadians(Pitch));
            front = Vector3.Normalize(front);
        }

        // Keyboard input
        private void GeneralInput(FrameEventArgs args)
        {
            if (IsMouseButtonPressed(MouseButton.Right) | IsKeyDown(Keys.LeftAlt))
            {
                CursorState = CursorState.Grabbed;
            }

            if (IsMouseButtonReleased(MouseButton.Right) | IsKeyReleased(Keys.LeftAlt))
            {
                CursorState = CursorState.Normal;
            }

            // Close editor
            if (IsKeyDown(Keys.Escape) | CloseWindow == true)
            {
                Close();
            }

            // Delete
            if (IsKeyPressed(Keys.Delete))
            {
                if (selectedObject > 0)
                {
                    R_3D.Objects.RemoveAt(selectedObject);
                    selectedObject -= 1;
                }
            }

            // X and Z movement
            if (IsKeyDown(Keys.W))
            {
                position += front * speed * (float)args.Time;
            }
            if (IsKeyDown(Keys.S))
            {
                position -= front * speed * (float)args.Time;
            }
            if (IsKeyDown(Keys.A))
            {
                position -= Vector3.Normalize(Vector3.Cross(front, up)) * speed * (float)args.Time;
            }
            if (IsKeyDown(Keys.D))
            {
                position += Vector3.Normalize(Vector3.Cross(front, up)) * speed * (float)args.Time;
            }

            // Y movement
            if (IsKeyDown(Keys.Q))
            {
                position -= up * speed * (float)args.Time;
            }
            if (IsKeyDown(Keys.E))
            {
                position += up * speed * (float)args.Time;
            }
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (IsKeyPressed(Keys.D1)) showStatistics = Math_Functions.ToggleBool(showStatistics);
            if (IsKeyPressed(Keys.D2)) showObjectProperties = Math_Functions.ToggleBool(showObjectProperties);
            if (IsKeyPressed(Keys.D3)) showLightProperties = Math_Functions.ToggleBool(showLightProperties);
            if (IsKeyPressed(Keys.D4)) showOutliner = Math_Functions.ToggleBool(showOutliner);
            if (IsKeyPressed(Keys.D5)) showSettings = Math_Functions.ToggleBool(showSettings);

            base.OnKeyDown(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _controller.MouseScroll(e.Offset);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            _controller.PressChar((char)e.Unicode);
        }
    }
}