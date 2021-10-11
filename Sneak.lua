--Sneak skill
--When sneaking to another location players in current room and players in new room have a chance of spotting you sneaking around if they equal
--or exceed the players skill sneak level
--This skill displays a set of messages when player is leaving one room and another set when he arrives in the new room
--functions go first so we can call them later on
calculation = "( Agility +Cunning ) / 2 + (( Agility + Cunning )/2 * ( DexterityRank * 0.10))";

function GetPercentage (room, percent, message)
--get all players and npcs in room
	if percent > 0 then
		playersInRoom = {room:GetObjectsInRoom("players", percent)};
		npcsInRoom = {room:GetObjectsInRoom("npcs", percent)};
   
		for i, players in ipairs(playersInRoom) do
			p = GetMethodResult("Server", "GetAUserFromList", {players});
			if p ~= nil and ParseAndCalculateCheckOther(p.Player) >= skillCheck then
				--let's make sure we don't send messages to the player doing the skill
				if (player.ID ~= p.Player.ID) then
					SendMessage(p.UserID, message .. direction .. "."); --message to other person in room
					SendMessage(playerID, ColorFont(p.Player.FirstName .. " observes you sneaking.", 33)); --message to player
				end
			end
		end

		for i, npcs in ipairs(npcsInRoom) do
			n = GetMethodResult("NPCUtils", "GetUserAsNPCFromList", {npcs});
			if n ~= nil and ParseAndCalculateCheckOther(n.Player) >= skillCheck then	
				if (player.ID ~= n.Player.ID) then			
					SendMessage(n.UserID, message .. direction .. "."); --message to other person in room
					SendMessage(playerID, ColorFont(n.Player.FirstName .. " observes you sneaking.", 33)); --message to player
				end
			end
		end

		return message;
	end
end 

function ReverseDirection (direction)
    if (direction == "north") then direction = "South"
    elseif (direction == "south") then direction = "North";
    elseif (direction == "west") then direction = "East";
    elseif (direction == "above") then direction = "Below";
    elseif (direction == "below") then direction = "Above";
    else direction = "West";
	end
	return direction;
end

function MoveToRoom (percent, messageLeave, messageArrive)
	GetPercentage(room, percent, messageLeave);
	GetMethodResult("CommandParser", "ExecuteCommandUser", { GetPlayer(""), direction, ""});
	direction = ReverseDirection(direction);
	room = GetMethodResult("Room", "GetRoom", { player.Location });
	GetPercentage(room, percent, messageArrive);
end
--returns the number of items in a table
function tableLength(T)
  local count = 0;
  if (T ~= nil)then
     for i=1, T.count do count = i end
  end
  return count
end

--set up variables we will need
player = GetPlayer("Character");
skillCheck = ParseAndCalculateCheck(player, calculation);
userCommand = GetProperty(null, "UserCommand", "", "Skill");
room = GetMethodResult("Room","GetRoom", {player.Location});
canPerform = true;

if (tableLength(userCommand) < 2) then
	AssignMessage("Player", "In which direction do you want to sneak?");
	canPerform = false;
else
	direction = string.lower(userCommand[2]);
	if (direction == "north" or direction == "south" or direction == "east" or direction == "west" or direction == "up" or direction == "down") then
		if direction == "up" then 
			direction = "above"; 
		end
		if direction == "down" then 
			direction = "below";
		end	
	direction = string.upper(string.sub(direction,1,1))..string.sub(direction,2);
	adjacentRoom = room:GetRoomExit(direction);
	else
		AssignMessage("Player", "You really think that is a direction?");
		canPerform = false;
	end	
end

if adjacentRoom == nil then
	AssignMessage("Player", "You can't sneak in that direction.");
	canPerform = false;
else
	adjacentRoom = room:GetRoomExit(direction).availableExits[direction];
end
--array of messages to select from
if direction == nil then
	direction = "void"
end

