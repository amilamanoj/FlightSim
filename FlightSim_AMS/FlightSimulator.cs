using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

/**
 * 
 * @author Amila Manoj
 * 
 * 
 * */
namespace FlightSim_AMS
{
    public struct VertexMultitextured
    {
        

        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 TextureCoordinate;
        public Vector4 TexWeights;

        public static int SizeInBytes = (3 + 3 + 4 + 4) * sizeof(float);
        public static VertexElement[] VertexElements = new VertexElement[]
          {
              new VertexElement( 0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0 ),
              new VertexElement( 0, sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0 ),
              new VertexElement( 0, sizeof(float) * 6, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0 ),
              new VertexElement( 0, sizeof(float) * 10, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1 ),
          };
    }

    public class FlightSimulator : Microsoft.Xna.Framework.Game
    {
        #region delarations

        GraphicsDeviceManager graphics;
        GraphicsDevice device;

        SpriteBatch spriteBatch;
        SpriteFont gameFont;
        SpriteFont optionsMenuFont;
        SpriteFont menuFont;

        LensFlareComponent lensFlare;
        
        int[,] cityFloorPlan;
        int[] buildingHeights = new int[] { 0, 2, 2, 6, 5, 4 };
        enum CollisionType { None, Building, Boundary, Terrain, Item, Warning}
        enum GameState { InGame, Menu, About, Options, Help }
        enum InputDevice { Mouse, Keyboard, Motion }
        enum SelectedOption { Controller, Camera, RMn, GMn, BMn,RMx, GMx, BMx }

        GameState currentState;
        InputDevice currentInput;
        SelectedOption currentOption;
        MotionDetector motionDetector;

        const int shadowMapWidthHeight = 2048;

        const int maxitemes = 50;
        const int maxTrees = 10;

        const float waterHeight = 1f;
        RenderTarget2D refractionRenderTarget;
        Texture2D refractionMap;
        RenderTarget2D reflectionRenderTarget;
        Texture2D reflectionMap;
        RenderTarget2D cloudsRenderTarget;
        Texture2D cloudStaticMap;
        VertexPositionTexture[] fullScreenVertices;
        VertexDeclaration fullScreenVertexDeclaration;

        Vector3 terrainPosition;
        Vector3 windDirection = new Vector3(0, 0, 1);
        Vector3 lightDirection = new Vector3(-1, -0.1f, 0.3f);
        Vector3 cameraPosition;
        Vector2 optConPos, optCamPos, optCamPrvPos;
        Vector2 optMaxColRPos, optMaxColGPos, optMaxColBPos;
        Vector2 optMinColRPos, optMinColGPos, optMinColBPos;
        
        VertexBuffer cityVertexBuffer;
        VertexDeclaration texturedVertexDeclaration;
        int terrainWidth;
        int terrainLength;
        int worldWidth;
        int worldHeight;
        float[,] heightData;

        VertexBuffer terrainVertexBuffer;
        IndexBuffer terrainIndexBuffer;
        VertexDeclaration terrainVertexDeclaration;

        VertexBuffer waterVertexBuffer;
        VertexDeclaration waterVertexDeclaration;

        BoundingBox[] buildingBoundingBoxes;
        BoundingBox completeWorldBox;
        List<BoundingSphere> itemList = new List<BoundingSphere>();
        List<BoundingSphere> treeList = new List<BoundingSphere>();
        BoundingSphere terrainSphere;

        Effect tEffect;
        Effect customEffect;
        Effect basicEffect;
        Matrix viewMatrix;
        Matrix projectionMatrix;
        Matrix reflectionViewMatrix;

        Texture2D grassTexture;
        Texture2D sandTexture;
        Texture2D rockTexture;
        //Texture2D snowTexture;

        Texture2D sceneryTexture;
        Texture2D mainMenuBG;
        Texture2D aboutMenuBG;
        Texture2D optionsMenuBG;
        Texture2D helpBG;
        Texture2D cloudMap;
        Texture2D waterBumpMap;
        Texture2D camFeed;
        Texture2D optSelectedBack;
        //Texture2D maxColTex;
        Texture2D ColTex;

        private Color maxCol, minCol;
        //Model car;
        //Texture2D[] planeTextures;
        Model shipModel;
        Model treeModel;
        Model itemModel;
        Model skyDome;
        Model textModel;
        /*
        // The shadow map render target, depth buffer, and texture
        RenderTarget2D shadowRenderTarget;
        DepthStencilBuffer shadowDepthBuffer;
        Texture2D shadowMap;

        // ViewProjection matrix from the lights perspective
        Matrix lightViewProjection;
        */
        ParticleSystem fireParticles;
        //ParticleSystem smokePlumeParticles;

        SoundEffect timeS;
        SoundEffect timeE;
        SoundEffect select;
        SoundEffect explode;
        SoundEffect item;
        SoundEffect shoot;
        SoundEffect go;
        SoundEffect warning;
        SoundEffectInstance warningInstance;
        SoundEffectInstance goInstance;
        SoundEffectInstance timeSInstance;

        Vector3 shipPosition;//(y,z,-x)
        Vector3 positionOnHeightMap;

        Quaternion shipRotation = Quaternion.Identity;
        Quaternion cameraRotation = Quaternion.Identity;

        // Input state.
        KeyboardState currentKeyboardState;
        KeyboardState lastKeyboardState;

        Random random = new Random();

        private bool started = false;
        private bool consoleData = true;
        private bool drawCamFeed = false;
        private bool camAvailable = false;
        private bool showWarning =false;
        private bool boundryWarning = false;
        private bool showTooHigh = false;

        private float tempz;
        private float warningTime = 0f;
        private float rotAngle = 0f;
        //private float k = 2f;
        private float upDownSpd=0f;
        private float rotateSpd = 0f;
        private float gameSpeed = 1f;
        private float moveSpeed;
        private float tempTime;
        private int score;
        private int multiplier = 5;
        private int lives;
        //private int selectedColorVal=0;
        private float gamestarttime = 0f;
        private string currentCameraName;
        private int screenWidth, screenHeight;
        TimerCallback camTcb;
        Timer camFeedTimer;
        
        BoundingSphere shipSphere;
        BoundingSphere warningSphere;
        //private BoundingSphereRenderer boundingSphereRenderer;

        Vector3 p1;

        #endregion


        public FlightSimulator()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            //graphics.MinimumPixelShaderProfile = ShaderProfile.PS_2_0;
            fireParticles = new FireParticleSystem(this, Content);
            // Set the draw order so the explosions and fire
            // will appear over the top of the smoke
            //smokePlumeParticles.DrawOrder = 100;
            fireParticles.DrawOrder = 500;
            // Register the particle system components.
            Components.Add(fireParticles);

            lensFlare = new LensFlareComponent(this);
            Components.Add(lensFlare);
        }

        #region initialization

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = graphics.GraphicsDevice.DisplayMode.Width;
            graphics.PreferredBackBufferHeight = graphics.GraphicsDevice.DisplayMode.Height;
            graphics.IsFullScreen = true;
            graphics.ApplyChanges();
            Window.Title = "Flight Simulator";
            lightDirection.Normalize();
            lensFlare.LightDirection = lightDirection;
            LoadFloorPlan();
            SetUpBoundingBoxes();
            terrainPosition = new Vector3(cityFloorPlan.GetLength(1) * multiplier -5, 0, 0);
            MediaPlayer.IsRepeating = true;
            currentState = GameState.Menu;
            currentInput = InputDevice.Mouse;
            camTcb = new TimerCallback(disableCamFeed);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            device = graphics.GraphicsDevice;
            screenWidth = device.Viewport.Width;
            screenHeight = device.Viewport.Height;
            camFeed = new Texture2D(device, 1, 1);
            motionDetector = new MotionDetector(screenHeight, screenWidth);
            camAvailable = motionDetector.isCameraAvailable();
            if (camAvailable){
            currentCameraName= motionDetector.ConnectToFeed();
            }

            optConPos = new Vector2((float)200 / 800 * screenWidth, (float)190 / 640 * screenHeight);
            optCamPos = new Vector2((float)200 / 800 * screenWidth, (float)220 / 640 * screenHeight);
            optMinColRPos = new Vector2((float)150 / 800 * screenWidth, (float)360 / 640 * screenHeight);
            optMinColGPos = new Vector2((float)150 / 800 * screenWidth, (float)390 / 640 * screenHeight);
            optMinColBPos = new Vector2((float)150 / 800 * screenWidth, (float)420 / 640 * screenHeight);
            optMaxColRPos = new Vector2((float)350 / 800 * screenWidth, (float)360 / 640 * screenHeight);
            optMaxColGPos = new Vector2((float)350 / 800 * screenWidth, (float)390 / 640 * screenHeight);
            optMaxColBPos = new Vector2((float)350 / 800 * screenWidth, (float)420 / 640 * screenHeight);
            optCamPrvPos = new Vector2((float)530 / 800 * screenWidth, (float)200 / 640 * screenHeight);

            sceneryTexture = Content.Load<Texture2D>("Textures/texturemap");
            grassTexture = Content.Load<Texture2D>("Textures/grass");
            sandTexture = Content.Load<Texture2D>("Textures/sand");
            rockTexture = Content.Load<Texture2D>("Textures/rock");
            mainMenuBG = Content.Load<Texture2D>("Textures/menuBack");
            aboutMenuBG = Content.Load<Texture2D>("Textures/aboutMenuBack");
            optionsMenuBG = Content.Load<Texture2D>("Textures/optionsMenuBack");
            helpBG = Content.Load<Texture2D>("Textures/helpMenuBack");
            cloudMap = Content.Load<Texture2D>("Textures/cloudMap");
            waterBumpMap = Content.Load<Texture2D>("Textures/waterbump");
            optSelectedBack = Content.Load<Texture2D>("Textures/optionsSelected");
            //maxColTex = new Texture2D(GraphicsDevice, 1, 1);
            ColTex = new Texture2D(GraphicsDevice, 1, 1);
            //maxColTex.SetData(new Color[] { new Color(new Vector3(motionDetector.getRMax(), motionDetector.getGMax(), motionDetector.getBMax())) });
            ColTex.SetData(new Color[] { new Color(new Vector3(motionDetector.getRMax(), motionDetector.getGMax(), motionDetector.getBMax())) });

