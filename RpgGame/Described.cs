﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Mogre;
using MogreNewt;
using System.Reflection;

namespace Gra
{
    public class Described : SelectableObject
    {
        public DescribedProfile Profile;
        public String Activatorr;

        Entity Entity;
        SceneNode Node;
        Body Body;

        public Container Container = new Container();

		public bool PerformAkt = false;

		Type Type;
		MethodInfo Method;
		object Instance;



        public Described(DescribedProfile profile)
        {
            Profile = profile.Clone();            

            Entity = Engine.Singleton.SceneManager.CreateEntity(Profile.MeshName);
            Node = Engine.Singleton.SceneManager.RootSceneNode.CreateChildSceneNode();
            Node.AttachObject(Entity);

            Vector3 scaledSize = Entity.BoundingBox.Size * Profile.BodyScaleFactor;

            IsContainer = profile.IsContainer;

            if (IsContainer)
            {
                Container = new Container();

                if (profile.PrizeID != null)
                {
                    Prize p1 = PrizeManager.P[profile.PrizeID].prize_Clone();
                    Container.Contains = new List<DescribedProfile>(p1.ItemsList);//PrizeManager.P[profile.PrizeID].ItemsList;
                    Container.Gold = p1.AmountGold;//PrizeManager.P[profile.PrizeID].AmountGold;
                    Container.MaxItems = p1.AmountExp;//PrizeManager.P[profile.PrizeID].AmountExp;
                }
            }

			if (Profile.Mass != 0)
			{
				ConvexCollision coll = new MogreNewt.CollisionPrimitives.ConvexHull(Engine.Singleton.NewtonWorld,
					Node,
					Quaternion.IDENTITY,
					0.01f,
					Engine.Singleton.GetUniqueBodyId());

				Vector3 inertia = new Vector3(1, 1, 1), offset;
				coll.CalculateInertialMatrix(out inertia, out offset);


				Body = new Body(Engine.Singleton.NewtonWorld, coll, true);
				coll.Dispose();
				Body.AttachNode(Node);
				Body.SetMassMatrix(Profile.Mass, Profile.Mass * inertia);
			}
			else
			{
				//Collision coll = new MogreNewt.CollisionPrimitives.TreeCollision(Engine.Singleton.NewtonWorld, Node, true, Engine.Singleton.GetUniqueBodyId());

				Collision coll = new MogreNewt.CollisionPrimitives.TreeCollision(Engine.Singleton.NewtonWorld, Entity, true, Engine.Singleton.GetUniqueBodyId());

				//Body = new Body(Engine.Singleton.NewtonWorld, coll, true);
				Body = new Body(Engine.Singleton.NewtonWorld, coll, false);
				coll.Dispose();
				Body.AttachNode(Node);

			}
            Body.UserData = this;
            Body.MaterialGroupID = Engine.Singleton.MaterialManager.DescribedMaterialID;

            
        }

        public void PrzypiszMetode()
        {
            if (Activatorr != "" && Activatorr != null)
            {
                Type = Type.GetType("Gra.Activators");
                Instance = Activator.CreateInstance(Type);
                Method = Type.GetMethod(Activatorr);
            }
        }

        public bool IsPickable
        {
            get { return Profile.IsPickable; }
        }
        

        public void TurnTo(Vector3 point)
        {
            Orientation = Vector3.UNIT_Z.GetRotationTo((point - Position) * new Vector3(1, 0, 1));
        }

        void BodyTransformCallback(Body sender, Quaternion orientation,
            Vector3 position, int threadIndex)
        {
            Node.Position = position;
            Node.Orientation = Orientation;
        }

        public override void Update()
        {
			if (PerformAkt && Activatorr != "" && Activatorr != null)
			{
				Method.Invoke(Instance, null);
				PerformAkt = false;
			}

        }
        public override Vector3 Position
        {
            get { return Body.Position; }
            set { Body.SetPositionOrientation(value, Orientation); }
        }
        public override Quaternion Orientation
        {
            get { return Body.Orientation; }
            set { Body.SetPositionOrientation(Body.Position, value); }
        }
        public override string DisplayName
        {
            get { return Profile.DisplayName; }
            set { Profile.DisplayName = value; }
        }
        public override string Description
        {
            get { return Profile.Description; }
            set { Profile.Description = value; }
        }
        public override Vector3 DisplayNameOffset
        {
            get { return Profile.DisplayNameOffset; }
            set { Profile.DisplayNameOffset = value; }
        }

        public override void Destroy()
        {
            Node.DetachAllObjects();
            Engine.Singleton.SceneManager.DestroySceneNode(Node);
            Engine.Singleton.SceneManager.DestroyEntity(Entity);
            Body.Dispose();
            Body = null;

            base.Destroy();
        }
    }
}
