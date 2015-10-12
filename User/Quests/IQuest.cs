using ClientHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quests {
    public interface IQuest {

        void StartQuest(string playerID);
        void ProcessQuestStep(Message message, Character.Iactor npc);
        void EndQuest(string playerID);

    }
}
