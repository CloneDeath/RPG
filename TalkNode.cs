﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        StartShop
    };

    public class TalkNode
    {
      public List<TalkText> Text;
      public List<TalkReply> Replies;
      public event Action Actions;

      public String Quest;
      public String Activator;
      public TalkEdge Edge;
      public Character WhoSays;

	  public bool IsEnding = false;

      public TalkNode()
      {
        Text = new List<TalkText>();
        Replies = new List<TalkReply>();
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
              }
          }
      }

      public void MakeEdgeFalse()
      {
          Edge.Other = false;
      }

      public void MakeEdgeTrue()
      {
          Edge.Other = true;
      }

      public void MakeFirstFalse()
      {
          Edge.FirstTalk = false;
      }

      public void MakeFirstTrue()
      {
          Edge.FirstTalk = true;
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

      public void ActivateActivator()
      {

      }

      public void StartShop()
      {
          WhoSays.Inventory.Add(Items.I["iKufel"]);
          WhoSays.Profile.Gold = 30;
          Engine.Singleton.HumanController.HUDShop.Shop = new Shop(WhoSays.Inventory, (int)WhoSays.Profile.Gold, WhoSays.Profile.DisplayName, WhoSays.Profile.MnoznikDlaShopa, WhoSays);
		  
		  //Engine.Singleton.HumanController.SwitchState(HumanController.HumanControllerState.FREE);
		  Engine.Singleton.HumanController.InitShop = true;
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