using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IQuest
    {
        string Id { get; set; }
        void StartQuest(string playerID);
        void ProcessQuestStep(IMessage message, IActor npc);
        void EndQuest(string playerID);
        List<IQuestStep> QuestSteps { get; }
        Dictionary<string, int> CurrentPlayerStep { get; set; }
        string QuestID { get; set; }
        int CurrentStep { get; set; }
        short TotalSteps { get; set; }
        bool AutoProcessNextStep { get; }
        bool AllowOutOfOrder { get; set; }
        Queue<string> AutoProcessPlayer { get; set; }

        void AutoProcessQuestStep(IActor actor);
    }

    public interface IQuestStep {
        HashSet<string> PlayerIDList { get; }
        ITrigger Trigger { get; set; }
        string QuestID { get; set; }
        int Step { get; set; }
        bool AppliesToNPC { get; set; }
        bool AutoProcess { get; set; }
        bool IfPreviousCompleted { get; set; }
        void ProcessStep(object sender, EventArgs e);
        bool AddPlayerToQuest(string playerID);
    }

    public class Quest : IQuest
    {
        public void StartQuest(string playerID) { }
        public void ProcessQuestStep(IMessage message, IActor npc) { }
        public void EndQuest(string playerID) { }

        public string Id { get; set; }
        public List<IQuestStep> QuestSteps { get; }
        public Dictionary<string, int> CurrentPlayerStep { get; set; }
        public string QuestID { get; set; }
        public int CurrentStep { get; set; }
        public short TotalSteps { get; set; }
        public bool AutoProcessNextStep { get; set; }
        public bool AllowOutOfOrder { get; set; }
        public Queue<string> AutoProcessPlayer { get; set; }
        public void AutoProcessQuestStep(IActor actor) { }

    }

    public class QuestStep : IQuestStep
    {
        public HashSet<string> PlayerIDList { get; }
        public ITrigger Trigger { get; set; }
        public string QuestID { get; set; }
        public int Step { get; set; }
        public bool AppliesToNPC { get; set; }
        public bool AutoProcess { get; set; }
        public bool IfPreviousCompleted { get; set; }

        public void ProcessStep(object sender, EventArgs e) { }
        public bool AddPlayerToQuest(string playerID) { return true; }
    }
}