﻿using A2m.Server;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Server.Reawakened.Entities.AIBehavior;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Helpers;
using Server.Reawakened.Rooms;
using Server.Reawakened.Rooms.Extensions;
using Server.Reawakened.Rooms.Models.Entities;
using Server.Reawakened.Rooms.Models.Planes;
using System;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using UnityEngine;

namespace Server.Reawakened.Entities.Entity.Enemies;
public class EnemyOrchid(Room room, string entityId, BaseComponent baseEntity) : Enemy(room, entityId, baseEntity)
{

    private float _behaviorEndTime;
    private float _initialDirection;

    public override void Initialize()
    {
        base.Initialize();

        BehaviorList = EnemyController.EnemyInfoXml.GetBehaviorsByName(Entity.PrefabName);

        MinBehaviorTime = Convert.ToSingle(BehaviorList.GetGlobalProperty("MinBehaviorTime"));
        EnemyGlobalProps.Global_FrontDetectionRangeX = Convert.ToSingle(BehaviorList.GetGlobalProperty("FrontDetectionRangeX"));
        EnemyGlobalProps.Global_FrontDetectionRangeUpY = Convert.ToSingle(BehaviorList.GetGlobalProperty("FrontDetectionRangeUpY"));
        EnemyGlobalProps.Global_FrontDetectionRangeDownY = Convert.ToSingle(BehaviorList.GetGlobalProperty("FrontDetectionRangeDownY"));
        EnemyGlobalProps.Global_BackDetectionRangeX = Convert.ToSingle(BehaviorList.GetGlobalProperty("BackDetectionRangeX"));
        EnemyGlobalProps.Global_ShootOffsetX = Convert.ToSingle(BehaviorList.GetGlobalProperty("ShootOffsetX"));
        EnemyGlobalProps.Global_ShootOffsetY = Convert.ToSingle(BehaviorList.GetGlobalProperty("ShootOffsetY"));
        EnemyGlobalProps.Global_ShootingProjectilePrefabName = BehaviorList.GetGlobalProperty("ProjectilePrefabName").ToString();

        AiData.Intern_Dir = 1;

        // Address magic numbers when we get to adding enemy effect mods
        Room.SendSyncEvent(AIInit(1, 1, 1));
        Room.SendSyncEvent(SyncBuilder.AIDo(Entity, Position, 1.0f, BehaviorList.IndexOf("Patrol"), string.Empty, Position.x, Position.y, AiData.Intern_Dir, false));

        // Set these calls to the xml later. Instead of using hardcoded "Patrol", "Aggro", etc.
        // the XML can just specify which behaviors to use when attacked, when moving, etc.
        AiBehavior = ChangeBehavior("Patrol");
    }

    public override void Damage(int damage, Player origin)
    {
        base.Damage(damage, origin);

        if (AiBehavior is not AIBehavior_Shooting)
        {
            Room.SendSyncEvent(SyncBuilder.AIDo(Entity, Position, 1.0f, BehaviorList.IndexOf("Shooting"), string.Empty, origin.TempData.Position.X, origin.TempData.Position.Y,
                 AiData.Intern_Dir, false));

            // For some reason, the SyncEvent doesn't initialize these properly, so I just do them here
            AiData.Sync_TargetPosX = origin.TempData.Position.X;
            AiData.Sync_TargetPosY = origin.TempData.Position.Y;

            AiBehavior = ChangeBehavior("Shooting");
            _behaviorEndTime = ResetBehaviorTime(MinBehaviorTime);
        }
    }

    public override void HandlePatrol()
    {
        base.HandlePatrol();
        DetectPlayers("Shooting");
    }

    public override void HandleShooting()
    {
        base.HandleShooting();

        if (!AiBehavior.Update(ref AiData, Room.Time))
        {
            Room.SendSyncEvent(SyncBuilder.AIDo(Entity, Position, 1.0f, BehaviorList.IndexOf("LookAround"), string.Empty, AiData.Sync_TargetPosX, AiData.Sync_TargetPosY,
            AiData.Intern_Dir, false));

            AiBehavior = ChangeBehavior("LookAround");
            _behaviorEndTime = ResetBehaviorTime(Convert.ToSingle(BehaviorList.GetBehaviorStat("LookAround", "lookTime")));
        }

        if (AiData.Intern_FireProjectile)
        {
            Room.SendSyncEvent(SyncBuilder.AILaunchItem(Entity, Position.x + EnemyGlobalProps.Global_ShootOffsetX, Position.y + EnemyGlobalProps.Global_ShootOffsetY, Position.z, (float)Math.Cos(AiData.Intern_FireAngle) * AiData.Intern_FireSpeed, (float)Math.Sin(AiData.Intern_FireAngle) * AiData.Intern_FireSpeed, 3, 0, 0));

            AiData.Intern_FireProjectile = false;
        }
    }

    public override void HandleLookAround()
    {
        base.HandleLookAround();
        DetectPlayers("Shooting");
        if (Room.Time >= _behaviorEndTime)
        {
            Room.SendSyncEvent(SyncBuilder.AIDo(Entity, Position, 1.0f, BehaviorList.IndexOf("Patrol"), string.Empty, Position.x, Position.y, AiData.Intern_Dir, false));

            AiBehavior = ChangeBehavior("Patrol");
        }
    }

    public override void DetectPlayers(string behaviorToRun)
    {
        foreach (var player in Room.Players)
        {
            if (PlayerInRange(player.Value.TempData.Position, false))
            {
                Room.SendSyncEvent(SyncBuilder.AIDo(Entity, Position, 1.0f, BehaviorList.IndexOf(behaviorToRun), string.Empty, player.Value.TempData.Position.X,
                    player.Value.TempData.Position.Y, Generic.Patrol_ForceDirectionX, false));

                // For some reason, the SyncEvent doesn't initialize these properly, so I just do them here
                AiData.Sync_TargetPosX = player.Value.TempData.Position.X;
                AiData.Sync_TargetPosY = player.Value.TempData.Position.Y;

                AiBehavior = ChangeBehavior(behaviorToRun);

                _behaviorEndTime = ResetBehaviorTime(MinBehaviorTime);
            }
        }
    }
}