            SetUpBuildingVertices();
            basicEffect = new BasicEffect(device, null);
            customEffect = Content.Load<Effect>("effects");
            tEffect = Content.Load<Effect>("tEffects");
            shipModel = Content.Load<Model>("Models/jet2");
            //car = Content.Load<Model>("Models/car");
            //textModel=Content.Load<Model>("Models/moratext");
            itemModel = BasicLoadModel("Models/bonus1");
            treeModel = BasicLoadModel("Models/tree2");
            //skyboxModel = LoadModel("Models/skybox2", out skyboxTextures);
            skyDome = Content.Load<Model>("Models/dome"); 
            skyDome.Meshes[0].MeshParts[0].Effect = tEffect.Clone(device);
            //boundingSphereRenderer = new BoundingSphereRenderer(this);
            //boundingSphereRenderer.OnCreateDevice();

            //camPreviewBorder = new Texture2D(device, 1, 1, 0, TextureUsage.Tiled, SurfaceFormat.Color);
            //Int32[] pixel = { 0xFFFFFF }; // White. 0xFF is Red, 0xFF0000 is Blue
            //camPreviewBorder.SetData(pixel,0,1,SetDataOptions.None);//, 0, camPreviewBorder.Width * camPreviewBorder.Height);

            PresentationParameters pp = device.PresentationParameters;
            refractionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, 1, device.DisplayMode.Format);
            reflectionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, 1, device.DisplayMode.Format);
            cloudsRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, 1, device.DisplayMode.Format);
            cloudStaticMap = CreateStaticMap(16);

            timeS = Content.Load<SoundEffect>("Sounds/timeS"); 
            timeE = Content.Load<SoundEffect>("Sounds/timeE");
            select = Content.Load<SoundEffect>("Sounds/select");
            explode = Content.Load<SoundEffect>("Sounds/explode");
            item = Content.Load<SoundEffect>("Sounds/item");
            shoot = Content.Load<SoundEffect>("Sounds/shoot");
            go = Content.Load<SoundEffect>("Sounds/go");
            warning = Content.Load<SoundEffect>("Sounds/warning");
            warningInstance = warning.CreateInstance();
            goInstance = go.CreateInstance();
            timeSInstance = timeS.CreateInstance();

            spriteBatch = new SpriteBatch(graphics.GraphicsDevice);
            gameFont = Content.Load<SpriteFont>("gameFont");
            menuFont = Content.Load<SpriteFont>("menuFont");
            optionsMenuFont = Content.Load<SpriteFont>("optionsFont");
            LoadTerrainVertices();
            //MediaPlayer.Volume = 0.6f;
            setupMenu();

        }

        private void setupMenu()
        {
            goInstance.Stop();
            timeSInstance.Stop();
            warningInstance.Stop();
            currentState = GameState.Menu;
            cameraPosition = new Vector3(0, 0.2f, 0);
            viewMatrix = Matrix.CreateLookAt(cameraPosition, new Vector3(0, 0, -5), new Vector3(0, 1, 0)); //camera 
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.1f, 5.0f);
            lives = 3;
            score = 0;
            tempTime = 0;
            MediaPlayer.Play(Content.Load<Song>("Sounds/menuMusic"));
        }

        private void SetUpBuildingVertices()//vertices of building
        {
            int differentBuildings = buildingHeights.Length - 1;
            float imagesInTexture = 1 + differentBuildings * 2;

            int cityWidth = cityFloorPlan.GetLength(0);
            int cityLength = cityFloorPlan.GetLength(1);


            List<VertexPositionNormalTexture> verticesList = new List<VertexPositionNormalTexture>();
            for (int x = 0; x < cityWidth; x++)
            {
                for (int z = 0; z < cityLength; z++)
                {

                    int currentbuilding = cityFloorPlan[x, z];

                    //floor or ceiling                                                                                                       up is up VVV                   texture position for the vertex in texture(bitmap)
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z) * multiplier, new Vector3(0, 1, 0), new Vector2((currentbuilding * 2 / imagesInTexture), 1)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1) * multiplier, new Vector3(0, 1, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z) * multiplier, new Vector3(0, 1, 0), new Vector2((currentbuilding * 2 + 1) / imagesInTexture, 1)));

                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1) * multiplier, new Vector3(0, 1, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z - 1) * multiplier, new Vector3(0, 1, 0), new Vector2((currentbuilding * 2 + 1) / imagesInTexture, 0)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z) * multiplier, new Vector3(0, 1, 0), new Vector2((currentbuilding * 2 + 1) / imagesInTexture, 1)));

                    if (currentbuilding != 0)
                    {
                        //front wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z - 1) * multiplier, new Vector3(0, 0, -1), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1) * multiplier, new Vector3(0, 0, -1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z - 1) * multiplier, new Vector3(0, 0, -1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 1)));

                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1) * multiplier, new Vector3(0, 0, -1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z - 1) * multiplier, new Vector3(0, 0, -1), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z - 1) * multiplier, new Vector3(0, 0, -1), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));

                        //back wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z) * multiplier, new Vector3(0, 0, 1), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z) * multiplier, new Vector3(0, 0, 1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z) * multiplier, new Vector3(0, 0, 1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));

                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z) * multiplier, new Vector3(0, 0, 1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z) * multiplier, new Vector3(0, 0, 1), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z) * multiplier, new Vector3(0, 0, 1), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));

                        //left wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z) * multiplier, new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z - 1) * multiplier, new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1) * multiplier, new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));

                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1) * multiplier, new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z) * multiplier, new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z) * multiplier, new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));

                        //right wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z) * multiplier, new Vector3(1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z - 1) * multiplier, new Vector3(1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z - 1) * multiplier, new Vector3(1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 1)));

                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z - 1) * multiplier, new Vector3(1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z) * multiplier, new Vector3(1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z) * multiplier, new Vector3(1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));

                    }
                }
                cityVertexBuffer = new VertexBuffer(device, verticesList.Count * VertexPositionNormalTexture.SizeInBytes, BufferUsage.WriteOnly);

                cityVertexBuffer.SetData<VertexPositionNormalTexture>(verticesList.ToArray());
                texturedVertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTexture.VertexElements);
            }
        }

        private void SetUpBoundingBoxes() //vertex for buildings and their bounding boxes
        {
            int cityWidth = cityFloorPlan.GetLength(0);
            int cityLength = cityFloorPlan.GetLength(1);

            List<BoundingBox> bbList = new List<BoundingBox>();
            
            for (int x = 0; x < cityWidth; x++)
            {
                for (int z = 0; z < cityLength; z++)
                {
                    int buildingType = cityFloorPlan[x, z];
                    if (buildingType != 0)
                    {
                        int buildingHeight = buildingHeights[buildingType];
                        Vector3[] buildingPoints = new Vector3[2];
                        buildingPoints[0] = new Vector3(x * multiplier, 0, -z * multiplier);
                        buildingPoints[1] = new Vector3((x + 1) * multiplier, buildingHeight * multiplier, (-z - 1) * multiplier);
                        BoundingBox buildingBox = BoundingBox.CreateFromPoints(buildingPoints);
                        bbList.Add(buildingBox);
                    }
                }
            }
            buildingBoundingBoxes = bbList.ToArray();

            Vector3[] boundaryPoints = new Vector3[2];
            boundaryPoints[0] = new Vector3(0, 0, 0);
            boundaryPoints[1] = new Vector3(300, 200, -150);
            completeWorldBox = BoundingBox.CreateFromPoints(boundaryPoints);
            worldWidth = cityWidth * multiplier;
            worldHeight = cityLength * multiplier;
        }

        private void LoadFloorPlan() //town n country planning. lol
        {
            cityFloorPlan = new int[,] //[21,22] now
              {
                 //0,-1,-2,-3,...
                  {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},//0
                  {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},//1
                  {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},//2
                  {1,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,1},//3
                  {1,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},//4
                  {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,1,0,1},//5
                  {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},//6
                  {1,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},//7
                  {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,1},//8
                  {1,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,1},//9
                  {1,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},//10
                  {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},//11
                  {1,0,0,0,0,1,0,0,0,0,0,1,0,0,0,1,0,0,0,1,0,1},//12
                  {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},//13
                  {1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,1},//14
                  {1,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,1,0,0,0,1},//15
                  {1,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},//16
                  {1,0,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,0,0,1},//17
                  {1,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,1},//18
                  {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1},//19
                  {1,1,0,0,0,0,1,1,1,1,0,1,1,0,1,1,0,1,0,1,1,1},//20
              };

            int differentBuildings = buildingHeights.Length - 1;
            for (int x = 0; x < cityFloorPlan.GetLength(0); x++)
                for (int y = 0; y < cityFloorPlan.GetLength(1); y++)
                    if (cityFloorPlan[x, y] == 1)
                        cityFloorPlan[x, y] = random.Next(differentBuildings) + 1;
        } 
        
        #region terrainStuff

        private void LoadTerrainVertices()
        {
            Texture2D heightMap = Content.Load<Texture2D>("Textures/heightmap"); 
            LoadHeightData(heightMap); //just load the values from the map to an array
            VertexMultitextured[] terrainVertices = SetUpTerrainVertices();
            int[] terrainIndices = SetUpTerrainIndices();
            terrainVertices = CalculateNormals(terrainVertices, terrainIndices);
            CopyToTerrainBuffers(terrainVertices, terrainIndices);
            terrainVertexDeclaration = new VertexDeclaration(device, VertexMultitextured.VertexElements);
            
            SetUpWaterVertices();
            waterVertexDeclaration = new VertexDeclaration(device, VertexPositionTexture.VertexElements);
            
            /*
            Texture2D treeMap = Content.Load<Texture2D>("treeMap");
            List<Vector3> treeList = GenerateTreePositions(treeMap, terrainVertices); CreateBillboardVerticesFromList(treeList);
            */
            
            fullScreenVertices = SetUpFullscreenVertices();
            fullScreenVertexDeclaration = new VertexDeclaration(device, VertexPositionTexture.VertexElements);
             
        }

        private void LoadHeightData(Texture2D heightMap)
        {
            float minimumHeight = float.MaxValue;
            float maximumHeight = float.MinValue;

            terrainWidth = heightMap.Width;
            terrainLength = heightMap.Height;

            Color[] heightMapColors = new Color[terrainWidth * terrainLength];
            heightMap.GetData(heightMapColors);

            heightData = new float[terrainWidth, terrainLength];
            for (int x = 0; x < terrainWidth; x++)
                for (int y = 0; y < terrainLength; y++)
                {
                    heightData[x, y] = heightMapColors[x + y * terrainWidth].R;
                    if (heightData[x, y] < minimumHeight) minimumHeight = heightData[x, y];
                    if (heightData[x, y] > maximumHeight) maximumHeight = heightData[x, y];
                }
            
            for (int x = 0; x < terrainWidth; x++)
                for (int y = 0; y < terrainLength; y++)
                {
                    heightData[x, y] = (heightData[x, y] - minimumHeight) / (maximumHeight - minimumHeight) * 10.0f;
                }

            worldWidth += terrainWidth;
            //worldHeight = terrainLength;
        }

        private VertexMultitextured[] SetUpTerrainVertices()
        {
            VertexMultitextured[] terrainVertices = new VertexMultitextured[terrainWidth * terrainLength];

            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainLength; y++)
                {
                    terrainVertices[x + y * terrainWidth].Position = new Vector3(x, heightData[x, y], -y) +terrainPosition; //change drawing height etc.
                    terrainVertices[x + y * terrainWidth].TextureCoordinate.X = (float)x / 30.0f;
                    terrainVertices[x + y * terrainWidth].TextureCoordinate.Y = (float)y / 30.0f;

                    //x-sand y-grass z-rock w-snow
                    terrainVertices[x + y * terrainWidth].TexWeights.X = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 0) / 1f, 0, 1);
                    terrainVertices[x + y * terrainWidth].TexWeights.Y = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 6) / 10.0f, 0, 1);
                    terrainVertices[x + y * terrainWidth].TexWeights.Z = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 10) / 4f, 0, 1);
                    //terrainVertices[x + y * terrainWidth].TexWeights.W = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 30) / 6.0f, 0, 1);

                    float total = terrainVertices[x + y * terrainWidth].TexWeights.X;
                    total += terrainVertices[x + y * terrainWidth].TexWeights.Y;
                    total += terrainVertices[x + y * terrainWidth].TexWeights.Z;
                    //total += terrainVertices[x + y * terrainWidth].TexWeights.W;

                    terrainVertices[x + y * terrainWidth].TexWeights.X /= total;
                    terrainVertices[x + y * terrainWidth].TexWeights.Y /= total;
                    terrainVertices[x + y * terrainWidth].TexWeights.Z /= total;
                    //terrainVertices[x + y * terrainWidth].TexWeights.W /= total;
                }
            }

            return terrainVertices;
        }

        private int[] SetUpTerrainIndices()
        {
            int[] indices = new int[(terrainWidth - 1) * (terrainLength - 1) * 6];
            int counter = 0;
            for (int y = 0; y < terrainLength - 1; y++)
            {
                for (int x = 0; x < terrainWidth - 1; x++)
                {
                    int lowerLeft = x + y * terrainWidth;
                    int lowerRight = (x + 1) + y * terrainWidth;
                    int topLeft = x + (y + 1) * terrainWidth;
                    int topRight = (x + 1) + (y + 1) * terrainWidth;

                    indices[counter++] = topLeft;
                    indices[counter++] = lowerRight;
                    indices[counter++] = lowerLeft;

                    indices[counter++] = topLeft;
                    indices[counter++] = topRight;
                    indices[counter++] = lowerRight;
                }
            }

            return indices;
        }

        private VertexMultitextured[] CalculateNormals(VertexMultitextured[] vertices, int[] indices)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal = new Vector3(0, 0, 0);

            for (int i = 0; i < indices.Length / 3; i++)
            {
                int index1 = indices[i * 3];
                int index2 = indices[i * 3 + 1];
                int index3 = indices[i * 3 + 2];

                Vector3 side1 = vertices[index1].Position - vertices[index3].Position;
                Vector3 side2 = vertices[index1].Position - vertices[index2].Position;
                Vector3 normal = Vector3.Cross(side1, side2);

                vertices[index1].Normal += normal;
                vertices[index2].Normal += normal;
                vertices[index3].Normal += normal;
            }

            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal.Normalize();

            return vertices;
        }

        private void CopyToTerrainBuffers(VertexMultitextured[] vertices, int[] indices)
        {
            terrainVertexBuffer = new VertexBuffer(device, vertices.Length * VertexMultitextured.SizeInBytes, BufferUsage.WriteOnly);
            terrainVertexBuffer.SetData(vertices);

            terrainIndexBuffer = new IndexBuffer(device, typeof(int), indices.Length, BufferUsage.WriteOnly);
            terrainIndexBuffer.SetData(indices);
        }

        private void SetUpWaterVertices()
        {
            VertexPositionTexture[] waterVertices = new VertexPositionTexture[6];

            waterVertices[0] = new VertexPositionTexture(new Vector3(0, waterHeight, 0), new Vector2(0, 1));
            waterVertices[2] = new VertexPositionTexture(new Vector3(terrainWidth, waterHeight, -terrainLength), new Vector2(1, 0));
            waterVertices[1] = new VertexPositionTexture(new Vector3(0, waterHeight, -terrainLength), new Vector2(0, 0));

            waterVertices[3] = new VertexPositionTexture(new Vector3(0, waterHeight, 0), new Vector2(0, 1));
            waterVertices[5] = new VertexPositionTexture(new Vector3(terrainWidth, waterHeight, 0), new Vector2(1, 1));
            waterVertices[4] = new VertexPositionTexture(new Vector3(terrainWidth, waterHeight, -terrainLength), new Vector2(1, 0));

            waterVertexBuffer = new VertexBuffer(device, waterVertices.Length * VertexPositionTexture.SizeInBytes, BufferUsage.WriteOnly);
            waterVertexBuffer.SetData(waterVertices);
        }

        #endregion
        /*
        private Model LoadModel(string assetName)
        {
            Model newModel = Content.Load<Model>(assetName); foreach (ModelMesh mesh in newModel.Meshes)
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                    meshPart.Effect = effect.Clone(device);
            return newModel;
       }*/

        private Model BasicLoadModel(string assetName) //to be used with basic effects
        {
            Model newModel = Content.Load<Model>(assetName);
            return newModel;
        }
        
        private void Additems()
        {
            itemList.Clear();//****

            //int cityWidth = cityFloorPlan.GetLength(0) ;
            //int cityLength = cityFloorPlan.GetLength(1) ;

            Random random = new Random();

            while (itemList.Count < maxitemes)
            {
                int x = random.Next(worldWidth);
                int z = -random.Next(100);
                float y = (float)random.Next(30);

                //float radius = (float)random.Next(1000) / 1000f * 0.2f + 0.01f;
                BoundingSphere newitem = new BoundingSphere(new Vector3(x , y , z), 0.1f * multiplier);

                if (CheckCollisionOnLoad(newitem) == CollisionType.None)
                    itemList.Add(newitem);
            }
        }

        private void AddTrees()
        {
            int cityWidth = cityFloorPlan.GetLength(0);
            int cityLength = cityFloorPlan.GetLength(1);

            Random random = new Random();

            while (treeList.Count < maxTrees)
            {
                int x = random.Next(cityWidth);
                int z = -random.Next(cityLength);
                float y = 0f;
                //float radius = 0.001f;

                BoundingSphere newTree = new BoundingSphere(new Vector3(x, y, z) * multiplier, 0.1f * multiplier);

                //if (CheckCollision(newTree, true) == CollisionType.None)
                treeList.Add(newTree);
            }
        }

        private CollisionType CheckCollisionOnLoad(BoundingSphere sphere)
        {
            if (completeWorldBox.Contains(sphere) != ContainmentType.Contains)
                return CollisionType.Boundary;
            for (int i = 0; i < buildingBoundingBoxes.Length; i++)
                if (buildingBoundingBoxes[i].Contains(sphere) != ContainmentType.Disjoint)
                    return CollisionType.Building;
            return CollisionType.None;
        }

        private Texture2D getCamFeed(Texture2D currentFeed)
        {
            //for webcam display

            Texture2D tex;// = new Texture2D(GraphicsDevice, 1, 1);
            System.Drawing.Image bmp = (System.Drawing.Image) new System.Drawing.Bitmap(1, 1);
            //System.Drawing.Image prevbmp = (System.Drawing.Image)new System.Drawing.Bitmap(1, 1);
            try
            {
                bmp = (System.Drawing.Image)motionDetector.GetFrame().Clone();
                //Texture2D 
            }
            catch (Exception e) 
            { }

            MemoryStream s = new MemoryStream();
            tex = new Texture2D(device, bmp.Width, bmp.Height, 1, TextureUsage.None, SurfaceFormat.Color);
            //bmp.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipX);
            bmp.Save(s, System.Drawing.Imaging.ImageFormat.Png);
            //prevbmp = bmp;
            s.Seek(0, SeekOrigin.Begin);
            tex = Texture2D.FromFile(device, s);

            if (tex.Width != 320)
            
                return currentFeed;
            
            else
                return tex;
        }

            /*
            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
            int bufferSize = data.Height * data.Stride;
            //create data buffer 
            byte[] bytes = new byte[bufferSize];    
            // copy bitmap data into buffer
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            // copy our buffer to the texture
            if (bytes.Length > 100)
            {
                tex.SetData(bytes,0,bytes.Length,SetDataOptions.None);
            }
            // unlock the bitmap data
            bitmap.UnlockBits(data);
                     * */

            //Texture2D myTex = new Texture2D(graphics.GraphicsDevice, bmp.Width, bmp.Height, 1, TextureUsage.None, SurfaceFormat.Color);
            //myTex.

        