successFailMessages = {
					"You try to sneak to the " .. direction .. " but fail.",
					"You sneak to the "
			        }
					  
messageOthersArrive = {
				player.Name .. " tried to be sneaky as he arrives from the ",
				player.Name .. " tries to sneak past you as he arrives from the ",
				player.Name .. " almost sneaks past you as he arrives from the ",
				player.Name .. " barely makes a sound as he sneaks past you arriving from the ",
				player.Name .. " almost sneaks past you from the ",
				}
				 
messageOthersLeave = {
				player.Name .. " tries to be sneaky but is clearly not as he leaves to the ",	
				player.Name .. " tries to sneak past you as he leaves to the ",
				player.Name .. " almost sneaks past you as he leaves to the ",
				player.Name .. " barely makes a sound as he sneaks past you leaving to the ",
				player.Name .. " almost sneaks past you as he leaves to the ",
				}
				 
msg = "";
msgOther ="";
failed = false;
--lets check the player state and make sure he can actually perform this skill in his current condition
action = string.upper(player.Action);
stance = string.upper(player.Stance);

if canPerform then
	if stance == "LAYING_UNCONCIOUS" or stance == "LAYING_DEAD" or stance == "DECOMPOSING" then
		canPerform = false;
		SendMessage(player.ID, "You can't hide while you are " .. stance);
	end
	if action ~= "HIDING" and action ~= "SNEAKING" and action ~= "NONE" then
		canPerform = false;
		SendMessage(player.ID, "You can't sneak when you are " .. action .. "!");
	end

	if action ~= "HIDING" then
		SendMessage(player.ID, "You have to be hidden before you can sneak around.");
		canPerform = false;
	else

--the business logic for the skill
		if canPerform then
			player:SetActionStateDouble(7); --set him to sneaking so he doesn't become visible on move
			if (skillCheck > -9999 and skillCheck <= 1) then --12
				player:SetActionStateDouble(6); -- if you fail to sneak you go to hiding and then become visible on move
				SendMessage(player.ID , successFailMessages[1] .. direction .. ".");
				GetMethodResult("Room" , "InformPlayersInRoom", { messageOthersLeave[1] .. direction .. ".", player.Location, {player.ID}});
				GetMethodResult("CommandParser", "ExecuteCommand", { player, "Move" });
				ReverseDirection(direction);
				GetMethodResult("Room" , "InformPlayersInRoom", { messageOthersArrive[1] .. direction .. ".", player.Location, {player.ID}});				
				failed = true;
			--everyone can see player go into hiding
			elseif (skillCheck > 1 and skillCheck <= 2) then --18
				SendMessage(player.ID , successFailMessages[2] .. direction .. ".");
				MoveToRoom(100 , messageOthersLeave[2], messageOthersArrive[2]);
			--other players/npc have 50% chance of seeing player hide if they match or pass the players skillCheck
			elseif (skillCheck > 2 and skillCheck <= 21) then
				SendMessage(player.ID , successFailMessages[2] .. direction .. ".");
				MoveToRoom(100 , messageOthersLeave[3], messageOthersArrive[3]);		
			elseif (skillCheck > 21 and skillCheck <= 24) then
				SendMessage(player.ID , successFailMessages[2] .. direction .. ".");
				MoveToRoom(100 , messageOthersLeave[4], messageOthersArrive[4]);				
			elseif (skillCheck > 24 and skillCheck <= 27) then
				SendMessage(player.ID , successFailMessages[2] .. direction .. ".");
				MoveToRoom(100 , messageOthersLeave[5], messageOthersArrive[5]);				
			elseif (skillCheck > 27 and skillCheck <= 9999) then
				SendMessage(player.ID , successFailMessages[2] .. direction .. " and no one notices.");
			end
			
			if failed == false then
				--player snuck successfully let's update his state
				player:SetActionStateDouble(6); --set player back to a hidden state
			end
		end
	end
end