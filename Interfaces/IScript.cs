using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Triggers {
    public enum ScriptTypes { Lua, Roslyn, None };

    public interface IScript {
		 void RunScript();
		 void AddVariable(object variable, string variableName);
		 ScriptTypes ScriptType { get; }
	}
}
