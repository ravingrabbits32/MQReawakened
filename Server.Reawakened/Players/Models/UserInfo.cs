﻿using Server.Base.Core.Models;
using Server.Reawakened.Configs;
using Server.Reawakened.Network.Services;
using Server.Reawakened.Players.Enums;
using Server.Reawakened.Players.Models.System;

namespace Server.Reawakened.Players.Models;

public class UserInfo : PersistantData
{
    public Dictionary<int, CharacterModel> Characters { get; set; }
    public Dictionary<int, SystemMailModel> Mail { get; set; }

    public string LastCharacterSelected { get; set; }

    public string AuthToken { get; set; }

    public Gender Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    public bool Member { get; set; }
    public string SignUpExperience { get; set; }
    public string Region { get; set; }
    public string TrackingShortId { get; set; }
    public int ChatLevel { get; set; }

    public UserInfo()
    {
        Characters = new Dictionary<int, CharacterModel>();
        Mail = new Dictionary<int, SystemMailModel>();
    }

    public UserInfo(int userId, Gender gender, DateTime dateOfBirth, string region, RandomKeyGenerator kGen,
        ServerStaticConfig config)
    {
        Region = region;
        UserId = userId;
        Gender = gender;
        DateOfBirth = dateOfBirth;

        LastCharacterSelected = string.Empty;
        AuthToken = kGen.GetRandomKey<UserInfo>(userId.ToString());

        SignUpExperience = config.DefaultSignUpExperience;
        Member = config.DefaultMemberStatus;
        TrackingShortId = config.DefaultTrackingShortId;
        ChatLevel = config.DefaultChatLevel;

        Characters = new Dictionary<int, CharacterModel>();
        Mail = new Dictionary<int, SystemMailModel>();
    }
}
