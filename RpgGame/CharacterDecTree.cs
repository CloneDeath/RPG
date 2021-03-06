﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;
using MogreNewt;

namespace Gra
{
    public class CharacterDecTree : DecTree.FirstSucc
    {
        DecTree.Node PickItemDownSeq;
        DecTree.Node GetSwordSeq;
        DecTree.Node HideSwordSeq;
		DecTree.Node AttackSeq;
        DecTree.Job TurnJob;

        public CharacterDecTree()
        {
            // #1
            DecTree.FirstSucc resetAnim = new DecTree.FirstSucc(
              new DecTree.Assert(ch => ch.AnimBlender.CurrentAnimSet == "PickItemDown"),
              new DecTree.Job(ch =>
              {
                  ch.AnimBlender.ResetAnimSet("PickItemDown");
                  ch.AnimBlender.SetAnimSet("PickItemDown");
                  ch.Velocity = Vector3.ZERO;
                  ch.TurnTo(ch.PickingTarget.Position);
                  return true;
              }));
            // #2
            Func<Character, bool> animBendDown = (
              ch => ch.AnimBlender.AnimSetPhase("PickItemDown") > 0.5f
              );
            DecTree.FirstSucc bendDown = new DecTree.FirstSucc(
              new DecTree.Assert(animBendDown),
              new DecTree.FirstFail(
                new DecTree.Assert(ch => ch.PickingTarget.Exists), // #2.1
                new DecTree.Job(animBendDown)
                )
              );
            // #3
            DecTree.FirstSucc pickItem = new DecTree.FirstSucc(
              new DecTree.Assert(ch => ch.PickingTarget == null || !ch.PickingTarget.Exists),
              new DecTree.Job(ch =>
              {
                  ch.Inventory.Add(ch.PickingTarget.Profile.Clone());
                  Engine.Singleton.ObjectManager.Destroy(ch.PickingTarget);
                  ch.PickingTarget = null;
                  return true; // #3.1
              }));
            // #4
            DecTree.Job bendUp = new DecTree.Job(
              ch => ch.AnimBlender.AnimSetPhase("PickItemDown") > 0.99f
              );

            PickItemDownSeq = new DecTree.FirstFail(
                resetAnim,
                bendDown,
                pickItem,
                bendUp);

            DecTree.Job cleanUp = new DecTree.Job(ch => { ch.PickItemOrder = false; ch.Contact = null; return true; });
            DecTree.FirstFail pickItemNode = new DecTree.FirstFail(
              new DecTree.Assert(ch => ch.PickItemOrder),
              new DecTree.FirstSucc(PickItemDownSeq, cleanUp), // #1
              cleanUp); // #2

            TurnJob = new DecTree.Job(ch =>
            {
                if (ch.TurnDelta != 0)
                {
                    Quaternion rotation = Quaternion.IDENTITY;
                    rotation.FromAngleAxis(new Degree(ch.TurnDelta), Vector3.UNIT_Y);
                    ch.Orientation *= rotation;
                    ch.TurnDelta = 0;
                }
                return true;
            });

            DecTree.FirstFail walkNode = new DecTree.FirstFail(
                new DecTree.Assert(ch => ch.MoveOrder),
                new DecTree.Job(ch =>
                {
                    ch.Velocity = ch.Orientation * Vector3.UNIT_Z * ch.Profile.WalkSpeed;


                    if (ch.RunOrder)
                        ch.AnimBlender.SetAnimSet("Run");
                    else if (ch.Sword != null)
                        if (ch.Sword.InUse)
                            ch.AnimBlender.SetAnimSet("WalkSword");
                        else
                            ch.AnimBlender.SetAnimSet("Walk");
                    else
                        ch.AnimBlender.SetAnimSet("Walk");

                    return true;
                }),
                TurnJob);

            DecTree.FirstFail walkNodeBack = new DecTree.FirstFail(
               new DecTree.Assert(ch => ch.MoveOrderBack),
               new DecTree.Job(ch =>
               {
                   ch.Velocity = -(ch.Orientation * Vector3.UNIT_Z * ch.Profile.WalkSpeed);
                   ch.AnimBlender.SetAnimSet("WalkBack");

                   return true;
               }),
               TurnJob);

            DecTree.FirstFail idleNode = new DecTree.FirstFail(
              new DecTree.Job(ch =>
              {
                  ch.TalkPerm = true;
                  ch.InventoryPerm = true;
                  ch.Velocity = Vector3.ZERO;
                  ch.AnimBlender.SetAnimSet("Idle");
                  return true;
              }),
              TurnJob);

            DecTree.FirstFail followPathNode = new DecTree.FirstFail(
                  new DecTree.Assert(ch => ch.FollowPathOrder),
                  new DecTree.Job(ch =>
                  {
                      if (Op2D.Dist(ch.Position, ch.WalkPath[0]) < 0.3f)
                      {
                          ch.WalkPath.RemoveAt(0);
                      }

					  if (ch.obejdz)
					  {
						  //ch.WalkPath.Insert(0, ch.Position + new Vector3(1, 0, 0));
						  ch.obejdz = false;
						  Engine.Singleton.CurrentLevel.navMesh.AStar(ch.Position + new Vector3(1, 0, 0), ch.Activities.Activities[ch.Activities.Index].v3);
						  if (Engine.Singleton.CurrentLevel.navMesh.TriPath.Count > 1)
						  {
							  Engine.Singleton.CurrentLevel.navMesh.GetPortals();
							  ch.WalkPath = Engine.Singleton.CurrentLevel.navMesh.Funnel();
						  }
					  }

                      if (ch.WalkPath.Count == 0)
                      {
                          ch.FollowPathOrder = false;

                          if (ch.Activities.InProgress
                                && ch.Activities.Activities[ch.Activities.Index].Type == ActivityType.WALK)
                              ch.Activities.EndActivity();

                          return true;
                      }
					  else if (ch.Contacts.Count > 0)
					  {
						  Engine.Singleton.CurrentLevel.navMesh.AStar(ch.Position, ch.Activities.Activities[ch.Activities.Index].v3);
						  if (Engine.Singleton.CurrentLevel.navMesh.TriPath.Count > 1)
						  {
							  Engine.Singleton.CurrentLevel.navMesh.GetPortals();
							  ch.WalkPath = Engine.Singleton.CurrentLevel.navMesh.Funnel();

							  ch.FollowPathOrder = true;
							  ch.Activities.InProgress = true;
						  }
					  }
                      else
                      {
                          ch.Orientation = Quaternion.Slerp(
                            0.2f,
                            ch.Orientation,
                            Vector3.UNIT_Z.GetRotationTo(
                              (Op2D.XZ * (ch.WalkPath[0] - ch.Position)).NormalisedCopy),
                            true
                          );
                          ch.Velocity = ch.Orientation * Vector3.UNIT_Z * ch.Profile.WalkSpeed;
                          ch.AnimBlender.SetAnimSet("Walk");
                      }
                      return false;
                  }
                ));

            // get sword

            DecTree.FirstSucc resetAnimGetSword = new DecTree.FirstSucc(
                new DecTree.Assert(ch => ch.AnimBlender.CurrentAnimSet == "GetSword"),
                new DecTree.Job(ch =>
                {
                    ch.Velocity = Vector3.ZERO;
                    ch.AnimBlender.ResetAnimSet("GetSword");
                    ch.AnimBlender.SetAnimSet("GetSword");

                    return true;
                }));

            Func<Character, bool> animGetSword1 = (ch => ch.AnimBlender.AnimSetPhase("GetSword") > 0.5f);
            DecTree.FirstSucc getSwordWait1 = new DecTree.FirstSucc(
                new DecTree.Assert(animGetSword1),
                new DecTree.FirstFail(
                    new DecTree.Assert(ch => ch.GetSwordOrder),
                    new DecTree.Job(animGetSword1)
                    )
                );

            Func<Character, bool> animGetSword2 = (ch => ch.AnimBlender.AnimSetPhase("GetSword") > 0.99f);
            DecTree.FirstSucc getSwordWait2 = new DecTree.FirstSucc(
                new DecTree.Assert(animGetSword2),
                new DecTree.FirstFail(
                    new DecTree.Assert(ch => ch.GetSwordOrder),
                    new DecTree.Job(animGetSword2)
                    )
                );

            DecTree.FirstSucc getSword = new DecTree.FirstSucc(
                new DecTree.Job(ch =>
                {
                    ItemSword sword = ch.Sword;
                    ch.UnequipSword();
                    ch.EquipSwordToSword(sword);

                    return true;
                }));

            GetSwordSeq = new DecTree.FirstFail(
                resetAnimGetSword,
                getSwordWait1,
                getSword,
                getSwordWait2
                );

            DecTree.Job cleanUpGetSword = new DecTree.Job(ch => { ch.GetSwordOrder = false; return true; });

            DecTree.FirstFail getSwordNode = new DecTree.FirstFail(
                new DecTree.Assert(ch => ch.GetSwordOrder),
                new DecTree.FirstSucc(GetSwordSeq, cleanUpGetSword),
                cleanUpGetSword);


            // hide sword

            DecTree.FirstSucc resetAnimHideSword = new DecTree.FirstSucc(
                new DecTree.Assert(ch => ch.AnimBlender.CurrentAnimSet == "HideSword"),
                new DecTree.Job(ch =>
                {
                    ch.Velocity = Vector3.ZERO;
                    ch.AnimBlender.ResetAnimSet("HideSword");
                    ch.AnimBlender.SetAnimSet("HideSword");
                    return true;
                }));

            Func<Character, bool> animHideSword1 = (ch => ch.AnimBlender.AnimSetPhase("HideSword") > 0.5f);
            DecTree.FirstSucc hideSwordWait1 = new DecTree.FirstSucc(
                new DecTree.Assert(animHideSword1),
                new DecTree.FirstFail(
                    new DecTree.Assert(ch => ch.HideSwordOrder),
                    new DecTree.Job(animHideSword1)
                    )
                );

            Func<Character, bool> animHideSword2 = (ch => ch.AnimBlender.AnimSetPhase("HideSword") > 0.99f);
            DecTree.FirstSucc hideSwordWait2 = new DecTree.FirstSucc(
                new DecTree.Assert(animHideSword2),
                new DecTree.FirstFail(
                    new DecTree.Assert(ch => ch.HideSwordOrder),
                    new DecTree.Job(animHideSword2)
                    )
                );

            DecTree.FirstSucc hideSword = new DecTree.FirstSucc(
                new DecTree.Job(ch =>
                {

                    ItemSword sword = ch.Sword;
                    ch.UnequipSword();
                    ch.EquipSwordToLongswordSheath(sword);


                    return true;
                }));

            HideSwordSeq = new DecTree.FirstFail(
                resetAnimHideSword,
                hideSwordWait1,
                hideSword,
                hideSwordWait2
                );

            DecTree.Job cleanUpHideSword = new DecTree.Job(ch => { ch.HideSwordOrder = false; return true; });

            DecTree.FirstFail hideSwordNode = new DecTree.FirstFail(
                new DecTree.Assert(ch => ch.HideSwordOrder),
                new DecTree.FirstSucc(HideSwordSeq, cleanUpHideSword),
                cleanUpHideSword);

			////////////////////////////////////////////////////////////////////////////////////////////////
			// go left
			DecTree.FirstFail go_left = new DecTree.FirstFail(
			   new DecTree.Assert(ch => ch.MoveLeftOrder),
			   new DecTree.Job(ch =>
			   {
				    Quaternion orient = Quaternion.IDENTITY;
				    orient.FromAngleAxis(new Degree(90), Vector3.UNIT_Y);
				    ch.Velocity = ch.Orientation * orient * Vector3.UNIT_Z * ch.Profile.WalkSpeed;
					ch.AnimBlender.SetAnimSet("Idle");  // ### ANIMACJA CHODZENIA!

				   return true;
			   }),
			   TurnJob);

			DecTree.FirstFail go_right = new DecTree.FirstFail(
			   new DecTree.Assert(ch => ch.MoveRightOrder),
			   new DecTree.Job(ch =>
			   {
				   Quaternion orient = Quaternion.IDENTITY;
				   orient.FromAngleAxis(new Degree(-90), Vector3.UNIT_Y);
				   ch.Velocity = ch.Orientation * orient * Vector3.UNIT_Z * ch.Profile.WalkSpeed;
				   ch.AnimBlender.SetAnimSet("Idle");  // ### ANIMACJA CHODZENIA!

				   return true;
			   }),
			   TurnJob);

			DecTree.FirstSucc resetAnimAttack = new DecTree.FirstSucc(
			  new DecTree.Assert(ch => ch.AnimBlender.CurrentAnimSet == "GetSword"),
			  new DecTree.Job(ch =>
			  {
				  ch.AnimBlender.ResetAnimSet("GetSword");
				  ch.AnimBlender.SetAnimSet("GetSword");
				  Engine.Singleton.SoundManager.PlaySound("Other/haa.mp3");
				  return true;
			  }));
			// #2
			Func<Character, bool> animAttack = (
			  ch => ch.AnimBlender.AnimSetPhase("GetSword") > 0.99f
			  );
			DecTree.FirstSucc AnimAttack = new DecTree.FirstSucc(
              new DecTree.Assert(animAttack),
              new DecTree.FirstFail(
                new DecTree.Job(animAttack)
                )
              );

			// #4

			AttackSeq = new DecTree.FirstFail(
				resetAnimAttack,
				AnimAttack);

            DecTree.Job cleanUpA = new DecTree.Job(ch => { ch.AttackOrder = false; Atakowanie(); return true; });

			DecTree.FirstFail AttackNode = new DecTree.FirstFail(
			  new DecTree.Assert(ch => ch.AttackOrder),
			  new DecTree.FirstSucc(AttackSeq, cleanUpA), // #1
			  cleanUpA);



            Children.Add(getSwordNode);
            Children.Add(hideSwordNode);
            Children.Add(walkNodeBack);
			Children.Add(go_left);
			Children.Add(go_right);
			Children.Add(AttackNode);

            Children.Add(pickItemNode);
            Children.Add(followPathNode);
            Children.Add(walkNode);
            Children.Add(idleNode);

        }

