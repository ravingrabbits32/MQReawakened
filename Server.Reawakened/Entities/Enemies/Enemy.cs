﻿using Server.Reawakened.Entities.Components;
using Server.Reawakened.Rooms.Models.Entities;
using UnityEngine;
using Server.Reawakened.Rooms;
using Microsoft.Extensions.Logging;
using Server.Reawakened.XMLs.Bundles;
using Server.Reawakened.XMLs.BundlesInternal;
using Microsoft.Extensions.DependencyInjection;
using Server.Reawakened.Rooms.Models.Planes;
using Server.Reawakened.Entities.Interfaces;
using Server.Reawakened.Players;
using A2m.Server;
using Server.Reawakened.Players.Extensions;
using Server.Reawakened.Players.Helpers;
using Server.Reawakened.Rooms.Extensions;
using Server.Reawakened.XMLs.Enums;
using Server.Reawakened.Configs;
using Server.Reawakened.Entities.Enemies.Utils;
using Server.Reawakened.Entities.Enemies.EnemyAI;
using Server.Reawakened.Rooms.Models.Entities.ColliderType;
using Server.Reawakened.Rooms.Models.Entities.Colliders;

namespace Server.Reawakened.Entities.Enemies;

public abstract class Enemy : IDestructible
{

    public readonly ILogger<BehaviorEnemy> Logger;
    public readonly InternalAchievement InternalAchievement;
    public readonly QuestCatalog QuestCatalog;
    public readonly ItemCatalog ItemCatalog;
    public readonly InternalDefaultEnemies InternalEnemy;
    public readonly ServerRConfig ServerRConfig;

    public bool Init;

    public Room Room;
    public string Id;
    public Vector3 Position;
    public Rect DetectionRange;
    public EnemyCollider Hitbox;
    public string ParentPlane;
    public bool IsFromSpawner;
    public float MinBehaviorTime;

    public int Health;
    public int MaxHealth;
    public int Level;
    public int DeathXp;
    public string OnDeathTargetId;
    public BaseSpawnerControllerComp LinkedSpawner;

    public BaseComponent Entity;
    public InterObjStatusComp Status;
    public EnemyControllerComp EnemyController;
    public BehaviorModel BehaviorList;

    public GlobalProperties EnemyGlobalProps;
    public AIProcessData AiData;
    public AISyncEventHelper SyncBuilder;

