calculation = "( Cunning + Wisdom )/2 + (( Cunning + Wisdom ) /2 * ( IntelligenceRank * 0.1))";

--functions go first so we can call them later on
function GetPercentage (room, percent)
	playersInRoom = {room:GetObjectsInRoom("players", percent)};
	npcsInRoom = {room:GetObjectsInRoom("npcs", percent)};
	itemsInRoom = {room:GetObjectsInRoom("items", percent)};
	
	local message = "";
   
	for i, players in ipairs(playersInRoom) do
		p = GetMethodResult("Server", "GetAUserFromList", {players});
		if p ~= nil then	
			message = message .. p.Player.FirstName .. " is " 
					.. string.lower(p.Player.StanceState) .. " there. \r\n";
		end
	end

	for i, npcs in ipairs(npcsInRoom) do
		n = GetMethodResult("NPCUtils", "GetUserAsNPCFromList", {npcs});
		if n ~= nil then		
			message = message .. n.Player.FirstName .. " is " 
					.. string.lower(n.Player.StanceState) .. " there. \r\n";
		end
	end

	for i, items in ipairs(itemsInRoom) do
		i = GetMethodResult("Items", "GetByIDFromList", {items});
		if i ~= nil then	
			message = message .. i.Name .. " is laying there. \r\n";
		end
	end

	return message;
end 

--set up variables we will need
player = GetPlayer("Character");
skillCheck = ParseAndCalculateCheck(player, calculation);
direction = GetProperty(null, "UserCommand", "", "Skill")[2]; 
room = GetMethodResult("Room", "GetRoom", { player.Location });
hasRoom = true;
roomExit = room:GetRoomExit(direction);
if roomExit ~= nil then
	roomID = GetDictionaryElement(roomExit.availableExits, direction).Id;
else
	hasRoom = false;
end
if (roomID ~= nil) then
	adjacentRoom = GetMethodResult("Room", "GetRoom", { roomID });
	if adjacentRoom ~= nil then
		isDark = adjacentRoom.IsDark;
		door = GetDictionaryElement(roomExit.doors, direction);
		hasDoor = roomExit.HasDoor;
    end
 end
--array of messages to select from
successFailMessages = {"You can't peek in that direction, the door is closed and has no keyhole!",
					   "There isn't a place to peek towards the " .. direction .. ".",
			           "You fail to see anything to the " .. direction .. ".",
			           "You see ",
					   "It's too dark to see anything!"					   
			          }
					  
messageOthers = {player.FirstName .. " takes a peek to the " .. direction .. ".",
				  player.FirstName .. " attempted to peek to the " .. direction .. "."
				 }
msg = "";
msgOther ="";

--the business logic for the skill
if (hasRoom == true) then
	if ((hasDoor == false) or (hasDoor == true and (door.IsPeekable == true))) then
		--player sees only title of room
		if (isDark == true) then
				AssignMessage("Player", successFailMessages[5]);
		else
			title = adjacentRoom.Title .. "\r\n";
			description = adjacentRoom.Description .. "\r\n";
			
			if  (skillCheck > -9999 and skillCheck <= 12) then
				msg = successFailMessages[3];
				msgOther = messageOthers[2];
			
			elseif (skillCheck > 12 and skillCheck <= 15) then
				msg = successFailMessages[4] .. title;				
				msgOther = messageOthers[1];
			
			--player sees room title and description and has 1/5 chance of seeing every player, npc and item in room
			elseif (skillCheck > 15 and skillCheck <= 19) then
				msg = successFailMessages[4].. title .. description .. GetPercentage(adjacentRoom, 20);				
				msgOther = messageOthers[1];
			
			elseif (skillCheck > 19 and skillCheck <= 23) then
				msg = successFailMessages[4].. title .. description .. GetPercentage(adjacentRoom,50);
				msgOther = messageOthers[1];
			
			elseif (skillCheck > 23 and skillCheck <= 25.0) then
				msg = successFailMessages[4].. title .. description .. GetPercentage(adjacentRoom,75);
				msgOther = messageOthers[1];
			
			elseif (skillCheck > 25.0 and skillCheck <= 9999) then
				msg = successFailMessages[3].. title .. description .. GetPercentage(adjacentRoom,100);
				msgOther = messageOthers[1];
			end
		end	
	else
		msg = successFailMessages[1];
		msgOther = messageOthers[2];
	end	
else 
	msg = successFailMessages[2];
	msgOther = messageOthers[2];
end

--if we had to send a different message to each player in a room dependent on a specific case we could use
AssignMessage("Player", msg);
AssignMessage("Room", msgOther);


	