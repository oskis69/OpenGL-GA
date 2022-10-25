﻿using System;
using ImGuiNET;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using static OpenTK_Learning.R_3D;

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

        private Texture diffuseMap;
        private Texture normalMap;
        public static Shader PhongShader = new Shader("./../../../Engine/Engine_Resources/shaders/Lightning/default.vert", "./../../../Engine/Engine_Resources/shaders/Lightning/default.frag", true);
        public static Shader LightShader = new Shader("./../../../Engine/Engine_Resources/shaders/Lightning/light.vert", "./../../../Engine/Engine_Resources/shaders/Lightning/light.frag");
        public static Shader WireframeShader = new Shader("./../../../Engine/Engine_Resources/shaders/Misc/Wireframe.vert", "./../../../Engine/Engine_Resources/shaders/Misc/Wireframe.frag");

        public static Material M_Default;
        public static Material M_Car;
        public static Material M_Floor;

        public static System.Numerics.Vector3 BG_Color = new System.Numerics.Vector3(0.12f);
        public static float fontSize = 0.55f;
        public static bool wireframeonoff = false;
        bool vsynconoff = true;
        float spacing = 2f;
        public static int selectedObject = 0;
        public static int selectedLight = 0;

        // Window bools
        public static bool showDemoWindow = false;
        public static bool showStatistics = false;
        public static bool showObjectProperties = true;
        public static bool showLightProperties = true;
        public static bool showOutliner = true;
        public static bool showSettings = false;
        public static bool CloseWindow = false;
        bool fullScreen = false;

        // Camera settings
        public static float WindowWidth;
        public static float WindowHeight;
        float CameraWidth;
        float CameraHeight;
        float Yaw;
        float Pitch = -90f;
        int FOV = 75;
        int speed = 12;
        float sensitivity = 0.25f;

        // Rendering
        public static float NoiseAmount = 0.5f;

        public static float lineWidth = 0.1f;

        ImGuiController _controller;

        // Camera transformations
        Vector3 front = new Vector3(0.0f, 0.0f, -1.0f);
        Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
        public static Vector3 position = new Vector3(0.0f, 3.0f, 3.0f);

        // Runs when the window is resizeds
        protected override void OnResize(ResizeEventArgs e)
        {
            _controller.WindowResized((int)WindowWidth, (int)WindowHeight);

            // Matches window scale to new resize
            WindowWidth = e.Width;
            WindowHeight = e.Height;

            GL.Viewport(0, 0, e.Width, e.Height);
            base.OnResize(e);
        }

        // Runs after Run();
        protected override void OnLoad()
        {
            IsVisible = true;
            VSync = VSyncMode.On;

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);
            GL.ClearColor(new Color4(0.5f, 0.5f, 0.5f, 1f));

            GL.LineWidth(1.5f);

            // Load textures
            diffuseMap = Texture.LoadFromFile("./../../../Resources/3D_Models/Car_diffuse.jpg", TextureUnit.Texture0);
            normalMap = Texture.LoadFromFile("./../../../Resources/3D_Models/NormTest/Car_normal.png", TextureUnit.Texture0);

            M_Default = new Material
            {
                ambient = new Vector3(0.1f),
                diffuse = new Vector4(1, 1, 1, 0),
                specular = new Vector3(0.5f),
                shininess = 96.0f
            };
            M_Car = new Material
            {
                ambient = new Vector3(0.1f),
                diffuse = new Vector4(1, 1, 1, 0),
                specular = new Vector3(0.5f),
                shininess = 64.0f
            };
            M_Floor = new Material
            {
                ambient = new Vector3(0.1f),
                diffuse = new Vector4(1, 1, 1, 0),
                specular = new Vector3(0.5f),
                shininess = 32.0f
            };

            // Add default objects
            AddObjectToArray(false, "Cube", M_Default,
                new Vector3(2f),          // Scale
                new Vector3(0f, 1f, 0f),  // Location
                new Vector3(0f),          // Rotation
                Cube.vertices, Cube.indices);
            AddObjectToArray(false, "Plane", M_Floor,
                new Vector3(15f), // Scale
                new Vector3(0f),  // Location
                new Vector3(0f),  // Rotation
                Plane.vertices, Plane.indices);

            // Load model into temporary variables
            R_Loading.LoadModel("./../../../Resources/3D_Models/Monkey.fbx");

            // A car
            R_Loading.LoadModel("./../../../Resources/3D_Models/Car.fbx");
            AddObjectToArray(false, "Car", M_Car,
                new Vector3(2f),            // Scale
                new Vector3(0, 4, 0),       // Location
                new Vector3(180f, 90f, 0f), // Rotation
                R_Loading.importedData, R_Loading.importindices);
            
            // Generate VAO, VBO and EBO
            ConstructObjects();

            // Add lights
            R_Loading.LoadModel("./../../../Engine/Engine_Resources/Primitives/PointLightMesh.fbx");

            AddLightToArray(0.75f, 5, 1, 1, "Directional Light", new Vector3(1f, 1f, 1f), LightShader, new Vector3(-1, 1, 1), new Vector3(10f, 8f, 10f), new Vector3(0f), R_Loading.importedData, R_Loading.importindices);
            AddLightToArray(1, 5, 1, 0, "Point Light", new Vector3(1f, 0f, 0f), LightShader, new Vector3(1f), new Vector3(4f, 8, 0f), new Vector3(0f), R_Loading.importedData, R_Loading.importindices);
            ConstructLights();

            PhongShader.SetFloat("NoiseAmount", NoiseAmount);

            // Generate two screen triangles
            GenFBO(CameraWidth, CameraHeight);
            GenScreenRect();

            _controller = new ImGuiController((int)WindowWidth, (int)WindowHeight);
            UI.LoadTheme();

            fboShader.SetFloat("zoom", 1);

            base.OnLoad();
        }

        public static bool isMainHovered;

        // Render loop
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Enable(EnableCap.DepthTest);

            // Bind FBO and clear color and depth buffer
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
            GL.Viewport(0, 0, (int)WindowWidth, (int)WindowHeight);
            GL.ClearColor(new Color4(BG_Color.X, BG_Color.Y, BG_Color.Z, 1f));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            // Draw 3D objects
            {
                // Allow keyboard input only when mouse is over viewport
                if (isMainHovered == true | IsMouseButtonDown(MouseButton.Right) | IsKeyDown(Keys.LeftAlt))
                {
                    // Editor camera movement
                    GeneralInput(args);
                }

                // Editor navigation on right click
                if (IsMouseButtonDown(MouseButton.Right) | IsKeyDown(Keys.LeftAlt))
                {
                    CursorState = CursorState.Grabbed;
                    MouseInput();
                }

                if (IsMouseButtonReleased(MouseButton.Right) | IsKeyReleased(Keys.LeftAlt)) CursorState = CursorState.Normal;

                // Use texture
                diffuseMap.Use(TextureUnit.Texture1);
                normalMap.Use(TextureUnit.Texture2);

                // Main function for drawing the array of objects
                if (wireframeonoff == true) GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                else GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                // Camera pos and FOV
                Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), CameraWidth / CameraHeight, 0.1f, 100.0f);
                Matrix4 view = Matrix4.LookAt(position, position + front, up);

                // Draw all objects
                DrawObjects(projection, view, wireframeonoff);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                
                // Draw all lights
                DrawLights(projection, view);
            }

            fboShader.Use();
            GL.BindVertexArray(rectVAO);
            GL.Disable(EnableCap.DepthTest);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // UI
            _controller.Update(this, (float)args.Time);
            ImGui.DockSpaceOverViewport();

            if (showDemoWindow) ImGui.ShowDemoWindow();

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

                if (ImGui.TreeNode("Rendering"))
                {
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    if (ImGui.Checkbox("V-Sync", ref vsynconoff))
                    {
                        if (vsynconoff == false) VSync = VSyncMode.Off;
                        if (vsynconoff == true) VSync = VSyncMode.On;
                    }
                    ImGui.Checkbox("Wireframe", ref wireframeonoff);
                    ImGui.TreePop();
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
                    ImGui.Text("Noise Amount"); ImGui.SameLine(); UI.HelpMarker("Values around 0.5 reduce banding \nHigh values causes visible noise");
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    if (ImGui.SliderFloat("##NA", ref NoiseAmount, 0.01f, 10.0f, "%.2f")) PhongShader.SetFloat("NoiseAmount", NoiseAmount);
                    ImGui.TreePop();
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                if (ImGui.TreeNode("Editor"))
                {
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    if (ImGui.SliderFloat("Font Size", ref fontSize, 0.1f, 2.0f, "%.1f"))
                    {
                        ImGui.GetIO().FontGlobalScale = fontSize;

                        ImGui.TreePop();
                    }

                    ImGui.SliderFloat("Spacing", ref spacing, 1f, 10f, "%.1f");
                }
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.End();
            }

            UI.LoadGameWindow(ref CameraWidth, ref CameraHeight);

            _controller.Render();
            ImGuiController.CheckGLError("End of frame");

            Context.SwapBuffers();            

            base.OnRenderFrame(args);
        }

        private void MouseInput()
        {
            Pitch += MouseState.Delta.X * sensitivity;
            Yaw -= MouseState.Delta.Y * sensitivity;
            
            front.X = (float)Math.Cos(MathHelper.DegreesToRadians(Math.Clamp(Yaw, -89, 89))) * (float)Math.Cos(MathHelper.DegreesToRadians(Pitch));
            front.Y = (float)Math.Sin(MathHelper.DegreesToRadians(Math.Clamp(Yaw, -89, 89)));
            front.Z = (float)Math.Cos(MathHelper.DegreesToRadians(Math.Clamp(Yaw, -89, 89))) * (float)Math.Sin(MathHelper.DegreesToRadians(Pitch));
            front = Vector3.Normalize(front);
        }

        float xoffset = 0;
        float yoffset = 0;
        float zoom = 1;
        float step = 0.05f;

        // Keyboard input
        private void GeneralInput(FrameEventArgs args)
        {
            if (IsKeyDown(Keys.Right))
            {
                xoffset += step;
                fboShader.SetFloat("Xoffset", xoffset);
            }

            if (IsKeyDown(Keys.Up))
            {
                yoffset += step;
                fboShader.SetFloat("Yoffset", yoffset);
            }

            if (IsKeyDown(Keys.Left))
            {
                xoffset -= step;
                fboShader.SetFloat("Xoffset", xoffset);
            }

            if (IsKeyDown(Keys.Down))
            {
                yoffset -= step;
                fboShader.SetFloat("Yoffset", yoffset);
            }

            if (IsKeyDown(Keys.M))
            {
                zoom -= 0.05f;
                fboShader.SetFloat("zoom", zoom);
            }

            if (IsKeyDown(Keys.N))
            {
                zoom += 0.05f;
                fboShader.SetFloat("zoom", zoom);
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
                    Objects.RemoveAt(selectedObject);
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
            if (isMainHovered)
            {
                if (IsKeyPressed(Keys.D1)) showStatistics = Math_Functions.ToggleBool(showStatistics);
                if (IsKeyPressed(Keys.D2)) showObjectProperties = Math_Functions.ToggleBool(showObjectProperties);
                if (IsKeyPressed(Keys.D3)) showLightProperties = Math_Functions.ToggleBool(showLightProperties);
                if (IsKeyPressed(Keys.D4)) showOutliner = Math_Functions.ToggleBool(showOutliner);
                if (IsKeyPressed(Keys.D5)) showSettings = Math_Functions.ToggleBool(showSettings);
            }

            if (IsKeyPressed(Keys.F11)) fullScreen = Math_Functions.ToggleBool(fullScreen);

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