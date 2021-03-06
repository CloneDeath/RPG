﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Gra
{
    public enum ActionType
    {
        MakeQuestFinished,
        MakeFirstFalse,
        MakeFirstTrue,
        MakeEdgeTrue,
        MakeEdgeFalse,
        GiveQuest,
        ActivateActivator,
        StartShop,
        GivePrizeNPC,
        GivePrizePlayer,
        RemovePrizePlayer
    };

    public class TalkNode
    {
      public List<TalkText> Text;
      public List<TalkReply> Replies;
      public event Action Actions;

      public String Quest;
      public String Activatorr;
      public List<TalkEdge> Edges;
      public Character WhoSays;
      public String DialogID;
      public Prize PrizeNPC;
      public Prize PrizePlayer;
      public Prize PrizePlayerRemove;

      Type Type;
      MethodInfo Method;
      object Instance;

	  public bool IsEnding = false;

      public TalkNode()
      {
        Text = new List<TalkText>();
        Replies = new List<TalkReply>();
        Edges = new List<TalkEdge>();
      }

      public void AddActions(List<ActionType> newActions)
      {
          foreach (ActionType action in newActions)
          {
              switch (action)
              {
                  case ActionType.MakeQuestFinished:
                  {
                      Actions += (() => MakeQuestFinished());
                      break;
                  }

                  case ActionType.ActivateActivator:
                  {
                      Actions += (() => ActivateActivator());
                      break;
                  }

                  case ActionType.GiveQuest:
                  {
                      Actions += (() => GiveQuest());
                      break;
                  }

                  case ActionType.MakeFirstFalse:
                  {
                      Actions += (() => MakeFirstFalse());
                      break;
                  }

                  case ActionType.MakeFirstTrue:
                  {
                      Actions += (() => MakeFirstTrue());
                      break;
                  }

                  case ActionType.MakeEdgeTrue:
                  {
                      Actions += (() => MakeEdgeTrue());
                      break;
                  }

                  case ActionType.MakeEdgeFalse:
                  {
                      Actions += (() => MakeEdgeFalse());
                      break;
                  }

                  case ActionType.StartShop:
                  {
                      Actions += (() => StartShop());
                      break;
                  }

                  case ActionType.GivePrizeNPC:
                  {
                      Actions += (() => GivePrizeNPC());
                      break;
                  }

                  case ActionType.GivePrizePlayer:
                  {
                      Actions += (() => GivePrizePlayer());
                      break;
                  }

                  case ActionType.RemovePrizePlayer:
                  {
                      Actions += (() => RemovePrizePlayer());
                      break;
                  }
              }
          }
      }

      public void MakeEdgeFalse()
      {
          foreach (TalkEdge e in Edges)
          {
              Conversations.D[DialogID].Edges[e.ID].Other = false;
          }
      }

      public void MakeEdgeTrue()
      {
          foreach (TalkEdge e in Edges)
              Conversations.D[DialogID].Edges[e.ID].Other = true;
      }

      public void MakeFirstFalse()
      {
          foreach (TalkEdge e in Edges)
          {
              Conversations.D[DialogID].Edges[e.ID].FirstTalk = false;
          }
      }

      public void MakeFirstTrue()
      {
          foreach (TalkEdge e in Edges)
              Conversations.D[DialogID].Edges[e.ID].FirstTalk = true;
      }

      public void GiveQuest()
      {
          Engine.Singleton.HumanController.Character.ActiveQuests.Add(Quests.Q[Quest]);
      }

      public void MakeQuestFinished()
      {
          foreach (Quest q in Engine.Singleton.HumanController.Character.ActiveQuests.Quests)
          {
              if (q == Quests.Q[Quest])
                  Engine.Singleton.HumanController.Character.ActiveQuests.MakeFinished(q);
          }
      }

      public void PrzypiszMetode()
      {
          if (Activatorr != null && Activatorr != "")
          {
              Type = Type.GetType("Gra.Activators");
              Instance = Activator.CreateInstance(Type);
              Method = Type.GetMethod(Activatorr);
          }

          else
          {
              Type = Type.GetType("Gra.Activators");
              Instance = Activator.CreateInstance(Type);
              Method = Type.GetMethod("Null");
          }
      }

      public void ActivateActivator()
      {
          if (Activatorr != null && Activatorr != "")
              Method.Invoke(Activatorr, null);
      }

      public void StartShop()
      {
          Engine.Singleton.HumanController.HUDShop.Shop = new Shop(WhoSays.Inventory, (int)WhoSays.Profile.Gold, WhoSays.Profile.DisplayName, WhoSays.Profile.MnoznikDlaShopa, WhoSays);
		  Engine.Singleton.HumanController.InitShop = true;
      }

      public void GivePrizeNPC()
      {
          foreach (DescribedProfile item in PrizeNPC.ItemsList)
              WhoSays.Inventory.Add(item.Clone());
          WhoSays.Profile.Gold += (ulong)PrizeNPC.AmountGold;
      }

      public void GivePrizePlayer()
      {
          foreach (DescribedProfile item in PrizePlayer.ItemsList)
              Engine.Singleton.HumanController.Character.Inventory.Add(item.Clone());
          Engine.Singleton.HumanController.Character.Profile.Gold += (ulong)PrizePlayer.AmountGold;
          Engine.Singleton.HumanController.Character.Profile.Exp += PrizePlayer.AmountExp;
      }

      public void RemovePrizePlayer()
      {
          List<DescribedProfile> Items2Remove = new List<DescribedProfile>();
          foreach (DescribedProfile prizeItem in PrizePlayerRemove.ItemsList)
          {
              foreach (DescribedProfile item in Engine.Singleton.HumanController.Character.Inventory)
              {
                  if (item.ProfileName == prizeItem.ProfileName)
                  {
                      Items2Remove.Add(item);
                      break;
                  }

              }
          }

          foreach (DescribedProfile item2remove in Items2Remove)
          {
              if (item2remove.IsEquipment && item2remove is ItemSword)
              {
                  Engine.Singleton.HumanController.Character.UnequipSword();
                  Engine.Singleton.HumanController.Character.Sword = null;
              }
              Engine.Singleton.HumanController.Character.Inventory.Remove(item2remove);
          }

          if (Engine.Singleton.HumanController.Character.Profile.Gold >= (ulong)PrizePlayerRemove.AmountGold)
              Engine.Singleton.HumanController.Character.Profile.Gold -= (ulong)PrizePlayerRemove.AmountGold;
          else
              Engine.Singleton.HumanController.Character.Profile.Gold = 0;
      }
 
      public void CallActions()
      {
        if (Actions!=null)
        {
          Actions();
		 
        }
      }
    }
}