        public void Atakowanie()
        {
            bool Blok = false;

            for (int i = 0; i < Engine.Singleton.HumanController.Character.Statistics.Ataki; i++)
            {
                bool Kryt = false;
                bool BlokNieUdany = true;
                bool Unikniety = false;

                if (Engine.Singleton.HumanController.Character.FocusedEnemy != null)
                {
                    if (Engine.Singleton.Procenty(Engine.Singleton.HumanController.Character.Statistics.WalkaWrecz))
                    {
                        if (Engine.Singleton.Procenty(50))  // REAKCJA WROGA
                        {
                            if (!Blok)
                            {
                                if (Engine.Singleton.Procenty(50)) // BLOK!
                                {
                                    Blok = true;

                                    if (Engine.Singleton.Random.Next(101) <= Engine.Singleton.HumanController.Character.FocusedEnemy.Statistics.WalkaWrecz)
                                    {
                                        BlokNieUdany = false;
                                    }
                                }

                                else
                                {
                                    //    UNIK !!

                                    if (Engine.Singleton.Random.Next(101) <= Engine.Singleton.HumanController.Character.FocusedEnemy.Statistics.Zrecznosc)
                                        Unikniety = true;
                                }
                            }

                            else
                            {
                                //    UNIK !!

                                if (Engine.Singleton.Random.Next(101) <= Engine.Singleton.HumanController.Character.FocusedEnemy.Statistics.Zrecznosc)
                                    Unikniety = true;
                            }
                        }

                        if (BlokNieUdany && !Unikniety)
                        {
                            int Obrazenia = Engine.Singleton.Kostka(Engine.Singleton.HumanController.Character.Sword.IloscRzutow, Engine.Singleton.HumanController.Character.Sword.JakoscRzutow);

                            if (Engine.Singleton.Procenty(5))
                            {
                                Kryt = true;
                                Obrazenia *= 2;
                            }

                            Obrazenia = Obrazenia + Engine.Singleton.HumanController.Character.Statistics.Sila
                                - Engine.Singleton.HumanController.Character.FocusedEnemy.Statistics.Wytrzymalosc;

                            if (Obrazenia < 0)
                                Obrazenia = 0;

                            Engine.Singleton.HumanController.Character.FocusedEnemy.Statistics.aktualnaZywotnosc -= Obrazenia;

                            if (Kryt)
                                Engine.Singleton.HumanController.HUD.LogAdd("Trafiasz za " + Obrazenia.ToString() + " (KRYT!)", new ColourValue(0.7f, 0.4f, 0));
                            else
                                Engine.Singleton.HumanController.HUD.LogAdd("Trafiasz za " + Obrazenia.ToString(), ColourValue.White);
                        }
                    }

                    else
                        Engine.Singleton.HumanController.HUD.LogAdd("Nie trafiasz", new ColourValue(0.4f, 0.5f, 0.9f));
                }

            }
        }
    }
}
