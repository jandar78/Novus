using System;
using System.IO;

namespace Interfaces {
    public enum ScriptTypes { Lua, Roslyn, None };

    public interface IScript {
        void RunScript();
        void AddVariable(object variable, string variableName);
        ScriptTypes ScriptType { get; set; }
        string Id { get; set; }
        byte[] ScriptByteArray { get; set; }
        string MemStreamAsString { get; }
        byte[] MemStreamAsByteArray { get; }
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

        public byte[] MemStreamAsByteArray {
            get;
        }

        public virtual void AddVariable(object variable, string variableName) { }
        public virtual void RunScript() { }
            
        public string Id { get; set; }
    }
}
