﻿using Server.Reawakened.Players;
using Server.Reawakened.Rooms;

namespace Server.Reawakened.Entities.Interfaces;

public interface IDestructible
{
    public void Destroy(Player player, Room room, string id);
}
