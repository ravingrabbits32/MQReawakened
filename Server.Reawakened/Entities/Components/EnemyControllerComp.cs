﻿using A2m.Server;
using Microsoft.Extensions.Logging;
using Server.Reawakened.Entities.Interfaces;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Extensions;
using Server.Reawakened.Rooms.Extensions;
using Server.Reawakened.Rooms.Models.Entities;
using Server.Reawakened.XMLs.BundlesInternal;
using Server.Reawakened.XMLs.Enums;
using UnityEngine;
using Room = Server.Reawakened.Rooms.Room;

namespace Server.Reawakened.Entities.Components;
public class EnemyControllerComp : Component<EnemyController>, IDestructible
{
    public int OnKillRepPoints => ComponentData.OnKillRepPoints;
    public bool TopBounceImmune => ComponentData.TopBounceImmune;
    public string OnKillMessageReceiver => ComponentData.OnKillMessageReceiver;
    public int EnemyLevelOffset => ComponentData.EnemyLevelOffset;
    public string OnDeathTargetID => ComponentData.OnDeathTargetID;
    public string EnemyDisplayName => ComponentData.EnemyDisplayName;
    public int EnemyDisplayNameSize => ComponentData.EnemyDisplayNameSize;
    public Vector3 EnemyDisplayNamePosition => ComponentData.EnemyDisplayNamePosition;
    public Color EnemyDisplayNameColor => ComponentData.EnemyDisplayNameColor;
    public EnemyScalingType EnemyScalingType => ComponentData.EnemyScalingType;
    public bool CanAutoScale => ComponentData.CanAutoScale;
    public bool CanAutoScaleResistance => ComponentData.CanAutoScaleResistance;
    public bool CanAutoScaleDamage => ComponentData.CanAutoScaleDamage;

    public ILogger<EnemyControllerComp> Logger { get; set; }
    public InternalDefaultEnemies EnemyInfoXml { get; set; }

    public int Level;

    public override void InitializeComponent() => Level = Room.LevelInfo.Difficulty + EnemyLevelOffset;

    public void Damage(int damage, Player origin)
    {
        var breakEvent = new AiHealth_SyncEvent(Id.ToString(), Room.Time, 0, damage, 0, 0, origin.CharacterName, false, true);
        origin.Room.SendSyncEvent(breakEvent);

        if (Room.Entities.TryGetValue(Id, out var comps))
            foreach (var comp in comps)
                if (comp is IDestructible destroyable)
                    destroyable.Destroy(origin, Room, Id);

        Room.Entities.Remove(Id);
    }

    public void Destroy(Player player, Room room, string id)
    {
        player.CheckObjective(ObjectiveEnum.Score, id, PrefabName, 1);
        player.CheckObjective(ObjectiveEnum.Scoremultiple, id, PrefabName, 1);

        player.CheckAchievement(AchConditionType.DefeatEnemy, string.Empty, Logger);
        player.CheckAchievement(AchConditionType.DefeatEnemy, PrefabName, Logger);
        player.CheckAchievement(AchConditionType.DefeatEnemyInLevel, player.Room.LevelInfo.Name, Logger);
        
        room.Enemies.Remove(id);
        room.Colliders.Remove(id);
    }
}
