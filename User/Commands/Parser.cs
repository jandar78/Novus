using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Interfaces;

//Todo:
//Need to figure out a clever way in which to see what state a character is that prevents him from accomplishing the action
//if the player is dead or unconcious, he shouldn't be able to do almost anything, same goes for trying to move while sitting, etc.

namespace Commands{

    public partial class CommandParser {

		 private static List<string> punctuation = new List<string>(new string[] { ",", ";", "\'", "\"", ":", "|", "\\", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "[", "]", "{", "}", "=", "+", "-", "_" });
		 private static List<string> uselessWords = new List<string>(new string[] { "and", "or", "if", "for", "yet", "nor", "but", "because", "so", "the", "to", "then", "that"});

		 static public void ParseCommands(IUser player) {
			 List<string> commands = ParseCommandLine(player.InBufferPeek);
             bool commandFound = false;
             
             foreach (Dictionary<string, CommandDelegate> AvailableCommands in CommandsList) {
                 if (AvailableCommands.ContainsKey(commands[1].ToUpper())) {
                     AvailableCommands[commands[1].ToUpper()](player, commands);
                     commandFound = true;
                     break;
                 }
             }

             //if all else fails auto-attack
             if (player.Player.InCombat && player.Player.CurrentTarget != null) {
                 //auto attack! or we could remove this and let a player figure out on their own they're being attacked
                 commands.Clear();
                 commands.Add("KILL");
                 commands.Add("target");
                 commands.Insert(0, commands[0] + " " + commands[1]);
             }

			 if (!commandFound && CombatCommands.ContainsKey(commands[1].ToUpper())) { //check to see if player provided a combat related command
				 CombatCommands[commands[1].ToUpper()](player, commands);
                 commandFound = true;
                 commands[0] = player.InBuffer; //just removing the command from the queue now
                 commands.Clear();
			 }


             if (commands.Count == 0) return;
			
			 //maybe this command shouldn't be a character method call...we'll see
			 if (commands.Count >= 2 && commands[1].ToLower() == "save") {
				 player.Player.Save();
				 player.MessageHandler("Save succesful!\r\n");
                 
                 commandFound = true;
			 }

             if (!commandFound && commands[0].Length > 0) {
                 player.MessageHandler("I don't know what you're trying to do, but that's not going to happen.");
             }

             commands[0] = player.InBuffer; //remove command from queue
		 }

         static public void ExecuteCommand(IActor actor, string command, string message = null) {
             IUser player = new Sockets.User(true);
             player.UserID = actor.Id;
             player.Player = actor;
             bool commandFound = false;

             if (CombatCommands.ContainsKey(command.ToUpper())) { //check to see if player provided a combat related command
                 CombatCommands[command.ToUpper()](player, new List<string>(new string[] { command, message }));
                 commandFound = true;
             }

             if (!commandFound) {
                 foreach (Dictionary<string, CommandDelegate> AvailableCommands in CommandsList) {
                     if (AvailableCommands.ContainsKey(command.ToUpper())) {
                         AvailableCommands[command.ToUpper()](player, new List<string>(new string[] { command + " " + message, command, message }));
                         break;
                     }
                 }
             }
         }

         static public void ExecuteCommandUser(IUser actor, string command, string message = null) {
             bool commandFound = false;

             if (CombatCommands.ContainsKey(command.ToUpper())) { //check to see if player provided a combat related command
                 CombatCommands[command.ToUpper()](actor, new List<string>(new string[] { command, message }));
                 commandFound = true;
             }

             if (!commandFound) {
                 foreach (Dictionary<string, CommandDelegate> AvailableCommands in CommandsList) {
                     if (AvailableCommands.ContainsKey(command.ToUpper())) {
                         AvailableCommands[command.ToUpper()](actor, new List<string>(new string[] { command + " " + message, command, message }));
                         break;
                     }
                 }
             }
         }

		static private List<string> ParseCommandLine(string command) {
			 List<string> commands = new List<string>();
			 string originalCommand = command;
			 
			//now we are going to get rid of punctuation and words we don't need
			 //we want to be able to end up pulling the verb and the noun(s) that are affected by the verb easily
			 command = RemovePunctuation(command);
			 
			 commands = command.Split(' ').ToList();

			 commands = RemoveUselessWords(commands);

             commands.Insert(0, originalCommand); //some commands may still need the full sentence
             
			 return commands;
		 }

		static private string RemovePunctuation(string input) {
			
			foreach (string punc in punctuation) {
				input = input.Replace(punc, "");
			}
			return input;
		}

		static private List<string> RemoveUselessWords(List<string> input) {
			List<string> wordsThatAreNeeded = new List<string>();

			foreach (string word in input) {
				if (!uselessWords.Contains(word)) {
					wordsThatAreNeeded.Add(word);
				}
			}

			return wordsThatAreNeeded;
		}
	 }

    public enum SkillLevel { NONE, NEWBIE, APPRENTICE, JOURNEYMAN, MASTER }
    public enum SkillTypes { PASSIVE, PASSIVE_ROOM, ACTIVE, ACTIVE_AOE };
}
