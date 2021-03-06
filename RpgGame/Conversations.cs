﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;
using MogreNewt;
using System.Xml;
using System.IO;

namespace Gra
{
    class Conversations
    {
        public static Dictionary<String, Dialog> D;

        public Conversations()
        {
            D = new Dictionary<String, Dialog>();

            if (File.Exists("Media\\Others\\Dialogi.xml"))
            {
                XmlDocument File1 = new XmlDocument();
                File1.Load("Media\\Others\\Dialogi.xml");
                XmlElement root = File1.DocumentElement;
                XmlNodeList Items = root.SelectNodes("//Dialogs//Dialog");

                foreach (XmlNode item in Items)
                {
                    Dialog justDialog = new Dialog();

                    XmlNodeList TalkReactions = item["Reactions"].ChildNodes;

                    foreach (XmlNode tr in TalkReactions)
                    {
                        TalkReaction justReaction = new TalkReaction();
                        justDialog.Reactions.Add(tr["TalkReactionID"].InnerText, justReaction);
                    }

                    XmlNodeList TalkReplies = item["Replies"].ChildNodes;

                    foreach (XmlNode rep in TalkReplies)
                    {
                        TalkReply justReply = new TalkReply();
                        justReply.IsEnding = bool.Parse(rep["IsEnding"].InnerText);
						
						float dur = float.Parse(rep["Duration"].InnerText);
						justReply.Text.Add(new TalkText((rep["Text"].InnerText), dur, rep["Sound"].InnerText));


                        if (!justReply.IsEnding)
                            justReply.Reaction = justDialog.Reactions[rep["TalkReaction"].InnerText];

                        justDialog.Replies.Add(rep["TalkReplyID"].InnerText, justReply);
                    }

                    XmlNodeList TalkNodes = item["Nodes"].ChildNodes;

                    foreach (XmlNode tn in TalkNodes)
                    {
                        TalkNode justNode = new TalkNode();

						float dur = float.Parse(tn["Duration"].InnerText);
						justNode.Text.Add(new TalkText((tn["Text"].InnerText), dur, tn["Sound"].InnerText));

                        XmlNodeList RepliesInNode = tn["NodeReplies"].ChildNodes;

                        foreach (XmlNode rin in RepliesInNode)
                        {
                            justNode.Replies.Add(justDialog.Replies[rin["ReplyID"].InnerText]);
                        }

                        XmlNodeList ActionsInNode = tn["Actions"].ChildNodes;
                        List<ActionType> actionList = new List<ActionType>();

                        foreach (XmlNode ain in ActionsInNode)
                        {
                            actionList.Add((ActionType)int.Parse(ain["ActionType"].InnerText));
                        }

                        justNode.AddActions(actionList);

                        if (tn["TalkEdges"].ChildNodes.Count > 0)
                        {
                            XmlNodeList nodelist = tn["TalkEdges"].ChildNodes;

                            foreach (XmlNode nod in nodelist)
                                justDialog.EdgesToNodes.Add(nod["TalkEdgeID"].InnerText, tn["TalkNodeID"].InnerText);
                        }

                        justNode.Quest = tn["QuestID"].InnerText;
                        if (tn["PrizeNPC"].InnerText != "")
                            justNode.PrizeNPC = PrizeManager.P[tn["PrizeNPC"].InnerText];

                        if (tn["PrizePlayer"].InnerText != "")
                            justNode.PrizePlayer = PrizeManager.P[tn["PrizePlayer"].InnerText];

                        if (tn["PrizePlayerRemove"].InnerText != "")
                            justNode.PrizePlayerRemove = PrizeManager.P[tn["PrizePlayerRemove"].InnerText];

                        if (tn["Activator"].InnerText != "" && tn["Activator"].InnerText != null)
                            justNode.Activatorr = tn["Activator"].InnerText;
                        else
                            justNode.Activatorr = "";
                        justNode.PrzypiszMetode();
                        justNode.DialogID = item["DialogID"].InnerText;
                        justDialog.Nodes.Add(tn["TalkNodeID"].InnerText, justNode);
                    }

                    XmlNodeList TalkEdges = item["Edges"].ChildNodes;

                    foreach (XmlNode te in TalkEdges)
                    {
                        TalkEdge justEdge = new TalkEdge(justDialog.Nodes[te["ToWhere"].InnerText]);
                        justEdge.ID = te["TalkEdgeID"].InnerText;
                        XmlNodeList ConditionsInEdge = te["Conditions"].ChildNodes;
                        List<Condition> listaWarunkow = new List<Condition>();

                        foreach (XmlNode cin in ConditionsInEdge)
                        {
                            listaWarunkow.Add((Condition)int.Parse(cin["ConditionType"].InnerText));
                        }

                        justEdge.AddConditions(listaWarunkow);

                        while (justDialog.EdgesToNodes.ContainsKey(te["TalkEdgeID"].InnerText))
                        {
                            justDialog.Nodes[justDialog.EdgesToNodes[te["TalkEdgeID"].InnerText]].Edges.Add(justEdge);
                            justDialog.EdgesToNodes.Remove(te["TalkEdgeID"].InnerText);
                        }

                        justEdge.Quest = te["ConditionQuestID"].InnerText;
                        justEdge.AmountGold = int.Parse(te["GoldRequired"].InnerText);

                        justDialog.Edges.Add(te["TalkEdgeID"].InnerText, justEdge);
                        justDialog.Reactions[te["FromWhere"].InnerText].Edges.Add(justDialog.Edges[te["TalkEdgeID"].InnerText]);
                    }

                    justDialog.ID = item["DialogID"].InnerText;
                    D.Add(item["DialogID"].InnerText, justDialog);
                }
            }

        }
    }
}