#endregion

        #region drawing

        protected override void Draw(GameTime gameTime)
        {
            float time = (float)gameTime.TotalGameTime.TotalMilliseconds / 100.0f;
            
            if (currentState.Equals(GameState.Help))
            {
                device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Red, 1.0f, 0);
                DrawHelp();
            }

            if (currentState.Equals(GameState.Menu))
            {
                device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Red, 1.0f, 0);
                DrawMenu();
            }
            else if (currentState.Equals(GameState.About))
            {
                device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Red, 1.0f, 0);
                DrawAboutMenu();
            }

            else if (currentState.Equals(GameState.Options))
            {
                device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Red, 1.0f, 0);
                DrawOptionsMenu();
                //DrawCamInGame();
            }
            else if (currentState.Equals(GameState.InGame))
            {
                //UpdateLight();
                DrawRefractionMap();
                DrawReflectionMap();
                GeneratePerlinNoise(time);
                device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1.0f, 0);

                DrawSkyDome(viewMatrix);
                DrawCity();
                //DrawTexturedModel(shipModel, planeTextures);
                DrawShipModel(viewMatrix);
                //DrawTextModel(viewMatrix);
                Drawitems();
                DrawTrees();
                //DrawCar();
                DrawTerrain(viewMatrix);
                DrawWater(time);
                if (consoleData) DrawOverlayText(gameTime);
                if (drawCamFeed) DrawCamInGame();
                if (showWarning) DrawWarning(gameTime);
                if (showTooHigh) DrawHighWarning(gameTime);
                if (boundryWarning) DrawBoundryWarning(gameTime);
                UpdateFire();
            }
            base.Draw(gameTime);
        }

        private void DrawMenu()
        {
            Rectangle screenRectangle = new Rectangle(0, 0, screenWidth, screenHeight);
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);
            spriteBatch.Draw(mainMenuBG, screenRectangle, Color.White);
            spriteBatch.End();
            DrawMenuShipModel();
            /*spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);
            string text = "Press Enter";
            spriteBatch.DrawString(menuFont, text, new Vector2(screenWidth / 2 - 50, screenHeight - 100), Color.Blue);
            spriteBatch.DrawString(menuFont, text, new Vector2(screenWidth / 2 - 52, screenHeight  - 102), Color.White);
            spriteBatch.End();*/
        }

        private void DrawCamInGame()
        {
            camFeed = getCamFeed(camFeed);
            Rectangle webcamRectangle = new Rectangle(screenWidth - 320, 0, camFeed.Width, camFeed.Height);
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);
            spriteBatch.Draw(camFeed, webcamRectangle, Color.White);
            spriteBatch.End();
  
        }

        private void DrawAboutMenu()
        {
            Rectangle screenRectangle = new Rectangle(0, 0, screenWidth, screenHeight);
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);
            spriteBatch.Draw(aboutMenuBG, screenRectangle, Color.White);
            spriteBatch.End();
        }

        private void DrawOptionsMenu()
        {
            Rectangle screenRectangle = new Rectangle(0, 0, screenWidth, screenHeight);
            camFeed = getCamFeed(camFeed);
            Rectangle webcamRectangle = new Rectangle(screenWidth - camFeed.Width-40, screenHeight-camFeed.Height-200, camFeed.Width, camFeed.Height);
            //Rectangle preRect = new Rectangle(webcamRectangle.Left-5, webcamRectangle.Top-5, camPreviewBorder.Width+10, camPreviewBorder.Height+10);
            Rectangle selectedOptionRect = new Rectangle(0, 0, 0, 0);
            Rectangle colRect = new Rectangle(0, 0, 100, 100);
            switch (currentOption)
            {
                case SelectedOption.Controller:
                    selectedOptionRect = new Rectangle((int)optConPos.X, (int)optConPos.Y, screenWidth / 3, 30);
                    break;
                case SelectedOption.Camera:
                    selectedOptionRect = new Rectangle((int)optCamPos.X, (int)optCamPos.Y, screenWidth / 3, 30);
                    break;
                case SelectedOption.RMx:
                    selectedOptionRect = new Rectangle((int)optMaxColRPos.X, (int)optMaxColRPos.Y, 60, 30);
                    break;
                case SelectedOption.GMx:
                    selectedOptionRect = new Rectangle((int)optMaxColGPos.X, (int)optMaxColGPos.Y, 60, 30);
                    break;
                case SelectedOption.BMx:
                    selectedOptionRect = new Rectangle((int)optMaxColBPos.X, (int)optMaxColBPos.Y, 60, 30);
                    break;
                case SelectedOption.RMn:
                    selectedOptionRect = new Rectangle((int)optMinColRPos.X, (int)optMinColRPos.Y, 60, 30);
                    break;
                case SelectedOption.GMn:
                    selectedOptionRect = new Rectangle((int)optMinColGPos.X, (int)optMinColGPos.Y, 60, 30);
                    break;
                case SelectedOption.BMn:
                    selectedOptionRect = new Rectangle((int)optMinColBPos.X, (int)optMinColBPos.Y, 60, 30);
                    break;
            }
            
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);
            spriteBatch.Draw(optionsMenuBG, screenRectangle, Color.White);
            spriteBatch.Draw(optSelectedBack, selectedOptionRect, Color.White);

            //string text = "Preview:";
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(webcamRectangle.Left, webcamRectangle.Top-20), Color.Blue);
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(webcamRectangle.Left+1, webcamRectangle.Top - 20+1), Color.White);

            string text = currentInput.ToString();
            //spriteBatch.DrawString(optionsMenuFont, text, optConPos, Color.Blue);
            spriteBatch.DrawString(optionsMenuFont, text, optConPos, Color.White);

            text = currentCameraName;
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(40, 140), Color.Blue);
            spriteBatch.DrawString(optionsMenuFont, text, optCamPos, Color.White);

            //text = "Color Filtering Options ";
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(40, 200), Color.Blue);
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(41, 201), Color.White);

            //text = "Max Color ";
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(40, 230), Color.Blue);
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(41, 231), Color.White);
            text =motionDetector.getRMax().ToString();
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(40, 260), Color.Blue);
            spriteBatch.DrawString(optionsMenuFont, text, optMaxColRPos, Color.White);
            text = motionDetector.getGMax().ToString();
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(40, 290), Color.Blue);
            spriteBatch.DrawString(optionsMenuFont, text, optMaxColGPos, Color.White);
            text = motionDetector.getBMax().ToString();
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(40, 320), Color.Blue);
            spriteBatch.DrawString(optionsMenuFont, text, optMaxColBPos, Color.White);

            //text = "Min Color ";
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(40, 380), Color.Blue);
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(41, 381), Color.White);
            text = motionDetector.getRMin().ToString();
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(40, 410), Color.Blue);
            spriteBatch.DrawString(optionsMenuFont, text, optMinColRPos, Color.White);
            text = motionDetector.getGMin().ToString();
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(40, 440), Color.Blue);
            spriteBatch.DrawString(optionsMenuFont, text, optMinColGPos, Color.White);
            text = motionDetector.getBMin().ToString();
            //spriteBatch.DrawString(optionsMenuFont, text, new Vector2(40, 470), Color.Blue);
            spriteBatch.DrawString(optionsMenuFont, text, optMinColBPos, Color.White);
            //spriteBatch.Draw(camPreviewBorder, preRect, Color.White);
            spriteBatch.Draw(camFeed, webcamRectangle, Color.White);
            //Color a = new Color(new Vector3(motionDetector.getRMax(), motionDetector.getGMax(), motionDetector.getBMax()));
            //minCol = new Color(new Vector3(255, 0, 0));
            //minCol = new Color(motionDetector.getRMax(), motionDetector.getGMax(), motionDetector.getBMax());
            //spriteBatch.Draw(ColTex, colRect,minCol);

            spriteBatch.End();
        }

        private void DrawHelp()
        {
            Rectangle screenRectangle = new Rectangle(0, 0, screenWidth, screenHeight);
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);
            spriteBatch.Draw(helpBG, screenRectangle, Color.White);
            spriteBatch.End();
        }

        private void DrawCity()
        {
            customEffect.CurrentTechnique = customEffect.Techniques["Textured"];
            customEffect.Parameters["xWorld"].SetValue(Matrix.Identity);
            customEffect.Parameters["xView"].SetValue(viewMatrix);
            customEffect.Parameters["xProjection"].SetValue(projectionMatrix);
            customEffect.Parameters["xTexture"].SetValue(sceneryTexture);
            customEffect.Parameters["xEnableLighting"].SetValue(true);
            customEffect.Parameters["xLightDirection"].SetValue(lightDirection);
            customEffect.Parameters["xAmbient"].SetValue(0.4f);
            
            customEffect.Begin();
            foreach (EffectPass pass in customEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                device.VertexDeclaration = texturedVertexDeclaration;
                device.Vertices[0].SetSource(cityVertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, cityVertexBuffer.SizeInBytes / VertexPositionNormalTexture.SizeInBytes / 3);
                pass.End();
            }
            customEffect.End();
        }

    /*    private void DrawTexturedModel(Model thisModel, Texture2D[] modelTextures) //experimental, similar to drawskybox
        {
             device.SamplerStates[0].AddressU = TextureAddressMode.Clamp;
             device.SamplerStates[0].AddressV = TextureAddressMode.Clamp;

            device.RenderState.DepthBufferWriteEnable = false;
            Matrix[] modelTransforms = new Matrix[thisModel.Bones.Count];
            thisModel.CopyAbsoluteBoneTransformsTo(modelTransforms);
            int i = 0;
            foreach (ModelMesh mesh in thisModel.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    //Matrix worldMatrix = modelTransforms[mesh.ParentBone.Index] * Matrix.CreateTranslation(shipPosition);
                    Matrix worldMatrix = Matrix.CreateScale(0.0005f, 0.0005f, 0.0005f) * Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateFromQuaternion(shipRotation) * Matrix.CreateTranslation(shipPosition);

                    currentEffect.CurrentTechnique = currentEffect.Techniques["Textured"];
                    currentEffect.Parameters["xWorld"].SetValue(worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xTexture"].SetValue(modelTextures[i++]);
                }
                mesh.Draw();
            }
            device.RenderState.DepthBufferWriteEnable = true;
        }
      */  
        
        private void DrawShipModel(Matrix currentViewMatrix)
        {
            Matrix worldMatrix = Matrix.CreateScale(0.00003f * multiplier, 0.00003f * multiplier, 0.00003f * multiplier)  * Matrix.CreateFromQuaternion(shipRotation) * Matrix.CreateTranslation(shipPosition);// pos,rot,scl
            Matrix[] shipTransforms = new Matrix[shipModel.Bones.Count];
            shipModel.CopyAbsoluteBoneTransformsTo(shipTransforms);
            foreach (ModelMesh mesh in shipModel.Meshes)
            {
                foreach (BasicEffect currentEffect in mesh.Effects)
                {
                    /*
                    currentEffect.CurrentTechnique = currentEffect.Techniques["Colored"];
                    currentEffect.Parameters["xWorld"].SetValue(shipTransforms[mesh.ParentBone.Index] * worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xEnableLighting"].SetValue(true);
                    currentEffect.Parameters["xLightDirection"].SetValue(lightDirection);
                    currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                    */
                    currentEffect.World = worldMatrix;
                    currentEffect.View = currentViewMatrix;
                    currentEffect.Projection = projectionMatrix;
                    currentEffect.LightingEnabled = true;
                    currentEffect.DiffuseColor = new Vector3(0.6f);
                    currentEffect.AmbientLightColor = new Vector3(0.5f);
                    currentEffect.DirectionalLight0.Enabled = true;
                    currentEffect.DirectionalLight0.DiffuseColor = Vector3.One;
                    currentEffect.DirectionalLight0.Direction = lightDirection;
                }
                mesh.Draw();
            }
                        
                       //boundingSphereRenderer.Draw(shipSphere, new Color(0,255,0));
                       //boundingSphereRenderer.Draw(warningSphere, new Color(255, 0, 0));
            
            }
        /*
         private void DrawTextModel(Matrix currentViewMatrix)
        {
            Matrix worldMatrix = Matrix.CreateScale(0.003f * multiplier, 0.003f * multiplier, 0.003f * multiplier)   *Matrix.CreateRotationY(MathHelper.Pi/2)*  Matrix.CreateTranslation(new Vector3(35*multiplier, 1.8f*multiplier, -3.8f*multiplier));// pos,rot,scl
            Matrix[] textTransforms = new Matrix[textModel.Bones.Count];
            textModel.CopyAbsoluteBoneTransformsTo(textTransforms);
            foreach (ModelMesh mesh in textModel.Meshes)
            {
                foreach (BasicEffect currentEffect in mesh.Effects)
                {
                    currentEffect.World = worldMatrix;
                    currentEffect.View = currentViewMatrix;
                    currentEffect.Projection = projectionMatrix;
                    currentEffect.LightingEnabled = true;
                    currentEffect.DiffuseColor = new Vector3(0.6f);
                    currentEffect.AmbientLightColor = new Vector3(0.5f);
                    currentEffect.DirectionalLight0.Enabled = true;
                    currentEffect.DirectionalLight0.DiffuseColor = Vector3.One;
                    currentEffect.DirectionalLight0.Direction = lightDirection;
                }
                mesh.Draw();
            }
                        
            }
        */
        private void DrawMenuShipModel()
        {
            Matrix worldMatrix = Matrix.CreateScale(0.0001f, 0.0001f, 0.0001f) * Matrix.CreateRotationY(rotAngle) * Matrix.CreateRotationX(MathHelper.Pi / 6) * Matrix.CreateTranslation(new Vector3(0, 0, -2));// pos,rot,scl
            Matrix[] shipTransforms = new Matrix[shipModel.Bones.Count];
            shipModel.CopyAbsoluteBoneTransformsTo(shipTransforms);
            foreach (ModelMesh mesh in shipModel.Meshes)
            {
                foreach (BasicEffect currentEffect in mesh.Effects)
                {
                    /*
                    currentEffect.CurrentTechnique = currentEffect.Techniques["Colored"];
                    currentEffect.Parameters["xWorld"].SetValue(shipTransforms[mesh.ParentBone.Index] * worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xEnableLighting"].SetValue(true);
                    currentEffect.Parameters["xLightDirection"].SetValue(new Vector3(0, -1, 0));
                    currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                    */
                    currentEffect.World = worldMatrix;
                    currentEffect.View = viewMatrix;
                    currentEffect.Projection = projectionMatrix;

                    currentEffect.LightingEnabled = true;
                    currentEffect.DiffuseColor = new Vector3(1f);
                    currentEffect.AmbientLightColor = new Vector3(0.5f);

                    currentEffect.DirectionalLight0.Enabled = true;
                    currentEffect.DirectionalLight0.DiffuseColor = Vector3.One;
                    currentEffect.DirectionalLight0.Direction = lightDirection;
                }
                mesh.Draw();
            }
        }
        
        private void Drawitems()
        {
            for (int i = 0; i < itemList.Count; i++)
            {
                Matrix worldMatrix = Matrix.CreateRotationY(rotAngle) * Matrix.CreateScale(0.0005f * multiplier, 0.0005f * multiplier, 0.0005f * multiplier) * Matrix.CreateTranslation(itemList[i].Center);

            Matrix[] itemTransforms = new Matrix[itemModel.Bones.Count];
            itemModel.CopyAbsoluteBoneTransformsTo(itemTransforms);
            foreach (ModelMesh mesh in itemModel.Meshes)
            {
                foreach (BasicEffect currentEffect in mesh.Effects)
                {
                    currentEffect.EnableDefaultLighting();
                    currentEffect.World = itemTransforms[mesh.ParentBone.Index] * worldMatrix;
                    currentEffect.View = viewMatrix;
                    currentEffect.Projection = projectionMatrix;       
                }
                    mesh.Draw();
                }

            //boundingSphereRenderer.Draw(itemList[i], Color.Yellow);
            }
        }

        private void DrawTrees()
        {
            for (int i = 0; i < treeList.Count; i++)
            {
                Matrix worldMatrix = Matrix.CreateScale(0.005f * multiplier, 0.005f * multiplier, 0.005f * multiplier) * Matrix.CreateTranslation(treeList[i].Center);

                Matrix[] treeTransforms = new Matrix[treeModel.Bones.Count];
                treeModel.CopyAbsoluteBoneTransformsTo(treeTransforms);
                foreach (ModelMesh mesh in treeModel.Meshes)
                {
                    foreach (BasicEffect currentEffect in mesh.Effects)
                    {
                        currentEffect.EnableDefaultLighting();
                        currentEffect.World = treeTransforms[mesh.ParentBone.Index] * worldMatrix;
                        currentEffect.View = viewMatrix;
                        currentEffect.Projection = projectionMatrix;
                    }
                    mesh.Draw();
                }
            }
        }
        /*
        private void DrawCar()
        {
       
                Matrix worldMatrix = Matrix.CreateScale(0.05f * multiplier, 0.05f * multiplier, 0.05f * multiplier) * Matrix.CreateTranslation(new Vector3(20,1,-15));

                Matrix[] carTransforms = new Matrix[car.Bones.Count];
                car.CopyAbsoluteBoneTransformsTo(carTransforms);
                foreach (ModelMesh mesh in car.Meshes)
                {
                    foreach (BasicEffect currentEffect in mesh.Effects)
                    {
                        currentEffect.EnableDefaultLighting();
                        currentEffect.World = carTransforms[mesh.ParentBone.Index] * worldMatrix;
                        currentEffect.View = viewMatrix;
                        currentEffect.Projection = projectionMatrix;
                    }
                    mesh.Draw();
                }
            
        }
        */
        private void DrawOverlayText(GameTime gametime)
        {
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred,
                SaveStateMode.SaveState);

            string text = //"\nShipPosition: " +shipPosition.ToString() +
                //"\nPosition of HeightMap: " + p1.ToString() +
                        "Flight Simulator" +
                          "\nScore: " + score +
                          "\nLives: " + lives +
                          "\nInput: " + currentInput.ToString() +
                          "\nCamera: " + currentCameraName +
                       // "\nMotionX: " + motionDetector.getX() +
                       // "\nMotionY: " + motionDetector.getY() +
                        "\nGame time: " + gametime.TotalGameTime;
                        //"\nworld size: " + worldWidth + "," + worldHeight + ",tem" + tempz;
            spriteBatch.DrawString(gameFont, text, new Vector2(15, 11), Color.Black);
            spriteBatch.DrawString(gameFont, text, new Vector2(14, 10), Color.Green);

            spriteBatch.End();
        }

        private void DrawWarning(GameTime gametime)
        {
            if ((gametime.TotalGameTime.Seconds - warningTime) > 0.1) showWarning = false;
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);

            string text = "Warning";

            spriteBatch.DrawString(menuFont, text, new Vector2(screenWidth / 2 - 122, 88), Color.Black);
            spriteBatch.DrawString(menuFont, text, new Vector2(screenWidth / 2 - 120, 90), Color.Red);

            spriteBatch.End();
        }

        private void DrawBoundryWarning(GameTime gametime)
        {
            boundryWarning = false;
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);
            
            string text = "Wrong Direction";
            spriteBatch.DrawString(menuFont, text, new Vector2(screenWidth / 2 - 112, 28), Color.Black);
            spriteBatch.DrawString(menuFont, text, new Vector2(screenWidth / 2 - 110, 30), Color.Red);

            spriteBatch.End();
        }

        private void DrawHighWarning(GameTime gametime)
        {
            showTooHigh = false;
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);

            string text = "Too high";
            spriteBatch.DrawString(menuFont, text, new Vector2(screenWidth / 2 - 112, 128), Color.Black);
            spriteBatch.DrawString(menuFont, text, new Vector2(screenWidth / 2 - 110, 130), Color.Red);

            spriteBatch.End();
        }


        private void DrawTerrain(Matrix currentViewMatrix)
        {
            tEffect.CurrentTechnique = tEffect.Techniques["MultiTextured"];
            tEffect.Parameters["xTexture0"].SetValue(sandTexture);
            tEffect.Parameters["xTexture1"].SetValue(grassTexture);
            tEffect.Parameters["xTexture2"].SetValue(rockTexture);
            //tEffect.Parameters["xTexture3"].SetValue(snowTexture);

            Matrix worldMatrix = Matrix.Identity;
            tEffect.Parameters["xWorld"].SetValue(worldMatrix);
            tEffect.Parameters["xView"].SetValue(currentViewMatrix);
            tEffect.Parameters["xProjection"].SetValue(projectionMatrix);

            tEffect.Parameters["xEnableLighting"].SetValue(true);
            tEffect.Parameters["xAmbient"].SetValue(0.4f);
            tEffect.Parameters["xLightDirection"].SetValue(new Vector3(-0.5f, -1, -0.5f));

            tEffect.Begin();
            foreach (EffectPass pass in tEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                device.Vertices[0].SetSource(terrainVertexBuffer, 0, VertexMultitextured.SizeInBytes);
                device.Indices = terrainIndexBuffer;
                device.VertexDeclaration = terrainVertexDeclaration;

                int noVertices = terrainVertexBuffer.SizeInBytes / VertexMultitextured.SizeInBytes;
                int noTriangles = terrainIndexBuffer.SizeInBytes / sizeof(int) / 3;
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, noVertices, 0, noTriangles);

                pass.End();
            }
            tEffect.End();

                //boundingSphereRenderer.Draw(terrainSphere, new Color(255,255,0));

        }


        private void DrawSkyDome(Matrix currentViewMatrix)
        {
            device.RenderState.DepthBufferWriteEnable = false;
            //device.RenderState.CullMode = CullMode.None;

            Matrix[] modelTransforms = new Matrix[skyDome.Bones.Count];
            skyDome.CopyAbsoluteBoneTransformsTo(modelTransforms);

            Matrix wMatrix = Matrix.CreateTranslation(0, -0.3f, 0) * Matrix.CreateScale(100) * Matrix.CreateTranslation(shipPosition);
            foreach (ModelMesh mesh in skyDome.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    Matrix worldMatrix = modelTransforms[mesh.ParentBone.Index] * wMatrix;

                    currentEffect.CurrentTechnique = currentEffect.Techniques["SkyDome"];
                    currentEffect.Parameters["xWorld"].SetValue(worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(currentViewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xTexture"].SetValue(cloudMap);
                    currentEffect.Parameters["xEnableLighting"].SetValue(false);
                }
                mesh.Draw();
            }
            device.RenderState.DepthBufferWriteEnable = true;
        }

        private void DrawWater(float time)
        {
            tEffect.CurrentTechnique = tEffect.Techniques["Water"];
            Matrix worldMatrix = Matrix.Identity * Matrix.CreateTranslation(new Vector3(20*multiplier,0,0));
            tEffect.Parameters["xWorld"].SetValue(worldMatrix);
            tEffect.Parameters["xView"].SetValue(viewMatrix);
            tEffect.Parameters["xReflectionView"].SetValue(reflectionViewMatrix);
            tEffect.Parameters["xProjection"].SetValue(projectionMatrix);
            tEffect.Parameters["xReflectionMap"].SetValue(reflectionMap);
            tEffect.Parameters["xRefractionMap"].SetValue(refractionMap);
            tEffect.Parameters["xWaterBumpMap"].SetValue(waterBumpMap);
            tEffect.Parameters["xWaveLength"].SetValue(0.1f);
            tEffect.Parameters["xWaveHeight"].SetValue(0.3f);
            tEffect.Parameters["xCamPos"].SetValue(cameraPosition);
            tEffect.Parameters["xTime"].SetValue(time);
            tEffect.Parameters["xWindForce"].SetValue(0.002f);
            tEffect.Parameters["xWindDirection"].SetValue(windDirection);

            tEffect.Begin();
            foreach (EffectPass pass in tEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                device.Vertices[0].SetSource(waterVertexBuffer, 0, VertexPositionTexture.SizeInBytes);
                device.VertexDeclaration = waterVertexDeclaration;
                int noVertices = waterVertexBuffer.SizeInBytes / VertexPositionTexture.SizeInBytes;
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, noVertices / 3);

                pass.End();
            }
            tEffect.End();
        }

        private Plane CreatePlane(float height, Vector3 planeNormalDirection, Matrix currentViewMatrix, bool clipSide)
        {
            planeNormalDirection.Normalize();
            Vector4 planeCoeffs = new Vector4(planeNormalDirection, height);
            if (clipSide)
                planeCoeffs *= -1;

            Matrix worldViewProjection = currentViewMatrix * projectionMatrix;
            Matrix inverseWorldViewProjection = Matrix.Invert(worldViewProjection);
            inverseWorldViewProjection = Matrix.Transpose(inverseWorldViewProjection);

            planeCoeffs = Vector4.Transform(planeCoeffs, inverseWorldViewProjection);
            Plane finalPlane = new Plane(planeCoeffs);

            return finalPlane;
        }

        private void DrawRefractionMap()
        {
            Plane refractionPlane = CreatePlane(waterHeight + 1.5f, new Vector3(0, -1, 0), viewMatrix, false);
            device.ClipPlanes[0].Plane = refractionPlane;
            device.ClipPlanes[0].IsEnabled = true;
            device.SetRenderTarget(0, refractionRenderTarget);
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            DrawTerrain(viewMatrix);
            device.ClipPlanes[0].IsEnabled = false;

            device.SetRenderTarget(0, null);
            refractionMap = refractionRenderTarget.GetTexture();
        }

        private void DrawReflectionMap()
        {
            Plane reflectionPlane = CreatePlane(waterHeight - 0.5f, new Vector3(0, -1, 0), reflectionViewMatrix, true);
            device.ClipPlanes[0].Plane = reflectionPlane;
            device.ClipPlanes[0].IsEnabled = true;
            device.SetRenderTarget(0, reflectionRenderTarget);
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            DrawSkyDome(reflectionViewMatrix);
            DrawTerrain(reflectionViewMatrix);
            DrawShipModel(reflectionViewMatrix);
            //DrawBillboards(reflectionViewMatrix);
            device.ClipPlanes[0].IsEnabled = false;

            device.SetRenderTarget(0, null);
            reflectionMap = reflectionRenderTarget.GetTexture();
        }

        private void GeneratePerlinNoise(float time)
        {
            device.SetRenderTarget(0, cloudsRenderTarget);
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);

            tEffect.CurrentTechnique = tEffect.Techniques["PerlinNoise"];
            tEffect.Parameters["xTexture"].SetValue(cloudStaticMap);
            tEffect.Parameters["xOvercast"].SetValue(1.1f);
            tEffect.Parameters["xTime"].SetValue(time / 1000.0f);
            tEffect.Begin();
            foreach (EffectPass pass in tEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                device.VertexDeclaration = fullScreenVertexDeclaration;
                device.DrawUserPrimitives(PrimitiveType.TriangleStrip, fullScreenVertices, 0, 2);

                pass.End();
            }
            tEffect.End();

            device.SetRenderTarget(0, null);
            cloudMap = cloudsRenderTarget.GetTexture();
        }

        private VertexPositionTexture[] SetUpFullscreenVertices()
        {
            VertexPositionTexture[] vertices = new VertexPositionTexture[4];

            vertices[0] = new VertexPositionTexture(new Vector3(-1, 1, 0f), new Vector2(0, 1));
            vertices[1] = new VertexPositionTexture(new Vector3(1, 1, 0f), new Vector2(1, 1));
            vertices[2] = new VertexPositionTexture(new Vector3(-1, -1, 0f), new Vector2(0, 0));
            vertices[3] = new VertexPositionTexture(new Vector3(1, -1, 0f), new Vector2(1, 0));

            return vertices;
        }

        private Texture2D CreateStaticMap(int resolution)
        {
            Random rand = new Random();
            Color[] noisyColors = new Color[resolution * resolution];
            for (int x = 0; x < resolution; x++)
                for (int y = 0; y < resolution; y++)
                    noisyColors[x + y * resolution] = new Color(new Vector3((float)rand.Next(1000) / 1000.0f, 0, 0));

            Texture2D noiseImage = new Texture2D(device, resolution, resolution, 1, TextureUsage.None, SurfaceFormat.Color);
            noiseImage.SetData(noisyColors);
            return noiseImage;
        }

        #endregion

        #region logic

        private void UpdateFire()
        {
            const int fireParticlesPerFrame = 10;
            //Vector3 a = new Vector3(8, 1, -5);
            Vector3 burnPosition = shipPosition + Vector3.Transform(new Vector3(0, 0, 0.2f * multiplier), shipRotation);
            // Create a number of fire particles, randomly positioned around a circle.
            for (int i = 0; i < fireParticlesPerFrame; i++)
            {
                //fireParticles.AddParticle(RandomPointOnCircle(), Vector3.Zero);

                fireParticles.AddParticle(burnPosition, Vector3.Zero);
            }
            // Create one smoke particle per frame, too.
            //smokePlumeParticles.AddParticle(RandomPointOnCircle(), Vector3.Zero);

            
            //fireParticles
        }

        private Vector3 RandomPointOnCircle()
        {
            const float radius = 0.01f;
            const float height = 1f;

            double angle = random.NextDouble() * Math.PI * 2;
            //double angle2 = random.NextDouble() * Math.PI * 2;

            float x = (float)Math.Cos(angle);
            float y = (float)Math.Sin(angle);
            //float z = (float)Math.Sin(angle2);

            return new Vector3(8+ x * radius, y * radius + height, -5);
        }
        
        /*private void UpdateLight()
        {
            lightDirection = new Vector3(k, -2, 6);
            lightDirection.Normalize();
        }*/

        private void UpdateCamera()
        {
            cameraRotation = Quaternion.Lerp(cameraRotation, shipRotation, 0.1f ); //camera delay

            Vector3 campos = new Vector3(0 * multiplier,0.1f * multiplier, 0.5f * multiplier); //cameraPosition relative To ship
            //Vector3 noise = new Vector3((float)random.NextDouble() * 0.001f * multiplier, (float)random.NextDouble() * 0.001f * multiplier, (float)random.NextDouble() * 0.001f * multiplier);
            campos = Vector3.Transform(campos, Matrix.CreateFromQuaternion(cameraRotation));
            //campos += noise;
            cameraPosition= campos + shipPosition;

            Vector3 camup = new Vector3(0, 1, 0);
            camup = Vector3.Transform(camup, Matrix.CreateFromQuaternion(cameraRotation));

            viewMatrix = Matrix.CreateLookAt(cameraPosition, shipPosition, camup);
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.2f, 500.0f);

            Vector3 reflCameraPosition = cameraPosition;
            reflCameraPosition.Y = -cameraPosition.Y + waterHeight * 2;
            Vector3 reflTargetPos = shipPosition;
            reflTargetPos.Y = -shipPosition.Y + waterHeight * 2;

            Vector3 cameraRight = Vector3.Transform(new Vector3(1, 0, 0), cameraRotation);
            Vector3 invUpVector = Vector3.Cross(cameraRight, reflTargetPos - reflCameraPosition);

            reflectionViewMatrix = Matrix.CreateLookAt(reflCameraPosition, reflTargetPos, invUpVector);




            //smokePlumeParticles.SetCamera(viewMatrix, projectionMatrix);
            fireParticles.SetCamera(viewMatrix, projectionMatrix);

            lensFlare.View = viewMatrix;
            lensFlare.Projection = projectionMatrix;
        }

        private void ProcessCollisions(GameTime gametime)
        {
            shipSphere = new BoundingSphere(shipPosition, 0.04f * multiplier);
            warningSphere = new BoundingSphere(shipPosition, 0.3f * multiplier);
            //CollisionType wType = CheckCollisionForWarning(warningSphere, false);
            CollisionType cType = CheckCollision(shipSphere, warningSphere);

            //go an item?
            if (cType == CollisionType.Warning)
            {
                if (warningInstance.State != SoundState.Playing)
                {
                    warningInstance.Play();
                    
                }
                showWarning = true;
                warningTime = gametime.TotalGameTime.Seconds;
            }
            else if (cType == CollisionType.Item)
            {
                item.Play();
                score += 100;
            }
            else if (cType != CollisionType.None)
            {
                warningInstance.Stop();
                explode.Play();
                lives--;
                if (lives == 0)
                {
                    setupMenu();
                }
                else
                    NewGame();
            }
          /*  else if (wType == CollisionType.Boundary || wType == CollisionType.Building)
            {
                if (warningInstance.State != SoundState.Playing)
                    warningInstance.Play();
            }*/
        }

        private void NewGame()
        {
            Additems();
            AddTrees();
            started = false;
            shipPosition = new Vector3(45*multiplier, 1.2f*multiplier, -3.8f*multiplier);
            shipRotation = Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationY(MathHelper.Pi / 2));//Quaternion.Identity;
            //MediaPlayer.Stop();
            MediaPlayer.Play(Content.Load<Song>("Sounds/Music01"));
            //gameSpeed /= 1.1f;
            currentState = GameState.InGame;

        }

        private void ProcessInput()//(float amountOfTime)
        {
            float leftRightRot = 0;
            float upDownRot = 0;

            lastKeyboardState = currentKeyboardState;
            currentKeyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            // OPTIONS MENU ==============================================================================
            switch (currentState)
            {
                case GameState.Options:

                    if (currentKeyboardState.IsKeyDown(Keys.Enter) && lastKeyboardState.IsKeyUp(Keys.Enter))
                    {
                        select.Play();
                        currentState = GameState.Menu;
                    }

                    if (currentKeyboardState.IsKeyDown(Keys.Down) && lastKeyboardState.IsKeyUp(Keys.Down))
                    {
                        select.Play();
                        if (currentOption.Equals(null) )
                        {
                            currentOption = SelectedOption.Controller;
                        }
                        else
                        {
                            currentOption++;
                            if (currentOption > SelectedOption.BMx)
                            {
                                currentOption = SelectedOption.Controller;
                            }
                        }
                    }

                    if (currentKeyboardState.IsKeyDown(Keys.Up) && lastKeyboardState.IsKeyUp(Keys.Up))
                    {
                        select.Play();
                        if (currentOption.Equals(null))
                        {
                            currentOption = SelectedOption.BMx;
                        }
                        else
                        {
                            currentOption--;
                            if (currentOption < SelectedOption.Controller)
                            {
                                currentOption = SelectedOption.BMx;
                            }
                        }
                    }
                    
                    if (currentKeyboardState.IsKeyDown(Keys.Right) && lastKeyboardState.IsKeyUp(Keys.Right))
                    {
                        select.Play();
                        switch (currentOption)
                        {
                            case SelectedOption.Controller:
                            if (currentInput > InputDevice.Keyboard)
                                currentInput = 0;
                            else
                                currentInput++;
                                    break;
                            case SelectedOption.Camera:
                                if (camAvailable)
                                {
                                    currentCameraName = motionDetector.ConnectToFeed();
                                }
                                break;
                            case SelectedOption.RMx:
                                if (motionDetector.getRMax()<255) motionDetector.setRMax(motionDetector.getRMax() + 5);
                                break;
                            case SelectedOption.GMx:
                                if (motionDetector.getGMax() < 255) motionDetector.setGMax(motionDetector.getGMax() + 5);
                                break;
                            case SelectedOption.BMx:
                                if (motionDetector.getBMax() < 255) motionDetector.setBMax(motionDetector.getBMax() + 5);
                                break;
                            case SelectedOption.RMn:
                                if (motionDetector.getRMin() < 255) motionDetector.setRMin(motionDetector.getRMin() + 5);
                                break;
                            case SelectedOption.GMn:
                                if (motionDetector.getGMin() < 255) motionDetector.setGMin(motionDetector.getGMin() + 5);
                                break;
                            case SelectedOption.BMn:
                                if (motionDetector.getBMin() < 255) motionDetector.setBMin(motionDetector.getBMin() + 5);
                                break;
                        }
                        //maxCol = new Color(new Vector3((int)motionDetector.getRMax(), (int)motionDetector.getGMax(), (int)motionDetector.getBMax()));
                        //minCol = new Color(new Vector3(motionDetector.getRMin() * 1f, motionDetector.getGMin() * 1f, motionDetector.getBMin() * 1f));
                    }

                    if (currentKeyboardState.IsKeyDown(Keys.Left) && lastKeyboardState.IsKeyUp(Keys.Left))
                    {
                        select.Play();
                        switch (currentOption)
                        {
                            case SelectedOption.Controller:
                                if (currentInput < InputDevice.Keyboard)
                                    currentInput = InputDevice.Motion;
                                else
                                    currentInput--;
                                break;
                            case SelectedOption.Camera:
                                if (camAvailable)
                                {
                                    currentCameraName = motionDetector.ConnectToFeed();
                                }
                                break;
                            case SelectedOption.RMx:
                                if (motionDetector.getRMax() > 0) motionDetector.setRMax(motionDetector.getRMax() - 5);
                                break;
                            case SelectedOption.GMx:
                                if (motionDetector.getGMax() > 0) motionDetector.setGMax(motionDetector.getGMax() - 5);
                                break;
                            case SelectedOption.BMx:
                                if (motionDetector.getBMax() > 0) motionDetector.setBMax(motionDetector.getBMax() - 5);
                                break;
                            case SelectedOption.RMn:
                                if (motionDetector.getRMin() > 0) motionDetector.setRMin(motionDetector.getRMin() - 5);
                                break;
                            case SelectedOption.GMn:
                                if (motionDetector.getGMin() > 0) motionDetector.setGMin(motionDetector.getGMin() - 5);
                                break;
                            case SelectedOption.BMn:
                                if (motionDetector.getBMin() > 0) motionDetector.setBMin(motionDetector.getBMin() - 5);
                                break;
                        } 
                        //maxCol = new Color(new Vector3(motionDetector.getRMax(), motionDetector.getGMax(), motionDetector.getBMax()));
                        //minCol= new Color(new Vector3(motionDetector.getRMin(), motionDetector.getGMin(), motionDetector.getBMin()));
                    }

                    break;

                // MAIN MENU ==============================================================================
                case GameState.Menu:

                    if (currentKeyboardState.IsKeyDown(Keys.Enter) && lastKeyboardState.IsKeyUp(Keys.Enter))
                    {
                        //currentState = GameState.Loading;
                        select.Play();
                        NewGame();
                    }
                    if (currentKeyboardState.IsKeyDown(Keys.F1) && lastKeyboardState.IsKeyUp(Keys.F1))
                    {
                        select.Play();
                        currentState = GameState.Help;
                    }

                    if (currentKeyboardState.IsKeyDown(Keys.Space) && lastKeyboardState.IsKeyUp(Keys.Space))
                    {
                        select.Play();
                        currentState = GameState.About;
                    }

                    if (currentKeyboardState.IsKeyDown(Keys.LeftControl) && lastKeyboardState.IsKeyUp(Keys.LeftControl))
                    {
                        select.Play();
                        currentState = GameState.Options;
                    }

                    if (currentKeyboardState.IsKeyDown(Keys.Q) && lastKeyboardState.IsKeyUp(Keys.Q))
                    {
                        MediaPlayer.Stop();
                        select.Play();
                        motionDetector.Disconnect();
                        Exit();
                    }
                    break;


                // ABOUT MENU ==============================================================================
                case GameState.About:

                    if (currentKeyboardState.IsKeyDown(Keys.Enter) && lastKeyboardState.IsKeyUp(Keys.Enter))
                    {
                        select.Play();
                        currentState = GameState.Menu;
                    }
                    if (currentKeyboardState.IsKeyDown(Keys.Tab) && lastKeyboardState.IsKeyUp(Keys.Tab))
                    {
                        select.Play();
                        System.Diagnostics.Process.Start("http://www.facebook.com/ProjectITS");
                    }
                    break;

                // HELP MENU ==============================================================================
                case GameState.Help:

                    if (currentKeyboardState.IsKeyDown(Keys.Enter) && lastKeyboardState.IsKeyUp(Keys.Enter))
                    {
                        select.Play();
                        currentState = GameState.Menu;
                    }

                    break;

                // IN GAME ==============================================================================
                case GameState.InGame:

                    if (currentKeyboardState.IsKeyDown(Keys.Tab) && lastKeyboardState.IsKeyUp(Keys.Tab))
                    {
                        select.Play();
                        if (currentInput > InputDevice.Keyboard)
                            currentInput = 0;
                        else
                            currentInput++;
                        if (currentInput == InputDevice.Motion)
                        {
                            drawCamFeed = true;
                            camFeedTimer = new Timer(camTcb, null, 5000, 100);
                        }
                        else
                        {
                            drawCamFeed = false;
                        }

                    }
                    if (currentKeyboardState.IsKeyDown(Keys.Back) && lastKeyboardState.IsKeyUp(Keys.Back))
                    {
                        if (camAvailable)
                        {
                            select.Play();
                            currentCameraName = motionDetector.ConnectToFeed();
                            drawCamFeed = true;
                            //camFeedTimer.Dispose();
                            camFeedTimer = new Timer(camTcb, null, 5000, 100);
                        }
                    }


                    if (currentKeyboardState.IsKeyDown(Keys.OemTilde) && lastKeyboardState.IsKeyUp(Keys.OemTilde))
                    {
                        select.Play();
                        consoleData = !consoleData;
                    }

                    if (currentKeyboardState.IsKeyDown(Keys.PageUp) && lastKeyboardState.IsKeyUp(Keys.PageUp))
                    {
                        if (camAvailable)
                        {
                            select.Play();
                            drawCamFeed = true;
                            camFeedTimer.Dispose();
                            camFeedTimer = new Timer(camTcb, null, 5000, 100);
                        }
                    }

                    if (currentKeyboardState.IsKeyDown(Keys.Q) && lastKeyboardState.IsKeyUp(Keys.Q))
                    {
                        setupMenu();
                    }
                    if (started)
                    {
                        switch (currentInput)
                        {
                            case InputDevice.Mouse:
                                rotateSpd = (screenWidth / 2 - mouseState.X) * 0.00005f;
                                leftRightRot = -rotateSpd * gameSpeed;
                                upDownSpd = (screenHeight / 2 - mouseState.Y) * 0.00005f;
                                upDownRot = upDownSpd * gameSpeed;
                                break;
                            case InputDevice.Keyboard:
                                float turningSpeed = 0.02f;
                                if (currentKeyboardState.IsKeyDown(Keys.Right))
                                    leftRightRot += turningSpeed * gameSpeed;
                                if (currentKeyboardState.IsKeyDown(Keys.Left))
                                    leftRightRot -= turningSpeed * gameSpeed;
                                if (currentKeyboardState.IsKeyDown(Keys.Down))
                                    upDownRot += turningSpeed * gameSpeed;
                                if (currentKeyboardState.IsKeyDown(Keys.Up))
                                    upDownRot -= turningSpeed * gameSpeed;
                                break;
                            case InputDevice.Motion:
                                rotateSpd = (screenWidth / 2 - motionDetector.getX()) * 0.00005f;
                                leftRightRot = -rotateSpd * gameSpeed;
                                upDownSpd = (screenHeight / 2 - motionDetector.getY()) * 0.00005f;
                                upDownRot = upDownSpd * gameSpeed;
                                break;
                        }
                        if (currentKeyboardState.IsKeyDown(Keys.Space) && lastKeyboardState.IsKeyUp(Keys.Space))
                        {
                            gameSpeed = 0.1f;
                            timeSInstance.Play();
                            MediaPlayer.Pause();

                        }
                        if (currentKeyboardState.IsKeyUp(Keys.Space) && lastKeyboardState.IsKeyDown(Keys.Space))
                        {
                            gameSpeed = 1f;
                            timeSInstance.Stop();
                            timeE.Play();
                            MediaPlayer.Resume();
                        }

                        if (mouseState.LeftButton == ButtonState.Pressed)
                        {
                            shoot.Play();
                        }


                        Quaternion additionalRot = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, -1), leftRightRot) * Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), upDownRot);
                        shipRotation *= additionalRot;
                    }
                    break;
            }

        }

        private void MoveForward(ref Vector3 position, Quaternion rotationQuat, float speed)
        {
            Vector3 addVector = Vector3.Transform(new Vector3(0, 0, -1), rotationQuat);
            if ((position + addVector * speed).X > worldWidth-5)
            {
                boundryWarning = true;
                addVector.X = 0;
            }
            else if ((position + addVector * speed).X < 0)
            {
                boundryWarning = true;
                addVector.X = 0;
            }
            if ((position + addVector * speed).Z< -worldHeight)
            {
                boundryWarning = true;
                addVector.Z = 0;
            }
             if ((position + addVector * speed).Z > -2)
            {
                boundryWarning = true;
                addVector.Z = 0;
            }

             if ((position + addVector * speed).Y > 35)
             {
                 showTooHigh = true;
                 addVector.Y = 0;
             }
             tempz = (position + addVector * speed).Z;
            position += addVector * speed;// *(float)gametime.TotalGameTime.TotalSeconds / 1000;
        }

        private CollisionType CheckCollision(BoundingSphere shipSphere, BoundingSphere warningSphere) // collision improve karanna ona
        {


            if (shipPosition.X > terrainPosition.X)
            {
            positionOnHeightMap = new Vector3((shipPosition.X - terrainPosition.X), 0, shipPosition.Z);
                int left = (int)positionOnHeightMap.X;
                int top = (int)-positionOnHeightMap.Z;
                float xNormalized = (positionOnHeightMap.X % 1) / 1; // here terrainscale=1
                float zNormalized = (-positionOnHeightMap.Z % 1) / 1;

                float topHeight = MathHelper.Lerp(heightData[left, top], heightData[left + 1, top], xNormalized);
                float bottomHeight = MathHelper.Lerp(heightData[left, top + 1], heightData[left + 1, top + 1], xNormalized);
                float positionheight = MathHelper.Lerp(topHeight, bottomHeight, zNormalized);

                p1 = new Vector3(shipPosition.X, positionheight, shipPosition.Z);
                terrainSphere= new BoundingSphere(p1,0.01f);
                if (terrainSphere.Contains(shipSphere) != ContainmentType.Disjoint)
                {
                    return CollisionType.Terrain;
                }
                else if (terrainSphere.Contains(warningSphere) != ContainmentType.Disjoint)
                {
                    return CollisionType.Warning;
                }
            }

            for (int i = 0; i < buildingBoundingBoxes.Length; i++)
            {
                if (buildingBoundingBoxes[i].Contains(shipSphere) != ContainmentType.Disjoint)
                    return CollisionType.Building;
                if (buildingBoundingBoxes[i].Contains(warningSphere) != ContainmentType.Disjoint)
                    return CollisionType.Warning;
            }
            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i].Contains(shipSphere) != ContainmentType.Disjoint)
                {
                    itemList.RemoveAt(i);
                    i--;
                    //AddTargets();

                    return CollisionType.Item;
                }
            }
            
            if (completeWorldBox.Contains(shipSphere) != ContainmentType.Contains)
            {
                return CollisionType.Boundary;
            }
            else if (completeWorldBox.Contains(warningSphere) != ContainmentType.Contains)
            {
                return CollisionType.Warning;
            }
            
            return CollisionType.None;
        }



        /*
        private CollisionType CheckCollisionForWarning(BoundingSphere sphere)
        {
            for (int i = 0; i < buildingBoundingBoxes.Length; i++)
                if (buildingBoundingBoxes[i].Contains(sphere) != ContainmentType.Disjoint)
                    return CollisionType.Building;
            
            if (completeWorldBox.Contains(sphere) != ContainmentType.Contains)
                return CollisionType.Boundary;

            return CollisionType.None;
        }*/

        protected override void Update(GameTime gameTime)
        {
            //float timeDifference = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
            ProcessInput();//(timeDifference);
            switch (currentState)
            {
                case GameState.InGame:
                    UpdateCamera();
                    if (started)
                    {

                        moveSpeed = gameSpeed / 50.0f * multiplier;
                        MoveForward(ref shipPosition, shipRotation, moveSpeed);
                        ProcessCollisions(gameTime);
                    }
                   else
                    {
                        moveSpeed = 0;
                        if (tempTime < 0.000000000000000001f)
                        {
                            goInstance.Play();
                            gamestarttime = gameTime.TotalGameTime.Milliseconds;
                        }

                        tempTime += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                        if (tempTime > 5500)
                        {
                            Mouse.SetPosition(graphics.PreferredBackBufferWidth / 2, graphics.PreferredBackBufferHeight / 2);
                            tempTime = 0;
                            moveSpeed = gameSpeed * multiplier / 50.0f;
                            //goInstance.Stop();
                            started = true;
                            //DrawCam
                        }
                    }

                    rotAngle += gameSpeed / 15;

                    
                    break;


                case GameState.Menu:
                    rotAngle += gameSpeed / 30;
                    break;
            }
            if (rotAngle>float.MaxValue-1) rotAngle=0;
            base.Update(gameTime);
        }

        #endregion

        public void disableCamFeed(object obj)
        {
            camFeedTimer.Dispose();
            drawCamFeed = false;
        }
        public Matrix CameraProjection
        {
            get
            {
                return projectionMatrix;
            }
        }

        public Matrix CameraView
        {
            get
            {
                return viewMatrix;
            }
        }
    }
}