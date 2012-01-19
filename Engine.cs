﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;
using MogreNewt;
using System.IO;
using System.Xml;

namespace Gra
{
    sealed class Engine
    {
        public Root Root;
        public RenderWindow RenderWindow;
        public SceneManager SceneManager;
        public Camera Camera;
        public Viewport Viewport;
        public MOIS.Keyboard Keyboard;
        public MOIS.Mouse Mouse;
        public MOIS.InputManager InputManager;
        public World NewtonWorld;
        public Debugger NewtonDebugger;

        public Level CurrentLevel;
        public GameCamera GameCamera;
        public ObjectManager ObjectManager;
        int BodyId;

        public const float FixedFPS = 60.0f;
        public const float FixedTimeStep = 1.0f / FixedFPS;
        float TimeAccumulator;
        public long LastTime;
        public float TimeStep;

        public MaterialManager MaterialManager;
        public TextLabeler Labeler;

        public HumanController HumanController;
        public TypedInput TypedInput;

        CharacterProfileManager CharacterProfileManager;
        public NPCManager NPCManager;
        public Items Items;
        public PrizeManager PrizeManager;
        public Quests Quests;

        public SoundManager SoundManager;

        public void Initialise()
        {
            Root = new Root();
            ConfigFile cf = new ConfigFile();
            cf.Load("Resources.cfg", "\t:=", true);

            ConfigFile.SectionIterator seci = cf.GetSectionIterator();

            while (seci.MoveNext())
            {
                ConfigFile.SettingsMultiMap settings = seci.Current;
                foreach (KeyValuePair<string, string> pair in settings)
                    ResourceGroupManager.Singleton.AddResourceLocation(pair.Value, pair.Key, seci.CurrentKey);
            }

            if (!Root.RestoreConfig())
                if (!Root.ShowConfigDialog())
                    return;

            RenderWindow = Root.Initialise(true, "Kolejny epicki erpeg");  // @@@@@@@@@@@@@@@ Nazwa okna gry.
            ResourceGroupManager.Singleton.InitialiseAllResourceGroups();
            
            SceneManager = Root.CreateSceneManager(SceneType.ST_GENERIC);
            Camera = SceneManager.CreateCamera("MainCamera");
            Viewport = RenderWindow.AddViewport(Camera);
            Camera.NearClipDistance = 0.1f;
            Camera.FarClipDistance = 1000.0f;
            Camera.AspectRatio = ((float)RenderWindow.Width / (float)RenderWindow.Height);

            MOIS.ParamList pl = new MOIS.ParamList();
            IntPtr windowHnd;
            RenderWindow.GetCustomAttribute("WINDOW", out windowHnd);
            pl.Insert("WINDOW", windowHnd.ToString());

            InputManager = MOIS.InputManager.CreateInputSystem(pl);

            Keyboard = (MOIS.Keyboard)InputManager.CreateInputObject(MOIS.Type.OISKeyboard, false);
            Mouse = (MOIS.Mouse)InputManager.CreateInputObject(MOIS.Type.OISMouse, false);

			NewtonWorld = new World();
            NewtonDebugger = new Debugger(NewtonWorld);
            NewtonDebugger.Init(SceneManager);

            GameCamera = new GameCamera();
            ObjectManager = new ObjectManager();

            MaterialManager = new MaterialManager();
            MaterialManager.Initialise();

            CharacterProfileManager = new CharacterProfileManager();
            Items = new Items();
            PrizeManager = new PrizeManager();  //////////////////// @@ Brand nju staff. Nawet trochę działa :Δ
            Quests = new Quests();
            NPCManager = new NPCManager();
            
            Labeler = new TextLabeler(5);
            HumanController = new HumanController();

            TypedInput = new TypedInput();


            SoundManager = new SoundManager();

           
        }

        public void Update()
        {
            long currentTime = Root.Timer.Milliseconds;
            TimeStep = (currentTime - LastTime) / 1000.0f;
            LastTime = currentTime;
            TimeAccumulator += TimeStep;
            TimeAccumulator = System.Math.Min(TimeAccumulator, FixedTimeStep * (FixedFPS / 15));

            Keyboard.Capture();
            Mouse.Capture();
            Root.RenderOneFrame();
            Labeler.Update();

            while (TimeAccumulator >= FixedTimeStep)
            {
                TypedInput.Update();
                
                NewtonWorld.Update(FixedTimeStep);
                HumanController.Update();
                ObjectManager.Update();
                GameCamera.Update();
                TimeAccumulator -= FixedTimeStep;

                        //// mjuzik status i ogarnięcie żeby przełączało na następną piosenkę z plejlisty po zakończeniu poprzedniej


            }
            WindowEventUtilities.MessagePump();
        }

