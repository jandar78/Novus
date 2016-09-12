using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Interfaces {
    public enum ScriptTypes { Lua, Roslyn, None };

    public interface IScript {
        void RunScript();
        void AddVariable(object variable, string variableName);
        ScriptTypes ScriptType { get; set; }
        string ID { get; set; }
        byte[] ScriptByteArray { get; set; }
    }

    public abstract class Script : IScript {
        protected MemoryStream MemStream { get; set; }

        public static void SaveScriptToDatabase(string scriptID, string scriptText, ScriptTypes scriptType) { }
        public static string GetScriptFromDatabase(string scriptID) { return String.Empty; }
        
        //added this because if we just initialize a the memory stream then we have very serious (crashing) issue when saving the items.
        //the memory stream causes a "timeout not supported by stream" exception.  It wasn't fun to track down or figure out.
        public byte[] ScriptByteArray {
            get;
            set;
        }

        public string MemStreamAsString {
            get;
        }

        public virtual ScriptTypes ScriptType {
            get; set;
        }

        protected byte[] MemStreamAsByteArray {
            get;
        }

        public virtual void AddVariable(object variable, string variableName) { }
        public virtual void RunScript() { }
            
        public string ID { get; set; }
    }
}
