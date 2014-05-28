calculation = "( Agility +Cunning ) / 2 + (( Agility + Cunning )/2 * ( DexterityRank * 0.10))";

--functions go first so we can call them later on
function GetPercentage (percent, message)
--get all players and npcs in room
	if percent > 0 then
		playersInRoom = {room:GetObjectsInRoom("players", 100)};
		npcsInRoom = {room:GetObjectsInRoom("npcs", 100)};
   
		for i, players in ipairs(playersInRoom) do
			p = GetMethodResult("Server", "GetAUserFromList", {players});
			if p ~= null and ParseAndCalculateCheckOther(p.Player) >= skillCheck then
				--let's make sure we don't send messages to the player doing the skill
				--if (playerID ~= GetProperty(GetProperty(p, "Player", "Character.Character"), "ID", "System.String")) then
				if (playerID ~= p.Player.ID) then
					SendMessage(p.UserID, message); --message to other person in room
					SendMessage(playerID, p.Player.FirstName .. " observes you going into hiding."); --message to player
				end
			end
		end

		for i, npcs in ipairs(npcsInRoom) do
			n = GetMethodResult("NPCUtils", "GetUserAsNPCFromList", {npcs});
			if n ~= null and ParseAndCalculateCheckOther(n.Player) >= skillCheck then	
				if (playerID ~= n.Player.ID) then			
					SendMessage(n.UserID, message); --message to other person in room
					SendMessage(playerID, n.Player.FirstName .. " observes you going into hiding."); --message to player
				end
			end
		end

		return message;
	end
end 

--set up variables we will need
player = GetPlayer("Character");
playerLocation = player.Location;
room = GetMethodResult("Room", "GetRoom", { playerLocation });
skillCheck = ParseAndCalculateCheck(player, calculation);
playerID = player.ID;
--array of messages to select from
successFailMessages = {"You fail at trying to hide, might as well walk around with a flare in your hand",
					   "Everyone watches you step into the shadows, you are now hidden from plain sight. ",
			           "You slowly transition into the shadows hiding yourself from plain sight.  Only a few may be aware you have gone into hiding.",
			           "You fade into the shadows hiding yourself from plain sight.  Only the keenest of observers have seen you vanish.",
					   "You are engulfed by the shadows as you become one with the darkness."					   
			          }
					  
messageOthers = { player.FirstName .. " tried to hide and failed.",
				  player.FirstName .. " steps into the shadows hiding from plain sight.",
				  player.FirstName .. " quietly moves into the shadows and is now hidden from sight.",
				  player.FirstName .. " fades into the shadows and is now hidden.",
				 }
msg = "";
msgOther ="";
failed = false;
--lets check the player state and make sure he can actually perform this skill in his current condition
action = string.upper(player.Action);
stance = string.upper(player.Stance);

canPerform = true;

if stance == "LAYING_UNCONCIOUS" or stance == "LAYING_DEAD" or stance == "DECOMPOSING" then
	canPerform = false;
	SendMessage(playerID, "You can't hide while you are " .. action);
end
if action ~= "HIDING" and action ~= "SNEAKING" and action ~= "NONE" then
	canPerform = false;
	SendMessage(playerID, "You can't hide when you are " .. action .. "!");
end

if action == "HIDING" or action =="SNEAKING" then
	player:SetActionStateDouble(0);
	SendMessage(playerID, ColorFont("You step out of the shadows.", 33));
	revealMessage = player.FirstName .. " steps out of the shadows.";
	AssignMessage("Room" , revealMessage);
else
--the business logic for the skill
	if canPerform then
		if (skillCheck > -9999 and skillCheck <= 12) then
			AssignMessage("Player" , successFailMessages[1]);
			AssignMessage("Room" , messageOthers[1]);
			failed = true;
		--everyone can see player go into hiding
		elseif (skillCheck > 12 and skillCheck <= 15) then
			AssignMessage("Player" , successFailMessages[2]);
			AssignMessage("Room" , messageOthers[2]);
		--other players/npc have 50% chance of seeing player hide if they match or pass the players skillCheck
		elseif (skillCheck > 15 and skillCheck <= 18) then
			SendMessage(playerID, ColorFont(successFailMessages[3], 33));
			GetPercentage(50, messageOthers[3]);	
		--other players/npc have 25% chance of seeing player hide
		elseif (skillCheck > 18 and skillCheck <= 21) then
			SendMessage(playerID, ColorFont(successFailMessages[4], 33));
			GetPercentage(25, messageOthers[4]);				
		--No ones can you see player go into hiding they are a master hider!
		elseif (skillCheck > 21 and skillCheck <= 9999) then
			SendMessage(playerID, ColorFont(successFailMessages[5], 33));
			GetPercentage(0, nil);				
		end
		
		if failed == false then
			--player hid successfully let's update his state
			player:SetActionStateDouble(6);
		end
	end
	
end