        public void Load()
        {
            while (Engine.Singleton.ObjectManager.Objects.Count > 0)
                Engine.Singleton.ObjectManager.Destroy(Engine.Singleton.ObjectManager.Objects[0]);

            //*************************************************************//
            //                                                             //
            //                            ITEMY                            //
            //                                                             //
            //*************************************************************//

            if (System.IO.File.Exists("Media\\Maps\\" + CurrentLevel.Name + "\\Items.xml"))
            {
                XmlDocument File = new XmlDocument();
                File.Load("Media\\Maps\\" + CurrentLevel.Name + "\\Items.xml");

                XmlElement root = File.DocumentElement;
                XmlNodeList Items = root.SelectNodes("//items/item");

                foreach (XmlNode item in Items)
                {
                    if (item["DescribedProfile"].InnerText != "")
                    {
                        Described newDescribed = new Described(Gra.Items.I[item["DescribedProfile"].InnerText]);
                        Vector3 Position = new Vector3();

                        Quaternion Orientation = new Quaternion(float.Parse(item["Orientation_w"].InnerText), float.Parse(item["Orientation_x"].InnerText), float.Parse(item["Orientation_y"].InnerText), float.Parse(item["Orientation_z"].InnerText));
                        newDescribed.Orientation = Orientation;

                        Position.x = float.Parse(item["Position_x"].InnerText);
                        Position.y = float.Parse(item["Position_y"].InnerText);
                        Position.z = float.Parse(item["Position_z"].InnerText);
                        newDescribed.Position = Position;

                        Engine.Singleton.ObjectManager.Add(newDescribed);
                    }

                    if (item["ItemSword"].InnerText != "")
                    {
                        Described newDescribed = new Described(Gra.Items.I[item["ItemSword"].InnerText]);
                        Vector3 Position = new Vector3();

                        Quaternion Orientation = new Quaternion(float.Parse(item["Orientation_w"].InnerText), float.Parse(item["Orientation_x"].InnerText), float.Parse(item["Orientation_y"].InnerText), float.Parse(item["Orientation_z"].InnerText));
                        newDescribed.Orientation = Orientation;

                        Position.x = float.Parse(item["Position_x"].InnerText);
                        Position.y = float.Parse(item["Position_y"].InnerText);
                        Position.z = float.Parse(item["Position_z"].InnerText);
                        newDescribed.Position = Position;

                        Engine.Singleton.ObjectManager.Add(newDescribed);
                    }
                }
            }

            //*************************************************************//
            //                                                             //
            //                            NPCs                             //
            //                                                             //
            //*************************************************************//

            if (System.IO.File.Exists("Media\\Maps\\" + CurrentLevel.Name + "\\NPCs.xml"))
            {
                XmlDocument File = new XmlDocument();
                File.Load("Media\\Maps\\" + CurrentLevel.Name + "\\NPCs.xml");

                XmlElement root = File.DocumentElement;
                XmlNodeList Items = root.SelectNodes("//npcs//npc");

                foreach (XmlNode item in Items)
                {
                    Console.WriteLine(item["ProfileName"].InnerText);
                    Character newCharacter = new Character(CharacterProfileManager.C[item["ProfileName"].InnerText]);
                    Vector3 Position = new Vector3();

                    Quaternion Orientation = new Quaternion(float.Parse(item["Orientation_w"].InnerText), float.Parse(item["Orientation_x"].InnerText), float.Parse(item["Orientation_y"].InnerText), float.Parse(item["Orientation_z"].InnerText));
                    newCharacter.Orientation = Orientation;

                    Position.x = float.Parse(item["Position_x"].InnerText);
                    Position.y = float.Parse(item["Position_y"].InnerText);
                    Position.z = float.Parse(item["Position_z"].InnerText);
                    newCharacter.Position = Position;

                    Engine.Singleton.ObjectManager.Add(newCharacter);
                }
            }

            //*************************************************************//
            //                                                             //
            //                           ENEMIES                           //
            //                                                             //
            //*************************************************************//

            if (System.IO.File.Exists("Media\\Maps\\" + CurrentLevel.Name + "\\Enemies.xml"))
            {
                XmlDocument File = new XmlDocument();
                File.Load("Media\\Maps\\" + CurrentLevel.Name + "\\Enemies.xml");

                XmlElement root = File.DocumentElement;
                XmlNodeList Items = root.SelectNodes("//enemies//enemy");

                foreach (XmlNode item in Items)
                {
                    Enemy newCharacter = new Enemy(Gra.CharacterProfileManager.E[item["ProfileName"].InnerText], false, 10, 5);
                    Vector3 Position = new Vector3();

                    Quaternion Orientation = new Quaternion(float.Parse(item["Orientation_w"].InnerText), float.Parse(item["Orientation_x"].InnerText), float.Parse(item["Orientation_y"].InnerText), float.Parse(item["Orientation_z"].InnerText));
                    newCharacter.Orientation = Orientation;

                    Position.x = float.Parse(item["Position_x"].InnerText);
                    Position.y = float.Parse(item["Position_y"].InnerText);
                    Position.z = float.Parse(item["Position_z"].InnerText);
                    newCharacter.Position = Position;

                    newCharacter.Statistics = new Statistics(20, 0);

                    Engine.Singleton.ObjectManager.Add(newCharacter);
                }
            }
        }

        public bool IsKeyTyped(MOIS.KeyCode code)
        {
            return TypedInput.IsKeyTyped[(int)code];
        }

        public int GetUniqueBodyId()
        {
            return BodyId++;
        }

        static Engine instance;

        Engine()
        {
        }

        static Engine()
        {
            instance = new Engine();
        }

        public static Engine Singleton
        {
            get
            {
                return instance;
            }
        }

        public static double Distance(Vector3 v1, Vector3 v2)
        {
            return
            (
               System.Math.Sqrt
               (
                   (v1.x - v2.x) * (v1.x - v2.x) +
                   (v1.y - v2.y) * (v1.y - v2.y) +
                   (v1.z - v2.z) * (v1.z - v2.z)
               )
            );
        }
    }
}
