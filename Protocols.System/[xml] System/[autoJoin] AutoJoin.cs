﻿using A2m.Server;
using Server.Base.Accounts.Models;
using Server.Reawakened.Core.Network.Protocols;
using Server.Reawakened.Levels.Services;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Helpers;
using Server.Reawakened.Players.Models;
using System.Xml;

namespace Protocols.System._xml__System;

public class AutoJoin : SystemProtocol
{
    public override string ProtocolName => "autoJoin";

    public LevelHandler LevelHandler { get; set; }

    public override void Run(XmlDocument xmlDoc)
    {
        var account = NetState.Get<Account>();
        var player = NetState.Get<Player>();

        player.QuickJoinLevel(0, NetState, LevelHandler);

        SendXt("cx", GetPropertyList(GetPropertiesOfUser(player.UserInfo, account)));
        SendXt("cl", GetCharacterList(player.UserInfo));
    }

    private static Dictionary<CharacterInfoHandler.ExternalProperties, object> GetPropertiesOfUser(UserInfo userInfo, Account account) =>
        new ()
        {
            { CharacterInfoHandler.ExternalProperties.Chat_Level, userInfo.ChatLevel },
            { CharacterInfoHandler.ExternalProperties.Gender, Enum.GetName(userInfo.Gender) },
            { CharacterInfoHandler.ExternalProperties.Country, userInfo.Region },
            {
                CharacterInfoHandler.ExternalProperties.Age,
                Convert.ToInt32(Math.Floor((DateTime.UtcNow - userInfo.DateOfBirth).TotalDays / 365.2425))
            },
            { CharacterInfoHandler.ExternalProperties.Birthdate, userInfo.DateOfBirth },
            { CharacterInfoHandler.ExternalProperties.AccountAge, account.Created },
            { CharacterInfoHandler.ExternalProperties.Silent, 0 },
            { CharacterInfoHandler.ExternalProperties.Uuid, userInfo.UserId },
            { CharacterInfoHandler.ExternalProperties.AccessRights, 2 },
            { CharacterInfoHandler.ExternalProperties.ClearCache, 0 },
            {
                CharacterInfoHandler.ExternalProperties.Now, DateTimeOffset.Now.ToUnixTimeSeconds()
            },
            { CharacterInfoHandler.ExternalProperties.Subscriber, userInfo.Member ? 1 : 0 }
        };

    private static string GetPropertyList(Dictionary<CharacterInfoHandler.ExternalProperties, object> properties)
    {
        var sb = new SeparatedStringBuilder('|');

        foreach (var property in properties)
        {
            sb.Append((int)property.Key);
            sb.Append(property.Value);
        }

        return sb.ToString();
    }

    private static string GetCharacterList(UserInfo userInfo)
    {
        var sb = new SeparatedStringBuilder('%');

        sb.Append(userInfo.LastCharacterSelected);

        foreach (var character in userInfo.Characters)
            sb.Append(character.Value.GetLightCharacterData());

        return sb.ToString();
    }
}
