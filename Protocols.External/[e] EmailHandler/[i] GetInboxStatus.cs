﻿using Server.Reawakened.Network.Protocols;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Extensions;
using Server.Reawakened.Players.Helpers;
using Server.Reawakened.Players.Models.System;

namespace Protocols.External._e__EmailHandler;

public class GetInboxStatus : ExternalProtocol
{
    public override string ProtocolName => "ei";

    public override void Run(string[] message)
    {
        var player = NetState.Get<Player>();
        var mail = GetInboxMail(player.GetCurrentCharacter().Emails);
        SendXt("ei", mail);
    }

    public static string GetInboxMail(List<EmailHeaderModel> emails)
    {
        var sb = new SeparatedStringBuilder('&');

        foreach (var email in emails)
            sb.Append(email);

        return sb.ToString();
    }
}