    public Enemy(Room room, string entityId, string prefabName, EnemyControllerComp enemyController, IServiceProvider services)
    {
        //Basic Stats
        Room = room;
        Id = entityId;
        IsFromSpawner = false;
        MinBehaviorTime = 0;
        SyncBuilder = new AISyncEventHelper();

        Logger = services.GetRequiredService<ILogger<BehaviorEnemy>>();
        InternalAchievement = services.GetRequiredService<InternalAchievement>();
        QuestCatalog = services.GetRequiredService<QuestCatalog>();
        ItemCatalog = services.GetRequiredService<ItemCatalog>();
        InternalEnemy = services.GetRequiredService<InternalDefaultEnemies>();
        ServerRConfig = services.GetRequiredService<ServerRConfig>();

        //Component Info
        EnemyController = enemyController;

        //Position Info
        ParentPlane = EnemyController.ParentPlane;
        Position = new Vector3(EnemyController.Position.X, EnemyController.Position.Y, EnemyController.Position.Z);

        var status = room.GetEntityFromId<InterObjStatusComp>(Id);
        if (status != null)
            Status = status;

        //Plane Wrapup
        if (ParentPlane.Equals("TemplatePlane"))
            CheckForSpawner();

        if (ParentPlane.Equals("Plane1"))
            Position.z = 20;

        //Stats
        BehaviorList = InternalEnemy.GetBehaviorsByName(prefabName);
        OnDeathTargetId = EnemyController.OnDeathTargetID;
        Health = EnemyController.EnemyHealth;
        MaxHealth = EnemyController.MaxHealth;
        DeathXp = EnemyController.OnKillExp;
        Level = EnemyController.Level;

        //Hitbox Info
        GenerateHitbox(BehaviorList.Hitbox);

        //This is just a dummy. AI_Stats_Global has no data, so these fields are populated in the specific Enemy classes
        EnemyGlobalProps = new GlobalProperties(true, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Generic", "", false, false, 0);
    }

    public virtual void Initialize() => Init = true;

    public virtual void Update()
    {
        if (!Init)
            Initialize();

        if (Room.IsObjectKilled(Id))
            return;
    }

    public virtual void CheckForSpawner()
    {
        IsFromSpawner = true;
        var spawnerId = Id.Split("_");
        LinkedSpawner = Room.GetEntityFromId<BaseSpawnerControllerComp>(spawnerId[0]);

        Position = new Vector3(LinkedSpawner.Position.X + LinkedSpawner.SpawningOffsetX, LinkedSpawner.Position.Y + LinkedSpawner.SpawningOffsetY, LinkedSpawner.Position.Z);
        ParentPlane = LinkedSpawner.ParentPlane;
    }

    public void GenerateHitbox(HitboxModel box)
    {
        var width = box.Width * EnemyController.Scale.X;
        var height = box.Height * EnemyController.Scale.Y;
        float offsetX;
        float offsetY;


        if (EnemyController.Scale.X >= 0)
            offsetX = box.XOffset * EnemyController.Scale.X - width / 2;
        else
        {
            width *= -1;
            offsetX = box.XOffset * -1 * EnemyController.Scale.X - width / 2;
        }

        if (EnemyController.Scale.Y >= 0)
            offsetY = box.YOffset * EnemyController.Scale.Y - height / 2;
        else
        {
            height *= -1;
            offsetY = box.YOffset * -1 * EnemyController.Scale.Y - height / 2;
        }


        Hitbox = new EnemyCollider(Id, new Vector3Model { X = offsetX, Y = offsetY, Z = Position.z },
             width, height, ParentPlane, Room)
        {
            Position = new Vector3(Position.x, Position.y, Position.z)
        };

        Room.Colliders.Add(Id, Hitbox);
    }

    public virtual void Damage(int damage, Player origin)
    {
        if (Room.IsObjectKilled(Id))
            return;

        var trueDamage = damage - GameFlow.StatisticData.GetValue(ItemEffectType.Defence, WorldStatisticsGroup.Enemy, Level);
        if (trueDamage <= 0)
            trueDamage = 1;

        Health -= trueDamage;

        var damageEvent = new AiHealth_SyncEvent(Id.ToString(), Room.Time, Health, trueDamage, 0, 0, origin == null ? string.Empty : origin.CharacterName, false, true);
        Room.SendSyncEvent(damageEvent);

        if (Health <= 0)
        {
            if (OnDeathTargetId is not null and not "0")
                foreach (var trigger in Room.GetEntitiesFromId<TriggerReceiverComp>(OnDeathTargetId))
                    trigger.Trigger(true);

            //Dynamic Loot Drop
            var chance = new System.Random();
            if (BehaviorList.EnemyLootTable != null)
                foreach (var drop in BehaviorList.EnemyLootTable)
                {
                    chance.NextDouble();
                    if (Level <= drop.MaxLevel && Level >= drop.MinLevel)
                        origin.GrantDynamicLoot(Level, drop, ItemCatalog);
                }

            //The XP Reward here is not accurate, but pretty close
            var xpAward = DeathXp - (origin.Character.Data.GlobalLevel - 1) * 5;
            Room.SendSyncEvent(AISyncEventHelper.AIDie(Id, Room.Time, string.Empty, xpAward > 0 ? xpAward : 1, true, origin == null ? "0" : origin.GameObjectId, false));
            origin.AddReputation(xpAward > 0 ? xpAward : 1, ServerRConfig);

            //For spawners
            if (IsFromSpawner)
            {
                var spawnCount = Id.Split("_");
                LinkedSpawner.NotifyEnemyDefeat(int.Parse(spawnCount[1]));
                Room.Enemies.Remove(Id);
                Room.Colliders.Remove(Id);
            }

            //Achievements
            origin.CheckObjective(ObjectiveEnum.Score, Id, EnemyController.PrefabName, 1, QuestCatalog);
            origin.CheckObjective(ObjectiveEnum.Scoremultiple, Id, EnemyController.PrefabName, 1, QuestCatalog);

            origin.CheckAchievement(AchConditionType.DefeatEnemy, string.Empty, InternalAchievement, Logger);
            origin.CheckAchievement(AchConditionType.DefeatEnemy, EnemyController.PrefabName, InternalAchievement, Logger);
            origin.CheckAchievement(AchConditionType.DefeatEnemyInLevel, origin.Room.LevelInfo.Name, InternalAchievement, Logger);

            Room.KillEntity(origin, Id);
        }
    }

    public virtual void GetInitData(Player player)
    {
    }

    public void Destroy(Player player, Room room, string id)
    {
        room.Enemies.Remove(id);
        room.Colliders.Remove(id);
    }
}
